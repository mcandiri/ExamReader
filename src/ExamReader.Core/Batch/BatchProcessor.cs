using ExamReader.Core.Grading;
using ExamReader.Core.Models;
using Microsoft.Extensions.Logging;

namespace ExamReader.Core.Batch;

public class BatchProcessor : IBatchProcessor
{
    private readonly IGradingEngine _gradingEngine;
    private readonly ILogger<BatchProcessor> _logger;

    public event EventHandler<BatchProgressEventArgs>? ProgressChanged;

    public BatchProcessor(IGradingEngine gradingEngine, ILogger<BatchProcessor> logger)
    {
        _gradingEngine = gradingEngine;
        _logger = logger;
    }

    public BatchResult ProcessBatch(
        List<AnswerSheet> answerSheets,
        AnswerKey answerKey,
        GradingOptions options)
    {
        var batchResult = new BatchResult
        {
            StartedAt = DateTime.UtcNow,
            TotalProcessed = answerSheets.Count
        };

        var progress = new BatchProgress
        {
            TotalStudents = answerSheets.Count
        };

        _logger.LogInformation("Starting batch processing of {Count} answer sheets", answerSheets.Count);

        foreach (var sheet in answerSheets)
        {
            try
            {
                progress.CurrentStudentName = sheet.StudentName;
                progress.StatusMessage = $"Grading {sheet.StudentName}...";
                OnProgressChanged(progress);

                var result = _gradingEngine.GradeStudent(sheet.ExtractedAnswers, answerKey, options);
                result.StudentId = sheet.StudentId;
                result.StudentName = sheet.StudentName;

                batchResult.Results.Add(result);
                batchResult.SuccessCount++;
                progress.SuccessCount++;

                _logger.LogDebug("Graded {StudentName}: {Percentage}%", sheet.StudentName, result.Percentage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error grading student {StudentId}: {StudentName}", sheet.StudentId, sheet.StudentName);

                batchResult.Errors.Add(new BatchError
                {
                    StudentId = sheet.StudentId,
                    StudentName = sheet.StudentName,
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                batchResult.ErrorCount++;
                progress.ErrorCount++;
            }
            finally
            {
                progress.ProcessedStudents++;
            }
        }

        batchResult.CompletedAt = DateTime.UtcNow;

        progress.StatusMessage = progress.IsComplete ? "Batch processing complete." : "Batch processing finished with errors.";
        OnProgressChanged(progress);

        _logger.LogInformation(
            "Batch complete: {Success} succeeded, {Errors} failed in {Duration}ms",
            batchResult.SuccessCount, batchResult.ErrorCount, batchResult.Duration.TotalMilliseconds);

        return batchResult;
    }

    public async Task<BatchResult> ProcessBatchAsync(
        List<AnswerSheet> answerSheets,
        AnswerKey answerKey,
        GradingOptions options,
        CancellationToken cancellationToken = default)
    {
        var batchResult = new BatchResult
        {
            StartedAt = DateTime.UtcNow,
            TotalProcessed = answerSheets.Count
        };

        var progress = new BatchProgress
        {
            TotalStudents = answerSheets.Count
        };

        _logger.LogInformation("Starting async batch processing of {Count} answer sheets", answerSheets.Count);

        foreach (var sheet in answerSheets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                progress.CurrentStudentName = sheet.StudentName;
                progress.StatusMessage = $"Grading {sheet.StudentName}...";
                OnProgressChanged(progress);

                var result = await _gradingEngine.GradeStudentAsync(
                    sheet.ExtractedAnswers, answerKey, options, cancellationToken);
                result.StudentId = sheet.StudentId;
                result.StudentName = sheet.StudentName;

                batchResult.Results.Add(result);
                batchResult.SuccessCount++;
                progress.SuccessCount++;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Batch processing cancelled at student {StudentName}", sheet.StudentName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error grading student {StudentId}: {StudentName}", sheet.StudentId, sheet.StudentName);

                batchResult.Errors.Add(new BatchError
                {
                    StudentId = sheet.StudentId,
                    StudentName = sheet.StudentName,
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
                batchResult.ErrorCount++;
                progress.ErrorCount++;
            }
            finally
            {
                progress.ProcessedStudents++;
            }
        }

        batchResult.CompletedAt = DateTime.UtcNow;

        progress.StatusMessage = progress.IsComplete ? "Batch processing complete." : "Batch processing finished with errors.";
        OnProgressChanged(progress);

        _logger.LogInformation(
            "Async batch complete: {Success} succeeded, {Errors} failed in {Duration}ms",
            batchResult.SuccessCount, batchResult.ErrorCount, batchResult.Duration.TotalMilliseconds);

        return batchResult;
    }

    private void OnProgressChanged(BatchProgress progress)
    {
        ProgressChanged?.Invoke(this, new BatchProgressEventArgs(progress));
    }
}
