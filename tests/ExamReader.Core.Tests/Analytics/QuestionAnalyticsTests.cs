using ExamReader.Core.Analytics;
using ExamReader.Core.Grading;
using ExamReader.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Analytics;

public class QuestionAnalyticsTests
{
    private readonly ExamAnalyzer _analyzer;
    private readonly GradingEngine _gradingEngine;

    public QuestionAnalyticsTests()
    {
        _analyzer = new ExamAnalyzer(NullLogger<ExamAnalyzer>.Instance);
        _gradingEngine = new GradingEngine(NullLogger<GradingEngine>.Instance);
    }

    private static AnswerKey CreateAnswerKey(int count = 5)
    {
        return new AnswerKey
        {
            ExamId = "TEST",
            ExamTitle = "Test Exam",
            Questions = Enumerable.Range(1, count).Select(i => new Question
            {
                Number = i,
                CorrectAnswer = "A",
                Weight = 1.0
            }).ToList()
        };
    }

    private List<GradingResult> GradeStudents(AnswerKey answerKey, List<List<string>> studentAnswerSets)
    {
        var options = new GradingOptions();
        return studentAnswerSets.Select((answers, idx) =>
        {
            var studentAnswers = answers.Select((a, i) => new StudentAnswer
            {
                QuestionNumber = i + 1,
                SelectedAnswer = a,
                Status = string.IsNullOrEmpty(a) ? AnswerStatus.Unanswered : AnswerStatus.Answered
            }).ToList();

            var result = _gradingEngine.GradeStudent(studentAnswers, answerKey, options);
            result.StudentId = $"S{idx + 1}";
            result.StudentName = $"Student {idx + 1}";
            return result;
        }).ToList();
    }

    [Fact]
    public void DifficultyIndex_AllCorrect_ShouldBe1()
    {
        var answerKey = CreateAnswerKey(3);
        var results = GradeStudents(answerKey, new()
        {
            new() { "A", "A", "A" },
            new() { "A", "A", "A" },
            new() { "A", "A", "A" }
        });

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.QuestionStats[0].DifficultyIndex.Should().Be(1.0);
    }

    [Fact]
    public void DifficultyIndex_NoneCorrect_ShouldBe0()
    {
        var answerKey = CreateAnswerKey(3);
        var results = GradeStudents(answerKey, new()
        {
            new() { "B", "A", "A" },
            new() { "C", "A", "A" },
            new() { "D", "A", "A" }
        });

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.QuestionStats[0].DifficultyIndex.Should().Be(0);
    }

    [Fact]
    public void DifficultyIndex_HalfCorrect_ShouldBeApprox05()
    {
        var answerKey = CreateAnswerKey(3);
        var results = GradeStudents(answerKey, new()
        {
            new() { "A", "A", "A" },
            new() { "B", "A", "A" },
            new() { "A", "A", "A" },
            new() { "B", "A", "A" }
        });

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.QuestionStats[0].DifficultyIndex.Should().Be(0.5);
    }

    [Fact]
    public void DiscriminationIndex_ShouldBeCalculated()
    {
        var answerKey = CreateAnswerKey(5);
        // Create diverse scores so top/bottom groups differ
        var results = GradeStudents(answerKey, new()
        {
            new() { "A", "A", "A", "A", "A" }, // 100%
            new() { "A", "A", "A", "A", "B" }, // 80%
            new() { "A", "A", "A", "B", "B" }, // 60%
            new() { "A", "A", "B", "B", "B" }, // 40%
            new() { "A", "B", "B", "B", "B" }, // 20%
            new() { "B", "B", "B", "B", "B" }, // 0%
            new() { "A", "A", "A", "A", "A" }, // 100%
            new() { "B", "B", "B", "B", "B" }, // 0%
        });

        var analytics = _analyzer.Analyze(results, answerKey);

        // Question 1: top group all get it right, bottom group not all -> positive discrimination
        analytics.QuestionStats.Should().AllSatisfy(qs =>
        {
            qs.DiscriminationIndex.Should().BeInRange(-1.0, 1.0);
        });
    }

