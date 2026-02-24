using ExamReader.Core.Analytics;
using ExamReader.Core.Batch;
using ExamReader.Core.Demo;
using ExamReader.Core.Grading;
using ExamReader.Core.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExamReader.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExamReaderCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Grading
        services.AddScoped<IGradingEngine, GradingEngine>();

        // Batch processing
        services.AddScoped<IBatchProcessor, BatchProcessor>();

        // Analytics
        services.AddScoped<IExamAnalyzer, ExamAnalyzer>();

        // Report generators
        services.AddScoped<IReportGenerator, HtmlReportGenerator>();
        services.AddScoped<IReportGenerator, JsonReportGenerator>();
        services.AddScoped<IReportGenerator, CsvReportGenerator>();

        // Demo data
        services.AddSingleton<DemoDataProvider>();

        return services;
    }
}
