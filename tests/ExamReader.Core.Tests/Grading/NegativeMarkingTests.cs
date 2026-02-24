using ExamReader.Core.Grading;
using ExamReader.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Grading;

public class NegativeMarkingTests
{
    private readonly GradingEngine _engine;

    public NegativeMarkingTests()
    {
        _engine = new GradingEngine(NullLogger<GradingEngine>.Instance);
    }

    private static AnswerKey CreateAnswerKey(int count, string correctAnswer = "A")
    {
        return new AnswerKey
        {
            ExamId = "TEST",
            Questions = Enumerable.Range(1, count).Select(i => new Question
            {
                Number = i,
                CorrectAnswer = correctAnswer,
                Weight = 1.0
            }).ToList()
        };
    }

    [Fact]
    public void NegativeMarking_WrongAnswer_ShouldReduceScore()
    {
        var answerKey = CreateAnswerKey(10);
        // 5 correct, 5 wrong
        var studentAnswers = Enumerable.Range(1, 10).Select(i => new StudentAnswer
        {
            QuestionNumber = i,
            SelectedAnswer = i <= 5 ? "A" : "B",
            Status = AnswerStatus.Answered
        }).ToList();

        var optionsWithNegative = new GradingOptions { NegativeMarking = true, NegativePenalty = 0.25 };
        var optionsWithout = new GradingOptions { NegativeMarking = false };

        var resultWith = _engine.GradeStudent(studentAnswers, answerKey, optionsWithNegative);
        var resultWithout = _engine.GradeStudent(studentAnswers, answerKey, optionsWithout);

        resultWith.RawScore.Should().BeLessThan(resultWithout.RawScore);
    }

    [Fact]
    public void NegativeMarking_025Penalty_ShouldDeductQuarterPoint()
    {
        var answerKey = CreateAnswerKey(4);
        // 2 correct, 2 wrong: 2 * 1 + 2 * (-0.25) = 2 - 0.5 = 1.5
        var studentAnswers = new List<StudentAnswer>
        {
            new() { QuestionNumber = 1, SelectedAnswer = "A", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 2, SelectedAnswer = "A", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 3, SelectedAnswer = "B", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 4, SelectedAnswer = "B", Status = AnswerStatus.Answered }
        };

        var options = new GradingOptions { NegativeMarking = true, NegativePenalty = 0.25 };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.RawScore.Should().Be(1.5);
        result.Correct.Should().Be(2);
        result.Incorrect.Should().Be(2);
    }

    [Fact]
    public void NegativeMarking_UnansweredQuestions_ShouldGetZero()
    {
        var answerKey = CreateAnswerKey(4);
        var studentAnswers = new List<StudentAnswer>
        {
            new() { QuestionNumber = 1, SelectedAnswer = "A", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 2, SelectedAnswer = "A", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 3, SelectedAnswer = string.Empty, Status = AnswerStatus.Unanswered },
            new() { QuestionNumber = 4, SelectedAnswer = string.Empty, Status = AnswerStatus.Unanswered }
        };

        var options = new GradingOptions { NegativeMarking = true, NegativePenalty = 0.25 };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        // 2 correct * 1 = 2, 2 unanswered * 0 = 0 -> 2.0 total
        result.RawScore.Should().Be(2.0);
        result.Unanswered.Should().Be(2);

        // Check that unanswered question results have 0 points, not negative
        var unansweredResults = result.QuestionResults
            .Where(qr => qr.Status == AnswerStatus.Unanswered)
            .ToList();
        unansweredResults.Should().AllSatisfy(qr => qr.PointsEarned.Should().Be(0));
    }

