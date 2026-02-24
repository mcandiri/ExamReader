using ExamReader.Core.Analytics;
using ExamReader.Core.Demo;
using ExamReader.Core.Grading;
using ExamReader.Core.Models;

namespace ExamReader.Web.Services;

public class DemoService
{
    private readonly DemoDataProvider _provider;
    private ExamDefinition? _exam;
    private List<GradingResult>? _results;
    private ExamAnalytics? _analytics;

    public DemoService(DemoDataProvider provider)
    {
        _provider = provider;
    }

    public ExamDefinition GetExamDefinition()
    {
        _exam ??= _provider.GetSampleExam();
        return _exam;
    }

    public GradingOptions GetGradingOptions()
    {
        return new GradingOptions
        {
            NegativeMarking = false,
            PassingScore = 60.0,
            GradeScale = LetterGradeScale.PlusMinus
        };
    }

    public List<GradingResult> GetResults()
    {
        _results ??= _provider.GetSampleResults();
        return _results;
    }

    public ExamAnalytics GetAnalytics()
    {
        _analytics ??= _provider.GetSampleAnalytics();
        return _analytics;
    }
}
