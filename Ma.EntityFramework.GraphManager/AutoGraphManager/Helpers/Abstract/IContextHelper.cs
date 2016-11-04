using Ma.EntityFramework.GraphManager.ManualGraphManager.Abstract;
using Ma.EntityFramework.GraphManager.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;

namespace Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers.Abstract
{
    public interface IContextHelper
    {
        DbContext Context { get; set; }
        ObjectContext ObjectContext { get; }
        HelperStore Store { get; }
        IEnumerable<NavigationDetail> GetNavigationDetails();
        List<RelationshipDetail> GetForeignKeyDetails();

        object GetUppermostPrincipalParent<TEntity>(TEntity entity)
            where TEntity : class;
        object GetUppermostParent<TEntity>(TEntity entity)
            where TEntity : class;
        IEnumerable<object> GetParents<TEntity>(
            TEntity entity,
            bool onlyPrincipal)
            where TEntity : class;

        void DetachWithDependants<TEntity>(
            TEntity entity,
            bool detachItself)
            where TEntity : class;
        void GetAllEntities<TEntity>(
            TEntity entity,
            List<object> relatedEntityList)
            where TEntity : class;

        IManualGraphManager DefineState();
        IManualGraphManager<TEntity> DefineState<TEntity>(
            TEntity entity,
            bool defineStateOfChildEntities)
            where TEntity : class;
        IManualGraphManager<TEntity> DefineState<TEntity>(
            List<TEntity> entityList,
            bool defineStateOfChildEntities)
            where TEntity : class;
    }
}
