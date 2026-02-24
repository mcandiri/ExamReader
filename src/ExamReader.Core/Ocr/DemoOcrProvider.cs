using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ExamReader.Core.Ocr;

public class DemoOcrProvider : IOcrProvider
{
    private readonly ILogger<DemoOcrProvider> _logger;
    private int _currentStudentIndex;

    public string ProviderName => "Demo OCR";
    public bool IsAvailable => true;

    private static readonly (string Name, string Id, string[] Answers)[] DemoStudents = new[]
    {
        ("Ahmet Yilmaz", "2024001", new[] { "A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B" }),
        ("Elif Kaya", "2024002", new[] { "A","B","C","D","A","B","C","A","A","B","C","D","A","C","C","D","A","B","C","D","A","B","D","D","A","B","C","D","A","B" }),
        ("Mehmet Demir", "2024003", new[] { "B","B","C","D","A","A","C","D","A","B","C","B","A","B","A","D","A","B","C","D","B","B","C","D","A","B","C","D","A","C" }),
        ("Zeynep Celik", "2024004", new[] { "A","B","C","D","B","B","C","D","A","A","C","D","A","B","C","D","A","D","C","D","A","B","C","D","A","A","C","D","B","B" }),
        ("Can Ozturk", "2024005", new[] { "A","C","C","D","A","B","B","D","A","B","C","D","C","B","C","D","A","B","C","A","A","B","C","D","A","B","D","D","A","B" }),
        ("Ayse Arslan", "2024006", new[] { "A","B","C","D","A","B","C","D","B","B","C","D","A","B","C","D","A","B","A","D","A","B","C","D","A","B","C","D","A","B" }),
        ("Burak Sahin", "2024007", new[] { "C","B","C","D","A","B","C","A","A","B","D","D","A","B","C","C","A","B","C","D","A","C","C","D","A","B","C","D","A","B" }),
        ("Selin Yildiz", "2024008", new[] { "A","B","D","D","A","B","C","D","A","C","C","D","A","B","C","D","B","B","C","D","A","B","C","A","A","B","C","D","A","D" }),
        ("Emre Tas", "2024009", new[] { "A","B","C","A","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B" }),
        ("Deniz Akin", "2024010", new[] { "A","A","C","D","A","C","C","D","A","B","A","D","A","B","C","D","A","B","C","D","A","B","C","D","B","B","C","D","A","A" }),
        ("Fatma Polat", "2024011", new[] { "D","B","C","D","A","B","C","D","A","B","C","D","A","D","C","D","A","B","C","D","A","B","B","D","A","B","C","C","A","B" }),
        ("Cem Erdogan", "2024012", new[] { "A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B" }),
        ("Merve Korkmaz", "2024013", new[] { "A","B","B","D","A","B","C","D","C","B","C","D","A","B","C","D","A","A","C","D","A","B","C","D","A","B","A","D","A","B" }),
        ("Onur Cetin", "2024014", new[] { "B","B","C","D","A","A","C","D","A","B","C","C","A","B","B","D","A","B","C","D","D","B","C","D","A","B","C","D","A","A" }),
        ("Gamze Kurt", "2024015", new[] { "A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B" }),
        ("Hakan Aydin", "2024016", new[] { "A","B","C","C","A","B","D","D","A","B","C","D","A","B","C","D","A","C","C","D","A","B","C","D","A","B","C","A","A","B" }),
        ("Irem Koc", "2024017", new[] { "A","D","C","D","A","B","C","D","A","B","C","D","B","B","C","D","A","B","C","D","A","A","C","D","A","B","C","D","C","B" }),
        ("Kaan Dogan", "2024018", new[] { "A","B","C","D","C","B","C","D","A","D","C","D","A","B","C","D","A","B","A","D","A","B","C","D","A","B","C","D","A","B" }),
        ("Tugba Kilic", "2024019", new[] { "A","B","C","D","A","B","C","D","A","B","D","D","A","B","C","D","A","B","C","A","A","B","C","D","A","C","C","D","A","B" }),
        ("Murat Sen", "2024020", new[] { "C","B","C","D","A","B","A","D","A","B","C","D","A","B","C","B","A","B","C","D","A","B","C","D","A","B","C","D","B","B" }),
        ("Pinar Ozcan", "2024021", new[] { "A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B" }),
        ("Serkan Yalcin", "2024022", new[] { "A","B","A","D","A","B","C","D","B","B","C","D","A","B","C","D","A","B","C","D","A","D","C","D","A","B","C","D","A","C" }),
        ("Nur Aksoy", "2024023", new[] { "A","B","C","D","A","D","C","D","A","B","C","D","A","B","C","D","A","B","C","D","C","B","C","D","A","B","C","D","A","B" }),
        ("Volkan Tekin", "2024024", new[] { "D","B","C","D","A","B","C","D","A","A","C","D","A","B","C","D","A","B","B","D","A","B","C","D","A","B","C","D","A","B" }),
        ("Buse Gunes", "2024025", new[] { "A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B","C","D","A","B" })
    };

