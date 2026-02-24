using ExamReader.Core.Models;

namespace ExamReader.Core.Grading;

public interface IGradingEngine
{
    GradingResult GradeStudent(List<StudentAnswer> studentAnswers, AnswerKey answerKey, GradingOptions options);
    Task<GradingResult> GradeStudentAsync(List<StudentAnswer> studentAnswers, AnswerKey answerKey, GradingOptions options, CancellationToken cancellationToken = default);
}
