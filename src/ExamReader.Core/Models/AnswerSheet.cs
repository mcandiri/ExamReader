namespace ExamReader.Core.Models;

public class AnswerSheet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public byte[]? ImageData { get; set; }
    public string? ImagePath { get; set; }
    public AnswerSheetTemplate? Template { get; set; }
    public List<StudentAnswer> ExtractedAnswers { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}
