using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;

namespace Ma.EntityFramework.GraphManager.Models
{
    /// <summary>
    /// Name of From and To classes and list of names of From and To properties
    /// </summary>
    public class RelationshipDetail
    {
        internal RelationshipDetail()
        {
            FromDetails = new ForeignKeyDetail();
            ToDetails = new ForeignKeyDetail();
        }

        /// <summary>
        /// Cosntruct RelationshipDetail accoring to ReferentialConstraint.
        /// </summary>
        /// <param name="referentialConstraint">Referential constraint to get foreign keys from.</param>
        internal RelationshipDetail(
            ReferentialConstraint referentialConstraint)
        {
            if (referentialConstraint == null)
                return;

            Initialize(referentialConstraint);
        }

        internal ForeignKeyDetail FromDetails { get; set; }
        internal ForeignKeyDetail ToDetails { get; set; }

        /// <summary>
        /// Initialize foreign key details according to referential constraint.
        /// </summary>
        /// <param name="referentialConstraint">Referential constraint to get foreign keys from.</param>
        private void Initialize(
            ReferentialConstraint referentialConstraint)
        {
            if (referentialConstraint == null)
                return;

            if (referentialConstraint.FromRole != null
                && referentialConstraint.FromProperties != null
                && referentialConstraint.FromProperties.Count > 0)
            {
                RefType fromType =
                    referentialConstraint.FromRole.TypeUsage.EdmType as RefType;

                if (fromType != null)
                {
                    FromDetails = new ForeignKeyDetail()
                    {
                        ContainerClass = fromType.ElementType.Name,
                        Keys =
                            referentialConstraint.FromProperties.Select(m => m.Name).ToList(),
                        RelationshipMultiplicity =
                            referentialConstraint.FromRole.RelationshipMultiplicity
                    };
                }

            }

            if (referentialConstraint.ToRole != null
                && referentialConstraint.ToProperties != null
                && referentialConstraint.ToProperties.Count > 0)
            {
                RefType toType =
                    referentialConstraint.ToRole.TypeUsage.EdmType as RefType;

                if (toType != null)
                {
                    ToDetails = new ForeignKeyDetail()
                    {
                        ContainerClass = toType.ElementType.Name,
                        Keys =
                            referentialConstraint.ToProperties.Select(m => m.Name).ToList(),
                        RelationshipMultiplicity =
                            referentialConstraint.ToRole.RelationshipMultiplicity
                    };
                }
            }
        }
    }
}
