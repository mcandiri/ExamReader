using ExamReader.Core.Ocr;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExamReader.Core.Tests.Ocr;

public class DemoOcrProviderTests
{
    private readonly DemoOcrProvider _provider;

    public DemoOcrProviderTests()
    {
        var logger = NullLogger<DemoOcrProvider>.Instance;
        _provider = new DemoOcrProvider(logger);
    }

    [Fact]
    public void IsAvailable_ShouldReturnTrue()
    {
        _provider.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void ProviderName_ShouldReturnDemoOcr()
    {
        _provider.ProviderName.Should().Be("Demo OCR");
    }

    [Fact]
    public async Task ProcessImageAsync_WithByteArray_ShouldReturnSuccessfulResult()
    {
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // Fake PNG header

        var result = await _provider.ProcessImageAsync(imageData);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ProcessImageAsync_WithStream_ShouldReturnSuccessfulResult()
    {
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        var result = await _provider.ProcessImageAsync(stream);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessImageAsync_ShouldReturnNonEmptyRawText()
    {
        var result = await _provider.ProcessImageAsync(new byte[] { 1, 2, 3 });

        result.RawText.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ProcessImageAsync_ShouldContainStudentNamePattern()
    {
        var result = await _provider.ProcessImageAsync(new byte[] { 1, 2, 3 });

        result.RawText.Should().Contain("Student Name:");
    }

    [Fact]
    public async Task ProcessImageAsync_ShouldContainStudentIdPattern()
    {
        var result = await _provider.ProcessImageAsync(new byte[] { 1, 2, 3 });

        result.RawText.Should().Contain("Student ID:");
    }

    [Fact]
    public async Task ProcessImageAsync_ShouldContainBubbleSheetAnswerPatterns()
    {
        var result = await _provider.ProcessImageAsync(new byte[] { 1, 2, 3 });

        // Should contain question answer patterns like Q1: [A]
        result.RawText.Should().Contain("Q1:");
        result.RawText.Should().Contain("Q30:");
    }

    [Fact]
    public async Task ProcessImageAsync_ShouldHaveReasonableConfidence()
    {
        var result = await _provider.ProcessImageAsync(new byte[] { 1, 2, 3 });

        result.OverallConfidence.Should().BeGreaterThan(0.8);
        result.OverallConfidence.Should().BeLessOrEqualTo(1.0);
    }

    [Fact]
    public async Task ProcessImageAsync_ShouldHaveRegions()
    {
        var result = await _provider.ProcessImageAsync(new byte[] { 1, 2, 3 });

        result.Regions.Should().NotBeEmpty();
        // 2 header lines + 1 separator + 30 answer lines = 33 regions
        result.Regions.Count.Should().BeGreaterOrEqualTo(30);
    }

    [Fact]
    public async Task ProcessImageAsync_ShouldSetProviderUsed()
    {
        var result = await _provider.ProcessImageAsync(new byte[] { 1, 2, 3 });

        result.ProviderUsed.Should().Be("Demo OCR");
    }

    [Fact]
    public async Task ProcessImageAsync_ShouldRecordProcessingTime()
    {
        var result = await _provider.ProcessImageAsync(new byte[] { 1, 2, 3 });

        result.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ProcessImageAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _provider.ProcessImageAsync(new byte[] { 1, 2, 3 }, cts.Token);

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task ProcessImageAsync_CalledMultipleTimes_ShouldReturnDifferentStudents()
    {
        var result1 = await _provider.ProcessImageAsync(new byte[] { 1 });
        var result2 = await _provider.ProcessImageAsync(new byte[] { 2 });

        // Each call should cycle to a different student
        result1.RawText.Should().NotBe(result2.RawText);
    }

    [Fact]
    public async Task ProcessImageAsync_RegionsShouldHaveBoundingBoxes()
    {
        var result = await _provider.ProcessImageAsync(new byte[] { 1, 2, 3 });

        foreach (var region in result.Regions)
        {
            region.BoundingBox.Should().NotBeNull();
            region.BoundingBox.Width.Should().BeGreaterThan(0);
            region.BoundingBox.Height.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task ProcessImageAsync_RegionsShouldHaveLineNumbers()
    {
        var result = await _provider.ProcessImageAsync(new byte[] { 1, 2, 3 });

        var lineNumbers = result.Regions.Select(r => r.LineNumber).ToList();
        lineNumbers.Should().BeInAscendingOrder();
        lineNumbers.First().Should().Be(1);
    }

    [Fact]
    public void GetAllDemoStudents_ShouldReturn25Students()
    {
        var students = DemoOcrProvider.GetAllDemoStudents();

        students.Count.Should().Be(25);
    }

    [Fact]
    public void GetAllDemoStudents_EachStudentShouldHave30Answers()
    {
        var students = DemoOcrProvider.GetAllDemoStudents();

        foreach (var student in students)
        {
            student.Answers.Length.Should().Be(30);
        }
    }

    [Fact]
    public void GetAllDemoStudents_ShouldHaveUniqueIds()
    {
        var students = DemoOcrProvider.GetAllDemoStudents();
        var ids = students.Select(s => s.Id).ToList();

        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GetAllDemoStudents_ShouldHaveTurkishNames()
    {
        var students = DemoOcrProvider.GetAllDemoStudents();

        students.Select(s => s.Name).Should().Contain(n => n.Contains("Yilmaz"));
        students.Select(s => s.Name).Should().Contain(n => n.Contains("Demir"));
        students.Select(s => s.Name).Should().Contain(n => n.Contains("Celik"));
    }
}
