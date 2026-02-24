namespace ExamReader.Core.Models;

public class AnswerSheetTemplate
{
    public int TotalQuestions { get; set; } = 30;
    public int Columns { get; set; } = 1;
    public int QuestionsPerColumn { get; set; } = 30;
    public List<string> AnswerOptions { get; set; } = new() { "A", "B", "C", "D" };
    public ExamFormat Format { get; set; } = ExamFormat.BubbleSheet;
    public bool HasStudentIdField { get; set; } = true;
    public bool HasStudentNameField { get; set; } = true;
}
