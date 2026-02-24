using ExamReader.Core.Grading;
using ExamReader.Core.Models;

namespace ExamReader.Core.Analytics;

public interface IExamAnalyzer
{
    ExamAnalytics Analyze(List<GradingResult> results, AnswerKey answerKey);
    Task<ExamAnalytics> AnalyzeAsync(List<GradingResult> results, AnswerKey answerKey, CancellationToken cancellationToken = default);
}
