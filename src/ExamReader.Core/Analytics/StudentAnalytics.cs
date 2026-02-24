namespace ExamReader.Core.Analytics;

public class StudentAnalytics
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public double Percentage { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public int Correct { get; set; }
    public int Incorrect { get; set; }
    public int Unanswered { get; set; }
    public double RawScore { get; set; }
    public double MaxScore { get; set; }

    /// <summary>
    /// Z-score: number of standard deviations from the mean.
    /// </summary>
    public double ZScore { get; set; }

    /// <summary>
    /// Percentile rank (0-100): percentage of students scored below this student.
    /// </summary>
    public double Percentile { get; set; }
}
