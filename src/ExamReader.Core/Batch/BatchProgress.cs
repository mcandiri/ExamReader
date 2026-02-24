namespace ExamReader.Core.Batch;

public class BatchProgress
{
    public int TotalStudents { get; set; }
    public int ProcessedStudents { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string CurrentStudentName { get; set; } = string.Empty;
    public double PercentComplete => TotalStudents > 0
        ? Math.Round((double)ProcessedStudents / TotalStudents * 100, 1)
        : 0;
    public bool IsComplete => ProcessedStudents >= TotalStudents;
    public string StatusMessage { get; set; } = string.Empty;
}

public class BatchProgressEventArgs : EventArgs
{
    public BatchProgress Progress { get; }

    public BatchProgressEventArgs(BatchProgress progress)
    {
        Progress = progress;
    }
}
