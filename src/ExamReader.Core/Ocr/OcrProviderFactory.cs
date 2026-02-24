using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamReader.Core.Ocr;

public class OcrProviderFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<OcrProviderFactory> _logger;

    public OcrProviderFactory(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<OcrProviderFactory>();
    }

    /// <summary>
    /// Returns the best available OCR provider using the fallback chain:
    /// Azure -> Tesseract -> Demo.
    /// </summary>
    public IOcrProvider GetProvider()
    {
        var preferred = _configuration["Ocr:PreferredProvider"];

        if (!string.IsNullOrWhiteSpace(preferred))
        {
            var provider = GetProviderByName(preferred);
            if (provider is not null && provider.IsAvailable)
            {
                _logger.LogInformation("Using preferred OCR provider: {Provider}", provider.ProviderName);
                return provider;
            }

            _logger.LogWarning("Preferred OCR provider '{Preferred}' is not available, falling back", preferred);
        }

        // Fallback chain: Azure -> Tesseract -> Demo
        var azure = new AzureOcrProvider(_configuration, _loggerFactory.CreateLogger<AzureOcrProvider>());
        if (azure.IsAvailable)
        {
            _logger.LogInformation("Using Azure Computer Vision OCR provider");
            return azure;
        }

        var tesseract = new TesseractOcrProvider(_configuration, _loggerFactory.CreateLogger<TesseractOcrProvider>());
        if (tesseract.IsAvailable)
        {
            _logger.LogInformation("Using Tesseract OCR provider");
            return tesseract;
        }

        _logger.LogInformation("No production OCR provider available, using Demo OCR provider");
        return new DemoOcrProvider(_loggerFactory.CreateLogger<DemoOcrProvider>());
    }

    /// <summary>
    /// Returns all available OCR providers.
    /// </summary>
    public IReadOnlyList<IOcrProvider> GetAllProviders()
    {
        var providers = new List<IOcrProvider>
        {
            new AzureOcrProvider(_configuration, _loggerFactory.CreateLogger<AzureOcrProvider>()),
            new TesseractOcrProvider(_configuration, _loggerFactory.CreateLogger<TesseractOcrProvider>()),
            new DemoOcrProvider(_loggerFactory.CreateLogger<DemoOcrProvider>())
        };

        return providers;
    }

    /// <summary>
    /// Returns a specific provider by name, or null if not recognized.
    /// </summary>
    public IOcrProvider? GetProviderByName(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "azure" => new AzureOcrProvider(_configuration, _loggerFactory.CreateLogger<AzureOcrProvider>()),
            "tesseract" => new TesseractOcrProvider(_configuration, _loggerFactory.CreateLogger<TesseractOcrProvider>()),
            "demo" => new DemoOcrProvider(_loggerFactory.CreateLogger<DemoOcrProvider>()),
            _ => null
        };
    }
}
