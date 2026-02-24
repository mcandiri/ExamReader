using System.Text;
using ExamReader.Core.Analytics;
using ExamReader.Core.Grading;
using ExamReader.Core.Models;
using ExamReader.Core.Reports;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Reports;

public class CsvReportGeneratorTests
{
    private readonly CsvReportGenerator _generator;

    public CsvReportGeneratorTests()
    {
        _generator = new CsvReportGenerator();
    }

    private static ReportData CreateSampleReportData(int studentCount = 3)
    {
        var answerKey = new AnswerKey
        {
            ExamId = "TEST",
            ExamTitle = "Test Exam",
            Questions = Enumerable.Range(1, 5).Select(i => new Question
            {
                Number = i,
                CorrectAnswer = "A"
            }).ToList()
        };

        var gradingEngine = new GradingEngine(NullLogger<GradingEngine>.Instance);
        var options = new GradingOptions();
        var results = new List<GradingResult>();

        for (int s = 0; s < studentCount; s++)
        {
            var answers = Enumerable.Range(1, 5).Select(i => new StudentAnswer
            {
                QuestionNumber = i,
                SelectedAnswer = i <= (s + 2) ? "A" : "B",
                Status = AnswerStatus.Answered
            }).ToList();

            var result = gradingEngine.GradeStudent(answers, answerKey, options);
            result.StudentId = $"S{s + 1}";
            result.StudentName = $"Student {s + 1}";
            results.Add(result);
        }

        var analyzer = new ExamAnalyzer(NullLogger<ExamAnalyzer>.Instance);
        var analytics = analyzer.Analyze(results, answerKey);

        return new ReportData
        {
            ReportTitle = "CSV Test Report",
            AnswerKey = answerKey,
            Results = results,
            Analytics = analytics
        };
    }

    private static string DecodeCsv(byte[] bytes)
    {
        // Strip BOM if present
        var preamble = Encoding.UTF8.GetPreamble();
        if (bytes.Length >= preamble.Length && bytes.Take(preamble.Length).SequenceEqual(preamble))
        {
            return Encoding.UTF8.GetString(bytes, preamble.Length, bytes.Length - preamble.Length);
        }
        return Encoding.UTF8.GetString(bytes);
    }

    [Fact]
    public async Task GenerateAsync_ShouldHaveCorrectHeaders()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var csv = DecodeCsv(bytes);
        var firstLine = csv.Split('\n')[0].Trim();

        firstLine.Should().StartWith("Rank,StudentId,StudentName,Score,Percentage,Grade,Status");
    }

    [Fact]
    public async Task GenerateAsync_ShouldHaveCorrectRowCount()
    {
        var data = CreateSampleReportData(3);
        var bytes = await _generator.GenerateAsync(data);
        var csv = DecodeCsv(bytes);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // 1 header + 3 students = 4 lines
        lines.Length.Should().Be(4);
    }

    [Fact]
    public async Task GenerateAsync_ValuesShouldBeCommaSeparated()
    {
        var data = CreateSampleReportData(1);
        var bytes = await _generator.GenerateAsync(data);
        var csv = DecodeCsv(bytes);
        var dataLine = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)[1];

        dataLine.Should().Contain(",");
        // Should have at least 7 base columns + 5 question columns
        var fields = dataLine.Split(',');
        fields.Length.Should().BeGreaterOrEqualTo(12);
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainStudentData()
    {
        var data = CreateSampleReportData(2);
        var bytes = await _generator.GenerateAsync(data);
        var csv = DecodeCsv(bytes);

        csv.Should().Contain("Student 1");
        csv.Should().Contain("Student 2");
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainQuestionHeaders()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var csv = DecodeCsv(bytes);
        var headerLine = csv.Split('\n')[0];

        headerLine.Should().Contain("Q1");
        headerLine.Should().Contain("Q5");
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnNonEmptyByteArray()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);

        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Format_ShouldBeCSV()
    {
        _generator.Format.Should().Be("CSV");
    }

    [Fact]
    public async Task GenerateAsync_ShouldIncludePassFailStatus()
    {
        var data = CreateSampleReportData(3);
        var bytes = await _generator.GenerateAsync(data);
        var csv = DecodeCsv(bytes);

        // At least one pass or fail should be present
        (csv.Contains("Pass") || csv.Contains("Fail")).Should().BeTrue();
    }
}
