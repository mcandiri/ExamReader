using System.Globalization;
using System.Text;

namespace ExamReader.Core.Reports;

public class CsvReportGenerator : IReportGenerator
{
    public string Format => "CSV";

    public Task<byte[]> GenerateAsync(ReportData data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sb = new StringBuilder();

        // Determine question count
        int questionCount = data.AnswerKey.TotalQuestions;

        // Header row
        var headers = new List<string>
        {
            "Rank", "StudentId", "StudentName", "Score", "Percentage", "Grade", "Status"
        };
        for (int q = 1; q <= questionCount; q++)
        {
            headers.Add($"Q{q}");
        }
        sb.AppendLine(string.Join(",", headers));

        // Sort by percentage descending
        var ranked = data.Results
            .OrderByDescending(r => r.Percentage)
            .ThenBy(r => r.StudentName)
            .ToList();

        for (int i = 0; i < ranked.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var r = ranked[i];
            var fields = new List<string>
            {
                (i + 1).ToString(CultureInfo.InvariantCulture),
                EscapeCsv(r.StudentId),
                EscapeCsv(r.StudentName),
                r.RawScore.ToString("F2", CultureInfo.InvariantCulture),
                r.Percentage.ToString("F2", CultureInfo.InvariantCulture),
                EscapeCsv(r.LetterGrade),
                r.Passed ? "Pass" : "Fail"
            };

            // Add each question answer
            var answerLookup = r.QuestionResults.ToDictionary(qr => qr.QuestionNumber);
            for (int q = 1; q <= questionCount; q++)
            {
                if (answerLookup.TryGetValue(q, out var qr))
                {
                    string marker = qr.IsCorrect ? qr.StudentAnswer : $"{qr.StudentAnswer}*";
                    fields.Add(EscapeCsv(string.IsNullOrEmpty(qr.StudentAnswer) ? "-" : marker));
                }
                else
                {
                    fields.Add("-");
                }
            }

            sb.AppendLine(string.Join(",", fields));
        }

        // Add BOM for Excel compatibility with Turkish characters
        var preamble = Encoding.UTF8.GetPreamble();
        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + csvBytes.Length];
        preamble.CopyTo(result, 0);
        csvBytes.CopyTo(result, preamble.Length);

        return Task.FromResult(result);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
