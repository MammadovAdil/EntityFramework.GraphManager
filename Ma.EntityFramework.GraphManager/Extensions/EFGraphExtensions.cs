﻿using Ma.EntityFramework.GraphManager.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Ma.EntityFramework.GraphManager.ManualGraphManager.Abstract;
using Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers;
using Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers.Abstract;

namespace Ma.EntityFramework.GraphManager.Extensions
{
    public static class EFGraphExtensions
    {
        /// <summary>
        /// Select properties according to property names from source.
        /// </summary>
        /// <remarks>
        /// This method is copied from StackOverflow answer (http://stackoverflow.com/a/723018/1380428).
        /// Alterations and additions made by Adil Mammadov.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// When source or propertyNames is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// When there is no item in propertyNames.
        /// </exception>
        /// <param name="source">Source to select from.</param>
        /// <param name="propertyNames">Name of properties to get data according to.</param>
        /// <returns>The result as IQueryable.</returns>
        public static IQueryable<object> SelectDynamic(
            this IQueryable source,
            IEnumerable<string> propertyNames)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (propertyNames == null)
                throw new ArgumentNullException(nameof(propertyNames));

            if (propertyNames.Count() == 0)
                throw new ArgumentOutOfRangeException("propertyNames", "There is no propertyName in propertyNames");

            Dictionary<string, PropertyInfo> sourceProperties = propertyNames
                .ToDictionary(name => name, name => source.ElementType.GetProperty(name));

            // Create anonymous type at runtime
            Type dynamicType = LinqRuntimeTypeBuilder.GetDynamicType(sourceProperties.Values);

            ParameterExpression sourceItem = Expression.Parameter(source.ElementType, "m");
            IEnumerable<MemberBinding> bindings = dynamicType.GetProperties()
                .Select(p => Expression.Bind(p, Expression.Property(sourceItem, sourceProperties[p.Name])))
                .OfType<MemberBinding>();

            Expression selector = Expression.Lambda(Expression.MemberInit(
                Expression.New(dynamicType.GetConstructor(Type.EmptyTypes)), bindings), sourceItem);

            IQueryable queryResult = source.Provider.CreateQuery(
                Expression.Call(typeof(Queryable),
                                "Select",
                                new Type[] { source.ElementType, dynamicType },
                                source.Expression,
                                selector));

            // Cast to IQueryable<object> to be able to use Linq
            return (IQueryable<object>)queryResult;
        }

        /// <summary>
        /// Define state of all entities in the context.
        /// </summary>
        /// <param name="context">Context to work on.</param>
        public static IManualGraphManager DefineState(
            this DbContext context)
        {
            ContextHelper contextHelper = new ContextHelper(context);
            return contextHelper.DefineState();
        }

