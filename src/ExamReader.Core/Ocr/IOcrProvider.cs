namespace ExamReader.Core.Ocr;

public interface IOcrProvider
{
    string ProviderName { get; }
    bool IsAvailable { get; }
    Task<OcrResult> ProcessImageAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<OcrResult> ProcessImageAsync(Stream imageStream, CancellationToken cancellationToken = default);
}
