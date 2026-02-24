using ExamReader.Core.Ocr;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ExamReader.Core.Tests.Ocr;

public class OcrProviderFactoryTests
{
    private readonly OcrProviderFactory _factory;

    public OcrProviderFactoryTests()
    {
        // Empty configuration - no Azure or Tesseract keys configured
        var configuration = new ConfigurationBuilder().Build();
        var loggerFactory = NullLoggerFactory.Instance;
        _factory = new OcrProviderFactory(configuration, loggerFactory);
    }

    private static IConfiguration BuildConfigFromJson(Dictionary<string, string?> settings)
    {
        // Convert flat key:value pairs (with colon-separated paths) to nested JSON
        var jsonObj = new Dictionary<string, object>();
        foreach (var kvp in settings)
        {
            var parts = kvp.Key.Split(':');
            if (parts.Length == 2)
            {
                if (!jsonObj.ContainsKey(parts[0]))
                    jsonObj[parts[0]] = new Dictionary<string, object>();
                ((Dictionary<string, object>)jsonObj[parts[0]])[parts[1]] = kvp.Value ?? "";
            }
            else
            {
                jsonObj[kvp.Key] = kvp.Value ?? "";
            }
        }

        var json = JsonSerializer.Serialize(jsonObj);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
    }

    [Fact]
    public void GetProvider_WithNoKeysConfigured_ShouldReturnDemoOcrProvider()
    {
        var provider = _factory.GetProvider();

        provider.Should().NotBeNull();
        provider.Should().BeOfType<DemoOcrProvider>();
    }

    [Fact]
    public void GetProvider_ShouldReturnAvailableProvider()
    {
        var provider = _factory.GetProvider();

        provider.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void GetProvider_ShouldReturnNonNullProvider()
    {
        var provider = _factory.GetProvider();

        provider.Should().NotBeNull();
    }

    [Fact]
    public void GetProvider_WithDemoPreferred_ShouldReturnDemoProvider()
    {
        var configuration = BuildConfigFromJson(new Dictionary<string, string?>
        {
            { "Ocr:PreferredProvider", "demo" }
        });
        var factory = new OcrProviderFactory(configuration, NullLoggerFactory.Instance);

        var provider = factory.GetProvider();

        provider.Should().BeOfType<DemoOcrProvider>();
    }

    [Fact]
    public void GetProvider_WithInvalidPreferred_ShouldFallbackToDemoProvider()
    {
        var configuration = BuildConfigFromJson(new Dictionary<string, string?>
        {
            { "Ocr:PreferredProvider", "nonexistent" }
        });
        var factory = new OcrProviderFactory(configuration, NullLoggerFactory.Instance);

        var provider = factory.GetProvider();

        // Should fall back through the chain to Demo
        provider.Should().BeOfType<DemoOcrProvider>();
    }

    [Fact]
    public void GetAllProviders_ShouldReturnThreeProviders()
    {
        var providers = _factory.GetAllProviders();

        providers.Count.Should().Be(3);
    }

    [Fact]
    public void GetAllProviders_ShouldContainDemoProvider()
    {
        var providers = _factory.GetAllProviders();

        providers.Should().Contain(p => p is DemoOcrProvider);
    }

    [Fact]
    public void GetProviderByName_Demo_ShouldReturnDemoProvider()
    {
        var provider = _factory.GetProviderByName("demo");

        provider.Should().NotBeNull();
        provider.Should().BeOfType<DemoOcrProvider>();
    }

    [Fact]
    public void GetProviderByName_Azure_ShouldReturnAzureProvider()
    {
        var provider = _factory.GetProviderByName("azure");

        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureOcrProvider>();
    }

    [Fact]
    public void GetProviderByName_Tesseract_ShouldReturnTesseractProvider()
    {
        var provider = _factory.GetProviderByName("tesseract");

        provider.Should().NotBeNull();
        provider.Should().BeOfType<TesseractOcrProvider>();
    }

    [Fact]
    public void GetProviderByName_Unknown_ShouldReturnNull()
    {
        var provider = _factory.GetProviderByName("unknown");

        provider.Should().BeNull();
    }

    [Fact]
    public void GetProvider_FallbackChain_AzureUnavailable_TesseractUnavailable_ShouldReturnDemo()
    {
        // With no configuration, Azure and Tesseract won't be available
        var provider = _factory.GetProvider();

        provider.Should().BeOfType<DemoOcrProvider>();
        provider.ProviderName.Should().Be("Demo OCR");
    }
}
