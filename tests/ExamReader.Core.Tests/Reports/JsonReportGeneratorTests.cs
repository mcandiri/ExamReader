using System.Text;
using System.Text.Json;
using ExamReader.Core.Analytics;
using ExamReader.Core.Grading;
using ExamReader.Core.Models;
using ExamReader.Core.Reports;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Reports;

public class JsonReportGeneratorTests
{
    private readonly JsonReportGenerator _generator;

    public JsonReportGeneratorTests()
    {
        _generator = new JsonReportGenerator();
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
            ReportTitle = "JSON Test Report",
            AnswerKey = answerKey,
            Results = results,
            Analytics = analytics
        };
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnValidJson()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var json = Encoding.UTF8.GetString(bytes);

        var act = () => JsonDocument.Parse(json);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainReportTitle()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var json = Encoding.UTF8.GetString(bytes);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("reportTitle").GetString().Should().Be("JSON Test Report");
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainSummary()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var json = Encoding.UTF8.GetString(bytes);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("summary", out var summary).Should().BeTrue();
        summary.TryGetProperty("totalStudents", out _).Should().BeTrue();
        summary.TryGetProperty("classAverage", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAsync_StudentCountShouldMatch()
    {
        var data = CreateSampleReportData(5);
        var bytes = await _generator.GenerateAsync(data);
        var json = Encoding.UTF8.GetString(bytes);
        var doc = JsonDocument.Parse(json);

        var students = doc.RootElement.GetProperty("students");
        students.GetArrayLength().Should().Be(5);
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainStudentsArray()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var json = Encoding.UTF8.GetString(bytes);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("students", out var students).Should().BeTrue();
        students.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainQuestionsArray()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var json = Encoding.UTF8.GetString(bytes);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("questions", out var questions).Should().BeTrue();
        questions.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GenerateAsync_ShouldContainGeneratedAt()
    {
        var data = CreateSampleReportData();
        var bytes = await _generator.GenerateAsync(data);
        var json = Encoding.UTF8.GetString(bytes);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("generatedAt", out _).Should().BeTrue();
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
    public void Format_ShouldBeJSON()
    {
        _generator.Format.Should().Be("JSON");
    }

    [Fact]
    public async Task GenerateAsync_StudentShouldHaveExpectedProperties()
    {
        var data = CreateSampleReportData(1);
        var bytes = await _generator.GenerateAsync(data);
        var json = Encoding.UTF8.GetString(bytes);
        var doc = JsonDocument.Parse(json);

        var student = doc.RootElement.GetProperty("students").EnumerateArray().First();
        student.TryGetProperty("studentId", out _).Should().BeTrue();
        student.TryGetProperty("studentName", out _).Should().BeTrue();
        student.TryGetProperty("percentage", out _).Should().BeTrue();
        student.TryGetProperty("letterGrade", out _).Should().BeTrue();
        student.TryGetProperty("passed", out _).Should().BeTrue();
    }
}
