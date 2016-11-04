using Ma.EntityFramework.GraphManager.Models;
using System;
using System.Collections.Generic;

namespace Ma.EntityFramework.GraphManager.DataStorage
{
    internal class MappingStorage
    {
        // This is for singleton pattern
        private static Lazy<MappingStorage> lazy =
            new Lazy<MappingStorage>(() => new MappingStorage());

        public static MappingStorage Instance { get { return lazy.Value; } }

        private MappingStorage()
        {
        }

        private List<PropertiesWithSource> uniqueProperties;
        private List<PropertiesWithSource> propertiesNotToCompare;
        private List<PropertiesWithSource> stateDefiners;
        private List<PropertiesWithSource> notUpdatableProperties;

        /// <summary>
        /// Unique properties of entities
        /// </summary>
        public List<PropertiesWithSource> UniqueProperties
        {
            get
            {
                if (uniqueProperties == null)
                    uniqueProperties = new List<PropertiesWithSource>();

                return uniqueProperties;
            }
        }
        /// <summary>
        /// Properties not to compare while defining if property should be updated
        /// </summary>
        public List<PropertiesWithSource> PropertiesNotToCompare
        {
            get
            {
                if (propertiesNotToCompare == null)
                    propertiesNotToCompare = new List<PropertiesWithSource>();

                return propertiesNotToCompare;
            }
        }
        /// <summary>
        /// Navigation properties of entities to check agains database
        /// to determine state of navigation property
        /// </summary>
        public List<PropertiesWithSource> StateDefiners
        {
            get
            {
                if (stateDefiners == null)
                    stateDefiners = new List<PropertiesWithSource>();

                return stateDefiners;
            }
        }
        /// <summary>
        /// Properties which are never updated after insert.
        /// </summary>
        public List<PropertiesWithSource> NotUpdatableProperties
        {
            get
            {
                if (notUpdatableProperties == null)
                    notUpdatableProperties = new List<PropertiesWithSource>();

                return notUpdatableProperties;
            }
        }
    }
}
