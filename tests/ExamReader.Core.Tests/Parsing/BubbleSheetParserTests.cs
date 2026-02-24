using ExamReader.Core.Models;
using ExamReader.Core.Ocr;
using ExamReader.Core.Parsing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Parsing;

public class BubbleSheetParserTests
{
    private readonly BubbleSheetParser _parser;
    private readonly AnswerSheetTemplate _template;

    public BubbleSheetParserTests()
    {
        _parser = new BubbleSheetParser(NullLogger<BubbleSheetParser>.Instance);
        _template = new AnswerSheetTemplate
        {
            TotalQuestions = 30,
            AnswerOptions = new List<string> { "A", "B", "C", "D" },
            Format = ExamFormat.BubbleSheet
        };
    }

    [Fact]
    public void CanParse_WithBubbleSheetFormat_ShouldReturnTrue()
    {
        _parser.CanParse(_template).Should().BeTrue();
    }

    [Fact]
    public void CanParse_WithGridFormat_ShouldReturnFalse()
    {
        var gridTemplate = new AnswerSheetTemplate { Format = ExamFormat.GridBased };
        _parser.CanParse(gridTemplate).Should().BeFalse();
    }

    [Fact]
    public async Task ParseAsync_WithTypicalBubbleSheetOcr_ShouldReturnAllAnswers()
    {
        var ocrResult = BuildBubbleSheetOcrResult(
            Enumerable.Range(1, 30).Select(i => (i, "ABCD"[(i - 1) % 4].ToString())).ToList());

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Should().HaveCount(30);
        answers.Should().AllSatisfy(a => a.QuestionNumber.Should().BeInRange(1, 30));
    }

    [Fact]
    public async Task ParseAsync_WithAbcdAnswers_ShouldExtractCorrectly()
    {
        var ocrResult = BuildBubbleSheetOcrResult(new List<(int, string)>
        {
            (1, "A"), (2, "B"), (3, "C"), (4, "D"),
            (5, "A"), (6, "B"), (7, "C"), (8, "D"),
            (9, "A"), (10, "B"), (11, "C"), (12, "D"),
            (13, "A"), (14, "B"), (15, "C"), (16, "D"),
            (17, "A"), (18, "B"), (19, "C"), (20, "D"),
            (21, "A"), (22, "B"), (23, "C"), (24, "D"),
            (25, "A"), (26, "B"), (27, "C"), (28, "D"),
            (29, "A"), (30, "B")
        });

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.First(a => a.QuestionNumber == 1).SelectedAnswer.Should().Be("A");
        answers.First(a => a.QuestionNumber == 2).SelectedAnswer.Should().Be("B");
        answers.First(a => a.QuestionNumber == 3).SelectedAnswer.Should().Be("C");
        answers.First(a => a.QuestionNumber == 4).SelectedAnswer.Should().Be("D");
    }

    [Fact]
    public async Task ParseAsync_WithBlankQuestion_ShouldMarkAsUnanswered()
    {
        var ocrResult = BuildBubbleSheetOcrResultWithBlanks(new int[] { 5, 10 });

        var answers = await _parser.ParseAsync(ocrResult, _template);

        var q5 = answers.First(a => a.QuestionNumber == 5);
        q5.Status.Should().Be(AnswerStatus.Unanswered);
        q5.SelectedAnswer.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_WithUnclearMark_ShouldMarkAsUnclear()
    {
        var rawText = "Student Name: Test Student\nStudent ID: 001\n---\n";
        for (int i = 1; i <= 30; i++)
        {
            if (i == 3)
                rawText += $"Q{i}: [?]\n";
            else
                rawText += $"Q{i}: [A]\n";
        }

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.9
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        var q3 = answers.First(a => a.QuestionNumber == 3);
        q3.Status.Should().Be(AnswerStatus.Unclear);
    }

    [Fact]
    public async Task ParseAsync_30QuestionBubbleSheet_ShouldReturn30Answers()
    {
        var ocrResult = BuildBubbleSheetOcrResult(
            Enumerable.Range(1, 30).Select(i => (i, "A")).ToList());

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Should().HaveCount(30);
        answers.Select(a => a.QuestionNumber).Should().BeEquivalentTo(Enumerable.Range(1, 30));
    }

    [Fact]
    public async Task ParseAsync_MissingQuestions_ShouldFillAsUnanswered()
    {
        // Only provide answers for Q1-Q10, the parser should fill Q11-Q30 as unanswered
        var rawText = "Student Name: Test\nStudent ID: 001\n---\n";
        for (int i = 1; i <= 10; i++)
        {
            rawText += $"Q{i}: [A]\n";
        }

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.9
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Should().HaveCount(30);
        var unanswered = answers.Where(a => a.Status == AnswerStatus.Unanswered).ToList();
        unanswered.Should().HaveCountGreaterOrEqualTo(20);
    }

    [Fact]
    public async Task ParseAsync_AnswersShouldBeOrderedByQuestionNumber()
    {
        var ocrResult = BuildBubbleSheetOcrResult(
            Enumerable.Range(1, 30).Select(i => (i, "A")).ToList());

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Select(a => a.QuestionNumber).Should().BeInAscendingOrder();
    }

    [Fact]
    public void ExtractStudentName_ShouldExtractNameFromOcrText()
    {
        var rawText = "Student Name: Ahmet Yilmaz\nStudent ID: 2024001\n---\n";
        var name = BubbleSheetParser.ExtractStudentName(rawText);
        name.Should().Be("Ahmet Yilmaz");
    }

    [Fact]
    public void ExtractStudentId_ShouldExtractIdFromOcrText()
    {
        var rawText = "Student Name: Ahmet Yilmaz\nStudent ID: 2024001\n---\n";
        var id = BubbleSheetParser.ExtractStudentId(rawText);
        id.Should().Be("2024001");
    }

    [Fact]
    public void ExtractStudentName_WithNoNameField_ShouldReturnNull()
    {
        var rawText = "Q1: [A]\nQ2: [B]\n";
        var name = BubbleSheetParser.ExtractStudentName(rawText);
        name.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ocrResult = new OcrResult { Success = true, RawText = "Q1: [A]", Regions = new() };
        var act = () => _parser.ParseAsync(ocrResult, _template, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static OcrResult BuildBubbleSheetOcrResult(List<(int Number, string Answer)> answers)
    {
        var rawText = "Student Name: Test Student\nStudent ID: TEST001\n---\n";
        foreach (var (num, answer) in answers)
        {
            rawText += $"Q{num}: [{answer}]\n";
        }

        return new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.95
        };
    }

    private static OcrResult BuildBubbleSheetOcrResultWithBlanks(int[] blankQuestions)
    {
        var rawText = "Student Name: Test Student\nStudent ID: TEST001\n---\n";
        for (int i = 1; i <= 30; i++)
        {
            if (blankQuestions.Contains(i))
                rawText += $"Q{i}: [ ]\n";
            else
                rawText += $"Q{i}: [A]\n";
        }

        return new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.9
        };
    }
}
