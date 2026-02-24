using ExamReader.Core.Analytics;
using ExamReader.Core.Grading;
using ExamReader.Core.Models;

namespace ExamReader.Core.Reports;

public class ReportData
{
    public string ReportTitle { get; set; } = "Exam Report";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public AnswerKey AnswerKey { get; set; } = new();
    public List<GradingResult> Results { get; set; } = new();
    public ExamAnalytics Analytics { get; set; } = new();
}
