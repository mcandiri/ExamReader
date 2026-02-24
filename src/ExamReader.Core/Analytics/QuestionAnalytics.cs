namespace ExamReader.Core.Analytics;

public class QuestionAnalytics
{
    public int QuestionNumber { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    public int UnansweredCount { get; set; }

    /// <summary>
    /// Percentage of students who answered correctly (0.0 - 1.0).
    /// Higher = easier question.
    /// </summary>
    public double DifficultyIndex { get; set; }

    /// <summary>
    /// Difference in correct rate between top 27% and bottom 27% of students.
    /// Higher values indicate better discrimination between strong and weak students.
    /// Values below 0.2 suggest the question may need review.
    /// </summary>
    public double DiscriminationIndex { get; set; }

    /// <summary>
    /// Distribution of selected answers (e.g., {"A": 5, "B": 12, "C": 3, "D": 5}).
    /// </summary>
    public Dictionary<string, int> AnswerDistribution { get; set; } = new();

    public string MostCommonWrongAnswer { get; set; } = string.Empty;

    /// <summary>
    /// Flagged when discrimination index is below 0.2 or difficulty is extreme (&lt;0.2 or &gt;0.95).
    /// </summary>
    public bool FlaggedForReview { get; set; }

    public string FlagReason { get; set; } = string.Empty;
}
