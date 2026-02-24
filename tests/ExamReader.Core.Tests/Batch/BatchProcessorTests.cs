using ExamReader.Core.Batch;
using ExamReader.Core.Grading;
using ExamReader.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Batch;

public class BatchProcessorTests
{
    private readonly BatchProcessor _processor;
    private readonly GradingEngine _engine;
    private readonly GradingOptions _options;
    private readonly AnswerKey _answerKey;

    public BatchProcessorTests()
    {
        _engine = new GradingEngine(NullLogger<GradingEngine>.Instance);
        _processor = new BatchProcessor(_engine, NullLogger<BatchProcessor>.Instance);
        _options = new GradingOptions();
        _answerKey = new AnswerKey
        {
            ExamId = "TEST",
            ExamTitle = "Test Exam",
            Questions = Enumerable.Range(1, 10).Select(i => new Question
            {
                Number = i,
                CorrectAnswer = "A",
                Weight = 1.0
            }).ToList()
        };
    }

    private static AnswerSheet CreateAnswerSheet(string studentId, string name, int correctCount, int totalQuestions = 10)
    {
        return new AnswerSheet
        {
            StudentId = studentId,
            StudentName = name,
            ExtractedAnswers = Enumerable.Range(1, totalQuestions).Select(i => new StudentAnswer
            {
                QuestionNumber = i,
                SelectedAnswer = i <= correctCount ? "A" : "B",
                Status = AnswerStatus.Answered
            }).ToList()
        };
    }

    [Fact]
    public void ProcessBatch_MultipleStudents_ShouldGradeAll()
    {
        var sheets = new List<AnswerSheet>
        {
            CreateAnswerSheet("S1", "Student One", 10),
            CreateAnswerSheet("S2", "Student Two", 7),
            CreateAnswerSheet("S3", "Student Three", 5)
        };

        var result = _processor.ProcessBatch(sheets, _answerKey, _options);

        result.Results.Should().HaveCount(3);
        result.TotalProcessed.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void ProcessBatch_ShouldContainAllStudentResults()
    {
        var sheets = new List<AnswerSheet>
        {
            CreateAnswerSheet("S1", "Ali", 10),
            CreateAnswerSheet("S2", "Veli", 5)
        };

        var result = _processor.ProcessBatch(sheets, _answerKey, _options);

        result.Results.Select(r => r.StudentName).Should().Contain("Ali");
        result.Results.Select(r => r.StudentName).Should().Contain("Veli");
    }

    [Fact]
    public void ProcessBatch_ProgressReporting_ShouldFireEvents()
    {
        var progressEvents = new List<BatchProgress>();
        _processor.ProgressChanged += (_, e) => progressEvents.Add(e.Progress);

        var sheets = new List<AnswerSheet>
        {
            CreateAnswerSheet("S1", "Student One", 10),
            CreateAnswerSheet("S2", "Student Two", 7)
        };

        _processor.ProcessBatch(sheets, _answerKey, _options);

        progressEvents.Should().NotBeEmpty();
        progressEvents.Last().IsComplete.Should().BeTrue();
    }

    [Fact]
    public void ProcessBatch_EmptyBatch_ShouldReturnEmptyResults()
    {
        var sheets = new List<AnswerSheet>();

        var result = _processor.ProcessBatch(sheets, _answerKey, _options);

        result.Results.Should().BeEmpty();
        result.TotalProcessed.Should().Be(0);
        result.SuccessCount.Should().Be(0);
    }

    [Fact]
    public void ProcessBatch_ShouldSetTimestamps()
    {
        var sheets = new List<AnswerSheet>
        {
            CreateAnswerSheet("S1", "Student One", 10)
        };

        var result = _processor.ProcessBatch(sheets, _answerKey, _options);

        result.StartedAt.Should().BeBefore(result.CompletedAt);
        result.Duration.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void ProcessBatch_ShouldAssignStudentIdAndName()
    {
        var sheets = new List<AnswerSheet>
        {
            CreateAnswerSheet("ID123", "Elif Kaya", 8)
        };

        var result = _processor.ProcessBatch(sheets, _answerKey, _options);

        result.Results[0].StudentId.Should().Be("ID123");
        result.Results[0].StudentName.Should().Be("Elif Kaya");
    }

    [Fact]
    public void ProcessBatch_ResultsShouldHaveCorrectScores()
    {
        var sheets = new List<AnswerSheet>
        {
            CreateAnswerSheet("S1", "Perfect Student", 10),
            CreateAnswerSheet("S2", "Half Student", 5)
        };

        var result = _processor.ProcessBatch(sheets, _answerKey, _options);

        result.Results.First(r => r.StudentId == "S1").Percentage.Should().Be(100.0);
        result.Results.First(r => r.StudentId == "S2").Percentage.Should().Be(50.0);
    }

    [Fact]
    public async Task ProcessBatchAsync_ShouldWorkSameAsSync()
    {
        var sheets = new List<AnswerSheet>
        {
            CreateAnswerSheet("S1", "Student One", 8),
            CreateAnswerSheet("S2", "Student Two", 6)
        };

        var result = await _processor.ProcessBatchAsync(sheets, _answerKey, _options);

        result.Results.Should().HaveCount(2);
        result.SuccessCount.Should().Be(2);
    }

    [Fact]
    public void ProcessBatch_HasErrors_ShouldBeFalseWhenNoErrors()
    {
        var sheets = new List<AnswerSheet>
        {
            CreateAnswerSheet("S1", "Student One", 10)
        };

        var result = _processor.ProcessBatch(sheets, _answerKey, _options);

        result.HasErrors.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ProcessBatch_BatchId_ShouldBeNonEmpty()
    {
        var sheets = new List<AnswerSheet>
        {
            CreateAnswerSheet("S1", "Student One", 10)
        };

        var result = _processor.ProcessBatch(sheets, _answerKey, _options);

        result.BatchId.Should().NotBeNullOrEmpty();
    }
}
