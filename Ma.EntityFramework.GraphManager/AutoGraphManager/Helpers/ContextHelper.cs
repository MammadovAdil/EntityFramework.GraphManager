using Ma.EntityFramework.GraphManager.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using Ma.ExtensionMethods.Reflection;
using System.Reflection;
using System.Linq.Expressions;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using Ma.EntityFramework.GraphManager.DataStorage;
using Ma.EntityFramework.GraphManager.ManualGraphManager.Abstract;
using Ma.EntityFramework.GraphManager.ManualGraphManager;
using Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers.Abstract;

namespace Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers
{
    internal class ContextHelper
        : IContextHelper, IContextFactory
    {
        public DbContext Context { get; set; }
        public HelperStore Store { get; private set; }

        private List<RelationshipDetail> ForeignKeyDetails { get; set; }

        public ContextHelper(DbContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            Context = context;

            // Initialize store
            Store = new HelperStore();
        }

        /// <summary>
        /// Get object context associated with context.
        /// </summary>
        /// <returns>Object context for context.</returns>
        public ObjectContext ObjectContext
        {
            get { return ((IObjectContextAdapter)Context).ObjectContext; }
        }

        /// <summary>
        /// Get navigation details of context.
        /// </summary>
        /// <returns>Navigation details of context.</returns>
        public IEnumerable<NavigationDetail> GetNavigationDetails()
        {
            IEnumerable<NavigationDetail> navigationDetails = ObjectContext
                .MetadataWorkspace
                .GetItems<EntityType>(DataSpace.CSpace)
                .Select(n => new NavigationDetail(n));

            return navigationDetails;
        }

        /// <summary>
        /// Get foreign key details.
        /// </summary>
        /// <returns>Foreign key details.</returns>
        public List<RelationshipDetail> GetForeignKeyDetails()
        {
            if (ForeignKeyDetails == null)
                ForeignKeyDetails = ObjectContext
                    .MetadataWorkspace
                    .GetItems<AssociationType>(DataSpace.CSpace)
                    .Where(m => m.Constraint != null)
                    .Select(m => new RelationshipDetail(m.Constraint))
                    .ToList();

            return ForeignKeyDetails;
        }

        /// <summary>
        /// Get the uppermost principal parent of entity.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When entity is null
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entity">Entity to get parent.</param>
        /// <returns>Uppermost principal parent of entity.</returns>
        public object GetUppermostPrincipalParent<TEntity>(TEntity entity)
            where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Try to get from store
            if (Store.UppermostPrincipalParent.ContainsKey(entity))
                return Store.UppermostPrincipalParent[entity];

            object currentUppermostPrincipal = entity;
            List<object> parents = null;

            do
            {
                parents = GetParents(currentUppermostPrincipal, true)
                    .ToList();

                if (parents != null
                        && parents.Count > 0)
                    currentUppermostPrincipal = parents.FirstOrDefault();
            }
            while (parents != null
                && parents.Count() > 0);

            // Add to store
            Store.UppermostPrincipalParent.Add(entity, currentUppermostPrincipal);
            return currentUppermostPrincipal;
        }

        /// <summary>
        /// Get uppermost parent entity which contains this entity
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When entity is null
        /// </exception>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <param name="entity">Entity to get uppermost parent</param>
        /// <returns>Uppermost parents</returns>
        public object GetUppermostParent<TEntity>(TEntity entity)
            where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Try to get from store
            if (Store.UppermostParent.ContainsKey(entity))
                return Store.UppermostParent[entity];

            List<object> result = new List<object>();
            object currentUppermostParent = entity;

            List<object> parents = GetParents(currentUppermostParent, false)
                .ToList();

            while (parents != null
                && parents.Count() > 0)
            {
                if (parents != null
                        && parents.Count == 1)
                    currentUppermostParent = parents.FirstOrDefault();

                parents = parents.SelectMany(m => GetParents(m, false)).ToList();
            }

