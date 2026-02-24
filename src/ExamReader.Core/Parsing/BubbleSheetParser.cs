using System.Text.RegularExpressions;
using ExamReader.Core.Models;
using ExamReader.Core.Ocr;
using Microsoft.Extensions.Logging;

namespace ExamReader.Core.Parsing;

public partial class BubbleSheetParser : IAnswerSheetParser
{
    private readonly ILogger<BubbleSheetParser> _logger;

    // Pattern matches: Q1: [A], Q2: [B], Q10: [C], etc.
    [GeneratedRegex(@"Q(\d+)\s*:\s*\[([A-Za-z?]|\s*)\]", RegexOptions.IgnoreCase)]
    private static partial Regex AnswerLineRegex();

    // Pattern matches: 1. A, 2. B, 10. C, etc.
    [GeneratedRegex(@"^(\d+)\.\s*([A-Da-d])\s*$", RegexOptions.Multiline)]
    private static partial Regex NumberedAnswerRegex();

    // Pattern matches student name header
    [GeneratedRegex(@"Student\s*Name\s*:\s*(.+)", RegexOptions.IgnoreCase)]
    private static partial Regex StudentNameRegex();

    // Pattern matches student ID header
    [GeneratedRegex(@"Student\s*ID\s*:\s*(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex StudentIdRegex();

    public BubbleSheetParser(ILogger<BubbleSheetParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(AnswerSheetTemplate template)
    {
        return template.Format == ExamFormat.BubbleSheet;
    }

    public Task<List<StudentAnswer>> ParseAsync(OcrResult ocrResult, AnswerSheetTemplate template, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var answers = new List<StudentAnswer>();
        var rawText = ocrResult.RawText;

        _logger.LogInformation("Parsing bubble sheet OCR result ({TextLength} chars, {RegionCount} regions)", rawText.Length, ocrResult.Regions.Count);

        // Try primary pattern: Q1: [A]
        var matches = AnswerLineRegex().Matches(rawText);
        if (matches.Count > 0)
        {
            answers = ParseFromQFormat(matches, template);
        }
        else
        {
            // Fallback pattern: 1. A
            var numberedMatches = NumberedAnswerRegex().Matches(rawText);
            if (numberedMatches.Count > 0)
            {
                answers = ParseFromNumberedFormat(numberedMatches, template);
            }
        }

        // If region-based parsing found more answers, use bounding box approach
        if (answers.Count < template.TotalQuestions && ocrResult.Regions.Count > 0)
        {
            var regionAnswers = ParseFromRegions(ocrResult.Regions, template);
            if (regionAnswers.Count > answers.Count)
            {
                answers = regionAnswers;
            }
        }

        // Fill in any missing question numbers as unanswered
        var answeredNumbers = answers.Select(a => a.QuestionNumber).ToHashSet();
        for (int i = 1; i <= template.TotalQuestions; i++)
        {
            if (!answeredNumbers.Contains(i))
            {
                answers.Add(new StudentAnswer
                {
                    QuestionNumber = i,
                    SelectedAnswer = string.Empty,
                    Confidence = 0,
                    Status = AnswerStatus.Unanswered
                });
            }
        }

        answers = answers.OrderBy(a => a.QuestionNumber).ToList();

        _logger.LogInformation("Parsed {AnswerCount} answers from bubble sheet ({Answered} answered, {Unanswered} unanswered)",
            answers.Count,
            answers.Count(a => a.Status == AnswerStatus.Answered),
            answers.Count(a => a.Status == AnswerStatus.Unanswered));

        return Task.FromResult(answers);
    }

    private List<StudentAnswer> ParseFromQFormat(MatchCollection matches, AnswerSheetTemplate template)
    {
        var answers = new List<StudentAnswer>();

        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups[1].Value, out var questionNumber))
                continue;

            if (questionNumber < 1 || questionNumber > template.TotalQuestions)
                continue;

            var answerText = match.Groups[2].Value.Trim().ToUpperInvariant();
            var status = AnswerStatus.Answered;
            var confidence = 0.95;

            if (string.IsNullOrWhiteSpace(answerText))
            {
                status = AnswerStatus.Unanswered;
                answerText = string.Empty;
                confidence = 0.80;
            }
            else if (answerText == "?")
            {
                status = AnswerStatus.Unclear;
                answerText = string.Empty;
                confidence = 0.40;
            }
            else if (!template.AnswerOptions.Contains(answerText))
            {
                status = AnswerStatus.Unclear;
                confidence = 0.50;
            }

            answers.Add(new StudentAnswer
            {
                QuestionNumber = questionNumber,
                SelectedAnswer = answerText,
                Confidence = confidence,
                Status = status
            });
        }

        return answers;
    }

    private static List<StudentAnswer> ParseFromNumberedFormat(MatchCollection matches, AnswerSheetTemplate template)
    {
        var answers = new List<StudentAnswer>();

        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups[1].Value, out var questionNumber))
                continue;

            if (questionNumber < 1 || questionNumber > template.TotalQuestions)
                continue;

            var answerText = match.Groups[2].Value.Trim().ToUpperInvariant();

            answers.Add(new StudentAnswer
            {
                QuestionNumber = questionNumber,
                SelectedAnswer = answerText,
                Confidence = 0.90,
                Status = AnswerStatus.Answered
            });
        }

        return answers;
    }

    private List<StudentAnswer> ParseFromRegions(List<OcrRegion> regions, AnswerSheetTemplate template)
    {
        var answers = new List<StudentAnswer>();

        foreach (var region in regions)
        {
            var match = AnswerLineRegex().Match(region.Text);
            if (!match.Success) continue;

            if (!int.TryParse(match.Groups[1].Value, out var questionNumber))
                continue;

            if (questionNumber < 1 || questionNumber > template.TotalQuestions)
                continue;

            var answerText = match.Groups[2].Value.Trim().ToUpperInvariant();
            var status = AnswerStatus.Answered;

            if (string.IsNullOrWhiteSpace(answerText))
            {
                status = AnswerStatus.Unanswered;
                answerText = string.Empty;
            }
            else if (answerText == "?")
            {
                status = AnswerStatus.Unclear;
                answerText = string.Empty;
            }

            answers.Add(new StudentAnswer
            {
                QuestionNumber = questionNumber,
                SelectedAnswer = answerText,
                Confidence = region.Confidence,
                Status = status
            });
        }

        return answers;
    }

    /// <summary>
    /// Extracts the student name from OCR text, if present.
    /// </summary>
    public static string? ExtractStudentName(string rawText)
    {
        var match = StudentNameRegex().Match(rawText);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    /// <summary>
    /// Extracts the student ID from OCR text, if present.
    /// </summary>
    public static string? ExtractStudentId(string rawText)
    {
        var match = StudentIdRegex().Match(rawText);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
