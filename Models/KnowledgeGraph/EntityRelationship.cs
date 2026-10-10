using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedLinkPortal.Models.KnowledgeGraph
{
    public class EntityRelationship
    {
        [Key]
        public int Id { get; set; }

        public int SourceEntityId { get; set; }

        [ForeignKey("SourceEntityId")]
        public virtual MedicalEntity? SourceEntity { get; set; }

        public int TargetEntityId { get; set; }

        [ForeignKey("TargetEntityId")]
        public virtual MedicalEntity? TargetEntity { get; set; }

        // treated_by, managed_with, indicates, performed_by, co_occurs_with, interacts_with, associated_with
        [Required]
        [MaxLength(60)]
        public string RelationshipType { get; set; } = "indicates";

        public double Weight { get; set; } = 1.0;

        public int EvidenceCount { get; set; } = 1;

        public string PropertiesJson { get; set; } = "{}";

        public DateTime LastObservedAt { get; set; } = DateTime.UtcNow;
    }
}
