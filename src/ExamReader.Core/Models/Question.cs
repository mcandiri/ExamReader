namespace ExamReader.Core.Models;

public class Question
{
    public int Number { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
    public List<string> Options { get; set; } = new() { "A", "B", "C", "D" };
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
}

public enum QuestionType
{
    MultipleChoice,
    MultiSelect,
    WrittenAnswer,
    TrueFalse
}
