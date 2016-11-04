using Ma.EntityFramework.GraphManager.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq.Expressions;
using System.Reflection;

namespace Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers.Abstract
{
    public interface IGraphEntityManager<TEntity>
        where TEntity : class
    {
        string TypeName { get; }
        List<string> GetPrimaryKeys();
        List<EdmProperty> GetSimpleEntityProperties();
        bool HasStoreGeneratedKey();
        List<PropertyInfo> GetUniqueProperties();
        List<RelationshipDetail> GetForeignKeyDetails();        
        NavigationDetail GetNavigationDetail();
        TEntity GetMatchingEntity(TEntity entity);
        Expression<Func<TEntity, bool>> ConstructFilterExpression(
            TEntity entity,
            FilterType typeOfFilter);
        void SynchronizeKeys(
            TEntity entity,
            TEntity matchingEntity,
            bool shouldSetParentKeys);
        bool DetectPropertyChanges(
            TEntity entity,
            TEntity entityFromSource);
    }
}
