using System;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Models.KnowledgeGraph
{
    public class KnowledgeGraphSnapshot
    {
        [Key]
        public int Id { get; set; }

        public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;

        public int TotalEntities { get; set; }

        public int TotalRelationships { get; set; }

        public string InsightsJson { get; set; } = "[]";

        [MaxLength(500)]
        public string Summary { get; set; } = string.Empty;
    }
}