    public DemoOcrProvider(ILogger<DemoOcrProvider> logger)
    {
        _logger = logger;
    }

    public Task<OcrResult> ProcessImageAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        return GenerateDemoResultAsync(cancellationToken);
    }

    public Task<OcrResult> ProcessImageAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        return GenerateDemoResultAsync(cancellationToken);
    }

    private async Task<OcrResult> GenerateDemoResultAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Simulate OCR processing delay
        await Task.Delay(Random.Shared.Next(200, 600), cancellationToken);

        var student = DemoStudents[_currentStudentIndex % DemoStudents.Length];
        _currentStudentIndex++;

        var result = BuildOcrResultForStudent(student.Name, student.Id, student.Answers);
        result.ProcessingTime = stopwatch.Elapsed;

        _logger.LogInformation("Demo OCR generated result for student {Name} ({Id})", student.Name, student.Id);

        return result;
    }

    private static OcrResult BuildOcrResultForStudent(string name, string id, string[] answers)
    {
        var sb = new StringBuilder();
        var regions = new List<OcrRegion>();
        var lineNumber = 0;
        var random = new Random(id.GetHashCode());

        // Header region: Student Name
        lineNumber++;
        var nameText = $"Student Name: {name}";
        sb.AppendLine(nameText);
        regions.Add(new OcrRegion
        {
            Text = nameText,
            Confidence = RandomConfidence(random, 0.92, 0.99),
            LineNumber = lineNumber,
            BoundingBox = new BoundingBox { X = 50, Y = 30, Width = 400, Height = 25 }
        });

        // Header region: Student ID
        lineNumber++;
        var idText = $"Student ID: {id}";
        sb.AppendLine(idText);
        regions.Add(new OcrRegion
        {
            Text = idText,
            Confidence = RandomConfidence(random, 0.95, 0.99),
            LineNumber = lineNumber,
            BoundingBox = new BoundingBox { X = 50, Y = 60, Width = 300, Height = 25 }
        });

        // Separator
        lineNumber++;
        sb.AppendLine("---");
        regions.Add(new OcrRegion
        {
            Text = "---",
            Confidence = 0.99,
            LineNumber = lineNumber,
            BoundingBox = new BoundingBox { X = 50, Y = 95, Width = 500, Height = 5 }
        });

        // Answer lines
        var yPos = 120.0;
        for (int i = 0; i < answers.Length; i++)
        {
            lineNumber++;
            var answer = answers[i];
            var confidence = RandomConfidence(random, 0.85, 0.99);

            // Simulate occasional low-confidence or problematic reads
            string lineText;
            if (random.NextDouble() < 0.02) // 2% chance of unclear mark
            {
                lineText = $"Q{i + 1}: [?]";
                confidence = RandomConfidence(random, 0.30, 0.50);
            }
            else if (random.NextDouble() < 0.03) // 3% chance of no mark
            {
                lineText = $"Q{i + 1}: [ ]";
                confidence = RandomConfidence(random, 0.70, 0.85);
            }
            else
            {
                lineText = $"Q{i + 1}: [{answer}]";
            }

            sb.AppendLine(lineText);
            regions.Add(new OcrRegion
            {
                Text = lineText,
                Confidence = confidence,
                LineNumber = lineNumber,
                BoundingBox = new BoundingBox
                {
                    X = 60,
                    Y = yPos,
                    Width = 200,
                    Height = 20
                }
            });

            yPos += 25;
        }

        var overallConfidence = regions.Average(r => r.Confidence);

        return new OcrResult
        {
            Success = true,
            RawText = sb.ToString(),
            Regions = regions,
            OverallConfidence = overallConfidence,
            ProviderUsed = "Demo OCR"
        };
    }

    private static double RandomConfidence(Random random, double min, double max)
    {
        return min + random.NextDouble() * (max - min);
    }

    /// <summary>
    /// Returns all demo student data for batch processing.
    /// </summary>
    public static IReadOnlyList<(string Name, string Id, string[] Answers)> GetAllDemoStudents()
    {
        return DemoStudents;
    }
}
