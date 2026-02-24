using ExamReader.Core.Models;
using ExamReader.Core.Ocr;
using ExamReader.Core.Parsing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Parsing;

public class WrittenAnswerParserTests
{
    private readonly WrittenAnswerParser _parser;
    private readonly AnswerSheetTemplate _template;

    public WrittenAnswerParserTests()
    {
        _parser = new WrittenAnswerParser(NullLogger<WrittenAnswerParser>.Instance);
        _template = new AnswerSheetTemplate
        {
            TotalQuestions = 5,
            Format = ExamFormat.WrittenAnswer
        };
    }

    [Fact]
    public void CanParse_WithWrittenAnswerFormat_ShouldReturnTrue()
    {
        _parser.CanParse(_template).Should().BeTrue();
    }

    [Fact]
    public void CanParse_WithMixedFormat_ShouldReturnTrue()
    {
        var mixedTemplate = new AnswerSheetTemplate { Format = ExamFormat.Mixed };
        _parser.CanParse(mixedTemplate).Should().BeTrue();
    }

    [Fact]
    public void CanParse_WithBubbleSheetFormat_ShouldReturnFalse()
    {
        var bubbleTemplate = new AnswerSheetTemplate { Format = ExamFormat.BubbleSheet };
        _parser.CanParse(bubbleTemplate).Should().BeFalse();
    }

    [Fact]
    public async Task ParseAsync_WithQFormat_ShouldExtractText()
    {
        var rawText = "Q1: Istanbul is the largest city Q2: Ankara is the capital Q3: The Black Sea Q4: Ataturk founded the republic Q5: 1923";

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.85
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Should().HaveCount(5);
        answers.First(a => a.QuestionNumber == 1).SelectedAnswer.Should().Contain("Istanbul");
    }

    [Fact]
    public async Task ParseAsync_WithNumberedFormat_ShouldExtractText()
    {
        var rawText = "1. Istanbul is the largest city\n2. Ankara\n3. The Black Sea\n4. Ataturk\n5. 1923\n";

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.85
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Should().HaveCount(5);
        var answeredCount = answers.Count(a => a.Status == AnswerStatus.Answered);
        answeredCount.Should().BeGreaterOrEqualTo(5);
    }

    [Fact]
    public async Task ParseAsync_ShouldTrimWhitespace()
    {
        var rawText = "Q1:   Istanbul is the largest city   Q2: Ankara Q3: Sea Q4: Ataturk Q5: 1923";

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.85
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        // Answers should be trimmed
        foreach (var a in answers.Where(a => a.Status == AnswerStatus.Answered))
        {
            a.SelectedAnswer.Should().NotStartWith(" ");
            a.SelectedAnswer.Should().NotEndWith(" ");
        }
    }

    [Fact]
    public async Task ParseAsync_MissingQuestions_ShouldFillAsUnanswered()
    {
        var rawText = "Q1: Some answer Q2: Another answer";

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.85
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Should().HaveCount(5);
        var unanswered = answers.Where(a => a.Status == AnswerStatus.Unanswered).ToList();
        unanswered.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task ParseAsync_AnswersShouldBeOrdered()
    {
        var rawText = "Q3: Third Q1: First Q5: Fifth Q2: Second Q4: Fourth";

        var ocrResult = new OcrResult
        {
            Success = true,
            RawText = rawText,
            Regions = new List<OcrRegion>(),
            OverallConfidence = 0.85
        };

        var answers = await _parser.ParseAsync(ocrResult, _template);

        answers.Select(a => a.QuestionNumber).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task ParseAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ocrResult = new OcrResult { Success = true, RawText = "Q1: test", Regions = new() };
        var act = () => _parser.ParseAsync(ocrResult, _template, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
