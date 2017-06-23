using Ma.EntityFramework.GraphManager.Models;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Reflection;

namespace Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers.Abstract
{
    /// <summary>
    /// Graph entity type manager.
    /// </summary>
    public interface IGraphEntityTypeManager
    {
        /// <summary>
        /// Get primary keys according to type of entity.
        /// </summary>
        /// <returns>List of primary keys.</returns>
        List<string> GetPrimaryKeys();

        /// <summary>
        /// If any of key members of entity is store generated.
        /// </summary>
        /// <returns>If any of key members of entity is store generated.</returns>
        bool HasStoreGeneratedKey();

        /// <summary>
        /// Get distinct unique properties of entity.
        /// </summary>
        /// <returns>List of unique properties.</returns>
        List<PropertyInfo> GetUniqueProperties();

        /// <summary>
        /// Get foreign keys according to type name.
        /// </summary>
        /// <returns>List of foreign keys.</returns>
        List<RelationshipDetail> GetForeignKeyDetails();

        /// <summary>
        /// Get simple properties of entity.
        /// </summary>
        /// <returns>Simple properties of entity.</returns>
        List<EdmProperty> GetSimpleEntityProperties();

        /// <summary>
        /// Get navigation details according to name of type
        /// </summary>
        /// <returns>Navigation details of type</returns>
        NavigationDetail GetNavigationDetail();

        /// <summary>
        /// Get the the origin class which this foreign key refers to.
        /// </summary>
        /// <param name="foreignKeyName">Name of foreign key property.</param>
        /// <returns>Name of class which this foreign key refers to.</returns>
        string GetOriginOfForeignKey(string foreignKeyName);

        /// <summary>
        /// Find on how many properties this type depends on.
        /// </summary>
        /// <param name="store">Calculation store.</param>
        /// <returns>Calculated count.</returns>
        int FindPrincipalCount(Dictionary<string, int> store);
    }
}
