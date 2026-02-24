using ExamReader.Core.Analytics;
using ExamReader.Core.Grading;
using ExamReader.Core.Models;

namespace ExamReader.Core.Demo;

public class DemoDataProvider
{
    public ExamDefinition GetSampleExam()
    {
        var answerKey = GetSampleAnswerKey();
        return new ExamDefinition
        {
            Id = "DEMO-EXAM-001",
            Title = "2024-2025 Matematik Final S\u0131nav\u0131",
            ExamDate = new DateTime(2025, 1, 15),
            TotalQuestions = 30,
            Format = ExamFormat.BubbleSheet,
            AnswerKey = answerKey,
            Template = new AnswerSheetTemplate()
        };
    }

    public AnswerKey GetSampleAnswerKey()
    {
        var questions = new List<Question>();
        for (int i = 0; i < 30; i++)
        {
            questions.Add(new Question
            {
                Number = i + 1,
                CorrectAnswer = SampleExamData.CorrectAnswers[i],
                Weight = 1.0,
                Options = new List<string> { "A", "B", "C", "D" },
                Type = QuestionType.MultipleChoice
            });
        }

        return new AnswerKey
        {
            ExamId = "DEMO-EXAM-001",
            ExamTitle = "2024-2025 Matematik Final S\u0131nav\u0131",
            Questions = questions
        };
    }

    public List<AnswerSheet> GetSampleAnswerSheets()
    {
        var sheets = new List<AnswerSheet>();

        foreach (var student in SampleExamData.Students)
        {
            var answers = new List<StudentAnswer>();
            for (int i = 0; i < student.Answers.Length; i++)
            {
                var answer = student.Answers[i];
                answers.Add(new StudentAnswer
                {
                    QuestionNumber = i + 1,
                    SelectedAnswer = answer,
                    Confidence = string.IsNullOrEmpty(answer) ? 0 : 0.95,
                    Status = string.IsNullOrEmpty(answer) ? AnswerStatus.Unanswered : AnswerStatus.Answered
                });
            }

            sheets.Add(new AnswerSheet
            {
                Id = student.Id,
                StudentId = student.Id,
                StudentName = student.Name,
                ExtractedAnswers = answers,
                ProcessedAt = DateTime.UtcNow
            });
        }

        return sheets;
    }

    public List<GradingResult> GetSampleResults()
    {
        var answerKey = GetSampleAnswerKey();
        var sheets = GetSampleAnswerSheets();
        var options = new GradingOptions
        {
            NegativeMarking = false,
            WeightedQuestions = false,
            PassingScore = 60.0,
            GradeScale = LetterGradeScale.PlusMinus
        };

        var results = new List<GradingResult>();
        foreach (var sheet in sheets)
        {
            var result = GradeStudentInternal(sheet.ExtractedAnswers, answerKey, options);
            result.StudentId = sheet.StudentId;
            result.StudentName = sheet.StudentName;
            results.Add(result);
        }

        return results;
    }

