using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ma.EntityFramework.GraphManager.Models
{
    /// <summary>
    /// Property with container source
    /// </summary>
    public class PropertiesWithSource
    {
        internal PropertiesWithSource()
        {
            Properties = new List<PropertyInfo>();
        }

        internal Type SourceType { get; set; }
        internal List<PropertyInfo> Properties { get; set; }
    }
}
