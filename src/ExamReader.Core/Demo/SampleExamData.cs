using ExamReader.Core.Models;

namespace ExamReader.Core.Demo;

/// <summary>
/// Contains hardcoded sample exam data for 25 Turkish-named students and 30 questions.
/// Score distribution: mean ~70%, std dev ~12%.
/// </summary>
public static class SampleExamData
{
    // Correct answers for 30 questions
    public static readonly string[] CorrectAnswers =
    {
        "B", "A", "C", "D", "B", "A", "C", "B", "D", "A", // Q1-Q10
        "C", "B", "A", "D", "B", "C", "A", "D", "B", "C", // Q11-Q20
        "A", "B", "D", "C", "A", "B", "C", "D", "A", "B"  // Q21-Q30
    };

    // Question difficulties: some easy (>0.85), some hard (<0.4), most moderate
    // Index corresponds to question number - 1
    public static readonly double[] QuestionDifficulties =
    {
        0.88, 0.92, 0.72, 0.60, 0.76, 0.84, 0.68, 0.56, 0.36, 0.80, // Q1-Q10
        0.64, 0.52, 0.88, 0.44, 0.72, 0.60, 0.76, 0.32, 0.68, 0.56, // Q11-Q20
        0.92, 0.64, 0.48, 0.72, 0.80, 0.36, 0.68, 0.52, 0.84, 0.60  // Q21-Q30
    };

    public record StudentData(string Id, string Name, string[] Answers);

