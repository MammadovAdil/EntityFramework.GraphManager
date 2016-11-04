using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Data.Entity.Core.Metadata.Edm;
using Ma.EntityFramework.GraphManager.Models;
using System.Reflection;
using Ma.EntityFramework.GraphManager.DataStorage;
using System.Linq.Expressions;
using Ma.ExtensionMethods.Reflection;
using System.Data.Entity.Infrastructure;
using Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers.Abstract;

namespace Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers
{
    /// <summary>
    /// Grapth entity manager.
    /// </summary>
    /// <typeparam name="TEntity">Type of entity.</typeparam>
    internal class GraphEntityManager<TEntity>
        : IGraphEntityManager<TEntity>
        where TEntity : class
    {   
        private IContextFactory ContextFactory { get; set; }

        public GraphEntityManager(IContextFactory contextFactory)
        {
            if (contextFactory == null)
                throw new ArgumentNullException(nameof(contextFactory));
            if (typeof(TEntity) == typeof(object))
                throw new InvalidOperationException(
                    "GraphEntityManager cannot be initialized for object type.");

            ContextFactory = contextFactory;
        }

        public string TypeName
        {
            get { return typeof(TEntity).Name; }
        }
        private IContextHelper ContextHelper
        {
            get { return ContextFactory.GetContextHelper(); }
        }     
        private IGraphEntityTypeManager EntityTypeManager
        {
            get { return ContextFactory.GetEntityTypeManager(TypeName); }
        }
        private DbContext Context
        {
            get { return ContextHelper.Context; }
        }

        /// <summary>
        /// Get primary keys according to type of entity.
        /// </summary>
        /// <returns>List of primary keys.</returns>
        public List<string> GetPrimaryKeys()
        {
            return EntityTypeManager.GetPrimaryKeys();
        }

        /// <summary>
        /// Get simple properties of entity.
        /// </summary>
        /// <returns>Simple properties of entity.</returns>
        public List<EdmProperty> GetSimpleEntityProperties()
        {
            return EntityTypeManager.GetSimpleEntityProperties();
        }

        /// <summary>
        /// If any of key members of entity is store generated.
        /// </summary>
        /// <returns>If any of key members of entity is store generated.</returns>
        public bool HasStoreGeneratedKey()
        {
            return EntityTypeManager.HasStoreGeneratedKey();
        }

        /// <summary>
        /// Get distinct unique properties of entity.
        /// </summary>
        /// <returns>List of unique properties.</returns>
        public List<PropertyInfo> GetUniqueProperties()
        {
            return EntityTypeManager.GetUniqueProperties();
        }

        /// <summary>
        /// Get foreign keys according to type name.
        /// </summary>
        /// <returns>List of foreign keys.</returns>
        public List<RelationshipDetail> GetForeignKeyDetails()
        {
            return EntityTypeManager.GetForeignKeyDetails();
        }

        /// <summary>
        /// Get navigation details according to name of type
        /// </summary>
        /// <returns>Navigation details of type</returns>
        public NavigationDetail GetNavigationDetail()
        {
            return EntityTypeManager.GetNavigationDetail();
        }

        /// <summary>
        /// Get entity with same primary or unique keys from the underlying source.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When entity is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When entity has no configured primary keys.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// When more than one matching entity has been found from underlying source.
        /// </exception>
        /// <param name="entity">Entity to look for in source.</param>
        /// <returns>Matching entity from underlying source.</returns>
        public TEntity GetMatchingEntity(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            IEnumerable<string> primaryKeyNames = GetPrimaryKeys();
            if (primaryKeyNames.Count() == 0)
                throw new ArgumentException(string.Format(
                    "'{0}' has no configured primary key.",
                    TypeName));

            ParameterExpression parameterExp = Expression.Parameter(entity.GetType(), "m");

            TEntity matchingEntity = null;

            Expression<Func<TEntity, bool>> filterExpression = 
                ConstructFilterExpression(entity, FilterType.IdOptionalUnique);

            if (filterExpression != null)
            {
                // If filter expression is not null query the database to find
                // matching entity. Otherwise if priamry keys have their default
                // values and there is no unique property defined for entity
                // there is no need to query the database.

                // Filter and load data into list.
                // Use AsNoTracking to not cache the matching entity.
                // Otherwise some problems will occur.
                List<TEntity> matchingEntities = Context.Set<TEntity>()
                    .AsNoTracking()
                    .Where(filterExpression)
                    .ToList();

                if (matchingEntities.Count() == 0)
                    matchingEntity = null;
                else if (matchingEntities.Count() == 1)
                    matchingEntity = matchingEntities.FirstOrDefault();
                else
                    throw new ArgumentOutOfRangeException(string.Format(
                        "The are more than one entity have been found from underlying source " +
                        "for '{0}'.\n" +
                        "Primary key data:\n{1}" +
                        "Unique key data:\n{2}",
                        TypeName,
                        entity.GetPropertyData(primaryKeyNames),
                        entity.GetPropertyData(GetUniqueProperties()
                            .Select(m => m.Name))));
            }            

            return matchingEntity;
        }

        /// <summary>
        /// Construct filter expression for entity. 
        /// </summary>
        /// <param name="typeOfFilter">Type of filter expression.</param>
        /// <returns>Filter expression according to entity.</returns>
        public Expression<Func<TEntity, bool>> ConstructFilterExpression(
            TEntity entity,
            FilterType typeOfFilter)
        {
            Expression<Func<TEntity, bool>> filterExpression = null;
            IEnumerable<string> primaryKeyNames = GetPrimaryKeys();

            if (primaryKeyNames.Count() == 0)
                throw new ArgumentException(string.Format(
                    "'{0}' has no configured primary key.",
                    typeof(TEntity).Name));

            List<Expression> equalityExpressions = new List<Expression>();
            IEnumerable<Expression> singleExpressionList;

            ParameterExpression parameterExp = Expression.Parameter(typeof(TEntity), "m");
            Expression pkFilter = null;
            Expression ukFilter = null;            

            bool primaryKeysHaveDefaultValues = entity.HasDefaultValues(primaryKeyNames);            

            if (!primaryKeysHaveDefaultValues
                && typeOfFilter != FilterType.OnlyUnique)
            {
                // If primary keys do not have their default values
                // add this check to equlity expression list
                singleExpressionList = parameterExp
                    .ConstructEqualityExpressions(
                        entity,
                        primaryKeyNames);

                pkFilter = singleExpressionList.ConstructAndChain();
            }            

            if (typeOfFilter != FilterType.OnlyId
                    && (primaryKeysHaveDefaultValues
                        || typeOfFilter != FilterType.IdOptionalUnique))
            {
                IEnumerable<PropertiesWithSource> uniqueProperties =
                    MappingStorage.Instance.UniqueProperties.Where(
                        m => m.SourceType.Equals(entity.GetType()));

                if (uniqueProperties.Count() > 0)
                {
                    foreach (PropertiesWithSource unique in uniqueProperties)
                    {
                        /*
                         ***********************************************************
                         * If any of current set of properties
                         * marked as unique is foreign key,
                         * has default value, and appropriate navigation property is not
                         * null and also origin of this foreign
                         * key has any store generated primary key then this 
                         * uniqueness must be ignored.
                         * For example if PersonId (int) and DocumentType (short) has been
                         * set as composite unique in PersonDocument and 
                         * if PersonId is foreign key to Person, which in its term has 
                         * Primary key which is store generated and if there is no navigation 
                         * property to Person from PersonDocument or PersonDocument.Person is not null
                         * then PersonId = 0 and DocumentType = 5 should not
                         * be treated as unique, because the real value of PersonId 
                         * will be computed when data will be inserted.
                         ***********************************************************
                        */                        

                        IGraphEntityTypeManager uniqueSourceTypeManager = ContextFactory
                            .GetEntityTypeManager(unique.SourceType.Name);

                        bool uniquenessMustBeIgnored = false;                        

                        var uniquePropertyNames = unique.Properties.Select(m => m.Name).ToList();
                        var uniqueForeignKeys = uniqueSourceTypeManager
                            .GetForeignKeyDetails()
                            .Where(m => m.FromDetails.ContainerClass != unique.SourceType.Name)
                            .Select(m => new
                            {
                                TargetClass = m.FromDetails.ContainerClass,
                                Keys = m.ToDetails.Keys
                                            .Intersect(uniquePropertyNames)
                            })
                            .Where(m => m.Keys != null
                                && m.Keys.Any());

                        NavigationDetail navigationDetailsOfCurrent = GetNavigationDetail();                        

                        // If unuque property is foreign key
                        if (uniqueForeignKeys != null
                            && uniqueForeignKeys.Any())
                        {
                            foreach (var uniqueFk in uniqueForeignKeys)
                            {
                                // If foreign key has default value
                                if (uniqueFk.Keys.Any(u => entity.HasDefaultValue(u)))
                                {
                                    // Get navigation relation according to foreign key
                                    NavigationRelation navigationRelation = navigationDetailsOfCurrent
                                        .Relations
                                        .FirstOrDefault(r => r.PropertyTypeName.Equals(uniqueFk.TargetClass)
                                            && r.ToKeyNames.Intersect(uniqueFk.Keys).Any());

                                    // If corresponding navigation property is not null
                                    // or there is no such navigation property
                                    if (navigationRelation == null
                                        || entity.GetPropertyValue(navigationRelation.PropertyName) != null)
                                    {
                                        bool foreignKeyHasStoreGeneratedPrimaryKey =
                                            uniqueFk.Keys.Any(k =>
                                            {
                                                /// Get origin of foreign key and check 
                                                /// if it has store generated key.
                                                string foreignKeyOrigin = uniqueSourceTypeManager
                                                    .GetOriginOfForeignKey(k);
                                                IGraphEntityTypeManager foreignKeyOriginTypeManger = ContextFactory
                                                    .GetEntityTypeManager(foreignKeyOrigin);
                                                return foreignKeyOriginTypeManger.HasStoreGeneratedKey();
                                            });

                                        // If origin of foreign key has store generated Primary key
                                        if (foreignKeyHasStoreGeneratedPrimaryKey)
                                        {
                                            uniquenessMustBeIgnored = true;
                                            break;
                                        }
                                    }
                                }
                            }                            
                        }                        

                        // If uniqueness must be ignored then skip this iteration
                        if (uniquenessMustBeIgnored)
                            continue;

                        singleExpressionList = parameterExp
                            .ConstructEqualityExpressions(
                                entity,
                                unique.Properties
                                .Select(m => m.Name).ToList());
                        equalityExpressions.Add(singleExpressionList.ConstructAndChain());
                    }

                    if (equalityExpressions.Count > 0)
                        ukFilter = equalityExpressions.ConstructOrChain();
                }
            }            

            equalityExpressions.Clear();
            if (pkFilter != null)
                equalityExpressions.Add(pkFilter);

            if (ukFilter != null)
                equalityExpressions.Add(ukFilter);

            if (equalityExpressions.Count > 0)
            {
                Expression filterBaseExpression = typeOfFilter == FilterType.IdAndUnique
                    ? equalityExpressions.ConstructAndChain()
                    : equalityExpressions.ConstructOrChain();

                filterExpression = Expression.Lambda<Func<TEntity, bool>>(
                        filterBaseExpression, parameterExp);
            }

            return filterExpression;
        }

        /// <summary>
        /// Synchronize keys of entity with matching entity.
        /// <remarks>
        /// In EntityFramework at one-to-one relationships, setting primary key
        /// of parent entity and setting state of entities to Unchanged or to Modified
        /// will also change primary key value of child entity.
        /// But vice-versa is not correct. This means that setting primary key value
        /// of child entity and setting state of entities to Unchanged or to Modified
        /// will not change primary key value of parent key, instead, it resets primary
        /// key value of child entity to its default value, (or to value of primary key
        /// of parent).
        /// This method is for synchronizing PKs of entity and PKs of matching entity and
        /// setting key of parent to key of child entities at one to relationships.
        /// </remarks>
        /// </summary>
        /// <param name="entity">Entity to set pk and parent key values.</param>
        /// <param name="matchingEntity">Found matching entity from underlying source.</param>
        /// <param name="shouldSetParentKeys">Set keys of parent entity 
        /// to value of child entity at one to one relations.</param>
        public void SynchronizeKeys(
            TEntity entity,
            TEntity matchingEntity,
            bool shouldSetParentKeys)
        {
            if (shouldSetParentKeys)
            {
                IEnumerable<RelationshipDetail> associatedRealtionships = GetForeignKeyDetails()
                    .Where(m => m.ToDetails.ContainerClass.Equals(entity.GetType().Name)
                        && m.ToDetails.RelationshipMultiplicity == RelationshipMultiplicity.One);


                foreach (RelationshipDetail relationshipDetail in associatedRealtionships)
                {
                    // Get parent property name from navigation details using information from foreign keys
                    IGraphEntityTypeManager entityTypeManager = ContextFactory
                        .GetEntityTypeManager(relationshipDetail.ToDetails.ContainerClass);
                    string parentPropertyName = entityTypeManager
                        .GetNavigationDetail()
                        .Relations
                        .Where(n => n.PropertyTypeName.Equals(relationshipDetail.FromDetails.ContainerClass)
                                    && n.SourceMultiplicity == relationshipDetail.ToDetails.RelationshipMultiplicity
                                    && n.TargetMultiplicity == relationshipDetail.FromDetails.RelationshipMultiplicity)
                        .Select(n => n.PropertyName)
                        .FirstOrDefault();

                    dynamic parent = entity.GetPropertyValue(parentPropertyName);

                    if (parent != null)
                    {
                        foreach (string keyName in relationshipDetail.FromDetails.Keys)
                        {
                            // At one-to-one relationships priamry key
                            // and foreign keys must match. So, parent
                            // and entity must have same property with name of keyName.
                            ReflectionExtensions.SetPropertyValue(parent,
                                keyName,
                                matchingEntity.GetPropertyValue(keyName));
                        }
                    }
                }
            }

            /*
            *   Description:
            *     PK value shuold be changed by using 
            *     context.Entry(entity).Property(pkName).CurrentValue = pkValue;
            *     becasue setting value by entity.pkName = pkValue will not synchronize
            *     it with dependent navigation properties automatically but prior method
            *     will do it.
            *     Primary key values of entity itself must be changed after
            *     principal parent keys has been synchronized. Because changing
            *     primary key value of entity using 
            *     context.Entry(entity).Property(pkName).CurrentValue = pkValue
            *     set principal parent navigation property to null.
            */
            IEnumerable<string> primaryKeyNames = GetPrimaryKeys();

            DbEntityEntry current = Context.Entry(entity);
            foreach (string primaryKey in primaryKeyNames)
            {
                current.Property(primaryKey).CurrentValue =
                    matchingEntity.GetPropertyValue(primaryKey);
            }
        }

        /// <summary>
        /// Compare entity and entity from source and detect which properties
        /// have been changed. Prepare changed proeprties for update.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When entity or entityFromSource is null.
        /// </exception>
        /// <param name="entity">Current entity to compare.</param>
        /// <param name="entityFromSource">Entity from source to compare.</param>
        /// <returns>True if any of propeties has been changed/False otherwise.</returns>
        public bool DetectPropertyChanges(
            TEntity entity,
            TEntity entityFromSource)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (entityFromSource == null)
                throw new ArgumentNullException(nameof(entityFromSource));

            bool anyPropertyHasChanged = false;

            IEnumerable<string> primaryKeyNames = GetPrimaryKeys();

            PropertiesWithSource notToCompareConfiguration = MappingStorage
                .Instance
                .PropertiesNotToCompare
                .Where(m => m.SourceType.Equals(typeof(TEntity)))
                .FirstOrDefault();

            PropertiesWithSource notUpdatableConfiguration = MappingStorage
                .Instance
                .NotUpdatableProperties
                .Where(m => m.SourceType.Equals(typeof(TEntity)))
                .FirstOrDefault();

            IEnumerable<string> propertiesNotToCompare;
            IEnumerable<string> notUpdatableProperties;

            if (notToCompareConfiguration != null)
                propertiesNotToCompare = notToCompareConfiguration
                    .Properties
                    .Select(m => m.Name);
            else
                propertiesNotToCompare = new List<string>();

            if (notUpdatableConfiguration != null)
                notUpdatableProperties = notUpdatableConfiguration
                    .Properties
                    .Select(m => m.Name);
            else
                notUpdatableProperties = new List<string>();


            // Get properties to compare
            List<string> propertiesToCompare = GetSimpleEntityProperties()
                .Where(m => !primaryKeyNames.Contains(m.Name)
                    && !propertiesNotToCompare.Contains(m.Name)
                    && !notUpdatableProperties.Contains(m.Name))
                .Select(m => m.Name)
                .ToList();

            DbEntityEntry<TEntity> entry = Context.Entry(entity);
            foreach (string propertyName in propertiesToCompare)
            {
                var currentValue = entity.GetPropertyValue(propertyName);
                var sourceValue = entityFromSource.GetPropertyValue(propertyName);

                if (!Utilities.IsEqual(
                    entity.GetPropertyValue(propertyName),
                    entityFromSource.GetPropertyValue(propertyName)))
                {
                    anyPropertyHasChanged = true;

                    entry.Property(propertyName).IsModified = true;
                }
            }

            // If any properties has changed also update
            // properties which marked not to compare
            if (anyPropertyHasChanged)
            {
                foreach (string propertyName in propertiesNotToCompare.Except(notUpdatableProperties))
                {
                    Context.Entry(entity)
                        .Property(propertyName).IsModified = true;
                }
            }

            return anyPropertyHasChanged;
        }        
    }
}