            // Add to store
            Store.UppermostParent.Add(entity, currentUppermostParent);
            return currentUppermostParent;
        }

        /// <summary>
        /// Get parents of entity which contains this entity
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When entity is null
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entity">Entity to get parent.</param>
        /// <param name="onlyPrincipal">Get only one-to-one parent of entity</param>
        /// <returns>Principal parent of entity.</returns>
        public IEnumerable<object> GetParents<TEntity>(
            TEntity entity,
            bool onlyPrincipal)
            where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            string typeName = entity.GetType().Name;
            List<RelationshipMultiplicity> principalRelationshipMultiplicity =
                new List<RelationshipMultiplicity>()
                {
                    RelationshipMultiplicity.One,
                    RelationshipMultiplicity.ZeroOrOne
                };

            IGraphEntityTypeManager graphEntityTypeMangeer = GetEntityTypeManager(typeName);
            NavigationDetail navigationDetailOfCurrent = graphEntityTypeMangeer
                .GetNavigationDetail();

            // Get only those parent property navigation details
            // which has navigation property to this entity
            var parentNavigationDetails = navigationDetailOfCurrent
                .Relations
                .Where(r => r.Direction == NavigationDirection.From)
                .Select(r =>
                {
                    IGraphEntityTypeManager typeManager =
                        GetEntityTypeManager(r.PropertyTypeName);
                    return new
                    {
                        SourceTypeName = r.PropertyTypeName,
                        Relation = typeManager
                             .GetNavigationDetail()
                             .Relations
                             .FirstOrDefault(pr =>
                                 pr.PropertyTypeName.Equals(typeName)
                                 && pr.SourceMultiplicity == r.TargetMultiplicity
                                 && pr.TargetMultiplicity == r.SourceMultiplicity
                                 && pr.ToKeyNames.SequenceEqual(r.ToKeyNames))
                    };
                })
                .Where(r => r.Relation != null);

            if (onlyPrincipal)
                parentNavigationDetails = parentNavigationDetails
                    .Where(r => principalRelationshipMultiplicity
                        .Contains(r.Relation.TargetMultiplicity));

            List<string> parentPropertyNames = navigationDetailOfCurrent
                .Relations
                .Where(r => parentNavigationDetails.Any(p =>
                    p.SourceTypeName == r.PropertyTypeName
                    && p.Relation.SourceMultiplicity == r.TargetMultiplicity
                    && p.Relation.TargetMultiplicity == r.SourceMultiplicity
                    && p.Relation.ToKeyNames.SequenceEqual(r.ToKeyNames)))
                .Select(r => r.PropertyName)
                .ToList();

            if (parentPropertyNames != null
                && parentPropertyNames.Count > 0)
            {
                foreach (string propertyName in parentPropertyNames)
                {
                    object parent = entity.GetPropertyValue(propertyName);
                    if (parent != null)
                        yield return parent;
                }
            }
        }

        /// <summary>
        /// Find duplicate entities in the local context, perform needed operations
        /// to be able insert or update value appropriately and detach duplicates.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entity">Entity to find duplicates of.</param>
        private void DealWithDuplicates<TEntity>(
            TEntity entity)
            where TEntity : class
        {
            IGraphEntityManager<TEntity> graphEntityManager = GetEntityManager<TEntity>();
            Expression<Func<TEntity, bool>> filterExpression =
                graphEntityManager.ConstructFilterExpression(entity, FilterType.IdOrUnique);


            bool duplicatesFoundAndEliminatedFlag = false;
            if (filterExpression != null)
            {
                /// This is used instead of Context.Set<TEntity>().Local
                /// to improve perfermance.
                var duplicateEntityFromLocal = Context
                    .ChangeTracker
                    .Entries<TEntity>()
                    .Select(m => m.Entity)
                    .Where(filterExpression.Compile())
                    .ToList();                

                if (duplicateEntityFromLocal.Any())
                {
                    dynamic uppermostPrincipalParentEntity =
                        GetUppermostPrincipalParent(entity);

                    foreach (TEntity duplicate in duplicateEntityFromLocal.Where(d => d != entity))
                    {
                        dynamic uppermostPrincipalParentDuplicate =
                            GetUppermostPrincipalParent(duplicate);

                        // If duplicate value is not in a collection
                        // we need to replace its duplicate value with 
                        // original one in parent entity. Becasue as we detach the 
                        // duplicate entry afterwards, not setting its value with original
                        // one will not send it to database where it has to be sent
                        ReplaceEntitiesInParents(
                            uppermostPrincipalParentDuplicate,
                            uppermostPrincipalParentEntity);

                        /*
                         * ******************************************************
                         * TO DO: ALSO CONSIDER WHEN ONE DUPLICATE IS IN A 
                         * NON-COLLECTION ENTITY AND ANOTHER IS IN A COLLECTION
                         * ******************************************************
                        */

                        DetachWithDependants(uppermostPrincipalParentDuplicate, true);
                    }

                    duplicatesFoundAndEliminatedFlag = true;
                }
            }

            // If no duplicaes has been found, get uppermost principal
            // parent entity and if it is not entity itself, try to find
            // and eliminate duplicates of parent
            if (!duplicatesFoundAndEliminatedFlag)
            {
                dynamic uppermostPrincipalParentEntity =
                        GetUppermostPrincipalParent(entity);

                if (!uppermostPrincipalParentEntity.Equals(entity))
                {
                    DealWithDuplicates(uppermostPrincipalParentEntity);
                }
            }
        }

        /// <summary>
        /// Set property of parent entity to targetValue 
        /// which has property value equals to currentValue.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="currentValue">Current value of property to replace.</param>
        /// <param name="targetValue">Target value to set value of property of parent.</param>
        private void ReplaceEntitiesInParents<TEntity>(
            TEntity currentValue,
            TEntity targetValue)
            where TEntity : class
        {
            IGraphEntityManager<TEntity> graphEntityManager = GetEntityManager<TEntity>();
            NavigationDetail navigationDetailOfCurrent = graphEntityManager
                .GetNavigationDetail();

            // Get parent properties of entity.
            // Properties which have navigation property type of which
            // is type of entity. Ignore navigation properties
            // which are mutual navigation properties with entity itself     
            string typeName = currentValue.GetType().Name;
            List<NavigationDetail> parentNavigationDetails = GetNavigationDetails()
                .Select(n => new NavigationDetail()
                {
                    SourceTypeName = n.SourceTypeName,
                    Relations = n.Relations
                        .Where(r => r.PropertyTypeName.Equals(typeName)
                                   && r.SourceMultiplicity == RelationshipMultiplicity.Many
                                   && !navigationDetailOfCurrent
                                        .Relations
                                        .Any(c => c.PropertyTypeName.Equals(n.SourceTypeName)
                                                && c.TargetMultiplicity == r.SourceMultiplicity))
                        .ToList()
                })
                .Where(n => n.Relations != null
                    && n.Relations.Count() > 0)
                .ToList();

            // Get assembly to be able to get types according to type name
            Assembly entityAssembly = currentValue.GetType().Assembly;

            if (parentNavigationDetails != null && parentNavigationDetails.Count() > 0)
            {
                foreach (NavigationDetail parentNavigation in parentNavigationDetails)
                {
                    Type parentType = entityAssembly.GetTypes()
                        .FirstOrDefault(t => t.Name.Equals(parentNavigation.SourceTypeName));

                    // Get local set of parent
                    IEnumerable<object> localParentSet = Context
                        .Set(parentType)
                        .Local
                        .CastToGeneric();

                    foreach (NavigationRelation navigationRelation in parentNavigation.Relations)
                    {
                        PropertyInfo childProperty =
                            parentType.GetProperty(navigationRelation.PropertyName);

                        if (!childProperty.PropertyType.IsCollectionType())
                        {
                            // Get all parent entities which have current entity inside
                            var containerParentCollection = localParentSet
                                .Where(m => m.GetPropertyValue(navigationRelation.PropertyName) != null
                                    && m.GetPropertyValue(navigationRelation.PropertyName).Equals(currentValue))
                                .ToList();

                            // If collection is empty then skip.
                            if (containerParentCollection == null
                                || containerParentCollection.Count == 0)
                                continue;

                            foreach (var containerParent in containerParentCollection)
                            {
                                // If parent is null then skip.
                                if (containerParent == null)
                                    continue;

                                // If parent with property value of currentValue found replace values
                                /*
                                 * If duplicate entity is in the entity, state of which has already
                                 * been defined and if state of this parent entity is Unchanged or
                                 * Modified, trying to change navigation property of this parent entity
                                 * will throw InvalidOperationException with following message:
                                 * "A referential integrity constraint violation occurred: 
                                 *  A primary key property that is a part of referential integrity constraint 
                                 *  cannot be changed when the dependent object is Unchanged unless it is being 
                                 *  set to the association's principal object. The principal object must 
                                 *  be tracked and not marked for deletion."
                                 * As a workaround I am storing current state of parent entity, changing the
                                 * state to Added, then replacing duplicate entity and in the end
                                 * I set state of parent entity to stored current state.
                                */

                                // Store current state
                                var currentState = Context.Entry(containerParent).State;
                                // Change state to added
                                Context.Entry(containerParent).State = EntityState.Added;

                                // Replace value through 
                                // context.Entry(containerParent).Member(propertyName).CurrentValue
                                // because otherwise EntityFramework will not be able to track entities
                                Context.Entry(containerParent)
                                    .Member(navigationRelation.PropertyName).CurrentValue = targetValue;

                                // Restore state to current state
                                Context.Entry(containerParent).State = currentState;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Detach dependants of entity.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When  entity is null
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entity">Entity to detach dependants.</param>
        /// <param name="detachItself">Also detach entity itself.</param>
        public void DetachWithDependants<TEntity>(
            TEntity entity,
            bool detachItself)
            where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.GetType().Name == "Person")
            {
                var pin = entity.GetPropertyValue("Pin").ToString();
                if (pin == "FAC3A")
                {

                }
            }

            string typeName = entity.GetType().Name;
            IGraphEntityTypeManager graphEntityTypeManager =
                GetEntityTypeManager(typeName);
            List<string> dependantPropertyTypes = graphEntityTypeManager
                .GetForeignKeyDetails()
                .Where(r => r.FromDetails
                               .ContainerClass
                               .Equals(typeName))
                .Select(r => r.ToDetails.ContainerClass)
                .ToList();

            List<PropertyInfo> dependantProperties = entity
                .GetType()
                .GetProperties()
                .Where(p => dependantPropertyTypes
                                .Contains(p.PropertyType.GetUnderlyingType().Name))
                .ToList();

            if (dependantProperties != null
                            && dependantProperties.Count > 0)
            {
                foreach (PropertyInfo childEntityProperty in dependantProperties)
                {
                    if (childEntityProperty.PropertyType.IsCollectionType())
                    {
                        // If child entity is collection detach all entities inside this collection
                        IEnumerable<object> enumerableChildEntity =
                                    ReflectionExtensions.GetPropertyValue(entity, childEntityProperty.Name)
                                    as IEnumerable<object>;

                        if (enumerableChildEntity != null)
                        {
                            foreach (dynamic childEntity in enumerableChildEntity.ToList())
                            {
                                if (childEntity != null)
                                    DetachWithDependants(childEntity, true);
                            }
                        }
                    }
                    else
                    {
                        // If child entity is not collection define state of its own                        
                        dynamic childEntity =
                            ReflectionExtensions.GetPropertyValue(entity, childEntityProperty.Name);

                        if (childEntity != null)
                            DetachWithDependants(childEntity, true);
                    }

                }
            }

            if (detachItself)
                Context.Entry(entity).State = EntityState.Detached;
        }

        /// <summary>
        /// Add all child or parents entities related to given entity
        /// and entity itself to the relatedEntityList.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When entity or relatedEntityList is null.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entity">Entity to get all related entities.</param>
        /// <param name="relatedEntityList">List of entities to add.</param>
        public void GetAllEntities<TEntity>(
            TEntity entity,
            List<object> relatedEntityList)
            where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (relatedEntityList == null)
                throw new ArgumentNullException(nameof(relatedEntityList));

            if (!relatedEntityList.Contains(entity))
                relatedEntityList.Add(entity);

            IGraphEntityManager<TEntity> graphEntityManager = GetEntityManager<TEntity>();
            NavigationDetail navigationDetail = graphEntityManager
                .GetNavigationDetail();

            List<PropertyInfo> navigationPropeties = navigationDetail
                .Relations
                .Select(n => entity.GetProperty(n.PropertyName))
                .ToList();

            if (navigationPropeties != null
                        && navigationPropeties.Count > 0)
            {
                foreach (PropertyInfo childEntityProperty in navigationPropeties)
                {
                    if (childEntityProperty.PropertyType.IsCollectionType())
                    {
                        // If child entity is collection define all entities inside this collection.
                        IEnumerable<object> enumerableChildEntity =
                                    entity.GetPropertyValue(childEntityProperty.Name) as IEnumerable<object>;

                        if (enumerableChildEntity != null)
                        {
                            for (int i = 0; i < enumerableChildEntity.Count(); i++)
                            {
                                dynamic childEntity = enumerableChildEntity.ElementAt(i);
                                if (childEntity != null
                                        && !relatedEntityList.Contains(childEntity))
                                    GetAllEntities(
                                        childEntity,
                                        relatedEntityList);
                            }
                        }
                    }
                    else
                    {
                        // If child entity is not collection define state of its own.
                        dynamic childEntity = entity.GetPropertyValue(childEntityProperty.Name);

                        if (childEntity != null
                                && !relatedEntityList.Contains(childEntity))
                            GetAllEntities(
                                    childEntity,
                                    relatedEntityList);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate state define order of added entities.
        /// </summary>
        /// <returns>Sorted stete define order.</returns>
        private IOrderedEnumerable<KeyValuePair<string, int>> CalculateStateDefineOrder()
        {
            // Initialize store
            Dictionary<string, int> store = new Dictionary<string, int>();

            List<string> typeNames = Context
                .ChangeTracker
                .Entries()
                .Select(m => m.Entity.GetType().Name)
                .ToList();

            foreach (string typeName in typeNames)
            {
                IGraphEntityTypeManager entityTypeManager =
                    GetEntityTypeManager(typeName);
                entityTypeManager.FindPrincipalCount(store);
            }

            IOrderedEnumerable<KeyValuePair<string, int>> sorted =
                store.OrderBy(m => m.Value);

            return sorted;
        }

        /// <summary>
        /// Define state of collection of complex 
        /// navigation properties of entity.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entity">Entity to define state of child entities.</param>
        /// <param name="definedEntityStore">Storage to store already state
        /// defined entities to not to define their states again</param>
        /// <param name="propertyCollection">Collection of complex 
        /// properties of entity to define state of.</param>
        /// <param name="shouldSetParentKeys">Set keys of parent entity
        /// to primary keys of child entity in one-to-one relationships.
        /// This parameter is mainly used while defining state of child entities
        /// when called by DefineStateOfChildEntities method.</param>
        /// <param name="definingStateOfStateDefiner">Identifies
        /// if defining state of normal navigation properties
        /// or explicitly set StateDefiners. 
        /// Mainly provided by DefineStateOfChildEntities method.</param>
        private void DefineStateOfProperties<TEntity>(
            TEntity entity,
            List<object> definedEntityStore,
            List<PropertyInfo> propertyCollection,
            bool shouldSetParentKeys,
            bool definingStateOfStateDefiner)
            where TEntity : class
        {
            if (propertyCollection != null
                        && propertyCollection.Count > 0)
            {
                foreach (PropertyInfo childEntityProperty in propertyCollection)
                {
                    if (childEntityProperty.PropertyType.IsCollectionType())
                    {
                        // If child entity is collection define all entities inside this collection.
                        IEnumerable<object> enumerableChildEntity =
                                    entity.GetPropertyValue(childEntityProperty.Name) as IEnumerable<object>;

                        if (enumerableChildEntity != null)
                        {
                            for (int i = 0; i < enumerableChildEntity.Count(); i++)
                            {
                                dynamic childEntity = enumerableChildEntity.ElementAt(i);
                                if (childEntity != null
                                        && !definedEntityStore.Contains(childEntity))
                                    DefineState(
                                        childEntity,
                                        definedEntityStore,
                                        true,
                                        shouldSetParentKeys,
                                        definingStateOfStateDefiner);
                            }
                        }
                    }
                    else
                    {
                        // If child entity is not collection define state of its own.
                        dynamic childEntity = entity.GetPropertyValue(childEntityProperty.Name);

                        if (childEntity != null
                                && !definedEntityStore.Contains(childEntity))
                            DefineState(
                                childEntity,
                                definedEntityStore,
                                true,
                                shouldSetParentKeys,
                                definingStateOfStateDefiner);
                    }

                }
            }
        }

        /// <summary>
        /// Define state of child entities of entity.
        /// </summary>
        /// <remarks>
        /// If there are entities which are in many-to-one relationship
        /// with parent entity and if there is any child entity marked as
        /// StateDefiner, thir state must be defined before state of entity itslef
        /// has been defined.
        /// </remarks>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entity">Entity to define state of child entities.</param>
        /// <param name="definedEntityStore">Storage to store already state
        /// defined entities to not to define their states again</param>
        /// <param name="stateDefineOrder">State of child entities 
        /// with what define order must be defined.</param>
        /// <param name="definingStateOfStateDefiner">Identifies
        /// if defining state of normal navigation properties
        /// or explicitly set StateDefiners. 
        /// Mainly provided by DefineStateOfChildEntities method.</param>
        private void DefineStateOfChildEntities<TEntity>(
            TEntity entity,
            List<object> definedEntityStore,
            DefineOrder stateDefineOrder,
            bool definingStateOfStateDefiner)
            where TEntity : class
        {
            // If defining the state of entity before itself, parent key values
            // should be set to primary key values of child if matching entity
            // for the child has been found. Otherwise this is not necessary.
            bool shouldSetParentKeys = stateDefineOrder == DefineOrder.Beforhand
                ? true
                : false;

            IGraphEntityManager<TEntity> graphEntityManager = GetEntityManager<TEntity>();
            NavigationDetail navigationDetail = graphEntityManager.GetNavigationDetail();
            IEnumerable<PropertyInfo> navigationPropeties = navigationDetail
                .Relations
                .Select(n => entity.GetProperty(n.PropertyName));

            // Get principal properties

            /*
             *   When defining state of explicitly set StateDefiners state of one-to-one
             *   principal parent should not be defined. Explicitly set StateDefiners
             *   are dependent one-to-one navigation properties of parent entity and their state
             *   must be defined before parent entity, trying to get one-to-one parent of
             *   StateDefiner and defining its state will cause to endless loop.
             *   So || !definingStateOfStateDefiner is used.
            */
            IEnumerable<PropertyInfo> principalProperties = navigationDetail.Relations
                .Where(r => r.Direction == NavigationDirection.From
                    && (r.SourceMultiplicity == RelationshipMultiplicity.Many
                        || !definingStateOfStateDefiner))
                .Select(r => navigationPropeties.FirstOrDefault(n => n.Name.Equals(r.PropertyName)));

            // Get state definers
            IEnumerable<PropertyInfo> stateDefiners = MappingStorage.Instance.StateDefiners
                .Where(s => s.SourceType.Equals(entity.GetType()))
                .SelectMany(s => s.Properties);

            if (stateDefineOrder == DefineOrder.Beforhand)
            {
                DefineStateOfProperties(
                    entity,
                    definedEntityStore,
                    stateDefiners.ToList(),
                    shouldSetParentKeys,
                    true);

                DefineStateOfProperties(
                    entity,
                    definedEntityStore,
                    principalProperties.ToList(),
                    shouldSetParentKeys,
                    false);
            }
            else
            {
                List<PropertyInfo> childEntities = navigationPropeties
                    .Except(stateDefiners)
                    .Except(principalProperties)
                    .ToList();

                DefineStateOfProperties(
                    entity,
                    definedEntityStore,
                    childEntities,
                    shouldSetParentKeys,
                    false);
            }
        }

        /// <summary>
        /// Define state of entity. If entity already exists in the source
        /// set and values has not been altered set the state to Unchanged, 
        /// else if values has been changed set the state of changed properties
        /// to Modified, otherwise set the state to Added.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entity">Entity to define state of.</param>
        /// <param name="definedEntityStore">Storage to store already state
        /// defined entities to not to define their states again</param>
        /// <param name="defineStateOfChildEntities">If set to true define state of
        /// configured child entities. This rule also applied to child entities
        /// of child entities and so on.</param>
        /// <param name="shouldSetParentKeys">Set keys of parent entity
        /// to primary keys of child entity in one-to-one relationships.
        /// This parameter is mainly used while defining state of child entities
        /// when called by DefineStateOfChildEntities method.</param>
        /// <param name="definingStateOfStateDefiner">Identifies
        /// if defining state of normal navigation properties
        /// or explicitly set StateDefiners. 
        /// Mainly provided by DefineStateOfChildEntities method.</param>
        private void DefineState<TEntity>(
            TEntity entity,
            List<object> definedEntityStore,
            bool defineStateOfChildEntities,
            bool shouldSetParentKeys,
            bool definingStateOfStateDefiner)
            where TEntity : class
        {
            IGraphEntityManager<TEntity> entityManager = GetEntityManager<TEntity>();

            if (defineStateOfChildEntities)
                // First define states of child entities of this entity
                // whith DefineOrder Beforhand.
                DefineStateOfChildEntities(
                    entity,
                    definedEntityStore,
                    DefineOrder.Beforhand,
                    definingStateOfStateDefiner);

            // If state of entity has already been definer
            // or it has already been detached do not define its state
            if (!definedEntityStore.Contains(entity)
                && Context.Entry(entity).State != EntityState.Detached)
            {
                // Get matching entity.
                TEntity matchingEntity = entityManager.GetMatchingEntity(entity);

                if (matchingEntity != null)
                {
                    /*
                     * If entity is already in the context, for example if 
                     * entity has been retrieved in program layer using .FirstOrDefault()
                     * without calling .AsNoTracking()
                     * then this entity is tracked by entity framework. If entity retrieved
                     * using this method then, some properties have been altered, setting its
                     * state to Unchanged will undo all changes. It means that made alterations
                     * will be lost, and all current values will be replaced by original values.
                     * And keys will not be altered in child entities by settting state to Unchanged
                     * which has been done below in this code ( after dealing with duplicates ).
                     * As a workaround, I detach and readd entity to context to clear original values.
                    */
                    if (Context.Entry(entity).State != EntityState.Added)
                    {
                        Context.Entry(entity).State = EntityState.Detached;
                        Context.Entry(entity).State = EntityState.Added;
                    }

                    entityManager.SynchronizeKeys(entity, matchingEntity, shouldSetParentKeys);
                }

                // Deal with duplicates before proceeding
                DealWithDuplicates(entity);

                if (matchingEntity != null)
                {
                    Context.Entry(entity).State = EntityState.Unchanged;
                    entityManager.DetectPropertyChanges(entity, matchingEntity);
                }
                else
                {
                    // When priamry keys of entity is not store generated
                    // and state of entity is added, value of primary
                    // keys will not reflected at child entities.
                    // If primary keys of  entity has values different 
                    // than default values then set its state to 
                    // unchanged to fixup keys to solve this issue
                    // and after that set state to Added
                    var primaryKeys = entityManager.GetPrimaryKeys();
                    if (!entity.HasDefaultValues(entityManager
                            .GetPrimaryKeys()))
                        Context.Entry(entity).State = EntityState.Unchanged;

                    Context.Entry(entity).State = EntityState.Added;
                }

                definedEntityStore.Add(entity);
            }


            if (defineStateOfChildEntities)
                // Finally, define states of child entities of this entity
                // whith DefineOrder Afterwards.
                DefineStateOfChildEntities(
                    entity,
                    definedEntityStore,
                    DefineOrder.Afterwards,
                    definingStateOfStateDefiner);

        }

        /// <summary>
        /// Define state of all entities in the context.
        /// </summary>
        /// <returns>IManualGraphManager to continue to work on.</returns>
        public IManualGraphManager DefineState()
        {
            // Calculate state define order.
            var orderedCollection = CalculateStateDefineOrder();
            List<Type> addedEntityTypes = Context
                .ChangeTracker
                .Entries()
                .Select(m => m.Entity.GetType())
                .ToList();

            List<string> addedEntityTypeNames = addedEntityTypes
                .Select(m => m.Name)
                .ToList();

            foreach (var ordered in orderedCollection)
            {
                // If entity exists according to current define order.
                bool entityExists = addedEntityTypeNames
                    .Contains(ordered.Key);

                if (!entityExists)
                    continue;

                // Get type of entity
                Type entityType = addedEntityTypes
                    .First(m => m.Name == ordered.Key);

                // Get list of entities to define state.
                List<object> definedEntityStore = new List<object>();
                IEnumerable<object> entitySet = Context
                         .Set(entityType)
                         .Local
                         .CastToGeneric();

                // Loop through entiteis and define state.
                for (int i = 0; i < entitySet.Count(); i++)
                {
                    dynamic entity = entitySet.ElementAt(i);

                    DefineState(
                        entity,
                        definedEntityStore,
                        false,
                        true,
                        false);
                }
            }

            ManualGraphManager.ManualGraphManager manualGraphManager =
                new ManualGraphManager.ManualGraphManager(Context);
            return manualGraphManager;
        }

        /// <summary>
        /// Define state of entity. If entity already exists in the source
        /// set and values has not been altered set the state to Unchanged, 
        /// else if values has been changed set the state of changed properties
        /// to Modified, otherwise set the state of entity to Added.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When entity is null.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entity">Entity to define state of.</param>
        /// <param name="defineStateOfChildEntities">If set to true define state of
        /// configured child entities. This rule also applied to child entities
        /// of child entities and so on.</param>
        /// <returns>IManualGraphManager associated with current context to work on further.</returns>
        public IManualGraphManager<TEntity> DefineState<TEntity>(
            TEntity entity,
            bool defineStateOfChildEntities)
            where TEntity : class
        {
            /*
             *********************************************************************
             * Description: 
             * The main purpose of this method is to hide some arguments of original
             * method, to perform some checks and to not be recursive.
             * 
             *********************************************************************
            */

            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Get all related entities to entity, incluing itself,
            // for using afterwards.
            List<object> relatedEntityList = new List<object>();
            GetAllEntities(entity, relatedEntityList);

            List<object> definedEntityStore = new List<object>();
            if (defineStateOfChildEntities)
            {
                /*
                 * As entities can be in reverse direction. For example 
                 * if Person entity has PersonDocument depandant navigation property
                 * and PersonDocument has also navigation property to Person. If PersonDocument
                 * was sent to define state and Person navigation property of PersonDocument
                 * is not null then state of Person should be defined with all its dependants
                 * So we need to get parent of entity and define state of parent.
                */
                dynamic parent = GetUppermostParent(entity);
                // If entity has already been detached it is not needed to define its state
                if (Context.Entry(parent).State != EntityState.Detached)
                    DefineState(
                        parent,
                        definedEntityStore,
                        defineStateOfChildEntities,
                        true,
                        false);
            }
            else
            {
                if (Context.Entry(entity).State != EntityState.Detached)
                    DefineState(
                        entity,
                        definedEntityStore,
                        defineStateOfChildEntities,
                        true,
                        false);
            }

            /*
             * Get entries state of which have not been defined above and define their state.
             * This is mostly possible when duplicates added to the context, then one duplicate
             * is removed from context by DealWithDuplicates method, then if replaced duplicate
             * has such navigation property which is not dependant, then they stay at the context,
             * but their state is not defined.
             * For example, Party has navigation property called AddressRelation, in its term,
             * AddressRelation has many-to-one navigation property called Address. If there is
             * duplicate Parties in the context one with AddressRelations and other without,
             * DealWithDuplicates method can detach Party with AddressRelations and when
             * this entity is detached AddressRelations will be detahced but Addresses will 
             * stay in the context, because Addresses are not dependant properties. But link between
             * objects will be broken, so state of Addresses will not be defined.
             * Below lines intended to prevent such occasions by defining state of this kind of
             * entities. 
             * We need to define state of all entities related to provided entity, state of which
             * has not been defined and which still exist in the context.
            */
            IEnumerable<object> stateNotDefinedEntities = relatedEntityList
                .Except(definedEntityStore)
                .Where(m => Context.Entry(m).State != EntityState.Detached);
            for (int i = 0; i < stateNotDefinedEntities.Count(); i++)
            {
                dynamic stateNotDefinedEntity = stateNotDefinedEntities
                    .ElementAt(i);

                DefineState(
                        stateNotDefinedEntity,
                        definedEntityStore,
                        defineStateOfChildEntities,
                        true,
                        false);
            }

            ManualGraphManager<TEntity> manualGraphManager =
                new ManualGraphManager<TEntity>(Context);
            manualGraphManager.EntityCollection = new List<TEntity>() { entity };

            return manualGraphManager;
        }

        /// <summary>
        /// Define state of list of entities. If entity already exists in the source
        /// set and values has not been altered set the state to Unchanged, 
        /// else if values has been changed set the state of changed properties
        /// to Modified, otherwise set the state of entity to Added.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When entityList is null.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entityList">List of entities to define state of.</param>
        /// <param name="defineStateOfChildEntities">If set to true define state of
        /// configured child entities. This rule also applied to child entities
        /// of child entities and so on.</param>
        /// <returns>IManualGraphManager associated with current context to work on further.</returns>
        public IManualGraphManager<TEntity> DefineState<TEntity>(
            List<TEntity> entityList,
            bool defineStateOfChildEntities)
            where TEntity : class
        {
            if (entityList == null)
                throw new ArgumentNullException(nameof(entityList));

            entityList.ForEach(e => DefineState(e, defineStateOfChildEntities));

            ManualGraphManager<TEntity> manualGraphManager =
                new ManualGraphManager<TEntity>(Context);
            manualGraphManager.EntityCollection = entityList;
            return manualGraphManager;
        }

        #region IContextFactory members

        public IContextHelper GetContextHelper()
        {
            return this;
        }

        public IGraphEntityManager<TEntity> GetEntityManager<TEntity>()
            where TEntity : class
        {
            Type entityType = typeof(TEntity);

            // Try to get from store
            if (Store.EntityManager.ContainsKey(entityType))
                return Store.EntityManager[entityType] as IGraphEntityManager<TEntity>;

            // Initialize
            IGraphEntityManager<TEntity> entityManager =
                new GraphEntityManager<TEntity>(this);

            // Add to store and return
            Store.EntityManager.Add(entityType, entityManager);
            return entityManager;
        }

        public IGraphEntityTypeManager GetEntityTypeManager(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));

            // Try to get from store
            if (Store.EntityTypeManager.ContainsKey(typeName))
                return Store.EntityTypeManager[typeName];

            // Initialize
            IGraphEntityTypeManager entityTypeManager =
                new GraphEntityTypeManager(this, typeName);

            // Add to store and return
            Store.EntityTypeManager.Add(typeName, entityTypeManager);
            return entityTypeManager;
        }

        #endregion
    }
}
