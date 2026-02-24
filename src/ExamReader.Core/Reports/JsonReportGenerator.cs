using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamReader.Core.Reports;

public class JsonReportGenerator : IReportGenerator
{
    public string Format => "JSON";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Task<byte[]> GenerateAsync(ReportData data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var report = new
        {
            data.ReportTitle,
            GeneratedAt = data.GeneratedAt.ToString("o"),
            Summary = new
            {
                data.Analytics.TotalStudents,
                data.Analytics.ClassAverage,
                data.Analytics.Median,
                data.Analytics.StandardDeviation,
                data.Analytics.HighestScore,
                data.Analytics.LowestScore,
                data.Analytics.PassCount,
                data.Analytics.FailCount,
                data.Analytics.PassRate,
                data.Analytics.GradeDistribution
            },
            Students = data.Results.Select(r => new
            {
                r.StudentId,
                r.StudentName,
                r.Correct,
                r.Incorrect,
                r.Unanswered,
                r.RawScore,
                r.MaxScore,
                r.Percentage,
                r.LetterGrade,
                r.Passed,
                Answers = r.QuestionResults.Select(qr => new
                {
                    qr.QuestionNumber,
                    qr.StudentAnswer,
                    qr.CorrectAnswer,
                    qr.IsCorrect,
                    qr.PointsEarned,
                    qr.PointsPossible
                })
            }),
            Questions = data.Analytics.QuestionStats.Select(q => new
            {
                q.QuestionNumber,
                q.CorrectAnswer,
                q.DifficultyIndex,
                q.DiscriminationIndex,
                q.AnswerDistribution,
                q.MostCommonWrongAnswer,
                q.FlaggedForReview,
                q.FlagReason
            })
        };

        var json = JsonSerializer.Serialize(report, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Task.FromResult(bytes);
    }
}
