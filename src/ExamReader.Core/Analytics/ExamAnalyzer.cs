using ExamReader.Core.Grading;
using ExamReader.Core.Models;
using Microsoft.Extensions.Logging;

namespace ExamReader.Core.Analytics;

public class ExamAnalyzer : IExamAnalyzer
{
    private readonly ILogger<ExamAnalyzer> _logger;

    public ExamAnalyzer(ILogger<ExamAnalyzer> logger)
    {
        _logger = logger;
    }

    public ExamAnalytics Analyze(List<GradingResult> results, AnswerKey answerKey)
    {
        if (results.Count == 0)
        {
            return new ExamAnalytics
            {
                ExamId = answerKey.ExamId,
                ExamTitle = answerKey.ExamTitle
            };
        }

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

        // Grade distribution
        analytics.GradeDistribution = results
            .GroupBy(r => r.LetterGrade)
            .ToDictionary(g => g.Key, g => g.Count());

        // Question analytics
        analytics.QuestionStats = AnalyzeQuestions(results, answerKey);

        // Student analytics
        analytics.StudentStats = AnalyzeStudents(results, analytics.ClassAverage, analytics.StandardDeviation);

        _logger.LogInformation(
            "Exam analysis complete: {Students} students, avg {Avg}%, pass rate {PassRate}%",
            analytics.TotalStudents, analytics.ClassAverage, analytics.PassRate);

        return analytics;
    }

    public Task<ExamAnalytics> AnalyzeAsync(
        List<GradingResult> results,
        AnswerKey answerKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var analytics = Analyze(results, answerKey);
        return Task.FromResult(analytics);
    }

    private List<QuestionAnalytics> AnalyzeQuestions(List<GradingResult> results, AnswerKey answerKey)
    {
        var questionStats = new List<QuestionAnalytics>();

        // Sort students by percentage for discrimination index calculation
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

            // Difficulty Index: proportion of students who answered correctly
            qa.DifficultyIndex = qa.TotalAttempts > 0
                ? Math.Round((double)qa.CorrectCount / qa.TotalAttempts, 3)
                : 0;

            // Answer distribution
            qa.AnswerDistribution = questionResults
                .Where(qr => !string.IsNullOrEmpty(qr.StudentAnswer))
                .GroupBy(qr => qr.StudentAnswer.ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.Count());

            // Most common wrong answer
            var wrongAnswers = questionResults
                .Where(qr => !qr.IsCorrect && qr.Status == AnswerStatus.Answered && !string.IsNullOrEmpty(qr.StudentAnswer))
                .GroupBy(qr => qr.StudentAnswer.ToUpperInvariant())
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            qa.MostCommonWrongAnswer = wrongAnswers?.Key ?? string.Empty;

            // Discrimination Index: top 27% correct rate - bottom 27% correct rate
            double topCorrectRate = CalculateGroupCorrectRate(topGroup, question.Number);
            double bottomCorrectRate = CalculateGroupCorrectRate(bottomGroup, question.Number);
            qa.DiscriminationIndex = Math.Round(topCorrectRate - bottomCorrectRate, 3);

            // Flag for review
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

            questionStats.Add(qa);
        }

        return questionStats;
    }

    private static double CalculateGroupCorrectRate(List<GradingResult> group, int questionNumber)
    {
        if (group.Count == 0) return 0;

        int correct = group
            .SelectMany(r => r.QuestionResults)
            .Count(qr => qr.QuestionNumber == questionNumber && qr.IsCorrect);

        return (double)correct / group.Count;
    }

    private List<StudentAnalytics> AnalyzeStudents(
        List<GradingResult> results,
        double classAverage,
        double standardDeviation)
    {
        var ranked = results
            .OrderByDescending(r => r.Percentage)
            .ThenBy(r => r.StudentName)
            .ToList();

        var studentStats = new List<StudentAnalytics>();

        for (int i = 0; i < ranked.Count; i++)
        {
            var r = ranked[i];
            int belowCount = results.Count(other => other.Percentage < r.Percentage);
            double percentile = results.Count > 1
                ? Math.Round((double)belowCount / (results.Count - 1) * 100, 1)
                : 100;

            double zScore = standardDeviation > 0
                ? Math.Round((r.Percentage - classAverage) / standardDeviation, 2)
                : 0;

            studentStats.Add(new StudentAnalytics
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

        return studentStats;
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
