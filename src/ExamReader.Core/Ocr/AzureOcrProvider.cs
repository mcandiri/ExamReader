using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamReader.Core.Ocr;

public class AzureOcrProvider : IOcrProvider
{
    private readonly ILogger<AzureOcrProvider> _logger;
    private readonly string? _endpoint;
    private readonly string? _apiKey;

    public string ProviderName => "Azure Computer Vision";

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_endpoint) && !string.IsNullOrWhiteSpace(_apiKey);

    public AzureOcrProvider(IConfiguration configuration, ILogger<AzureOcrProvider> logger)
    {
        _logger = logger;
        _endpoint = configuration["Azure:Vision:Endpoint"];
        _apiKey = configuration["Azure:Vision:ApiKey"];

        if (!IsAvailable)
        {
            _logger.LogWarning("Azure Computer Vision is not configured. Set Azure:Vision:Endpoint and Azure:Vision:ApiKey in configuration.");
        }
    }

    public async Task<OcrResult> ProcessImageAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return new OcrResult
            {
                Success = false,
                ErrorMessage = "Azure Computer Vision is not configured. Missing endpoint or API key.",
                ProviderUsed = ProviderName
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing image with Azure Computer Vision ({ByteCount} bytes)", imageData.Length);

            var credential = new Azure.AzureKeyCredential(_apiKey!);
            var client = new Azure.AI.Vision.ImageAnalysis.ImageAnalysisClient(new Uri(_endpoint!), credential);

            var data = BinaryData.FromBytes(imageData);
            var result = await client.AnalyzeAsync(
                data,
                Azure.AI.Vision.ImageAnalysis.VisualFeatures.Read,
                cancellationToken: cancellationToken);

            var ocrResult = new OcrResult
            {
                Success = true,
                ProviderUsed = ProviderName,
                ProcessingTime = stopwatch.Elapsed
            };

            var readResult = result.Value.Read;
            var lineNumber = 0;
            var textLines = new List<string>();

            foreach (var block in readResult.Blocks)
            {
                foreach (var line in block.Lines)
                {
                    lineNumber++;
                    textLines.Add(line.Text);

                    var points = line.BoundingPolygon;
                    var minX = points.Min(p => p.X);
                    var minY = points.Min(p => p.Y);
                    var maxX = points.Max(p => p.X);
                    var maxY = points.Max(p => p.Y);

                    ocrResult.Regions.Add(new OcrRegion
                    {
                        Text = line.Text,
                        Confidence = line.Words.Average(w => w.Confidence),
                        LineNumber = lineNumber,
                        BoundingBox = new BoundingBox
                        {
                            X = minX,
                            Y = minY,
                            Width = maxX - minX,
                            Height = maxY - minY
                        }
                    });
                }
            }

            ocrResult.RawText = string.Join("\n", textLines);
            ocrResult.OverallConfidence = ocrResult.Regions.Count > 0
                ? ocrResult.Regions.Average(r => r.Confidence)
                : 0;

            _logger.LogInformation("Azure OCR completed: {RegionCount} regions, confidence {Confidence:P1}", ocrResult.Regions.Count, ocrResult.OverallConfidence);

            return ocrResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Computer Vision processing failed");
            stopwatch.Stop();

            return new OcrResult
            {
                Success = false,
                ErrorMessage = $"Azure OCR failed: {ex.Message}",
                ProviderUsed = ProviderName,
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<OcrResult> ProcessImageAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        return await ProcessImageAsync(memoryStream.ToArray(), cancellationToken);
    }
}
