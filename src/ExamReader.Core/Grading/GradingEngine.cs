using ExamReader.Core.Models;
using Microsoft.Extensions.Logging;

namespace ExamReader.Core.Grading;

public class GradingEngine : IGradingEngine
{
    private readonly ILogger<GradingEngine> _logger;

    public GradingEngine(ILogger<GradingEngine> logger)
    {
        _logger = logger;
    }

    public GradingResult GradeStudent(List<StudentAnswer> studentAnswers, AnswerKey answerKey, GradingOptions options)
    {
        var result = new GradingResult
        {
            TotalQuestions = answerKey.TotalQuestions,
            MaxScore = CalculateMaxScore(answerKey, options)
        };

        var answerLookup = studentAnswers.ToDictionary(a => a.QuestionNumber, a => a);

        foreach (var question in answerKey.Questions)
        {
            var questionResult = GradeQuestion(question, answerLookup, options);
            result.QuestionResults.Add(questionResult);

            switch (questionResult.Status)
            {
                case AnswerStatus.Answered when questionResult.IsCorrect:
                    result.Correct++;
                    break;
                case AnswerStatus.Answered when !questionResult.IsCorrect:
                    result.Incorrect++;
                    break;
                default:
                    result.Unanswered++;
                    break;
            }

            result.RawScore += questionResult.PointsEarned;
        }

        // Ensure raw score doesn't go below zero
        if (result.RawScore < 0)
            result.RawScore = 0;

        result.Percentage = result.MaxScore > 0
            ? Math.Round(result.RawScore / result.MaxScore * 100, 2)
            : 0;

        result.LetterGrade = GradingSummary.GetLetterGrade(result.Percentage, options.GradeScale);
        result.Passed = GradingSummary.IsPassing(result.Percentage, options.PassingScore);

        _logger.LogDebug(
            "Graded student {StudentId}: {Correct}/{Total} correct, {Percentage}% ({Grade})",
            result.StudentId, result.Correct, result.TotalQuestions, result.Percentage, result.LetterGrade);

        return result;
    }

    public Task<GradingResult> GradeStudentAsync(
        List<StudentAnswer> studentAnswers,
        AnswerKey answerKey,
        GradingOptions options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = GradeStudent(studentAnswers, answerKey, options);
        return Task.FromResult(result);
    }

    private QuestionResult GradeQuestion(
        Question question,
        Dictionary<int, StudentAnswer> answerLookup,
        GradingOptions options)
    {
        double pointsPossible = options.WeightedQuestions ? question.Weight : 1.0;

        if (!answerLookup.TryGetValue(question.Number, out var studentAnswer) ||
            studentAnswer.Status == AnswerStatus.Unanswered)
        {
            return new QuestionResult
            {
                QuestionNumber = question.Number,
                CorrectAnswer = question.CorrectAnswer,
                StudentAnswer = string.Empty,
                IsCorrect = false,
                PointsEarned = 0,
                PointsPossible = pointsPossible,
                Status = AnswerStatus.Unanswered
            };
        }

        if (studentAnswer.Status == AnswerStatus.MultipleMarks ||
            studentAnswer.Status == AnswerStatus.Unclear)
        {
            double penalty = options.NegativeMarking ? -options.NegativePenalty * pointsPossible : 0;
            return new QuestionResult
            {
                QuestionNumber = question.Number,
                CorrectAnswer = question.CorrectAnswer,
                StudentAnswer = studentAnswer.SelectedAnswer,
                IsCorrect = false,
                PointsEarned = penalty,
                PointsPossible = pointsPossible,
                Status = studentAnswer.Status
            };
        }

        // Handle multi-select with partial credit
        if (question.Type == QuestionType.MultiSelect && options.PartialCredit)
        {
            return GradeMultiSelect(question, studentAnswer, pointsPossible, options);
        }

        // Standard single-answer comparison
        bool isCorrect = string.Equals(
            studentAnswer.SelectedAnswer.Trim(),
            question.CorrectAnswer.Trim(),
            StringComparison.OrdinalIgnoreCase);

        double pointsEarned;
        if (isCorrect)
        {
            pointsEarned = pointsPossible;
        }
        else if (options.NegativeMarking)
        {
            pointsEarned = -options.NegativePenalty * pointsPossible;
        }
        else
        {
            pointsEarned = 0;
        }

        return new QuestionResult
        {
            QuestionNumber = question.Number,
            CorrectAnswer = question.CorrectAnswer,
            StudentAnswer = studentAnswer.SelectedAnswer,
            IsCorrect = isCorrect,
            PointsEarned = pointsEarned,
            PointsPossible = pointsPossible,
            Status = AnswerStatus.Answered
        };
    }

    private static QuestionResult GradeMultiSelect(
        Question question,
        StudentAnswer studentAnswer,
        double pointsPossible,
        GradingOptions options)
    {
        var correctAnswers = question.CorrectAnswer
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(a => a.ToUpperInvariant())
            .ToHashSet();

        var studentAnswers = studentAnswer.SelectedAnswer
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(a => a.ToUpperInvariant())
            .ToHashSet();

        int correctSelections = studentAnswers.Intersect(correctAnswers).Count();
        int incorrectSelections = studentAnswers.Except(correctAnswers).Count();
        int totalCorrectOptions = correctAnswers.Count;

        // Partial credit: correct selections minus wrong selections, divided by total correct
        double partialScore = Math.Max(0, correctSelections - incorrectSelections) / (double)totalCorrectOptions;
        double pointsEarned = partialScore * pointsPossible;
        bool isFullyCorrect = correctSelections == totalCorrectOptions && incorrectSelections == 0;

        return new QuestionResult
        {
            QuestionNumber = question.Number,
            CorrectAnswer = question.CorrectAnswer,
            StudentAnswer = studentAnswer.SelectedAnswer,
            IsCorrect = isFullyCorrect,
            PointsEarned = pointsEarned,
            PointsPossible = pointsPossible,
            Status = AnswerStatus.Answered
        };
    }

    private static double CalculateMaxScore(AnswerKey answerKey, GradingOptions options)
    {
        if (options.WeightedQuestions)
        {
            return answerKey.Questions.Sum(q => q.Weight);
        }
        return answerKey.TotalQuestions;
    }
}