        /// <summary>
        /// Define state of entity. If entity already exists in the source
        /// set and values has not been altered set the state to Unchanged, 
        /// else if values has been changed set the state of changed properties
        /// to Modified, otherwise set the state of entity to Added.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When context or entity is null.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="context">Context to work on.</param>
        /// <param name="entity">Entity to define state of.</param>
        /// <param name="defineStateOfChildEntities">If set to true define state of
        /// configured child entities. This rule also applied to child entities
        /// of child entities and so on.</param>
        /// <returns>IManualGraphManager associated with current context to work on further.</returns>
        public static IManualGraphManager<TEntity> DefineState<TEntity>(
            this DbContext context,
            TEntity entity,
            bool defineStateOfChildEntities)
            where TEntity : class
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            ContextHelper contextHelper = new ContextHelper(context);
            return contextHelper.DefineState(entity, defineStateOfChildEntities);
        }

        /// <summary>
        /// Define state of entity. If entity already exists in the source
        /// set and values has not been altered set the state to Unchanged, 
        /// else if values has been changed set the state of changed properties
        /// to Modified, otherwise set the state of entity to Added. Do it for
        /// all child entities also.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When context or entity is null.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="context">Context to work on.</param>
        /// <param name="entity">Entity to define state of.</param>
        /// <returns>IManualGraphManager associated with current context to work on further.</returns>
        public static IManualGraphManager<TEntity> DefineState<TEntity>(
            this DbContext context,
            TEntity entity)
            where TEntity : class
        {
            return context.DefineState(entity, true);
        }

        /// <summary>
        /// Define state of list of entities. If entity already exists in the source
        /// set and values has not been altered set the state to Unchanged, 
        /// else if values has been changed set the state of changed properties
        /// to Modified, otherwise set the state of entity to Added.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When context or entity is null.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="context">Context to work on.</param>
        /// <param name="entityList">List of entities to define state of.</param>
        /// <param name="defineStateOfChildEntities">If set to true define state of
        /// configured child entities. This rule also applied to child entities
        /// of child entities and so on.</param>
        /// <returns>IManualGraphManager associated with current context to work on further.</returns>
        public static IManualGraphManager<TEntity> DefineState<TEntity>(
            this DbContext context,
            List<TEntity> entityList,
            bool defineStateOfChildEntities)
            where TEntity : class
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (entityList == null)
                throw new ArgumentNullException(nameof(entityList));

            ContextHelper contextHelper = new ContextHelper(context);
            return contextHelper.DefineState(entityList, defineStateOfChildEntities);
        }

        /// <summary>
        /// Define state of list of entities. If entity already exists in the source
        /// set and values has not been altered set the state to Unchanged, 
        /// else if values has been changed set the state of changed properties
        /// to Modified, otherwise set the state of entity to Added. Do it for child
        /// entities also.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When context or entity is null.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="context">Context to work on.</param>
        /// <param name="entityList">List of entities to define state of.</param>
        /// <returns>IManualGraphManager associated with current context to work on further.</returns>
        public static IManualGraphManager<TEntity> DefineState<TEntity>(
            this DbContext context,
            List<TEntity> entityList)
            where TEntity : class
        {
            return context.DefineState(entityList, true);
        }

        /// <summary>
        /// Add entity to context.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="context">Context to work on.</param>
        /// <param name="entity">Entity to add to context.</param>
        private static void Add<TEntity>(
            this DbContext context,
            TEntity entity)
            where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // If entity is not detached detach it to add again with
            // all navigation properties. Setting state to
            // added as below:
            // Context.Entry(entity).State = EntityState.Addded
            // will not add navigation properties
            if (context.Entry(entity).State != EntityState.Detached)
                context.Entry(entity).State = EntityState.Detached;

            context.Set<TEntity>().Add(entity);
        }

        /// <summary>
        /// Add or update entity. If entity already exists in the source
        /// set and values has not been altered set the state to Unchanged, 
        /// else if values has been changed set the state of changed properties
        /// to Modified, otherwise set the state of entity to Added. Do it for
        /// all child entities also.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When context or entity is null.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="context">Context to work on.</param>
        /// <param name="entity">Entity to add or update.</param>
        /// <returns>IManualGraphManager associated with current context to work on further.</returns>
        public static IManualGraphManager<TEntity> AddOrUpdate<TEntity>(
            this DbContext context,
            TEntity entity)
            where TEntity : class
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            context.Add(entity);            
            return context.DefineState(entity, true);
        }

        /// <summary>
        /// Add or update list of entities. If entity already exists in the source
        /// set and values has not been altered set the state to Unchanged, 
        /// else if values has been changed set the state of changed properties
        /// to Modified, otherwise set the state of entity to Added. Do it for child
        /// entities also.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When context or entity is null.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="context">Context to work on.</param>
        /// <param name="entityList">List of entities to add or update.</param>
        /// <returns>IManualGraphManager associated with current context to work on further.</returns>
        public static IManualGraphManager<TEntity> AddOrUpdate<TEntity>(
            this DbContext context,
            List<TEntity> entityList)
            where TEntity : class
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (entityList == null)
                throw new ArgumentNullException(nameof(entityList));

            entityList.ForEach(m => context.Add(m));
            return context.DefineState(entityList, true);
        }

        /// <summary>
        /// Detach dependants of entity.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When context or entity is null
        /// </exception>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <param name="context">Context to work on.</param>
        /// <param name="entity">Entity to detach dependants.</param>
        public static void DetachWithDependants<TEntity>(
            this DbContext context,
            TEntity entity)
            where TEntity : class
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            ContextHelper contextHelper = new ContextHelper(context);
            contextHelper.DetachWithDependants(entity, true);
        }

        /// <summary>
        /// Get primary keys according to type of entity.
        /// </summary>
        /// <remarks>
        /// Generally useful when logging to get key data to later find entity easily.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// When context or typeName is null.
        /// </exception>
        /// <param name="context">Context to get primary keys from.</param>
        /// <param name="typeName">Name of type to get primary keys for.</param>
        /// <returns>List of primary keys.</returns>
        public static List<string> GetPrimaryKeys(
            this DbContext context,
            string typeName)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));

            ContextHelper contextHelper = new ContextHelper(context);
            IGraphEntityTypeManager entityTypeManager = contextHelper
                .GetEntityTypeManager(typeName);

            return entityTypeManager.GetPrimaryKeys();
        }

        /// <summary>
        /// Get list of unique properties.
        /// </summary>
        /// <remarks>
        /// Generally useful when logging to get key data to later find entity easily.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// When context or typeName is null.
        /// </exception>
        /// <param name="context">Context to get unique keys from.</param>
        /// <param name="typeName">Name of type to get unique keys for.</param>
        /// <returns>List of unique properties</returns>
        public static List<PropertyInfo> GetUniqueProperties(
            this DbContext context,
            string typeName)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));

            ContextHelper contextHelper = new ContextHelper(context);
            IGraphEntityTypeManager entityTypeManager = contextHelper
                .GetEntityTypeManager(typeName);

            return entityTypeManager.GetUniqueProperties();
        }
    }
}