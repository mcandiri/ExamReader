using ExamReader.Core.Models;
using ExamReader.Core.Ocr;

namespace ExamReader.Core.Parsing;

public interface IAnswerSheetParser
{
    bool CanParse(AnswerSheetTemplate template);
    Task<List<StudentAnswer>> ParseAsync(OcrResult ocrResult, AnswerSheetTemplate template, CancellationToken cancellationToken = default);
}
