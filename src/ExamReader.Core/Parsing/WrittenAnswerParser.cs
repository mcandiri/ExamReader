using System.Text.RegularExpressions;
using ExamReader.Core.Models;
using ExamReader.Core.Ocr;
using Microsoft.Extensions.Logging;

namespace ExamReader.Core.Parsing;

public partial class WrittenAnswerParser : IAnswerSheetParser
{
    private readonly ILogger<WrittenAnswerParser> _logger;

    // Pattern: "Q1:" or "Question 1:" followed by answer text
    [GeneratedRegex(@"(?:Q|Question)\s*(\d+)\s*[:\.\)]\s*(.+?)(?=(?:Q|Question)\s*\d+\s*[:\.\)]|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex QuestionBlockRegex();

    // Pattern: numbered answer "1. answer text" or "1) answer text"
    [GeneratedRegex(@"^(\d+)\s*[:\.\)]\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex NumberedAnswerRegex();

    // Pattern for answer blocks separated by line breaks
    [GeneratedRegex(@"^Answer\s*(\d+)\s*:\s*(.+?)$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex AnswerBlockRegex();

    public WrittenAnswerParser(ILogger<WrittenAnswerParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(AnswerSheetTemplate template)
    {
        return template.Format is ExamFormat.WrittenAnswer or ExamFormat.Mixed;
    }

    public Task<List<StudentAnswer>> ParseAsync(OcrResult ocrResult, AnswerSheetTemplate template, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var answers = new List<StudentAnswer>();
        var rawText = ocrResult.RawText;

        _logger.LogInformation("Parsing written answer sheet ({TextLength} chars)", rawText.Length);

        // Try "Q1: answer" or "Question 1: answer" format
        var questionMatches = QuestionBlockRegex().Matches(rawText);
        if (questionMatches.Count > 0)
        {
            answers = ParseQuestionBlocks(questionMatches, ocrResult.Regions, template);
        }

        // Try "Answer 1: text" format
        if (answers.Count == 0)
        {
            var answerMatches = AnswerBlockRegex().Matches(rawText);
            if (answerMatches.Count > 0)
            {
                answers = ParseAnswerBlocks(answerMatches, template);
            }
        }

        // Try simple numbered format: "1. answer text"
        if (answers.Count == 0)
        {
            var numberedMatches = NumberedAnswerRegex().Matches(rawText);
            if (numberedMatches.Count > 0)
            {
                answers = ParseNumberedAnswers(numberedMatches, template);
            }
        }

        // Fill missing questions as unanswered
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

        _logger.LogInformation("Parsed {Count} written answers ({Answered} answered)",
            answers.Count, answers.Count(a => a.Status == AnswerStatus.Answered));

        return Task.FromResult(answers);
    }

    private List<StudentAnswer> ParseQuestionBlocks(MatchCollection matches, List<OcrRegion> regions, AnswerSheetTemplate template)
    {
        var answers = new List<StudentAnswer>();

        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups[1].Value, out var questionNumber))
                continue;

            if (questionNumber < 1 || questionNumber > template.TotalQuestions)
                continue;

            var answerText = match.Groups[2].Value.Trim();
            var confidence = CalculateWrittenAnswerConfidence(answerText, regions, questionNumber);

            answers.Add(new StudentAnswer
            {
                QuestionNumber = questionNumber,
                SelectedAnswer = answerText,
                Confidence = confidence,
                Status = string.IsNullOrWhiteSpace(answerText) ? AnswerStatus.Unanswered : AnswerStatus.Answered
            });
        }

        return answers;
    }

    private static List<StudentAnswer> ParseAnswerBlocks(MatchCollection matches, AnswerSheetTemplate template)
    {
        var answers = new List<StudentAnswer>();

        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups[1].Value, out var questionNumber))
                continue;

            if (questionNumber < 1 || questionNumber > template.TotalQuestions)
                continue;

            var answerText = match.Groups[2].Value.Trim();

            answers.Add(new StudentAnswer
            {
                QuestionNumber = questionNumber,
                SelectedAnswer = answerText,
                Confidence = string.IsNullOrWhiteSpace(answerText) ? 0.5 : 0.80,
                Status = string.IsNullOrWhiteSpace(answerText) ? AnswerStatus.Unanswered : AnswerStatus.Answered
            });
        }

        return answers;
    }

    private static List<StudentAnswer> ParseNumberedAnswers(MatchCollection matches, AnswerSheetTemplate template)
    {
        var answers = new List<StudentAnswer>();

        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups[1].Value, out var questionNumber))
                continue;

            if (questionNumber < 1 || questionNumber > template.TotalQuestions)
                continue;

            var answerText = match.Groups[2].Value.Trim();

            answers.Add(new StudentAnswer
            {
                QuestionNumber = questionNumber,
                SelectedAnswer = answerText,
                Confidence = 0.75,
                Status = string.IsNullOrWhiteSpace(answerText) ? AnswerStatus.Unanswered : AnswerStatus.Answered
            });
        }

        return answers;
    }

    /// <summary>
    /// Calculates confidence for a written answer based on OCR region data.
    /// </summary>
    private static double CalculateWrittenAnswerConfidence(string answerText, List<OcrRegion> regions, int questionNumber)
    {
        if (string.IsNullOrWhiteSpace(answerText))
            return 0.5;

        // Find matching regions to get OCR confidence
        var matchingRegions = regions
            .Where(r => r.Text.Contains(answerText, StringComparison.OrdinalIgnoreCase)
                        || answerText.Contains(r.Text, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingRegions.Count > 0)
        {
            return matchingRegions.Average(r => r.Confidence);
        }

        // Default confidence based on text characteristics
        if (answerText.Length < 2)
            return 0.60; // Very short answers may be partial reads
        if (answerText.Length > 200)
            return 0.70; // Very long answers may have accumulated errors

        return 0.80;
    }
}
