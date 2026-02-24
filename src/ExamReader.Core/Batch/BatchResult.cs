using ExamReader.Core.Grading;

namespace ExamReader.Core.Batch;

public class BatchResult
{
    public string BatchId { get; set; } = Guid.NewGuid().ToString();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<GradingResult> Results { get; set; } = new();
    public List<BatchError> Errors { get; set; } = new();
    public bool HasErrors => Errors.Count > 0;
}

public class BatchError
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}
