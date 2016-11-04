using System.Data.Entity.Core.Metadata.Edm;

namespace Ma.EntityFramework.GraphManager.Models
{
    public class ForeignKeyDetail : KeyDetail
    {
        public RelationshipMultiplicity RelationshipMultiplicity { get; set; }
    }
}
