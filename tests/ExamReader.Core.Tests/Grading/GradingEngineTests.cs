using ExamReader.Core.Grading;
using ExamReader.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Grading;

public class GradingEngineTests
{
    private readonly GradingEngine _engine;
    private readonly GradingOptions _defaultOptions;

    public GradingEngineTests()
    {
        _engine = new GradingEngine(NullLogger<GradingEngine>.Instance);
        _defaultOptions = new GradingOptions();
    }

    private static AnswerKey CreateAnswerKey(int count, Func<int, string>? answerFunc = null)
    {
        answerFunc ??= i => "ABCD"[(i - 1) % 4].ToString();
        return new AnswerKey
        {
            ExamId = "TEST001",
            ExamTitle = "Test Exam",
            Questions = Enumerable.Range(1, count).Select(i => new Question
            {
                Number = i,
                CorrectAnswer = answerFunc(i),
                Weight = 1.0,
                Options = new List<string> { "A", "B", "C", "D" },
                Type = QuestionType.MultipleChoice
            }).ToList()
        };
    }

    private static List<StudentAnswer> CreateAnswers(int count, Func<int, string> answerFunc)
    {
        return Enumerable.Range(1, count).Select(i => new StudentAnswer
        {
            QuestionNumber = i,
            SelectedAnswer = answerFunc(i),
            Confidence = 1.0,
            Status = string.IsNullOrEmpty(answerFunc(i)) ? AnswerStatus.Unanswered : AnswerStatus.Answered
        }).ToList();
    }

    [Fact]
    public void GradeStudent_PerfectScore_ShouldReturn100Percent()
    {
        var answerKey = CreateAnswerKey(30);
        var studentAnswers = CreateAnswers(30, i => "ABCD"[(i - 1) % 4].ToString());

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Percentage.Should().Be(100.0);
        result.Correct.Should().Be(30);
        result.Incorrect.Should().Be(0);
        result.Unanswered.Should().Be(0);
    }

    [Fact]
    public void GradeStudent_PerfectScore_ShouldReturnGradeA()
    {
        var answerKey = CreateAnswerKey(30);
        var studentAnswers = CreateAnswers(30, i => "ABCD"[(i - 1) % 4].ToString());

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.LetterGrade.Should().Be("A");
    }

