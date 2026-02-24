namespace ExamReader.Core.Reports;

public interface IReportGenerator
{
    string Format { get; }
    Task<byte[]> GenerateAsync(ReportData data, CancellationToken cancellationToken = default);
}
