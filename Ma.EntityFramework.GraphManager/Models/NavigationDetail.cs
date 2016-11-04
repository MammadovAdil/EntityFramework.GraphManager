using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;

namespace Ma.EntityFramework.GraphManager.Models
{
    public class NavigationDetail
    {        
        public NavigationDetail()
        {
            Relations = new List<NavigationRelation>();
        }

        /// <summary>
        /// Cosntruct NavigationDetail according to EntityType.
        /// </summary>
        /// <param name="entityType">EntityType to get navigation details.</param>
        public NavigationDetail(EntityType entityType)            
        {
            if (entityType == null)
                return;

            Relations = new List<NavigationRelation>();
            Initialize(entityType);            
        }

        public string SourceTypeName { get; set; }
        public List<NavigationRelation> Relations { get; set; }

        /// <summary>
        /// Initialize navigation details according entity type.
        /// </summary>
        /// <param name="entityType">EntityType to get navigation details.</param>
        private void Initialize(
            EntityType entityType)
        {
            if (entityType == null)
                return;

            SourceTypeName = entityType.Name;

            if (entityType.NavigationProperties != null
                && entityType.NavigationProperties.Count > 0)
            {
                foreach (var property in entityType.NavigationProperties)
                {
                    NavigationRelation relation = new NavigationRelation();
                    relation.PropertyName = property.Name;

                    if (property.FromEndMember != null)
                        relation.SourceMultiplicity =
                            property.FromEndMember.RelationshipMultiplicity;

                    if (property.ToEndMember != null)
                    {
                        relation.TargetMultiplicity =
                            property.ToEndMember.RelationshipMultiplicity;

                        AssociationType associationType = property.ToEndMember.DeclaringType as AssociationType;

                        if (associationType != null
                            && associationType.Constraint != null)
                        {

                            relation.FromKeyNames = associationType
                                .Constraint
                                .FromProperties
                                .Select(m => m.Name)
                                .ToList();

                            relation.ToKeyNames = associationType
                                .Constraint
                                .ToProperties
                                .Select(m => m.Name)
                                .ToList();

                            if (property.ToEndMember.Name == associationType.Constraint.ToRole.Name)
                                relation.Direction = NavigationDirection.To;
                            else if (property.ToEndMember.Name == associationType.Constraint.FromRole.Name)
                                relation.Direction = NavigationDirection.From;
                        }

                        RefType toRefType = property.ToEndMember.TypeUsage.EdmType as RefType;

                        if (toRefType != null)
                            relation.PropertyTypeName =
                                toRefType.ElementType.Name;
                    }

                    Relations.Add(relation);
                }
            }
        }
    }

    public class NavigationRelation
    {
        public string PropertyName { get; set; }        
        public string PropertyTypeName { get; set; }
        public List<string> FromKeyNames { get; set; }
        public List<string> ToKeyNames { get; set; }
        public RelationshipMultiplicity SourceMultiplicity { get; set; }
        public RelationshipMultiplicity TargetMultiplicity { get; set; }
        public NavigationDirection Direction { get; set; }
    }

    public enum NavigationDirection
    {
        NotSpecified = 0, From, To
    }
}
