namespace ExamReader.Core.Models;

public class AnswerOption
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public double Confidence { get; set; }
}

public enum AnswerStatus
{
    Answered,
    Unanswered,
    MultipleMarks,
    Unclear
}
