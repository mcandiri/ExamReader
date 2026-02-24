using System.Text.RegularExpressions;
using ExamReader.Core.Models;
using ExamReader.Core.Ocr;
using Microsoft.Extensions.Logging;

namespace ExamReader.Core.Parsing;

public partial class GridParser : IAnswerSheetParser
{
    private readonly ILogger<GridParser> _logger;

    // Pattern: row with question number followed by marked cells
    // e.g., "1  X _ _ _" or "1  _ X _ _" where X=selected, _=empty
    [GeneratedRegex(@"^(\d+)\s+([\sXxOo_\.\-\|]+)$", RegexOptions.Multiline)]
    private static partial Regex GridRowRegex();

    // Pattern: "1: A B [C] D" where brackets indicate selected
    [GeneratedRegex(@"^(\d+)\s*:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex LabeledGridRowRegex();

    // Pattern to detect selected option in bracketed format
    [GeneratedRegex(@"\[([A-Da-d])\]")]
    private static partial Regex BracketedSelectionRegex();

    public GridParser(ILogger<GridParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(AnswerSheetTemplate template)
    {
        return template.Format == ExamFormat.GridBased;
    }

    public Task<List<StudentAnswer>> ParseAsync(OcrResult ocrResult, AnswerSheetTemplate template, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var answers = new List<StudentAnswer>();
        var rawText = ocrResult.RawText;

        _logger.LogInformation("Parsing grid-based answer sheet ({TextLength} chars)", rawText.Length);

        // Try labeled grid format first: "1: A B [C] D"
        var labeledMatches = LabeledGridRowRegex().Matches(rawText);
        if (labeledMatches.Count > 0)
        {
            answers = ParseLabeledGrid(labeledMatches, template);
        }

        // Try X-mark grid format: "1  _ X _ _"
        if (answers.Count == 0)
        {
            var gridMatches = GridRowRegex().Matches(rawText);
            if (gridMatches.Count > 0)
            {
                answers = ParseXMarkGrid(gridMatches, template);
            }
        }

        // Try region-based detection for grid layouts
        if (answers.Count == 0 && ocrResult.Regions.Count > 0)
        {
            answers = ParseFromGridRegions(ocrResult.Regions, template);
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

        _logger.LogInformation("Parsed {Count} answers from grid sheet", answers.Count);

        return Task.FromResult(answers);
    }

    private List<StudentAnswer> ParseLabeledGrid(MatchCollection matches, AnswerSheetTemplate template)
    {
        var answers = new List<StudentAnswer>();

        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups[1].Value, out var questionNumber))
                continue;

            if (questionNumber < 1 || questionNumber > template.TotalQuestions)
                continue;

            var rowContent = match.Groups[2].Value;
            var bracketMatch = BracketedSelectionRegex().Match(rowContent);

            if (bracketMatch.Success)
            {
                answers.Add(new StudentAnswer
                {
                    QuestionNumber = questionNumber,
                    SelectedAnswer = bracketMatch.Groups[1].Value.ToUpperInvariant(),
                    Confidence = 0.90,
                    Status = AnswerStatus.Answered
                });
            }
            else
            {
                answers.Add(new StudentAnswer
                {
                    QuestionNumber = questionNumber,
                    SelectedAnswer = string.Empty,
                    Confidence = 0.70,
                    Status = AnswerStatus.Unanswered
                });
            }
        }

        return answers;
    }

    private List<StudentAnswer> ParseXMarkGrid(MatchCollection matches, AnswerSheetTemplate template)
    {
        var answers = new List<StudentAnswer>();

        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups[1].Value, out var questionNumber))
                continue;

            if (questionNumber < 1 || questionNumber > template.TotalQuestions)
                continue;

            var cells = match.Groups[2].Value;
            var selectedIndex = FindMarkedCell(cells);

            if (selectedIndex >= 0 && selectedIndex < template.AnswerOptions.Count)
            {
                answers.Add(new StudentAnswer
                {
                    QuestionNumber = questionNumber,
                    SelectedAnswer = template.AnswerOptions[selectedIndex],
                    Confidence = 0.85,
                    Status = AnswerStatus.Answered
                });
            }
            else
            {
                answers.Add(new StudentAnswer
                {
                    QuestionNumber = questionNumber,
                    SelectedAnswer = string.Empty,
                    Confidence = 0.60,
                    Status = selectedIndex == -2 ? AnswerStatus.MultipleMarks : AnswerStatus.Unanswered
                });
            }
        }

        return answers;
    }

    private List<StudentAnswer> ParseFromGridRegions(List<OcrRegion> regions, AnswerSheetTemplate template)
    {
        var answers = new List<StudentAnswer>();

        // Group regions by approximate Y position to identify rows
        var sortedRegions = regions
            .Where(r => !string.IsNullOrWhiteSpace(r.Text))
            .OrderBy(r => r.BoundingBox.Y)
            .ThenBy(r => r.BoundingBox.X)
            .ToList();

        foreach (var region in sortedRegions)
        {
            var labeledMatch = LabeledGridRowRegex().Match(region.Text);
            if (!labeledMatch.Success) continue;

            if (!int.TryParse(labeledMatch.Groups[1].Value, out var questionNumber))
                continue;

            if (questionNumber < 1 || questionNumber > template.TotalQuestions)
                continue;

            var bracketMatch = BracketedSelectionRegex().Match(labeledMatch.Groups[2].Value);
            answers.Add(new StudentAnswer
            {
                QuestionNumber = questionNumber,
                SelectedAnswer = bracketMatch.Success ? bracketMatch.Groups[1].Value.ToUpperInvariant() : string.Empty,
                Confidence = region.Confidence,
                Status = bracketMatch.Success ? AnswerStatus.Answered : AnswerStatus.Unanswered
            });
        }

        return answers;
    }

    /// <summary>
    /// Finds the index of the marked cell in an X-mark grid row.
    /// Returns -1 if none found, -2 if multiple marks.
    /// </summary>
    private static int FindMarkedCell(string cells)
    {
        var markedPositions = new List<int>();
        var parts = cells.Split(new[] { ' ', '\t', '|' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            var cell = parts[i].Trim().ToUpperInvariant();
            if (cell is "X" or "O")
            {
                markedPositions.Add(i);
            }
        }

        return markedPositions.Count switch
        {
            1 => markedPositions[0],
            0 => -1,
            _ => -2
        };
    }
}
