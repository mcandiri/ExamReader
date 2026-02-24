namespace ExamReader.Core.Grading;

public class GradingOptions
{
    public bool NegativeMarking { get; set; } = false;
    public double NegativePenalty { get; set; } = 0.25;
    public bool PartialCredit { get; set; } = false;
    public bool WeightedQuestions { get; set; } = false;
    public double PassingScore { get; set; } = 60.0;
    public LetterGradeScale GradeScale { get; set; } = LetterGradeScale.Standard;
}

public enum LetterGradeScale
{
    Standard,
    PlusMinus,
    PassFail
}
