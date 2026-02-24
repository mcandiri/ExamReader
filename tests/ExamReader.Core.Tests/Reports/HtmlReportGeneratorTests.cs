using System.Text;
using ExamReader.Core.Analytics;
using ExamReader.Core.Grading;
using ExamReader.Core.Models;
using ExamReader.Core.Reports;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Reports;

public class HtmlReportGeneratorTests
{
    private readonly HtmlReportGenerator _generator;

    public HtmlReportGeneratorTests()
    {
        _generator = new HtmlReportGenerator();
    }

    private static ReportData CreateSampleReportData()
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
        var students = new[]
        {
            ("S1", "Ahmet Yilmaz", 5),
            ("S2", "Elif Kaya", 4),
            ("S3", "Mehmet Demir", 3),
            ("S4", "Zeynep Celik", 2),
            ("S5", "Can Ozturk", 1)
        };

        foreach (var (id, name, correct) in students)
        {
            var answers = Enumerable.Range(1, 5).Select(i => new StudentAnswer
            {
                QuestionNumber = i,
                SelectedAnswer = i <= correct ? "A" : "B",
                Status = AnswerStatus.Answered
            }).ToList();

            var result = gradingEngine.GradeStudent(answers, answerKey, options);
            result.StudentId = id;
            result.StudentName = name;
            results.Add(result);
        }

        var analyzer = new ExamAnalyzer(NullLogger<ExamAnalyzer>.Instance);
        var analytics = analyzer.Analyze(results, answerKey);

        return new ReportData
        {
            ReportTitle = "Test Exam Report",
            AnswerKey = answerKey,
            Results = results,
            Analytics = analytics
        };
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
    public async Task GenerateAsync_ShouldContainHtmlOpenTag()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var html = Encoding.UTF8.GetString(bytes);

        html.Should().Contain("<html");
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainHtmlCloseTag()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var html = Encoding.UTF8.GetString(bytes);

        html.Should().Contain("</html>");
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainStudentNames()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var html = Encoding.UTF8.GetString(bytes);

        html.Should().Contain("Ahmet Yilmaz");
        html.Should().Contain("Elif Kaya");
        html.Should().Contain("Mehmet Demir");
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainScores()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var html = Encoding.UTF8.GetString(bytes);

        html.Should().Contain("100.0");  // Perfect score
        html.Should().Contain("20.0");   // Lowest score
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainReportTitle()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var html = Encoding.UTF8.GetString(bytes);

        html.Should().Contain("Test Exam Report");
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainDoctype()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var html = Encoding.UTF8.GetString(bytes);

        html.Should().StartWith("<!DOCTYPE html>");
    }

    [Fact]
    public void Format_ShouldBeHTML()
    {
        _generator.Format.Should().Be("HTML");
    }
}