    [Fact]
    public void GradeStudent_ZeroScore_ShouldReturn0Percent()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        // All wrong: student always picks B when correct is A
        var studentAnswers = CreateAnswers(10, _ => "B");

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Percentage.Should().Be(0);
        result.Correct.Should().Be(0);
        result.Incorrect.Should().Be(10);
    }

    [Fact]
    public void GradeStudent_ZeroScore_ShouldReturnGradeF()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, _ => "B");

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.LetterGrade.Should().Be("F");
    }

    [Fact]
    public void GradeStudent_MixedResults_ShouldCalculateCorrectPercentage()
    {
        // 10 questions, 7 correct, 3 wrong -> 70%
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 7 ? "A" : "B");

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Percentage.Should().Be(70.0);
        result.Correct.Should().Be(7);
        result.Incorrect.Should().Be(3);
    }

    [Fact]
    public void GradeStudent_90Percent_ShouldReturnGradeA()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 9 ? "A" : "B");

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Percentage.Should().Be(90.0);
        result.LetterGrade.Should().Be("A");
    }

    [Fact]
    public void GradeStudent_80Percent_ShouldReturnGradeB()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 8 ? "A" : "B");

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Percentage.Should().Be(80.0);
        result.LetterGrade.Should().Be("B");
    }

    [Fact]
    public void GradeStudent_70Percent_ShouldReturnGradeC()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 7 ? "A" : "B");

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Percentage.Should().Be(70.0);
        result.LetterGrade.Should().Be("C");
    }

    [Fact]
    public void GradeStudent_60Percent_ShouldReturnGradeD()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 6 ? "A" : "B");

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Percentage.Should().Be(60.0);
        result.LetterGrade.Should().Be("D");
    }

    [Fact]
    public void GradeStudent_Below50_ShouldReturnGradeF()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 4 ? "A" : "B");

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Percentage.Should().Be(40.0);
        result.LetterGrade.Should().Be("F");
    }

    [Fact]
    public void GradeStudent_PlusMinusScale_80Percent_ShouldReturnBPlus()
    {
        var answerKey = CreateAnswerKey(20, _ => "A");
        var studentAnswers = CreateAnswers(20, i => i <= 16 ? "A" : "B"); // 80%
        var options = new GradingOptions { GradeScale = LetterGradeScale.PlusMinus };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.Percentage.Should().Be(80.0);
        result.LetterGrade.Should().Be("B+");
    }

    [Fact]
    public void GradeStudent_PlusMinusScale_60Percent_ShouldReturnC()
    {
        var answerKey = CreateAnswerKey(20, _ => "A");
        var studentAnswers = CreateAnswers(20, i => i <= 12 ? "A" : "B"); // 60%
        var options = new GradingOptions { GradeScale = LetterGradeScale.PlusMinus };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.Percentage.Should().Be(60.0);
        result.LetterGrade.Should().Be("C");
    }

    [Fact]
    public void GradeStudent_DefaultPassingScore_60Percent_ShouldPass()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 6 ? "A" : "B"); // 60%

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void GradeStudent_DefaultPassingScore_50Percent_ShouldFail()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 5 ? "A" : "B"); // 50%

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void GradeStudent_CustomPassingScore70_70Percent_ShouldPass()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 7 ? "A" : "B"); // 70%
        var options = new GradingOptions { PassingScore = 70.0 };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void GradeStudent_CustomPassingScore70_60Percent_ShouldFail()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 6 ? "A" : "B"); // 60%
        var options = new GradingOptions { PassingScore = 70.0 };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void GradeStudent_UnansweredQuestions_ShouldScoreZeroNotNegative()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        // 5 correct, 5 unanswered (no answer provided)
        var studentAnswers = new List<StudentAnswer>();
        for (int i = 1; i <= 5; i++)
        {
            studentAnswers.Add(new StudentAnswer
            {
                QuestionNumber = i,
                SelectedAnswer = "A",
                Status = AnswerStatus.Answered
            });
        }
        for (int i = 6; i <= 10; i++)
        {
            studentAnswers.Add(new StudentAnswer
            {
                QuestionNumber = i,
                SelectedAnswer = string.Empty,
                Status = AnswerStatus.Unanswered
            });
        }

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Percentage.Should().Be(50.0);
        result.Correct.Should().Be(5);
        result.Unanswered.Should().Be(5);
        result.RawScore.Should().Be(5.0);
    }

    [Fact]
    public void GradeStudent_QuestionResults_ShouldContainAllQuestions()
    {
        var answerKey = CreateAnswerKey(10);
        var studentAnswers = CreateAnswers(10, i => "ABCD"[(i - 1) % 4].ToString());

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.QuestionResults.Should().HaveCount(10);
        result.QuestionResults.Should().AllSatisfy(qr =>
        {
            qr.QuestionNumber.Should().BeInRange(1, 10);
            qr.PointsPossible.Should().Be(1.0);
        });
    }

    [Fact]
    public void GradeStudent_QuestionResults_CorrectQuestion_ShouldEarnFullPoints()
    {
        var answerKey = CreateAnswerKey(5, _ => "A");
        var studentAnswers = CreateAnswers(5, _ => "A");

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.QuestionResults.Should().AllSatisfy(qr =>
        {
            qr.IsCorrect.Should().BeTrue();
            qr.PointsEarned.Should().Be(1.0);
        });
    }

    [Fact]
    public void GradeStudent_WithWeightedQuestions_ShouldUseWeights()
    {
        var answerKey = new AnswerKey
        {
            ExamId = "TEST",
            Questions = new List<Question>
            {
                new() { Number = 1, CorrectAnswer = "A", Weight = 2.0 },
                new() { Number = 2, CorrectAnswer = "B", Weight = 3.0 },
                new() { Number = 3, CorrectAnswer = "C", Weight = 5.0 }
            }
        };

        var studentAnswers = new List<StudentAnswer>
        {
            new() { QuestionNumber = 1, SelectedAnswer = "A", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 2, SelectedAnswer = "B", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 3, SelectedAnswer = "D", Status = AnswerStatus.Answered } // wrong
        };

        var options = new GradingOptions { WeightedQuestions = true };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.RawScore.Should().Be(5.0); // 2 + 3 = 5
        result.MaxScore.Should().Be(10.0); // 2 + 3 + 5 = 10
        result.Percentage.Should().Be(50.0);
    }

    [Fact]
    public void GradeStudent_CaseInsensitiveComparison()
    {
        var answerKey = CreateAnswerKey(3, _ => "A");
        var studentAnswers = new List<StudentAnswer>
        {
            new() { QuestionNumber = 1, SelectedAnswer = "a", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 2, SelectedAnswer = "A", Status = AnswerStatus.Answered },
            new() { QuestionNumber = 3, SelectedAnswer = " a ", Status = AnswerStatus.Answered }
        };

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.Correct.Should().Be(3);
    }

    [Fact]
    public void GradeStudent_TotalQuestions_ShouldMatchAnswerKey()
    {
        var answerKey = CreateAnswerKey(30);
        var studentAnswers = CreateAnswers(30, i => "ABCD"[(i - 1) % 4].ToString());

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.TotalQuestions.Should().Be(30);
    }

    [Fact]
    public void GradeStudent_MaxScore_WithoutWeights_ShouldEqualTotalQuestions()
    {
        var answerKey = CreateAnswerKey(20);
        var studentAnswers = CreateAnswers(20, _ => "A");

        var result = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);

        result.MaxScore.Should().Be(20.0);
    }

    [Fact]
    public async Task GradeStudentAsync_ShouldProduceSameResultAsSync()
    {
        var answerKey = CreateAnswerKey(10);
        var studentAnswers = CreateAnswers(10, i => "ABCD"[(i - 1) % 4].ToString());

        var syncResult = _engine.GradeStudent(studentAnswers, answerKey, _defaultOptions);
        var asyncResult = await _engine.GradeStudentAsync(studentAnswers, answerKey, _defaultOptions);

        asyncResult.Percentage.Should().Be(syncResult.Percentage);
        asyncResult.Correct.Should().Be(syncResult.Correct);
        asyncResult.LetterGrade.Should().Be(syncResult.LetterGrade);
    }

    [Fact]
    public async Task GradeStudentAsync_WithCancellation_ShouldThrow()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var answerKey = CreateAnswerKey(10);
        var studentAnswers = CreateAnswers(10, _ => "A");

        var act = () => _engine.GradeStudentAsync(studentAnswers, answerKey, _defaultOptions, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void GradeStudent_PassFailScale_PassingStudent()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 7 ? "A" : "B"); // 70%
        var options = new GradingOptions { GradeScale = LetterGradeScale.PassFail };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.LetterGrade.Should().Be("P");
    }

    [Fact]
    public void GradeStudent_PassFailScale_FailingStudent()
    {
        var answerKey = CreateAnswerKey(10, _ => "A");
        var studentAnswers = CreateAnswers(10, i => i <= 5 ? "A" : "B"); // 50%
        var options = new GradingOptions { GradeScale = LetterGradeScale.PassFail };

        var result = _engine.GradeStudent(studentAnswers, answerKey, options);

        result.LetterGrade.Should().Be("F");
    }
}
