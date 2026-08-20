using System;

namespace MedLinkPortal.Models
{
    public class AIAnalysis
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = "Diagnostic";
        public string Status { get; set; } = "Processing"; // Normal, Action Needed, Critical
        public string AnalysisResult { get; set; } = string.Empty;
        
        // Added for mobile dashboard alignment
        public string? ReportTitle { get; set; }
        public string? ReportContent { get; set; }
        public string? Sentiment { get; set; }
        public int Score { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
