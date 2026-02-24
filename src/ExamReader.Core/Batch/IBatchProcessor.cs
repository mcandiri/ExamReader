using ExamReader.Core.Grading;
using ExamReader.Core.Models;

namespace ExamReader.Core.Batch;

public interface IBatchProcessor
{
    event EventHandler<BatchProgressEventArgs>? ProgressChanged;

    BatchResult ProcessBatch(
        List<AnswerSheet> answerSheets,
        AnswerKey answerKey,
        GradingOptions options);

    Task<BatchResult> ProcessBatchAsync(
        List<AnswerSheet> answerSheets,
        AnswerKey answerKey,
        GradingOptions options,
        CancellationToken cancellationToken = default);
}
