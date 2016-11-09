using System;
using System.Linq.Expressions;
using Ma.EntityFramework.GraphManager.CustomMappings.MappingHelpers;

namespace Ma.EntityFramework.GraphManager.CustomMappings
{
    /// <summary>
    /// Extended entity type configurations.
    /// </summary>
    /// <typeparam name="TEntity">Type of entity.</typeparam>
    public interface IExtendedEntityTypeConfiguration<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Mark properties as unique.
        /// </summary>
        /// <typeparam name="TProperty">Type of property.</typeparam>
        /// <param name="propertyLambda">Lambda expression to mark properties as unique.</param>
        void HasUnique<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyLambda);

        /// <summary>
        /// Mark properties state of which has to be defined in order
        /// to be able to correctly define state of entity itslef.
        /// Properties from which state of entity is dependant should be marked.
        /// </summary>
        /// <typeparam name="TProperty">Type of property.</typeparam>
        /// <param name="propertyLambda">Lambda expression to get 
        /// properties state of which must be defined.</param>
        void HasStateDefiner<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyLambda);

        /// <summary>
        /// Get property of source to work on.
        /// </summary>
        /// <typeparam name="TProperty">Type of property.</typeparam>
        /// <param name="propertyLambda">Lmbda expression to get property.</param>
        /// <returns>Extended property helper to be able to work on property.</returns>
        ExtendedPropertyHelper<TEntity> ExtendedProperty<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyLambda);
    }
}
