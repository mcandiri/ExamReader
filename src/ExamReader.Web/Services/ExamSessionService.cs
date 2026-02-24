using ExamReader.Core.Analytics;
using ExamReader.Core.Grading;
using ExamReader.Core.Models;

namespace ExamReader.Web.Services;

public class ExamSessionService
{
    public ExamDefinition? CurrentExam { get; set; }
    public GradingOptions GradingOptions { get; set; } = new();
    public List<AnswerSheet> UploadedSheets { get; set; } = new();
    public List<GradingResult> GradingResults { get; set; } = new();
    public ExamAnalytics? Analytics { get; set; }
    public bool HasResults => GradingResults.Count > 0 && Analytics != null;
    public bool IsDemo { get; set; }

    public void Clear()
    {
        CurrentExam = null;
        GradingOptions = new GradingOptions();
        UploadedSheets.Clear();
        GradingResults.Clear();
        Analytics = null;
        IsDemo = false;
    }

    public void SetResults(
        ExamDefinition exam,
        GradingOptions options,
        List<GradingResult> results,
        ExamAnalytics analytics,
        bool isDemo = false)
    {
        CurrentExam = exam;
        GradingOptions = options;
        GradingResults = results;
        Analytics = analytics;
        IsDemo = isDemo;
    }

    public GradingResult? GetStudentResult(string studentId)
    {
        return GradingResults.FirstOrDefault(r => r.StudentId == studentId);
    }

    public StudentAnalytics? GetStudentAnalytics(string studentId)
    {
        return Analytics?.StudentStats.FirstOrDefault(s => s.StudentId == studentId);
    }
}
