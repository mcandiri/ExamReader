namespace ExamReader.Core.Grading;

public static class GradingSummary
{
    public static string GetLetterGrade(double percentage, LetterGradeScale scale)
    {
        return scale switch
        {
            LetterGradeScale.Standard => GetStandardGrade(percentage),
            LetterGradeScale.PlusMinus => GetPlusMinusGrade(percentage),
            LetterGradeScale.PassFail => percentage >= 60.0 ? "P" : "F",
            _ => GetStandardGrade(percentage)
        };
    }

    public static bool IsPassing(double percentage, double passingScore)
    {
        return percentage >= passingScore;
    }

    private static string GetStandardGrade(double percentage)
    {
        return percentage switch
        {
            >= 90 => "A",
            >= 80 => "B",
            >= 70 => "C",
            >= 60 => "D",
            _ => "F"
        };
    }

    private static string GetPlusMinusGrade(double percentage)
    {
        return percentage switch
        {
            >= 90 => "A",
            >= 85 => "A-",
            >= 80 => "B+",
            >= 75 => "B",
            >= 70 => "B-",
            >= 65 => "C+",
            >= 60 => "C",
            >= 50 => "D",
            _ => "F"
        };
    }
}