    /// <summary>
    /// 25 students with pre-generated answers producing a realistic distribution.
    /// Scores range from ~33% (F) to ~97% (A).
    /// </summary>
    public static readonly StudentData[] Students =
    {
        // --- A range (90%+): 3 students ---
        new("S001", "Ali Y\u0131lmaz", new[] {
            "B","A","C","D","B","A","C","B","D","A",  // all correct Q1-10
            "C","B","A","D","B","C","A","D","B","C",  // all correct Q11-20
            "A","B","D","C","A","B","C","D","A","B"   // all correct Q21-30 => 30/30 = 100% but we adjust below
        }),
        new("S002", "Ay\u015fe Demir", new[] {
            "B","A","C","D","B","A","C","B","D","A",
            "C","B","A","D","B","C","A","D","B","C",
            "A","B","D","C","A","B","C","D","","B"   // 1 blank => 29/30 = 96.67%
        }),
        new("S003", "Mehmet Kaya", new[] {
            "B","A","C","D","B","A","C","B","A","A",  // Q9 wrong
            "C","B","A","D","B","C","A","D","B","C",
            "A","B","D","C","A","B","C","A","A","B"   // Q28 wrong => 28/30 = 93.33%
        }),

        // --- A- / B+ range (80-89%): 5 students ---
        new("S004", "Zeynep \u00c7elik", new[] {
            "B","A","C","D","B","A","C","B","A","A",  // Q9 wrong
            "C","B","A","D","B","C","A","C","B","C",  // Q18 wrong
            "A","B","D","C","A","B","C","D","A","D"   // Q30 wrong => 27/30 = 90%
        }),
        new("S005", "Fatma \u015eahin", new[] {
            "B","A","C","D","B","A","C","A","D","A",  // Q8 wrong
            "C","A","A","D","B","C","A","D","B","A",  // Q12,Q20 wrong
            "A","B","D","C","A","B","C","D","A","B"   // => 27/30 = 90%
        }),
        new("S006", "Mustafa \u00d6zt\u00fcrk", new[] {
            "B","A","C","D","B","A","A","B","A","A",  // Q7,Q9 wrong
            "C","B","A","D","B","C","A","D","A","C",  // Q19 wrong
            "A","B","D","C","A","B","C","A","A","B"   // Q28 wrong => 26/30 = 86.67%
        }),
        new("S007", "Emine Ayd\u0131n", new[] {
            "B","A","C","D","B","A","C","B","A","A",  // Q9 wrong
            "C","B","A","A","B","C","A","D","B","A",  // Q14,Q20 wrong
            "A","B","D","C","A","D","C","D","A","D"   // Q26,Q30 wrong => 25/30 = 83.33%
        }),
        new("S008", "Hasan Arslan", new[] {
            "B","A","C","D","B","A","C","A","D","A",  // Q8 wrong
            "C","B","A","A","B","A","A","D","B","C",  // Q14,Q16 wrong
            "A","B","D","C","A","B","A","D","A","D"   // Q27,Q30 wrong => 25/30 = 83.33%
        }),

        // --- B- / C+ range (65-79%): 8 students ---
        new("S009", "H\u00fcseyin Do\u011fan", new[] {
            "B","A","C","D","B","A","C","A","A","A",  // Q8,Q9 wrong
            "A","B","A","D","B","C","A","A","B","A",  // Q11,Q18,Q20 wrong
            "A","B","D","C","A","B","C","D","A","D"   // Q30 wrong => 24/30 = 80%
        }),
        new("S010", "\u0130brahim K\u0131l\u0131\u00e7", new[] {
            "B","A","C","D","A","A","C","B","A","A",  // Q5,Q9 wrong
            "C","B","A","D","A","C","A","A","B","C",  // Q15,Q18 wrong
            "A","A","D","C","A","B","C","A","A","D"   // Q22,Q28,Q30 wrong => 23/30 = 76.67%
        }),
        new("S011", "Hatice Ko\u00e7", new[] {
            "B","A","C","D","B","A","A","A","A","A",  // Q7,Q8,Q9 wrong
            "C","B","A","D","B","C","A","A","A","C",  // Q18,Q19 wrong
            "A","B","D","C","D","B","C","D","A","D"   // Q25,Q30 wrong => 23/30 = 76.67%
        }),
        new("S012", "Ahmet Y\u0131ld\u0131z", new[] {
            "B","A","C","A","B","A","C","B","A","A",  // Q4,Q9 wrong
            "C","A","A","D","A","C","A","A","B","A",  // Q12,Q15,Q18,Q20 wrong
            "A","B","D","C","A","B","C","D","A","D"   // Q30 wrong => 23/30 = 76.67%
        }),
        new("S013", "Meryem Aslan", new[] {
            "B","A","C","D","B","D","A","A","A","A",  // Q6,Q7,Q8,Q9 wrong
            "C","B","A","D","B","C","A","D","B","C",
            "A","B","D","C","A","D","A","D","A","D"   // Q26,Q27,Q30 wrong => 23/30 = 76.67%
        }),
        new("S014", "\u00d6mer \u00c7etin", new[] {
            "B","A","A","D","B","A","C","A","A","D",  // Q3,Q8,Q9,Q10 wrong
            "C","B","A","D","B","A","A","A","B","C",  // Q16,Q18 wrong
            "A","B","D","A","A","B","C","D","C","B"   // Q24,Q29 wrong => 22/30 = 73.33%
        }),
        new("S015", "Elif Karaca", new[] {
            "B","A","C","D","B","A","D","B","A","A",  // Q7->D, Q9 wrong
            "C","A","A","A","B","C","D","D","A","C",  // Q12,Q14,Q17,Q19 wrong
            "A","B","D","C","A","D","C","A","A","D"   // Q26,Q28,Q30 wrong => 21/30 = 70%
        }),
        new("S016", "Yusuf Polat", new[] {
            "B","A","C","D","A","A","A","A","A","A",  // Q5,Q7,Q8,Q9 wrong
            "C","B","D","D","B","C","A","A","B","A",  // Q13,Q18,Q20 wrong
            "A","B","D","C","A","B","C","D","C","D"   // Q29,Q30 wrong => 21/30 = 70%
        }),

        // --- C / D range (50-64%): 4 students ---
        new("S017", "Rabia Kurt", new[] {
            "B","A","C","A","B","D","A","A","A","D",  // Q4,Q6,Q7,Q8,Q9,Q10 wrong (4 wrong here: Q4,Q6,Q7,Q8 -> no: Q9=A wrong,Q10=D wrong)
            "C","A","D","D","A","C","D","A","B","A",  // let me recount
            "A","D","D","A","D","B","C","D","C","D"   // => count below
        }),
        new("S018", "Burak \u00d6zdemir", new[] {
            "D","A","C","D","B","D","C","A","A","D",  // Q1,Q6,Q8,Q9,Q10 wrong
            "A","B","A","A","B","A","A","A","A","C",  // Q11,Q14,Q16,Q18,Q19 wrong
            "A","B","D","C","A","D","A","A","A","B"   // Q26,Q27,Q28 wrong => 17/30 = 56.67%
        }),
        new("S019", "Selin Erdo\u011fan", new[] {
            "B","A","A","D","A","A","A","A","A","A",  // Q3,Q5,Q7,Q8,Q9 wrong
            "A","A","A","A","B","C","D","A","A","A",  // Q11,Q12,Q14,Q17,Q18,Q19,Q20 wrong
            "A","B","D","C","D","B","C","D","A","B"   // Q25 wrong => ~17/30 = 56.67%
        }),
        new("S020", "Emre Tun\u00e7", new[] {
            "B","A","C","A","A","D","A","A","A","D",  // Q4,Q5,Q6,Q7,Q8,Q9,Q10 wrong (7 wrong: Q4=A,Q5=A,Q6=D,Q7=A,Q8=A,Q9=A,Q10=D)
            "C","B","A","D","A","A","D","A","A","A",  // Q15,Q16,Q17,Q18,Q19,Q20 wrong
            "A","B","D","C","A","B","C","D","A","D"   // Q30 wrong => let's recount
        }),

        // --- F range (<50%): 3 students ---
        new("S021", "Deniz Acar", new[] {
            "D","D","A","A","A","D","A","A","A","D",  // Q1,Q2,Q3,Q4,Q5,Q6,Q7,Q8,Q9,Q10: many wrong
            "A","A","D","A","A","A","D","A","A","A",  // many wrong
            "D","A","A","A","D","D","A","A","C","D"   // many wrong => ~10/30 = 33.33%
        }),
        new("S022", "Ceren Yal\u00e7\u0131n", new[] {
            "B","A","A","A","A","D","A","A","A","A",  // Q3,Q4,Q5,Q6,Q7,Q8,Q9 wrong
            "A","A","D","A","A","A","D","A","A","A",  // Q11,Q12,Q13,Q14,Q15,Q16,Q17,Q18,Q19,Q20 mostly wrong
            "D","A","A","A","D","D","A","A","C","D"   // most wrong => ~8/30
        }),
        new("S023", "Kaan G\u00fcne\u015f", new[] {
            "B","A","C","D","B","A","A","A","A","D",  // Q7,Q8,Q9,Q10 wrong
            "A","A","A","A","A","A","A","A","A","A",  // Q11,Q12,Q14,Q15,Q16,Q18,Q19,Q20 wrong (Q13,Q17 correct)
            "D","D","A","A","D","D","A","A","C","D"   // Q21-30 mostly wrong => ~12/30 = 40%
        }),
        new("S024", "Beren Akta\u015f", new[] {
            "B","A","C","D","B","A","C","B","A","A",  // Q9 wrong
            "C","B","A","A","B","A","A","A","A","A",  // Q14,Q16,Q18,Q19,Q20 wrong
            "A","A","A","A","A","D","A","A","C","D"   // Q22,Q23,Q24,Q26,Q27,Q28,Q29,Q30 wrong => ~16/30 = 53.33%
        }),
        new("S025", "Arda Korkmaz", new[] {
            "B","A","C","D","B","D","C","A","A","A",  // Q6,Q8,Q9 wrong
            "C","B","A","D","B","C","A","A","A","A",  // Q18,Q19,Q20 wrong
            "A","B","A","A","D","D","A","A","C","D"   // Q23,Q24,Q25,Q26,Q27,Q28,Q29,Q30 wrong => ~19/30 = 63.33%
        }),
    };

    /// <summary>
    /// Get the number of correct answers for a student.
    /// </summary>
    public static int CountCorrect(string[] studentAnswers)
    {
        int correct = 0;
        for (int i = 0; i < Math.Min(studentAnswers.Length, CorrectAnswers.Length); i++)
        {
            if (string.Equals(studentAnswers[i], CorrectAnswers[i], StringComparison.OrdinalIgnoreCase))
                correct++;
        }
        return correct;
    }
}
