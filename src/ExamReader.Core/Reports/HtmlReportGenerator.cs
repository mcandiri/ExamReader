using System.Globalization;
using System.Text;

namespace ExamReader.Core.Reports;

public class HtmlReportGenerator : IReportGenerator
{
    public string Format => "HTML";

    public Task<byte[]> GenerateAsync(ReportData data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"tr\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>{Encode(data.ReportTitle)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCss());
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine($"<h1>{Encode(data.ReportTitle)}</h1>");
        sb.AppendLine($"<p class=\"meta\">Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}</p>");

        // Summary section
        sb.AppendLine("<section class=\"summary\">");
        sb.AppendLine("<h2>Class Summary</h2>");
        sb.AppendLine("<div class=\"stats-grid\">");
        AppendStat(sb, "Total Students", data.Analytics.TotalStudents.ToString());
        AppendStat(sb, "Class Average", $"{data.Analytics.ClassAverage:F1}%");
        AppendStat(sb, "Median", $"{data.Analytics.Median:F1}%");
        AppendStat(sb, "Std Deviation", $"{data.Analytics.StandardDeviation:F1}");
        AppendStat(sb, "Highest Score", $"{data.Analytics.HighestScore:F1}%");
        AppendStat(sb, "Lowest Score", $"{data.Analytics.LowestScore:F1}%");
        AppendStat(sb, "Pass Rate", $"{data.Analytics.PassRate:F1}%");
        AppendStat(sb, "Pass / Fail", $"{data.Analytics.PassCount} / {data.Analytics.FailCount}");
        sb.AppendLine("</div>");
        sb.AppendLine("</section>");

        // Score distribution
        sb.AppendLine("<section class=\"distribution\">");
        sb.AppendLine("<h2>Score Distribution</h2>");
        sb.AppendLine("<div class=\"chart\">");
        int maxCount = data.Analytics.Distribution.Buckets.Max(b => b.Count);
        foreach (var bucket in data.Analytics.Distribution.Buckets)
        {
            int barWidth = maxCount > 0 ? (int)((double)bucket.Count / maxCount * 100) : 0;
            sb.AppendLine($"<div class=\"bar-row\">");
            sb.AppendLine($"  <span class=\"bar-label\">{bucket.Label}</span>");
            sb.AppendLine($"  <div class=\"bar\" style=\"width: {barWidth}%\"></div>");
            sb.AppendLine($"  <span class=\"bar-value\">{bucket.Count}</span>");
            sb.AppendLine($"</div>");
        }
        sb.AppendLine("</div>");
        sb.AppendLine("</section>");

        // Grade distribution
        sb.AppendLine("<section class=\"grades\">");
        sb.AppendLine("<h2>Grade Distribution</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Grade</th><th>Count</th></tr>");
        foreach (var grade in data.Analytics.GradeDistribution.OrderBy(g => g.Key))
        {
            sb.AppendLine($"<tr><td>{Encode(grade.Key)}</td><td>{grade.Value}</td></tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");

        // Student results
        sb.AppendLine("<section class=\"students\">");
        sb.AppendLine("<h2>Student Results</h2>");
        sb.AppendLine("<table class=\"results-table\">");
        sb.AppendLine("<tr><th>Rank</th><th>Student ID</th><th>Name</th><th>Correct</th><th>Wrong</th><th>Blank</th><th>Score</th><th>%</th><th>Grade</th><th>Status</th></tr>");

        var ranked = data.Results
            .OrderByDescending(r => r.Percentage)
            .ThenBy(r => r.StudentName)
            .ToList();

        for (int i = 0; i < ranked.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var r = ranked[i];
            string statusClass = r.Passed ? "pass" : "fail";
            sb.AppendLine($"<tr class=\"{statusClass}\">");
            sb.AppendLine($"  <td>{i + 1}</td>");
            sb.AppendLine($"  <td>{Encode(r.StudentId)}</td>");
            sb.AppendLine($"  <td>{Encode(r.StudentName)}</td>");
            sb.AppendLine($"  <td>{r.Correct}</td>");
            sb.AppendLine($"  <td>{r.Incorrect}</td>");
            sb.AppendLine($"  <td>{r.Unanswered}</td>");
            sb.AppendLine($"  <td>{r.RawScore.ToString("F1", CultureInfo.InvariantCulture)}</td>");
            sb.AppendLine($"  <td>{r.Percentage.ToString("F1", CultureInfo.InvariantCulture)}</td>");
            sb.AppendLine($"  <td><strong>{Encode(r.LetterGrade)}</strong></td>");
            sb.AppendLine($"  <td class=\"status-{statusClass}\">{(r.Passed ? "Pass" : "Fail")}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");

        // Question analysis
        sb.AppendLine("<section class=\"questions\">");
        sb.AppendLine("<h2>Question Analysis</h2>");
        sb.AppendLine("<table class=\"question-table\">");
        sb.AppendLine("<tr><th>Q#</th><th>Answer</th><th>Correct</th><th>Difficulty</th><th>Discrimination</th><th>Common Wrong</th><th>Flag</th></tr>");

        foreach (var q in data.Analytics.QuestionStats)
        {
            string flagClass = q.FlaggedForReview ? " class=\"flagged\"" : "";
            sb.AppendLine($"<tr{flagClass}>");
            sb.AppendLine($"  <td>{q.QuestionNumber}</td>");
            sb.AppendLine($"  <td>{Encode(q.CorrectAnswer)}</td>");
            sb.AppendLine($"  <td>{q.CorrectCount}/{q.TotalAttempts}</td>");
            sb.AppendLine($"  <td>{q.DifficultyIndex.ToString("F2", CultureInfo.InvariantCulture)}</td>");
            sb.AppendLine($"  <td>{q.DiscriminationIndex.ToString("F2", CultureInfo.InvariantCulture)}</td>");
            sb.AppendLine($"  <td>{Encode(q.MostCommonWrongAnswer)}</td>");
            sb.AppendLine($"  <td>{(q.FlaggedForReview ? Encode(q.FlagReason) : "-")}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");

        sb.AppendLine("<footer><p>ExamReader v2 Report</p></footer>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return Task.FromResult(bytes);
    }

    private static void AppendStat(StringBuilder sb, string label, string value)
    {
        sb.AppendLine($"<div class=\"stat\"><span class=\"stat-value\">{value}</span><span class=\"stat-label\">{label}</span></div>");
    }

    private static string Encode(string value)
    {
        return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static string GetCss()
    {
        return """
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body {
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                max-width: 1200px; margin: 0 auto; padding: 2rem;
                color: #1a1a2e; background: #f5f5f5;
            }
            h1 { font-size: 2rem; margin-bottom: 0.25rem; color: #16213e; }
            h2 { font-size: 1.4rem; margin-bottom: 1rem; color: #0f3460; border-bottom: 2px solid #e94560; padding-bottom: 0.5rem; }
            .meta { color: #666; margin-bottom: 2rem; }
            section { background: white; padding: 1.5rem; border-radius: 8px; margin-bottom: 1.5rem; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
            .stats-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 1rem; }
            .stat { text-align: center; padding: 1rem; background: #f8f9fa; border-radius: 6px; }
            .stat-value { display: block; font-size: 1.6rem; font-weight: 700; color: #0f3460; }
            .stat-label { display: block; font-size: 0.85rem; color: #666; margin-top: 0.25rem; }
            table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
            th { background: #16213e; color: white; padding: 0.6rem 0.8rem; text-align: left; }
            td { padding: 0.5rem 0.8rem; border-bottom: 1px solid #eee; }
            tr:hover { background: #f0f7ff; }
            .pass { }
            .fail { background: #fff5f5; }
            .status-pass { color: #2e7d32; font-weight: 600; }
            .status-fail { color: #c62828; font-weight: 600; }
            .flagged { background: #fff8e1; }
            .chart { padding: 0.5rem 0; }
            .bar-row { display: flex; align-items: center; margin-bottom: 0.4rem; }
            .bar-label { width: 60px; font-size: 0.85rem; text-align: right; padding-right: 0.5rem; }
            .bar { height: 22px; background: linear-gradient(90deg, #0f3460, #e94560); border-radius: 3px; min-width: 2px; transition: width 0.3s; }
            .bar-value { padding-left: 0.5rem; font-size: 0.85rem; font-weight: 600; }
            footer { text-align: center; padding: 1rem; color: #999; font-size: 0.85rem; }
            @media print {
                body { background: white; padding: 0; }
                section { box-shadow: none; break-inside: avoid; }
            }
            """;
    }
}
