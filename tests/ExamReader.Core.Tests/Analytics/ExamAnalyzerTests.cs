using ExamReader.Core.Analytics;
using ExamReader.Core.Grading;
using ExamReader.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Analytics;

public class ExamAnalyzerTests
{
    private readonly ExamAnalyzer _analyzer;
    private readonly GradingEngine _gradingEngine;

    public ExamAnalyzerTests()
    {
        _analyzer = new ExamAnalyzer(NullLogger<ExamAnalyzer>.Instance);
        _gradingEngine = new GradingEngine(NullLogger<GradingEngine>.Instance);
    }

    private static AnswerKey CreateAnswerKey(int count = 10, string correctAnswer = "A")
    {
        return new AnswerKey
        {
            ExamId = "TEST",
            ExamTitle = "Test Exam",
            Questions = Enumerable.Range(1, count).Select(i => new Question
            {
                Number = i,
                CorrectAnswer = correctAnswer,
                Weight = 1.0
            }).ToList()
        };
    }

    private List<GradingResult> CreateGradedResults(int[] correctCounts, int totalQuestions = 10)
    {
        var answerKey = CreateAnswerKey(totalQuestions);
        var options = new GradingOptions();

        return correctCounts.Select((correct, idx) =>
        {
            var answers = Enumerable.Range(1, totalQuestions).Select(i => new StudentAnswer
            {
                QuestionNumber = i,
                SelectedAnswer = i <= correct ? "A" : "B",
                Status = AnswerStatus.Answered
            }).ToList();

            var result = _gradingEngine.GradeStudent(answers, answerKey, options);
            result.StudentId = $"S{idx + 1:D3}";
            result.StudentName = $"Student {idx + 1}";
            return result;
        }).ToList();
    }

    [Fact]
    public void Analyze_ClassAverage_ShouldBeCorrect()
    {
        // 3 students: 100%, 50%, 70% -> avg = 73.33%
        var results = CreateGradedResults(new[] { 10, 5, 7 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.ClassAverage.Should().BeApproximately(73.33, 0.01);
    }

    [Fact]
    public void Analyze_Median_OddCount_ShouldBeMiddleValue()
    {
        // 3 students: 50%, 70%, 100% -> median = 70%
        var results = CreateGradedResults(new[] { 10, 5, 7 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.Median.Should().Be(70.0);
    }

    [Fact]
    public void Analyze_Median_EvenCount_ShouldBeAverageOfMiddleTwo()
    {
        // 4 students: 40%, 60%, 80%, 100% -> median = (60+80)/2 = 70%
        var results = CreateGradedResults(new[] { 10, 8, 6, 4 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.Median.Should().Be(70.0);
    }

    [Fact]
    public void Analyze_StandardDeviation_ShouldBeNonNegative()
    {
        var results = CreateGradedResults(new[] { 10, 5, 7, 8, 3 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.StandardDeviation.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Analyze_StandardDeviation_AllSameScore_ShouldBeZero()
    {
        var results = CreateGradedResults(new[] { 7, 7, 7, 7 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.StandardDeviation.Should().Be(0);
    }

    [Fact]
    public void Analyze_PassRate_ShouldBeCorrect()
    {
        // 4 students: 100%, 80%, 50%, 30% -> 2 pass (>=60%), 2 fail -> 50% pass rate
        var results = CreateGradedResults(new[] { 10, 8, 5, 3 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.PassCount.Should().Be(2);
        analytics.FailCount.Should().Be(2);
        analytics.PassRate.Should().Be(50.0);
    }

    [Fact]
    public void Analyze_HighestScore_ShouldBeMax()
    {
        var results = CreateGradedResults(new[] { 10, 5, 7 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.HighestScore.Should().Be(100.0);
    }

    [Fact]
    public void Analyze_LowestScore_ShouldBeMin()
    {
        var results = CreateGradedResults(new[] { 10, 5, 7 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.LowestScore.Should().Be(50.0);
    }

    [Fact]
    public void Analyze_TotalStudents_ShouldMatchInputCount()
    {
        var results = CreateGradedResults(new[] { 10, 8, 6, 4, 2 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.TotalStudents.Should().Be(5);
    }

    [Fact]
    public void Analyze_GradeDistribution_ShouldContainAllGrades()
    {
        var results = CreateGradedResults(new[] { 10, 8, 7, 6, 3 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.GradeDistribution.Should().NotBeEmpty();
        analytics.GradeDistribution.Values.Sum().Should().Be(5);
    }

    [Fact]
    public void Analyze_QuestionStats_ShouldHaveOnePerQuestion()
    {
        var results = CreateGradedResults(new[] { 10, 5, 7 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.QuestionStats.Should().HaveCount(10);
    }

    [Fact]
    public void Analyze_StudentStats_ShouldHaveOnePerStudent()
    {
        var results = CreateGradedResults(new[] { 10, 5, 7 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.StudentStats.Should().HaveCount(3);
    }

    [Fact]
    public void Analyze_ScoreDistribution_ShouldHave10Buckets()
    {
        var results = CreateGradedResults(new[] { 10, 5, 7 });
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.Distribution.Buckets.Should().HaveCount(10);
    }

    [Fact]
    public void Analyze_WithDemoData_25Students_ShouldProduceAnalytics()
    {
        // Create 25 students with varying scores to simulate demo data
        var correctCounts = new[]
        {
            10, 9, 9, 8, 8, 8, 7, 7, 7, 7,
            6, 6, 6, 6, 5, 5, 5, 5, 4, 4,
            4, 3, 3, 2, 1
        };

        var results = CreateGradedResults(correctCounts);
        var answerKey = CreateAnswerKey();

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.TotalStudents.Should().Be(25);
        analytics.ClassAverage.Should().BeGreaterThan(0);
        analytics.ClassAverage.Should().BeLessThanOrEqualTo(100);
        analytics.StandardDeviation.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Analyze_EmptyResults_ShouldReturnBasicAnalytics()
    {
        var answerKey = CreateAnswerKey();
        var analytics = _analyzer.Analyze(new List<GradingResult>(), answerKey);

        analytics.TotalStudents.Should().Be(0);
        analytics.ClassAverage.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldProduceSameResult()
    {
        var results = CreateGradedResults(new[] { 10, 5, 7 });
        var answerKey = CreateAnswerKey();

        var syncResult = _analyzer.Analyze(results, answerKey);
        var asyncResult = await _analyzer.AnalyzeAsync(results, answerKey);

        asyncResult.ClassAverage.Should().Be(syncResult.ClassAverage);
        asyncResult.Median.Should().Be(syncResult.Median);
        asyncResult.TotalStudents.Should().Be(syncResult.TotalStudents);
    }
}
