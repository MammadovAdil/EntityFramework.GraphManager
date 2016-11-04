using System;
using System.Data.Entity;
using System.Linq.Expressions;

namespace Ma.EntityFramework.GraphManager.ManualGraphManager.Helpers.Abstract
{
    public interface IEntryHelper<T>
        where T : class
    {
        EntityState State { get; set; }
        IEntryPropertyHelper<TProperty> Property<TProperty>(
            Expression<Func<T, TProperty>> propertyLambda);
    }
}
