using ExamReader.Core.Models;
using ExamReader.Core.Ocr;
using ExamReader.Core.Parsing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Parsing;

public class GridParserTests
{
    private readonly GridParser _parser;
    private readonly AnswerSheetTemplate _template;

    public GridParserTests()
    {
        _parser = new GridParser(NullLogger<GridParser>.Instance);
        _template = new AnswerSheetTemplate
        {
            TotalQuestions = 10,
            AnswerOptions = new List<string> { "A", "B", "C", "D" },
            Format = ExamFormat.GridBased
        };
    }

    [Fact]
    public void CanParse_WithGridFormat_ShouldReturnTrue()
    {
        _parser.CanParse(_template).Should().BeTrue();
    }

    [Fact]
    public void CanParse_WithBubbleSheetFormat_ShouldReturnFalse()
    {
        var template = new AnswerSheetTemplate { Format = ExamFormat.BubbleSheet };
        _parser.CanParse(template).Should().BeFalse();
    }

    [Fact]
    public async Task ParseAsync_WithLabeledGridFormat_ShouldExtractAnswers()
    {
        var rawText = "1: A B [C] D\n2: [A] B C D\n3: A [B] C D\n";
        for (int i = 4; i <= 10; i++)
        {
            rawText += $"{i}: A B C [D]\n";
        }

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.9
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Should().HaveCount(10);
        answers.First(a => a.QuestionNumber == 1).SelectedAnswer.Should().Be("C");
        answers.First(a => a.QuestionNumber == 2).SelectedAnswer.Should().Be("A");
        answers.First(a => a.QuestionNumber == 3).SelectedAnswer.Should().Be("B");
    }

    [Fact]
    public async Task ParseAsync_WithXMarkGrid_ShouldExtractAnswers()
    {
        var rawText = "1  _ X _ _\n2  _ _ X _\n3  X _ _ _\n";
        for (int i = 4; i <= 10; i++)
        {
            rawText += $"{i}  _ _ _ X\n";
        }

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.85
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Should().HaveCount(10);
        answers.First(a => a.QuestionNumber == 1).SelectedAnswer.Should().Be("B");
        answers.First(a => a.QuestionNumber == 2).SelectedAnswer.Should().Be("C");
        answers.First(a => a.QuestionNumber == 3).SelectedAnswer.Should().Be("A");
    }

    [Fact]
    public async Task ParseAsync_WithEmptyCells_ShouldMarkAsUnanswered()
    {
        var rawText = "1: A B C D\n2: [A] B C D\n";
        for (int i = 3; i <= 10; i++)
        {
            rawText += $"{i}: [A] B C D\n";
        }

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.85
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Should().HaveCount(10);
        // Q1 has no bracketed selection
        answers.First(a => a.QuestionNumber == 1).Status.Should().Be(AnswerStatus.Unanswered);
    }

    [Fact]
    public async Task ParseAsync_MissingQuestions_ShouldFillAsUnanswered()
    {
        // Only provide 3 out of 10 questions
        var rawText = "1: [A] B C D\n2: A [B] C D\n3: A B [C] D\n";

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.9
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Should().HaveCount(10);
        var unansweredCount = answers.Count(a => a.Status == AnswerStatus.Unanswered);
        unansweredCount.Should().BeGreaterOrEqualTo(7);
    }

    [Fact]
    public async Task ParseAsync_AnswersShouldBeOrdered()
    {
        var rawText = "";
        for (int i = 1; i <= 10; i++)
        {
            rawText += $"{i}: [A] B C D\n";
        }

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.9
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Select(a => a.QuestionNumber).Should().BeInAscendingOrder();
    }
}
