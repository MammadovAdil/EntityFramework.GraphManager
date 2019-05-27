using Ma.EntityFramework.GraphManager.Models;
using Ma.EntityFramework.GraphManager.CustomMappings.MappingHelpers;
using Ma.EntityFramework.GraphManager.DataStorage;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Ma.ExtensionMethods.Reflection;

namespace Ma.EntityFramework.GraphManager.CustomMappings
{
    /// <summary>
    /// ExtendedEntityTypeConfiguration extends EntityTypeConfiguration
    /// and adds additional methods to enhance working with entities.
    /// </summary>
    /// <typeparam name="TEntity">Type of entity to map.</typeparam>
    public class ExtendedEntityTypeConfiguration<TEntity>
        : EntityTypeConfiguration<TEntity>, IExtendedEntityTypeConfiguration<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Mark properties as unique.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When lambda expression is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When lambda expression deos not select any property.
        /// When lambda expression selects not appropriate properties.
        /// When lambda expression selects already selected combination
        /// of properties to set as unique.
        /// </exception>
        /// <typeparam name="TProperty">Type of property.</typeparam>
        /// <param name="propertyLambda">Lambda expression to mark properties as unique.</param>
        public void HasUnique<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyLambda)
        {
            if (propertyLambda == null)
                throw new ArgumentNullException("propertyLambda");

            var markedProperties = propertyLambda.GetPropertyInfoList();

            if (markedProperties == null
                || markedProperties.Count == 0)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' for '{1}' marks no property to set as unique",
                    propertyLambda.ToString(),
                    typeof(TEntity).Name));

            // Selects properties which are not appropriate to set as unique
            var violatedProperties = markedProperties
                .Where(m => m.PropertyType.IsCollectionType()
                    || (!m.PropertyType.IsBuiltinType()
                        && (!m.PropertyType.IsEnum
                            && !m.PropertyType.IsNullableEnum())));


            if (violatedProperties != null
                && violatedProperties.Count() > 0)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' for '{1}' selects inappropriate properties to set unique.\n" +
                    "Only built in value types or enums can be set as unique.",
                    propertyLambda.ToString(),
                    typeof(TEntity).Name));

            var markedAsUnique = new PropertiesWithSource()
            {
                SourceType = typeof(TEntity),
                Properties = markedProperties
            };

            var duplicates = MappingStorage.Instance.UniqueProperties
                .Where(m => m.SourceType.Equals(markedAsUnique.SourceType)
                        && m.Properties
                            .Select(p => p.Name)
                            .OrderBy(p => p)
                            .SequenceEqual(markedAsUnique
                                            .Properties
                                            .Select(p => p.Name)
                                            .OrderBy(p => p)));

            if (duplicates.Any())
                return;

            if (!MappingStorage.Instance.UniqueProperties.Contains(markedAsUnique))
                MappingStorage.Instance.UniqueProperties.Add(markedAsUnique);
        }

        /// <summary>
        /// Mark properties state of which has to be defined before entity itself
        /// in order to be able to correctly define state of entity itself. These properties
        /// are those uniqueness of which can be determined easily according to their 
        /// property values. Properties from which state of entity is dependant from 
        /// and which are in one-to-one relationship with this entity should be marked. 
        /// Parents of entity or entities which are in many-to-one relationship with this entity
        /// should not be marked, they are automatically ordered.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When lambda expression is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When lambda expression selects no property at all.
        /// When lambda expression selects inappropriate properties to define state of.
        /// When lambda expression selects already selected property to define state of.
        /// </exception>
        /// <typeparam name="TProperty">Type of property.</typeparam>
        /// <param name="propertyLambda">Lambda expression to get properties 
        /// state of which must be defined.</param>        
        public void HasStateDefiner<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyLambda)
        {
            if (propertyLambda == null)
                throw new ArgumentNullException("propertyLambda");

            var markedProperties = propertyLambda.GetPropertyInfoList();

            if (markedProperties == null
                || markedProperties.Count == 0)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' for '{1}' marks no property to define state of",
                    propertyLambda.ToString(),
                    typeof(TEntity).Name));

            // Selects properties which are not appropriate to set as to define sate of
            var violatedProperties = markedProperties
                .Where(m => m.PropertyType.IsValueType
                    || (m.PropertyType.IsBuiltinType()
                        && m.PropertyType.IsCollectionType()
                        && m.PropertyType.IsGenericType
                        && (m.PropertyType
                            .GenericTypeArguments
                            .FirstOrDefault()
                            .IsBuiltinType()
                            || !m.PropertyType
                                .GenericTypeArguments
                                .FirstOrDefault()
                                .IsClass)));

            if (violatedProperties != null
                && violatedProperties.Count() > 0)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' for '{1}' selects inappropriate properties to define state of.\n" +
                    "Only properties of user defined class type or collections of those classes" +
                    "can be set to define state of",
                    propertyLambda.ToString(),
                    typeof(TEntity).Name));


            var markedToDefineStateOf = MappingStorage.Instance.StateDefiners
                .Where(m => m.SourceType.Equals(typeof(TEntity)))
                .FirstOrDefault();

            if (markedToDefineStateOf == null)
                markedToDefineStateOf = new PropertiesWithSource()
                {
                    SourceType = typeof(TEntity)
                };


            var alreadyAdded = markedToDefineStateOf.Properties
                .Any(m => markedProperties
                            .Select(p => p.Name)
                            .Contains(m.Name));

            if (alreadyAdded)
                return;

            markedToDefineStateOf.Properties.AddRange(markedProperties);

            if (!MappingStorage.Instance.StateDefiners.Contains(markedToDefineStateOf))
                MappingStorage.Instance.StateDefiners.Add(markedToDefineStateOf);
        }

        /// <summary>
        /// Get property of source to work on.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When propertyLambda is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When propertyLambda does not select a proeprty
        /// </exception>
        /// <typeparam name="TProperty">Type of property.</typeparam>
        /// <param name="propertyLambda">Lambda expression to get property.</param>
        /// <returns>Extended property helper to be able to work on property.</returns>
        public ExtendedPropertyHelper<TEntity> ExtendedProperty<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyLambda)
        {
            if (propertyLambda == null)
                throw new ArgumentNullException("propertyLambda");

            var property = propertyLambda.GetPropertyInfo();

            if (property == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' does not select any property",
                    propertyLambda.ToString()));

            var helper = new ExtendedPropertyHelper<TEntity>(property);
            return helper;
        }
    }
}