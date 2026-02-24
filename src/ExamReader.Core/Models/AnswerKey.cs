namespace ExamReader.Core.Models;

public class AnswerKey
{
    public string ExamId { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public List<Question> Questions { get; set; } = new();
    public int TotalQuestions => Questions.Count;
}
