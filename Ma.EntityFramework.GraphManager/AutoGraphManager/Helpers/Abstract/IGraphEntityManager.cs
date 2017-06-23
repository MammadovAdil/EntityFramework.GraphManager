using Ma.EntityFramework.GraphManager.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq.Expressions;
using System.Reflection;

namespace Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers.Abstract
{
    /// <summary>
    /// Graph entity type manager.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IGraphEntityManager<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Name of type.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Get primary keys according to type of entity.
        /// </summary>
        /// <returns>List of primary keys.</returns>
        List<string> GetPrimaryKeys();

        /// <summary>
        /// Get simple properties of entity.
        /// </summary>
        /// <returns>Simple properties of entity.</returns>
        List<EdmProperty> GetSimpleEntityProperties();

        /// <summary>
        /// If any of key members of entity is store generated.
        /// </summary>
        /// <returns>If any of key members of entity is store generated.</returns>
        bool HasStoreGeneratedKey();

        /// <summary>
        /// Get distinct unique properties of entity.
        /// </summary>
        /// <returns>List of unique properties.</returns>
        List<PropertyInfo> GetUniqueProperties();

        /// <summary>
        /// Get foreign keys according to type name.
        /// </summary>
        /// <returns>List of foreign keys.</returns>
        List<RelationshipDetail> GetForeignKeyDetails();

        /// <summary>
        /// Get navigation details according to name of type
        /// </summary>
        /// <returns>Navigation details of type</returns>    
        NavigationDetail GetNavigationDetail();

        /// <summary>
        /// Get entity with same primary or unique keys from the underlying source.
        /// </summary>
        /// <param name="entity">Entity to look for in source.</param>
        /// <returns>Matching entity from underlying source.</returns>
        TEntity GetMatchingEntity(TEntity entity);

        /// <summary>
        /// Construct filter expression for entity. 
        /// </summary>
        /// <param name="entity">Entity to construct filter expression for.</param>
        /// <param name="typeOfFilter">Type of filter expression.</param>
        /// <returns>Filter expression according to entity.</returns>
        Expression<Func<TEntity, bool>> ConstructFilterExpression(
            TEntity entity,
            FilterType typeOfFilter);

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
        void SynchronizeKeys(
            TEntity entity,
            TEntity matchingEntity);

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
        bool DetectPropertyChanges(
            TEntity entity,
            TEntity entityFromSource);
    }
}
