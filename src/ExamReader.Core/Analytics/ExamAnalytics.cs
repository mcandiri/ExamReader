namespace ExamReader.Core.Analytics;

public class ExamAnalytics
{
    public string ExamId { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    // Class-level statistics
    public int TotalStudents { get; set; }
    public double ClassAverage { get; set; }
    public double Median { get; set; }
    public double StandardDeviation { get; set; }
    public double HighestScore { get; set; }
    public double LowestScore { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public double PassRate { get; set; }

    // Grade distribution
    public Dictionary<string, int> GradeDistribution { get; set; } = new();

    // Score distribution histogram
    public ScoreDistribution Distribution { get; set; } = new();

    // Per-question analytics
    public List<QuestionAnalytics> QuestionStats { get; set; } = new();

    // Per-student analytics
    public List<StudentAnalytics> StudentStats { get; set; } = new();
}
