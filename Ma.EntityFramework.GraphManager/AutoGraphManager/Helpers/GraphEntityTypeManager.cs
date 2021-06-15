using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using Ma.EntityFramework.GraphManager.Models;
using System.Reflection;
using Ma.EntityFramework.GraphManager.DataStorage;
using Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers.Abstract;

namespace Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers
{
    internal class GraphEntityTypeManager
        : IGraphEntityTypeManager
    {
        private IContextFactory ContextFactory { get; set; }
        internal string EntityTypeName { get; private set; }

        public GraphEntityTypeManager(IContextFactory contextFactory,
            string entityTypeName)
        {
            if (contextFactory == null)
                throw new ArgumentNullException(nameof(contextFactory));

            if (string.IsNullOrEmpty(entityTypeName))
                throw new ArgumentNullException(nameof(entityTypeName));

            ContextFactory = contextFactory;
            EntityTypeName = entityTypeName;
        }

        private IContextHelper ContextHelper
        {
            get { return ContextFactory.GetContextHelper(); }
        }

        private DbContext Context
        {
            get { return ContextHelper.Context; }
        }

        private HelperStore Store
        {
            get { return ContextHelper.Store; }
        }

        /// <summary>
        /// Get primary keys according to type of entity.
        /// </summary>
        /// <returns>List of primary keys.</returns>
        public List<string> GetPrimaryKeys()
        {
            IEnumerable<string> primaryKeys = ContextHelper
                .ObjectContext
                .MetadataWorkspace
                .GetItems<EntityType>(DataSpace.CSpace)
                .FirstOrDefault(m => m.Name.Equals(EntityTypeName))
                .KeyMembers
                .Select(m => m.Name);

            return primaryKeys.ToList();
        }

        /// <summary>
        /// If any of key members of entity is store generated.
        /// </summary>
        /// <returns>If any of key members of entity is store generated.</returns>
        public bool HasStoreGeneratedKey()
        {
            bool hasStoreGeneratedKey = false;

            EntityType entityType = ContextHelper
                .ObjectContext
                .MetadataWorkspace
                .GetItems<EntityType>(DataSpace.SSpace)
                .Where(m => m.Name.Equals(EntityTypeName))
                .FirstOrDefault();

            if (entityType != null)
                hasStoreGeneratedKey = entityType
                    .KeyMembers
                    .Any(m => m.IsStoreGeneratedIdentity
                            || m.IsStoreGeneratedComputed);

            return hasStoreGeneratedKey;
        }

        /// <summary>
        /// Get distinct unique properties of entity.
        /// </summary>
        /// <returns>List of unique properties.</returns>
        public List<PropertyInfo> GetUniqueProperties()
        {
            return MappingStorage.Instance.UniqueProperties
                            .Where(m => m.SourceType.Name.Equals(EntityTypeName))
                            .SelectMany(m => m.Properties)
                            .Distinct()
                            .ToList();
        }

        /// <summary>
        /// Get foreign keys according to type name.
        /// </summary>
        /// <returns>List of foreign keys.</returns>
        public List<RelationshipDetail> GetForeignKeyDetails()
        {
            IEnumerable<RelationshipDetail> foreignKeys = ContextHelper
                .GetForeignKeyDetails()
                .Where(m =>
                    m.FromDetails.ContainerClass.Equals(EntityTypeName)
                    || m.ToDetails.ContainerClass.Equals(EntityTypeName));

            return foreignKeys.ToList();
        }

        /// <summary>
        /// Get simple properties of entity.
        /// </summary>
        /// <returns>Simple properties of entity.</returns>
        public List<EdmProperty> GetSimpleEntityProperties()
        {
            IEnumerable<EdmProperty> entityProperties = ContextHelper
                .ObjectContext
                .MetadataWorkspace
                .GetItems<EntityType>(DataSpace.CSpace)
                .FirstOrDefault(m => m.Name.Equals(EntityTypeName))
                .Properties;

            return entityProperties.ToList();
        }

        /// <summary>
        /// Get navigation details according to name of type
        /// </summary>
        /// <returns>Navigation details of type</returns>
        public NavigationDetail GetNavigationDetail()
        {
            // Try to get from store
            if (Store.NavigationDetail.ContainsKey(EntityTypeName))
                return Store.NavigationDetail[EntityTypeName];

            NavigationDetail navigationDetail = ContextHelper
                .ObjectContext
                .MetadataWorkspace
                .GetItems<EntityType>(DataSpace.CSpace)
                .Where(m => m.Name.Equals(EntityTypeName))
                .Select(n => new NavigationDetail(n))
                .FirstOrDefault();

            // Add to store
            Store.NavigationDetail.Add(EntityTypeName, navigationDetail);
            return navigationDetail;
        }

        /// <summary>
        /// Get the the origin class which this foreign key refers to.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When foreignKeyName is null.
        /// </exception>
        /// <param name="foreignKeyName">Name of foreign key property.</param>
        /// <returns>Name of class which this foreign key refers to.</returns>
        public string GetOriginOfForeignKey(string foreignKeyName)
        {
            if (string.IsNullOrEmpty(foreignKeyName))
                throw new ArgumentNullException(nameof(foreignKeyName));

            // Try to get from store
            Tuple<string, string> key = new Tuple<string, string>(EntityTypeName, foreignKeyName);
            if (Store.ForeignKeyOrigin.ContainsKey(key))
                return Store.ForeignKeyOrigin[key];

            string originOfForeignKey = string.Empty;

            // Principal relationship multiplicities
            List<RelationshipMultiplicity> principalRelationshipMultiplicity =
                new List<RelationshipMultiplicity>()
                {
                    RelationshipMultiplicity.One,
                    RelationshipMultiplicity.ZeroOrOne
                };

            // Get the relationship detail which this foreign keys refers.
            RelationshipDetail parentType = GetForeignKeyDetails()
                .Where(r => r.ToDetails.ContainerClass.Equals(EntityTypeName)
                            && r.ToDetails.Keys.Any(k => k.Equals(foreignKeyName))
                            && r.FromDetails != null
                            && !string.IsNullOrEmpty(r.FromDetails.ContainerClass)
                            && principalRelationshipMultiplicity
                                .Contains(r.FromDetails.RelationshipMultiplicity))
                .FirstOrDefault();

            if (parentType != null)
            {
                var currentReferencedTypeName = parentType.FromDetails.ContainerClass;
                var currentForeignKeyName = GetMatchingFromForeignKeyName(parentType, foreignKeyName);
                while (!string.IsNullOrEmpty(currentReferencedTypeName))
                {
                    // If EntityTypeName and currentReferencedTypeName is same,
                    // then this is self reference, so we need to break infinete iteration.
                    if (EntityTypeName == currentReferencedTypeName)
                        break;

                    IGraphEntityTypeManager referencedTypeManager = ContextFactory
                        .GetEntityTypeManager(currentReferencedTypeName);

                    // Get one-to-one principal parent relationship detail,
                    // because the origin of foreign key is uppermost principal parent.
                    RelationshipDetail principalParent = referencedTypeManager
                        .GetForeignKeyDetails()
                        .Where(r => r.ToDetails.ContainerClass.Equals(currentReferencedTypeName)
                            && r.ToDetails.Keys.Contains(currentForeignKeyName)
                            && r.FromDetails != null
                            && !string.IsNullOrEmpty(r.FromDetails.ContainerClass)
                            && principalRelationshipMultiplicity
                                .Contains(r.FromDetails.RelationshipMultiplicity))
                        .FirstOrDefault();

                    if (principalParent != null)
                    {
                        // If principal parent is not null set current to this.
                        currentReferencedTypeName = principalParent.FromDetails.ContainerClass;
                        currentForeignKeyName = GetMatchingFromForeignKeyName(principalParent, currentForeignKeyName);
                    }
                    else
                    {
                        // If principal parent is null exit the iteration.
                        break;
                    }
                }

                originOfForeignKey = currentReferencedTypeName;
            }

            // Add to store
            Store.ForeignKeyOrigin.Add(key, originOfForeignKey);
            return originOfForeignKey;
        }

        /// <summary>
        /// Find on how many properties this type depends on.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When store is null.
        /// </exception>
        /// <param name="store">Calculation store.</param>
        /// <returns>Calculated count.</returns>
        public int FindPrincipalCount(Dictionary<string, int> store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            // If principal count has already been
            // calculated for current type, get if from store.
            if (store.ContainsKey(EntityTypeName))
                return store[EntityTypeName];

            // Get principal properties.
            var navigationDetail = GetNavigationDetail();
            var principalCollection = navigationDetail
                    .Relations
                    .Where(m => m.Direction == NavigationDirection.From);

            // Get state definers
            IEnumerable<PropertyInfo> stateDefiners = MappingStorage.Instance.StateDefiners
                .Where(s => s.SourceType.Name.Equals(EntityTypeName))
                .SelectMany(s => s.Properties);

            // Get types which this type is state definer for
            List<string> stateDefinerFor = MappingStorage
                .Instance
                .StateDefiners
                .Where(m => m.Properties
                    .Select(pr => pr.PropertyType.Name)
                    .Contains(EntityTypeName))
                .Select(m => m.SourceType.Name)
                .ToList();

            // If this type has no principal property
            // or stete definer return 0.
            if (principalCollection.Count() == 0
                && stateDefiners.Count() == 0)
            {
                store.Add(EntityTypeName, 0);
                return 0;
            }

            // Calculate initial count of principal properties
            // and state definers.
            int count = principalCollection.Count()
                + stateDefiners.Count();

            // Loop through principal properties and add count of
            // principal properties of them to the current type.
            // Because when FirstType depends on SecondType,
            // and SecondType depends on ThirdType this means
            // that FirstType depends on SecondType and ThirdType.
            foreach (var principal in principalCollection)
            {
                // If this principal property is itself, do not count it
                if (principal.PropertyTypeName == EntityTypeName)
                    continue;

                // If this type is stete definer for principal
                // type, do not count it
                if (stateDefinerFor.Contains(principal.PropertyTypeName))
                    continue;

                IGraphEntityTypeManager entityTypeManager = ContextFactory
                    .GetEntityTypeManager(principal.PropertyTypeName);
                count += entityTypeManager.FindPrincipalCount(store);
            }

            // Add count of principal properties of state definers also.
            foreach (var stateDefiner in stateDefiners)
            {
                IGraphEntityTypeManager entityTypeManager = ContextFactory
                    .GetEntityTypeManager(stateDefiner.PropertyType.Name);
                count += entityTypeManager.FindPrincipalCount(store);
            }

            // Store calculation for further use.
            store.Add(EntityTypeName, count);
            return count;
        }

        /// <summary>
        /// Get matching From ForeignKeyName according to To ForeignKeyName.
        /// </summary>
        /// <param name="parentType">Parent relationship detail.</param>
        /// <param name="toForeignKeyName">To ForeignKeyName.</param>
        /// <returns></returns>
        private string GetMatchingFromForeignKeyName(RelationshipDetail parentType, string toForeignKeyName)
        {
            if (parentType == null || string.IsNullOrEmpty(toForeignKeyName))
            {
                return string.Empty;
            }

            for (int i = 0; i < parentType.ToDetails.Keys.Count; i++)
            {
                var foreignKeyAtIndex = parentType.ToDetails.Keys.ElementAt(i);                
                if (string.Equals(foreignKeyAtIndex, toForeignKeyName, StringComparison.Ordinal))
                {
                    return parentType.FromDetails.Keys.ElementAt(i);
                }
            }

            return string.Empty;
        }
    }
}
