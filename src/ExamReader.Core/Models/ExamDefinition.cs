namespace ExamReader.Core.Models;

public class ExamDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public DateTime ExamDate { get; set; } = DateTime.Now;
    public int TotalQuestions { get; set; }
    public ExamFormat Format { get; set; } = ExamFormat.BubbleSheet;
    public AnswerKey AnswerKey { get; set; } = new();
    public AnswerSheetTemplate Template { get; set; } = new();
}

public enum ExamFormat
{
    BubbleSheet,
    GridBased,
    WrittenAnswer,
    Mixed
}
