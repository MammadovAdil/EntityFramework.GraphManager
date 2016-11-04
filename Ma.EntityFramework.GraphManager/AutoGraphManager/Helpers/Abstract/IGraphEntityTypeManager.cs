using Ma.EntityFramework.GraphManager.Models;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Reflection;

namespace Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers.Abstract
{
    public interface IGraphEntityTypeManager
    {        
        List<string> GetPrimaryKeys();
        bool HasStoreGeneratedKey();
        List<PropertyInfo> GetUniqueProperties();
        List<RelationshipDetail> GetForeignKeyDetails();
        List<EdmProperty> GetSimpleEntityProperties();
        NavigationDetail GetNavigationDetail();
        string GetOriginOfForeignKey(string foreignKeyName);
        int FindPrincipalCount(Dictionary<string, int> store);
    }
}
