namespace ExamReader.Core.Models;

public class StudentAnswer
{
    public int QuestionNumber { get; set; }
    public string SelectedAnswer { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public AnswerStatus Status { get; set; } = AnswerStatus.Answered;
}
