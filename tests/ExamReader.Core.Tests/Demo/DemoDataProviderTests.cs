using ExamReader.Core.Demo;
using ExamReader.Core.Models;
using FluentAssertions;
using Xunit;

namespace ExamReader.Core.Tests.Demo;

public class DemoDataProviderTests
{
    private readonly DemoDataProvider _provider;

    public DemoDataProviderTests()
    {
        _provider = new DemoDataProvider();
    }

    [Fact]
    public void GetSampleExam_ShouldReturnExamWith30Questions()
    {
        var exam = _provider.GetSampleExam();

        exam.TotalQuestions.Should().Be(30);
        exam.AnswerKey.Questions.Should().HaveCount(30);
    }

    [Fact]
    public void GetSampleExam_ShouldHaveBubbleSheetFormat()
    {
        var exam = _provider.GetSampleExam();

        exam.Format.Should().Be(ExamFormat.BubbleSheet);
    }

    [Fact]
    public void GetSampleExam_ShouldHaveTitle()
    {
        var exam = _provider.GetSampleExam();

        exam.Title.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetSampleExam_ShouldHaveId()
    {
        var exam = _provider.GetSampleExam();

        exam.Id.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetSampleAnswerKey_ShouldReturn30Answers()
    {
        var answerKey = _provider.GetSampleAnswerKey();

        answerKey.Questions.Should().HaveCount(30);
        answerKey.TotalQuestions.Should().Be(30);
    }

    [Fact]
    public void GetSampleAnswerKey_AllQuestionsShouldHaveCorrectAnswer()
    {
        var answerKey = _provider.GetSampleAnswerKey();

        answerKey.Questions.Should().AllSatisfy(q =>
        {
            q.CorrectAnswer.Should().NotBeNullOrWhiteSpace();
            q.CorrectAnswer.Should().MatchRegex("^[A-D]$");
        });
    }

    [Fact]
    public void GetSampleAnswerKey_QuestionNumbersShouldBeSequential()
    {
        var answerKey = _provider.GetSampleAnswerKey();
        var numbers = answerKey.Questions.Select(q => q.Number).ToList();

        numbers.Should().BeEquivalentTo(Enumerable.Range(1, 30));
    }

    [Fact]
    public void GetSampleAnswerSheets_ShouldReturn25Students()
    {
        var sheets = _provider.GetSampleAnswerSheets();

        sheets.Should().HaveCount(25);
    }

    [Fact]
    public void GetSampleAnswerSheets_AllSheetsShouldHave30Answers()
    {
        var sheets = _provider.GetSampleAnswerSheets();

        sheets.Should().AllSatisfy(sheet =>
        {
            sheet.ExtractedAnswers.Should().HaveCount(30);
        });
    }

    [Fact]
    public void GetSampleAnswerSheets_AllStudentsShouldHaveNames()
    {
        var sheets = _provider.GetSampleAnswerSheets();

        sheets.Should().AllSatisfy(sheet =>
        {
            sheet.StudentName.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void GetSampleAnswerSheets_AllStudentsShouldHaveIds()
    {
        var sheets = _provider.GetSampleAnswerSheets();

        sheets.Should().AllSatisfy(sheet =>
        {
            sheet.StudentId.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void GetSampleResults_ShouldReturn25Results()
    {
        var results = _provider.GetSampleResults();

        results.Should().HaveCount(25);
    }

    [Fact]
    public void GetSampleResults_AllResultsShouldHavePercentage()
    {
        var results = _provider.GetSampleResults();

        results.Should().AllSatisfy(r =>
        {
            r.Percentage.Should().BeInRange(0, 100);
        });
    }

    [Fact]
    public void GetSampleResults_AllResultsShouldHaveLetterGrade()
    {
        var results = _provider.GetSampleResults();

        results.Should().AllSatisfy(r =>
        {
            r.LetterGrade.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void GetSampleAnalytics_ShouldHaveReasonableValues()
    {
        var analytics = _provider.GetSampleAnalytics();

        analytics.TotalStudents.Should().Be(25);
        analytics.ClassAverage.Should().BeGreaterThan(0);
        analytics.ClassAverage.Should().BeLessThan(100);
        analytics.StandardDeviation.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetSampleAnalytics_PassRateShouldBeRealistic()
    {
        var analytics = _provider.GetSampleAnalytics();

        analytics.PassRate.Should().BeGreaterThan(0);
        analytics.PassRate.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void GetSampleAnalytics_HighestShouldBeAboveLowest()
    {
        var analytics = _provider.GetSampleAnalytics();

        analytics.HighestScore.Should().BeGreaterThan(analytics.LowestScore);
    }

    [Fact]
    public void GetSampleAnswerSheets_StudentNamesShouldBeTurkish()
    {
        var sheets = _provider.GetSampleAnswerSheets();
        var names = sheets.Select(s => s.StudentName).ToList();

        // Check for some expected Turkish names
        names.Should().Contain(n => n.Contains("Y") && (n.Contains("lmaz") || n.Contains("ld")));
        names.Should().Contain(n => n.Contains("Demir") || n.Contains("Kaya") || n.Contains("elik"));
    }

    [Fact]
    public void GetSampleResults_ScoreDistributionShouldBeRealistic()
    {
        var results = _provider.GetSampleResults();
        var percentages = results.Select(r => r.Percentage).ToList();
        var mean = percentages.Average();

        // Mean should be roughly around 60-80% for a realistic exam
        mean.Should().BeGreaterThan(40);
        mean.Should().BeLessThan(95);
    }

    [Fact]
    public void GetSampleResults_ShouldHaveBothPassingAndFailing()
    {
        var results = _provider.GetSampleResults();

        results.Should().Contain(r => r.Passed);
        results.Should().Contain(r => !r.Passed);
    }

    [Fact]
    public void GetSampleAnalytics_ShouldHaveQuestionStats()
    {
        var analytics = _provider.GetSampleAnalytics();

        analytics.QuestionStats.Should().HaveCount(30);
    }

    [Fact]
    public void GetSampleAnalytics_ShouldHaveStudentStats()
    {
        var analytics = _provider.GetSampleAnalytics();

        analytics.StudentStats.Should().HaveCount(25);
    }

    [Fact]
    public void GetSampleAnalytics_GradeDistributionShouldNotBeEmpty()
    {
        var analytics = _provider.GetSampleAnalytics();

        analytics.GradeDistribution.Should().NotBeEmpty();
        analytics.GradeDistribution.Values.Sum().Should().Be(25);
    }

    [Fact]
    public void SampleExamData_CorrectAnswers_ShouldHave30Entries()
    {
        SampleExamData.CorrectAnswers.Should().HaveCount(30);
    }

    [Fact]
    public void SampleExamData_Students_ShouldHave25Entries()
    {
        SampleExamData.Students.Should().HaveCount(25);
    }

    [Fact]
    public void SampleExamData_CountCorrect_ShouldReturnValidCount()
    {
        foreach (var student in SampleExamData.Students)
        {
            var correct = SampleExamData.CountCorrect(student.Answers);
            correct.Should().BeInRange(0, 30);
        }
    }
}