    public ExamAnalytics GetSampleAnalytics()
    {
        var results = GetSampleResults();
        var answerKey = GetSampleAnswerKey();

        if (results.Count == 0)
            return new ExamAnalytics();

        var percentages = results.Select(r => r.Percentage).OrderBy(p => p).ToList();

        var analytics = new ExamAnalytics
        {
            ExamId = answerKey.ExamId,
            ExamTitle = answerKey.ExamTitle,
            AnalyzedAt = DateTime.UtcNow,
            TotalStudents = results.Count,
            ClassAverage = Math.Round(percentages.Average(), 2),
            Median = Math.Round(CalculateMedian(percentages), 2),
            StandardDeviation = Math.Round(CalculateStdDev(percentages), 2),
            HighestScore = percentages.Last(),
            LowestScore = percentages.First(),
            PassCount = results.Count(r => r.Passed),
            FailCount = results.Count(r => !r.Passed),
            Distribution = ScoreDistribution.FromPercentages(percentages)
        };

        analytics.PassRate = Math.Round((double)analytics.PassCount / analytics.TotalStudents * 100, 2);

        analytics.GradeDistribution = results
            .GroupBy(r => r.LetterGrade)
            .ToDictionary(g => g.Key, g => g.Count());

        // Question analytics
        analytics.QuestionStats = AnalyzeQuestions(results, answerKey);

        // Student analytics
        var ranked = results.OrderByDescending(r => r.Percentage).ThenBy(r => r.StudentName).ToList();
        analytics.StudentStats = new List<StudentAnalytics>();
        for (int i = 0; i < ranked.Count; i++)
        {
            var r = ranked[i];
            int belowCount = results.Count(other => other.Percentage < r.Percentage);
            double percentile = results.Count > 1
                ? Math.Round((double)belowCount / (results.Count - 1) * 100, 1)
                : 100;
            double zScore = analytics.StandardDeviation > 0
                ? Math.Round((r.Percentage - analytics.ClassAverage) / analytics.StandardDeviation, 2)
                : 0;

            analytics.StudentStats.Add(new StudentAnalytics
            {
                StudentId = r.StudentId,
                StudentName = r.StudentName,
                Rank = i + 1,
                Percentage = r.Percentage,
                LetterGrade = r.LetterGrade,
                Passed = r.Passed,
                Correct = r.Correct,
                Incorrect = r.Incorrect,
                Unanswered = r.Unanswered,
                RawScore = r.RawScore,
                MaxScore = r.MaxScore,
                ZScore = zScore,
                Percentile = percentile
            });
        }

        return analytics;
    }

    private static GradingResult GradeStudentInternal(
        List<StudentAnswer> studentAnswers,
        AnswerKey answerKey,
        GradingOptions options)
    {
        var result = new GradingResult
        {
            TotalQuestions = answerKey.TotalQuestions,
            MaxScore = answerKey.TotalQuestions
        };

        var answerLookup = studentAnswers.ToDictionary(a => a.QuestionNumber, a => a);

        foreach (var question in answerKey.Questions)
        {
            double pointsPossible = options.WeightedQuestions ? question.Weight : 1.0;

            if (!answerLookup.TryGetValue(question.Number, out var studentAnswer) ||
                studentAnswer.Status == AnswerStatus.Unanswered ||
                string.IsNullOrEmpty(studentAnswer.SelectedAnswer))
            {
                result.QuestionResults.Add(new QuestionResult
                {
                    QuestionNumber = question.Number,
                    CorrectAnswer = question.CorrectAnswer,
                    StudentAnswer = string.Empty,
                    IsCorrect = false,
                    PointsEarned = 0,
                    PointsPossible = pointsPossible,
                    Status = AnswerStatus.Unanswered
                });
                result.Unanswered++;
                continue;
            }

            bool isCorrect = string.Equals(
                studentAnswer.SelectedAnswer.Trim(),
                question.CorrectAnswer.Trim(),
                StringComparison.OrdinalIgnoreCase);

            double pointsEarned = isCorrect ? pointsPossible : 0;

            result.QuestionResults.Add(new QuestionResult
            {
                QuestionNumber = question.Number,
                CorrectAnswer = question.CorrectAnswer,
                StudentAnswer = studentAnswer.SelectedAnswer,
                IsCorrect = isCorrect,
                PointsEarned = pointsEarned,
                PointsPossible = pointsPossible,
                Status = AnswerStatus.Answered
            });

            if (isCorrect) result.Correct++;
            else result.Incorrect++;

            result.RawScore += pointsEarned;
        }

        result.Percentage = result.MaxScore > 0
            ? Math.Round(result.RawScore / result.MaxScore * 100, 2)
            : 0;
        result.LetterGrade = GradingSummary.GetLetterGrade(result.Percentage, options.GradeScale);
        result.Passed = GradingSummary.IsPassing(result.Percentage, options.PassingScore);

        return result;
    }

