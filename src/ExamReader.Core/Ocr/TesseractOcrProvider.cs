using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamReader.Core.Ocr;

public class TesseractOcrProvider : IOcrProvider
{
    private readonly ILogger<TesseractOcrProvider> _logger;
    private readonly string _tessDataPath;
    private readonly string _language;
    private bool? _isAvailableCache;

    public string ProviderName => "Tesseract OCR";

    public bool IsAvailable
    {
        get
        {
            _isAvailableCache ??= CheckTesseractAvailability();
            return _isAvailableCache.Value;
        }
    }

    public TesseractOcrProvider(IConfiguration configuration, ILogger<TesseractOcrProvider> logger)
    {
        _logger = logger;
        _tessDataPath = configuration["Tesseract:TessDataPath"] ?? GetDefaultTessDataPath();
        _language = configuration["Tesseract:Language"] ?? "eng";
    }

    public async Task<OcrResult> ProcessImageAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return new OcrResult
            {
                Success = false,
                ErrorMessage = "Tesseract OCR is not available. Ensure Tesseract is installed and tessdata path is configured.",
                ProviderUsed = ProviderName
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing image with Tesseract OCR ({ByteCount} bytes)", imageData.Length);

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var engine = new Tesseract.TesseractEngine(_tessDataPath, _language, Tesseract.EngineMode.Default);
                using var pix = Tesseract.Pix.LoadFromMemory(imageData);
                using var page = engine.Process(pix);

                var ocrResult = new OcrResult
                {
                    Success = true,
                    RawText = page.GetText(),
                    OverallConfidence = page.GetMeanConfidence(),
                    ProviderUsed = ProviderName,
                    ProcessingTime = stopwatch.Elapsed
                };

                using var iter = page.GetIterator();
                iter.Begin();
                var lineNumber = 0;

                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (iter.TryGetBoundingBox(Tesseract.PageIteratorLevel.TextLine, out var bounds))
                    {
                        var lineText = iter.GetText(Tesseract.PageIteratorLevel.TextLine);
                        if (!string.IsNullOrWhiteSpace(lineText))
                        {
                            lineNumber++;
                            ocrResult.Regions.Add(new OcrRegion
                            {
                                Text = lineText.Trim(),
                                Confidence = iter.GetConfidence(Tesseract.PageIteratorLevel.TextLine) / 100.0,
                                LineNumber = lineNumber,
                                BoundingBox = new BoundingBox
                                {
                                    X = bounds.X1,
                                    Y = bounds.Y1,
                                    Width = bounds.Width,
                                    Height = bounds.Height
                                }
                            });
                        }
                    }
                } while (iter.Next(Tesseract.PageIteratorLevel.TextLine));

                _logger.LogInformation("Tesseract OCR completed: {RegionCount} regions, confidence {Confidence:P1}", ocrResult.Regions.Count, ocrResult.OverallConfidence);

                return ocrResult;
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tesseract OCR processing failed");
            stopwatch.Stop();

            return new OcrResult
            {
                Success = false,
                ErrorMessage = $"Tesseract OCR failed: {ex.Message}",
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

    private bool CheckTesseractAvailability()
    {
        try
        {
            if (!Directory.Exists(_tessDataPath))
            {
                _logger.LogWarning("Tesseract data path not found: {Path}", _tessDataPath);
                return false;
            }

            var engTrainedData = Path.Combine(_tessDataPath, $"{_language}.traineddata");
            if (!File.Exists(engTrainedData))
            {
                _logger.LogWarning("Tesseract trained data not found: {Path}", engTrainedData);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check Tesseract availability");
            return false;
        }
    }

    private static string GetDefaultTessDataPath()
    {
        if (OperatingSystem.IsWindows())
            return @"C:\Program Files\Tesseract-OCR\tessdata";
        if (OperatingSystem.IsMacOS())
            return "/usr/local/share/tessdata";
        return "/usr/share/tesseract-ocr/5/tessdata";
    }
}