    [Fact]
    public void NegativeMarking_ScoreShouldNotGoBelowZero()
    {
        var answerKey = CreateAnswerKey(10);
        // All 10 wrong with negative marking: 10 * (-0.25) = -2.5, but should clamp to 0
        var studentAnswers = Enumerable.Range(1, 10).Select(i => new StudentAnswer
        {
            QuestionNumber = i,
            SelectedAnswer = "B",
            Status = AnswerStatus.Answered
        }).ToList();

        var options = new GradingOptions { NegativeMarking = true, NegativePenalty = 1.0 };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.RawScore.Should().BeGreaterOrEqualTo(0);
        result.Percentage.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void NegativeMarking_CustomPenalty_05_ShouldDeductHalfPoint()
    {
        var answerKey = CreateAnswerKey(4);
        // 2 correct, 2 wrong: 2 * 1 + 2 * (-0.5) = 2 - 1 = 1.0
        var studentAnswers = new List<StudentAnswer>
        {
            new() { QuestionNumber = 1, SelectedAnswer = "A", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 2, SelectedAnswer = "A", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 3, SelectedAnswer = "B", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 4, SelectedAnswer = "B", Status = AnswerStatus.Answered }
        };

        var options = new GradingOptions { NegativeMarking = true, NegativePenalty = 0.5 };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.RawScore.Should().Be(1.0);
    }

    [Fact]
    public void NegativeMarking_CustomPenalty_033_ShouldDeductThirdPoint()
    {
        var answerKey = CreateAnswerKey(4);
        // 1 correct, 3 wrong: 1 * 1 + 3 * (-0.33) = 1 - 0.99 = 0.01
        var studentAnswers = new List<StudentAnswer>
        {
            new() { QuestionNumber = 1, SelectedAnswer = "A", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 2, SelectedAnswer = "B", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 3, SelectedAnswer = "B", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 4, SelectedAnswer = "B", Status = AnswerStatus.Answered }
        };

        var options = new GradingOptions { NegativeMarking = true, NegativePenalty = 0.33 };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.RawScore.Should().BeApproximately(0.01, 0.01);
    }

    [Fact]
    public void NegativeMarking_PerfectScore_ShouldNotBeAffected()
    {
        var answerKey = CreateAnswerKey(10);
        var studentAnswers = Enumerable.Range(1, 10).Select(i => new StudentAnswer
        {
            QuestionNumber = i,
            SelectedAnswer = "A",
            Status = AnswerStatus.Answered
        }).ToList();

        var options = new GradingOptions { NegativeMarking = true, NegativePenalty = 0.25 };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.RawScore.Should().Be(10.0);
        result.Percentage.Should().Be(100.0);
    }

    [Fact]
    public void NegativeMarking_WithWeightedQuestions_ShouldApplyPenaltyToWeight()
    {
        var answerKey = new AnswerKey
        {
            ExamId = "TEST",
            Questions = new List<Question>
            {
                new() { Number = 1, CorrectAnswer = "A", Weight = 2.0 },
                new() { Number = 2, CorrectAnswer = "A", Weight = 3.0 }
            }
        };

        // Q1 correct (2pts), Q2 wrong (-0.25 * 3 = -0.75)
        var studentAnswers = new List<StudentAnswer>
        {
            new() { QuestionNumber = 1, SelectedAnswer = "A", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 2, SelectedAnswer = "B", Status = AnswerStatus.Answered }
        };

        var options = new GradingOptions
        {
            NegativeMarking = true,
            NegativePenalty = 0.25,
            WeightedQuestions = true
        };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.RawScore.Should().Be(1.25); // 2.0 - 0.75 = 1.25
    }

    [Fact]
    public void NegativeMarking_HighPenalty_AllWrong_ShouldClampToZero()
    {
        var answerKey = CreateAnswerKey(5);
        var studentAnswers = Enumerable.Range(1, 5).Select(i => new StudentAnswer
        {
            QuestionNumber = i,
            SelectedAnswer = "B",
            Status = AnswerStatus.Answered
        }).ToList();

        var options = new GradingOptions { NegativeMarking = true, NegativePenalty = 5.0 };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.RawScore.Should().Be(0);
        result.Percentage.Should().Be(0);
    }
}
