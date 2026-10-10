using System.Collections.Generic;
using System.Threading.Tasks;
using MedLinkPortal.Models.KnowledgeGraph;

namespace MedLinkPortal.Services
{
    public class GraphVisualizationData
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphLink> Links { get; set; } = new();
    }

    public class GraphNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Code { get; set; }
    }

    public class GraphLink
    {
        public int Source { get; set; }
        public int Target { get; set; }
        public string Type { get; set; } = string.Empty;
        public double Weight { get; set; }
    }

    public interface IKnowledgeGraphService
    {
        Task IngestCaseAsync(AnonymizedCaseData caseData);
        Task<GraphVisualizationData> GetGraphDataAsync();
        Task<List<EntityRelationship>> QueryRelationshipsAsync(string entityName);
        Task<List<string>> GetInsightsAsync();
        Task<List<AnonymizedCaseData>> ExportDatasetAsync(int limit = 200);
        Task SeedInitialGraphAsync();
    }
}
