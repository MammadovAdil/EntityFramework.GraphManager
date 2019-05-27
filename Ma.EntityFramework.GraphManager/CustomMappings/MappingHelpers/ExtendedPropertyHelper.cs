using Ma.EntityFramework.GraphManager.Models;
using System;
using System.Linq;
using System.Reflection;
using Ma.ExtensionMethods.Reflection;
using Ma.EntityFramework.GraphManager.DataStorage;

namespace Ma.EntityFramework.GraphManager.CustomMappings.MappingHelpers
{
    /// <summary>
    /// Helper class for extended property to be able to use
    /// fluent chaining
    /// </summary>
    /// <typeparam name="T">Type of source</typeparam>
    public class ExtendedPropertyHelper<T>
        : IMappingHelper
        where T : class
    {
        private PropertyInfo Property { get; set; }

        internal ExtendedPropertyHelper(PropertyInfo propertyParam)
        {
            Property = propertyParam ?? throw new ArgumentNullException("propertyParam");
        }

        /// <summary>
        /// Mark property as should not be compared to define if 
        /// entity or property should be updated. This property will
        /// not be checked against database value to detect if values
        /// has been changed. If any other property has been changed
        /// this property will also be updated to its current value.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// When not appropriate or already marked property is marked.
        /// </exception>
        /// <returns>Current ExtendedPropertyHelper.</returns>
        public ExtendedPropertyHelper<T> ShouldNotBeCompared()
        {
            if (!Property.PropertyType.GetUnderlyingType().IsBuiltinType())
                throw new ArgumentException(string.Format(
                    "Property '{0}' for '{1}' is inappropriate property to set as should not to compared.\n" +
                    "Only properties underlying type of which is built in types can be set as should not to compared.",
                    Property.Name,
                    typeof(T).Name));

            var markedNotToCompare =
                MappingStorage.Instance.PropertiesNotToCompare
                .Where(m => m.SourceType.Equals(typeof(T)))
                .FirstOrDefault();

            if (markedNotToCompare == null)
                markedNotToCompare = new PropertiesWithSource() { SourceType = typeof(T) };

            var alreadyAdded = markedNotToCompare.Properties
                .Any(m => Property.Name.Equals(m.Name));
            if (alreadyAdded)
                return this;

            markedNotToCompare.Properties.Add(Property);

            if (!MappingStorage.Instance.PropertiesNotToCompare.Contains(markedNotToCompare))
                MappingStorage.Instance.PropertiesNotToCompare.Add(markedNotToCompare);

            return this;
        }

        /// <summary>
        /// Mark property as not updatable. Not updatable properties
        /// will not be updated after inserted. Not updatable properties
        /// will not be checked against database to define if value of property
        /// has been changed.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// When not appropriate or already marked property is marked.
        /// </exception>
        /// <returns>Current ExtendedPropertyHelper.</returns>
        public ExtendedPropertyHelper<T> NotUpdatable()
        {
            var sourceType = typeof(T);

            if (!Property.PropertyType.GetUnderlyingType().IsBuiltinType())
                throw new ArgumentException(string.Format(
                    "Property '{0}' for '{1}' is inappropriate property to set as not updatable.\n" +
                    "Only properties underlying type of which is built in types can be set as not updatable.",
                    Property.Name,
                    typeof(T).Name));

            var markedNotUpdatable =
                MappingStorage.Instance.NotUpdatableProperties
                .Where(m => m.SourceType.Equals(typeof(T)))
                .FirstOrDefault();

            if (markedNotUpdatable == null)
                markedNotUpdatable = new PropertiesWithSource() { SourceType = typeof(T) };

            var alreadyAdded = markedNotUpdatable.Properties
                .Any(m => Property.Name.Equals(m.Name));
            if (alreadyAdded)
                return this;

            markedNotUpdatable.Properties.Add(Property);
            
            if (!MappingStorage.Instance.NotUpdatableProperties.Contains(markedNotUpdatable))
                MappingStorage.Instance.NotUpdatableProperties.Add(markedNotUpdatable);

            return this;
        }
    }
}
