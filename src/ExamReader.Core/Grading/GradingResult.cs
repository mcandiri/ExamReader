using ExamReader.Core.Models;

namespace ExamReader.Core.Grading;

public class GradingResult
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int Correct { get; set; }
    public int Incorrect { get; set; }
    public int Unanswered { get; set; }
    public double RawScore { get; set; }
    public double MaxScore { get; set; }
    public double Percentage { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public List<QuestionResult> QuestionResults { get; set; } = new();
}

public class QuestionResult
{
    public int QuestionNumber { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public string StudentAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public double PointsEarned { get; set; }
    public double PointsPossible { get; set; }
    public AnswerStatus Status { get; set; }
}
