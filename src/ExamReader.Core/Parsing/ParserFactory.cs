using ExamReader.Core.Models;
using Microsoft.Extensions.Logging;

namespace ExamReader.Core.Parsing;

public class ParserFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ParserFactory> _logger;

    public ParserFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ParserFactory>();
    }

    /// <summary>
    /// Returns the appropriate parser for the given answer sheet template.
    /// </summary>
    public IAnswerSheetParser GetParser(AnswerSheetTemplate template)
    {
        var parsers = GetAllParsers();

        foreach (var parser in parsers)
        {
            if (parser.CanParse(template))
            {
                _logger.LogInformation("Selected parser {ParserType} for format {Format}",
                    parser.GetType().Name, template.Format);
                return parser;
            }
        }

        _logger.LogWarning("No specific parser found for format {Format}, falling back to BubbleSheetParser", template.Format);
        return new BubbleSheetParser(_loggerFactory.CreateLogger<BubbleSheetParser>());
    }

    /// <summary>
    /// Returns all available parsers.
    /// </summary>
    public IReadOnlyList<IAnswerSheetParser> GetAllParsers()
    {
        return new List<IAnswerSheetParser>
        {
            new BubbleSheetParser(_loggerFactory.CreateLogger<BubbleSheetParser>()),
            new GridParser(_loggerFactory.CreateLogger<GridParser>()),
            new WrittenAnswerParser(_loggerFactory.CreateLogger<WrittenAnswerParser>())
        };
    }
}
