using System;
using System.Collections.Generic;
using System.Data.Entity;
using Ma.EntityFramework.GraphManager.ManualGraphManager.Helpers;
using System.Data.Entity.Infrastructure;
using Ma.EntityFramework.GraphManager.ManualGraphManager.Abstract;
using Ma.EntityFramework.GraphManager.ManualGraphManager.Helpers.Abstract;
using Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers;

namespace Ma.EntityFramework.GraphManager.ManualGraphManager
{
    public class ManualGraphManager
        : IManualGraphManager
    {
        private Lazy<ContextHelper> lazyContextHelper;

        private DbContext Context { get; set; }       

        /// <summary>
        /// List of entities to manipulate.
        /// </summary>
        //public List<T> EntityCollection { get; set; }

        public ManualGraphManager(DbContext contextParam)
        {
            if (contextParam == null)
                throw new ArgumentNullException("contextParam");

            Context = contextParam;
            lazyContextHelper = new Lazy<ContextHelper>(
                () => new ContextHelper(contextParam));
        }

        private ContextHelper ContextHelper
        {
            get { return lazyContextHelper.Value; }
        }

        /// <summary>
        /// Get Entry relevant to entity.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When entity is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When no entry was found according to entity.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="entity">Entity to get entry relevant to.</param>
        /// <returns>Entry helper to be able to work on entry.</returns>
        public IEntryHelper<TEntity> Entry<TEntity>(TEntity entity)
            where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            DbEntityEntry entry = Context.Entry(entity);

            if (entry == null)
                throw new ArgumentException(string.Format(
                    "No entry was found relevant to entity of type '{0}'.",
                    entity.GetType().Name));

            return new EntryHelper<TEntity>(entry);
        }

        /// <summary>
        /// Detach entity from context.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When entity is null
        /// </exception>
        /// <typeparam name="TEntity">Type of entity to detach.</typeparam>
        /// <param name="entity">Entity to detach.</param>
        /// <param name="detachDependants">Also detach dependant 
        /// navigation properties.</param>
        public void DetachWithDependants<TEntity>(TEntity entity)
            where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            ContextHelper.DetachWithDependants(entity, true);
        }
    }

    public class ManualGraphManager<T>
        : ManualGraphManager, IManualGraphManager<T>
        where T : class
    {
        /// <summary>
        /// List of entities to manipulate.
        /// </summary>
        public List<T> EntityCollection { get; set; }

        public ManualGraphManager(DbContext contextParam)
            : base(contextParam)
        {
        }
    }
}