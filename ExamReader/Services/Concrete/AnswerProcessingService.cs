using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExamReader.Models;
using ExamReader.Services.Interfaces;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace ExamReader.Services.Concrete
{
    public class AnswerProcessingService : IAnswerProcessingService
    {
        private readonly IComputerVisionClient _computerVisionClient;

        public AnswerProcessingService(IComputerVisionClient computerVisionClient)
        {
            _computerVisionClient = computerVisionClient;
        }

        public async Task<Dictionary<int, string>> ExtractAnswersFromImageAsync(Stream imageStream)
        {
            var answers = new Dictionary<int, string>();
            var textHeaders = await _computerVisionClient.ReadInStreamAsync(imageStream);
            string operationLocation = textHeaders.OperationLocation;
            var questionNumbers = new List<int>();
            string operationId = operationLocation.Substring(operationLocation.Length - 36);

            ReadOperationResult result;
            do
            {
                result = await _computerVisionClient.GetReadResultAsync(Guid.Parse(operationId));
                await Task.Delay(1000); // Throttle requests to avoid spamming the service.
            }
            while (result.Status == OperationStatusCodes.Running || result.Status == OperationStatusCodes.NotStarted);

            if (result.Status == OperationStatusCodes.Succeeded)
            {
                foreach (var page in result.AnalyzeResult.ReadResults)
                {
                    int currentQuestionNumber = 0;
                    foreach (var line in page.Lines)
                    {
                        var numberMatch = Regex.Match(line.Text, @"^(\d+)$");
                        if (numberMatch.Success)
                        {
                            currentQuestionNumber = int.Parse(numberMatch.Groups[1].Value);
                            questionNumbers.Add(currentQuestionNumber);
                        }
                        else
                        {
                            var answerMatch = Regex.Match(line.Text, @"(?i)^([a-e])$");
                            if (answerMatch.Success && currentQuestionNumber != 0)
                            {
                                answers[currentQuestionNumber] = answerMatch.Groups[1].Value;
                                currentQuestionNumber = 0; // Reset for next question.
                            }
                        }
                    }
                }
            }

            // Identify and mark unanswered questions.
            var allQuestionNumbers = Enumerable.Range(1, questionNumbers.Max());
            var unansweredQuestions = allQuestionNumbers.Except(questionNumbers);
            foreach (var qNum in unansweredQuestions)
            {
                answers[qNum] = "Blank";
            }

            return answers;
        }
        public async Task<ProcessingResult> ProcessUploadedFileAsync(IFormFile fileUpload, string[] answerKey)
        {
            // Extract answers from the uploaded image file.
            Dictionary<int, string> extractedAnswers;
            using (var stream = fileUpload.OpenReadStream())
            {
                extractedAnswers = await ExtractAnswersFromImageAsync(stream);
            }

            // Evaluate the extracted answers against the key.
            var correctAnswers = new List<int>();
            var incorrectAnswers = new List<int>();
            var missingAnswers = new List<int>();
            for (int i = 0; i < answerKey.Length; i++)
            {
                int questionNumber = i + 1;

                if (extractedAnswers.TryGetValue(questionNumber, out var studentAnswer))
                {
                    if (string.IsNullOrWhiteSpace(studentAnswer))
                    {
                        missingAnswers.Add(questionNumber);
                    }
                    else if (studentAnswer.Equals(answerKey[i], StringComparison.OrdinalIgnoreCase))
                    {
                        correctAnswers.Add(questionNumber);
                    }
                    else
                    {
                        incorrectAnswers.Add(questionNumber);
                    }
                }
                else
                {
                    missingAnswers.Add(questionNumber);
                }
            }

            // Compile and return the results.
            var results = new ProcessingResult
            {
                CorrectCount = correctAnswers.Count,
                IncorrectCount = incorrectAnswers.Count,
                UnansweredCount = missingAnswers.Count,
                CorrectQuestions = string.Join(", ", correctAnswers),
                IncorrectQuestions = string.Join(", ", incorrectAnswers),
                UnansweredQuestions = string.Join(", ", missingAnswers)
            };

            return results;
        }
    }

}
