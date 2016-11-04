using System;
using System.Linq.Expressions;
using Ma.EntityFramework.GraphManager.CustomMappings.MappingHelpers;

namespace Ma.EntityFramework.GraphManager.CustomMappings
{
    public interface IExtendedEntityTypeConfiguration<T>
        where T : class
    {
        /// <summary>
        /// Mark properties as unique.
        /// </summary>
        /// <typeparam name="TProperty">Type of property.</typeparam>
        /// <param name="propertyLambda">Lambda expression to mark properties as unique.</param>
        void HasUnique<TProperty>(
            Expression<Func<T, TProperty>> propertyLambda);

        /// <summary>
        /// Mark properties state of which has to be defined in order
        /// to be able to correctly define state of entity itslef.
        /// Properties from which state of entity is dependant should be marked.
        /// </summary>
        /// <typeparam name="TProperty">Type of property.</typeparam>
        /// <param name="propertyLambda">Lambda expression to get 
        /// properties state of which must be defined.</param>
        void HasStateDefiner<TProperty>(
            Expression<Func<T, TProperty>> propertyLambda);

        /// <summary>
        /// Get property of source to work on.
        /// </summary>
        /// <typeparam name="TProperty">Type of property.</typeparam>
        /// <param name="propertyLambda">Lmbda expression to get property.</param>
        /// <returns>Extended property helper to be able to work on property.</returns>
        ExtendedPropertyHelper<T> ExtendedProperty<TProperty>(
            Expression<Func<T, TProperty>> propertyLambda);
    }
}
