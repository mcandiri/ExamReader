namespace ExamReader.Core.Ocr;

public class OcrResult
{
    public bool Success { get; set; }
    public string RawText { get; set; } = string.Empty;
    public List<OcrRegion> Regions { get; set; } = new();
    public double OverallConfidence { get; set; }
    public string ProviderUsed { get; set; } = string.Empty;
    public TimeSpan ProcessingTime { get; set; }
    public string? ErrorMessage { get; set; }
}
