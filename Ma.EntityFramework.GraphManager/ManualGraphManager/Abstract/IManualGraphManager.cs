using Ma.EntityFramework.GraphManager.ManualGraphManager.Helpers.Abstract;
using System.Collections.Generic;

namespace Ma.EntityFramework.GraphManager.ManualGraphManager.Abstract
{
    public interface IManualGraphManager
    {
        /// <summary>
        /// Get Entry relevant to entity.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entity">Entity to get entry relevant to.</param>
        /// <returns>Entry helper to be able to work on entry.</returns>
        IEntryHelper<TEntity> Entry<TEntity>(TEntity entity)
            where TEntity : class;

        /// <summary>
        /// Detach entity from context.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity to detach.</typeparam>
        /// <param name="entity">Entity to detach.</param>
        /// <param name="detachDependants">Also detach dependant 
        /// navigation properties.</param>
        void DetachWithDependants<TEntity>(TEntity entity)
            where TEntity : class;
    }

    public interface IManualGraphManager<T>
        : IManualGraphManager
        where T : class
    {
        /// <summary>
        /// List of entities to manipulate.
        /// </summary>
        List<T> EntityCollection { get; set; }
    }
}