    [Fact]
    public void AnswerDistribution_ShouldContainAllSelectedAnswers()
    {
        var answerKey = CreateAnswerKey(3);
        var results = GradeStudents(answerKey, new()
        {
            new() { "A", "A", "A" },
            new() { "B", "A", "A" },
            new() { "C", "A", "A" },
            new() { "D", "A", "A" }
        });

        var analytics = _analyzer.Analyze(results, answerKey);

        var q1dist = analytics.QuestionStats[0].AnswerDistribution;
        q1dist.Should().ContainKey("A");
        q1dist.Should().ContainKey("B");
        q1dist.Should().ContainKey("C");
        q1dist.Should().ContainKey("D");
        q1dist["A"].Should().Be(1);
        q1dist["B"].Should().Be(1);
        q1dist["C"].Should().Be(1);
        q1dist["D"].Should().Be(1);
    }

    [Fact]
    public void MostCommonWrongAnswer_ShouldBeIdentified()
    {
        var answerKey = CreateAnswerKey(3);
        var results = GradeStudents(answerKey, new()
        {
            new() { "A", "A", "A" }, // correct
            new() { "B", "A", "A" }, // wrong: B
            new() { "B", "A", "A" }, // wrong: B
            new() { "C", "A", "A" }  // wrong: C
        });

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.QuestionStats[0].MostCommonWrongAnswer.Should().Be("B");
    }

    [Fact]
    public void FlaggedForReview_LowDiscrimination_ShouldBeTrue()
    {
        var answerKey = CreateAnswerKey(3);
        // All students get Q1 right -> discrimination = 0 (top and bottom both get it right)
        var results = GradeStudents(answerKey, new()
        {
            new() { "A", "A", "A" },
            new() { "A", "B", "B" },
            new() { "A", "A", "B" },
            new() { "A", "B", "A" }
        });

        var analytics = _analyzer.Analyze(results, answerKey);

        // Q1 difficulty is 1.0 (all correct) -> should be flagged as too easy
        var q1 = analytics.QuestionStats[0];
        q1.FlaggedForReview.Should().BeTrue();
    }

    [Fact]
    public void QuestionAnalytics_ShouldTrackCorrectAndIncorrectCounts()
    {
        var answerKey = CreateAnswerKey(3);
        var results = GradeStudents(answerKey, new()
        {
            new() { "A", "A", "A" }, // Q1 correct
            new() { "B", "A", "A" }, // Q1 wrong
            new() { "A", "A", "A" }  // Q1 correct
        });

        var analytics = _analyzer.Analyze(results, answerKey);

        var q1 = analytics.QuestionStats[0];
        q1.CorrectCount.Should().Be(2);
        q1.IncorrectCount.Should().Be(1);
        q1.TotalAttempts.Should().Be(3);
    }

    [Fact]
    public void QuestionAnalytics_UnansweredCount_ShouldBeTracked()
    {
        var answerKey = CreateAnswerKey(3);
        var results = GradeStudents(answerKey, new()
        {
            new() { "A", "A", "A" },
            new() { "", "A", "A" },  // Q1 unanswered
            new() { "", "A", "A" }   // Q1 unanswered
        });

        var analytics = _analyzer.Analyze(results, answerKey);

        var q1 = analytics.QuestionStats[0];
        q1.UnansweredCount.Should().Be(2);
    }

    [Fact]
    public void QuestionAnalytics_CorrectAnswer_ShouldMatch()
    {
        var answerKey = CreateAnswerKey(3);
        var results = GradeStudents(answerKey, new()
        {
            new() { "A", "A", "A" }
        });

        var analytics = _analyzer.Analyze(results, answerKey);

        analytics.QuestionStats.Should().AllSatisfy(qs =>
        {
            qs.CorrectAnswer.Should().Be("A");
        });
    }
}