    private List<QuestionAnalytics> AnalyzeQuestions(List<GradingResult> results, AnswerKey answerKey)
    {
        var stats = new List<QuestionAnalytics>();
        var sortedResults = results.OrderByDescending(r => r.Percentage).ToList();
        int groupSize = Math.Max(1, (int)Math.Ceiling(results.Count * 0.27));
        var topGroup = sortedResults.Take(groupSize).ToList();
        var bottomGroup = sortedResults.TakeLast(groupSize).ToList();

        foreach (var question in answerKey.Questions)
        {
            var qa = new QuestionAnalytics
            {
                QuestionNumber = question.Number,
                CorrectAnswer = question.CorrectAnswer
            };

            var questionResults = results
                .SelectMany(r => r.QuestionResults)
                .Where(qr => qr.QuestionNumber == question.Number)
                .ToList();

            qa.TotalAttempts = questionResults.Count;
            qa.CorrectCount = questionResults.Count(qr => qr.IsCorrect);
            qa.IncorrectCount = questionResults.Count(qr => !qr.IsCorrect && qr.Status == AnswerStatus.Answered);
            qa.UnansweredCount = questionResults.Count(qr => qr.Status == AnswerStatus.Unanswered);

            qa.DifficultyIndex = qa.TotalAttempts > 0
                ? Math.Round((double)qa.CorrectCount / qa.TotalAttempts, 3)
                : 0;

            qa.AnswerDistribution = questionResults
                .Where(qr => !string.IsNullOrEmpty(qr.StudentAnswer))
                .GroupBy(qr => qr.StudentAnswer.ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.Count());

            var wrongAnswers = questionResults
                .Where(qr => !qr.IsCorrect && qr.Status == AnswerStatus.Answered && !string.IsNullOrEmpty(qr.StudentAnswer))
                .GroupBy(qr => qr.StudentAnswer.ToUpperInvariant())
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            qa.MostCommonWrongAnswer = wrongAnswers?.Key ?? string.Empty;

            double topCorrectRate = topGroup.Count > 0
                ? (double)topGroup.SelectMany(r => r.QuestionResults)
                    .Count(qr => qr.QuestionNumber == question.Number && qr.IsCorrect) / topGroup.Count
                : 0;
            double bottomCorrectRate = bottomGroup.Count > 0
                ? (double)bottomGroup.SelectMany(r => r.QuestionResults)
                    .Count(qr => qr.QuestionNumber == question.Number && qr.IsCorrect) / bottomGroup.Count
                : 0;
            qa.DiscriminationIndex = Math.Round(topCorrectRate - bottomCorrectRate, 3);

            if (qa.DiscriminationIndex < 0.2)
            {
                qa.FlaggedForReview = true;
                qa.FlagReason = $"Low discrimination index ({qa.DiscriminationIndex:F2})";
            }
            else if (qa.DifficultyIndex < 0.2)
            {
                qa.FlaggedForReview = true;
                qa.FlagReason = $"Too difficult (only {qa.DifficultyIndex:P0} correct)";
            }
            else if (qa.DifficultyIndex > 0.95)
            {
                qa.FlaggedForReview = true;
                qa.FlagReason = $"Too easy ({qa.DifficultyIndex:P0} correct)";
            }

            stats.Add(qa);
        }

        return stats;
    }

    private static double CalculateMedian(List<double> sorted)
    {
        int count = sorted.Count;
        if (count == 0) return 0;
        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        return sorted[count / 2];
    }

    private static double CalculateStdDev(List<double> values)
    {
        if (values.Count <= 1) return 0;
        double avg = values.Average();
        double sumSquares = values.Sum(v => (v - avg) * (v - avg));
        return Math.Sqrt(sumSquares / (values.Count - 1));
    }
}
