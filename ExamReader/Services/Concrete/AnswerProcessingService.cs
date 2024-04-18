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
            return await ExtractFromImageAsync(imageStream);
        }
        public async Task<Dictionary<int, string>> ExtractAnswerKeyFromImageAsync(Stream imageStream)
        {
            return await ExtractFromImageAsync(imageStream);
        }

        //private async Task<Dictionary<int, string>> ExtractFromImageAsync(Stream imageStream)
        //{
        //    var answers = new Dictionary<int, string>();
        //    var textHeaders = await _computerVisionClient.ReadInStreamAsync(imageStream);
        //    string operationLocation = textHeaders.OperationLocation;
        //    string operationId = operationLocation.Substring(operationLocation.Length - 36);

        //    ReadOperationResult result;
        //    do
        //    {
        //        result = await _computerVisionClient.GetReadResultAsync(Guid.Parse(operationId));
        //        await Task.Delay(1000);
        //    }
        //    while (result.Status == OperationStatusCodes.Running || result.Status == OperationStatusCodes.NotStarted);

        //    if (result.Status == OperationStatusCodes.Succeeded)
        //    {
        //        string value = "";
        //        foreach (var page in result.AnalyzeResult.ReadResults)
        //        {

        //            foreach (var line in page.Lines)
        //            {
        //                value += line.Text + " ";

        //            }
        //        }
        //        MatchCollection matches = Regex.Matches(value, @"(\d+)\s*[:.)]?\s*([A-Ea-e])");
        //        foreach (Match match in matches)
        //        {
        //            int questionNumber = int.Parse(match.Groups[1].Value);
        //            string answer = match.Groups[2].Value.ToUpper();
        //            answers[questionNumber] = answer;
        //        }
        //    }

        //    return answers;
        //}
        private async Task<Dictionary<int, string>> ExtractFromImageAsync(Stream imageStream)
        {
            var answers = new Dictionary<int, string>();
            var textHeaders = await _computerVisionClient.ReadInStreamAsync(imageStream);
            string operationLocation = textHeaders.OperationLocation;
            string operationId = operationLocation.Substring(operationLocation.Length - 36);

            ReadOperationResult result;
            int delay = 500; 
            do
            {
                result = await _computerVisionClient.GetReadResultAsync(Guid.Parse(operationId));
                if (result.Status == OperationStatusCodes.Running || result.Status == OperationStatusCodes.NotStarted)
                {
                    await Task.Delay(delay);
                    delay = Math.Min(4000, delay * 2); 
                }
            }
            while (result.Status == OperationStatusCodes.Running || result.Status == OperationStatusCodes.NotStarted);

            if (result.Status == OperationStatusCodes.Succeeded)
            {
                string value = "";
                foreach (var page in result.AnalyzeResult.ReadResults)
                {
                    foreach (var line in page.Lines)
                    {
                        value += line.Text + " ";
                    }
                }
                MatchCollection matches = Regex.Matches(value, @"(\d+)\s*[:.)]?\s*([A-Ea-e])");
                foreach (Match match in matches)
                {
                    int questionNumber = int.Parse(match.Groups[1].Value);
                    string answer = match.Groups[2].Value.ToUpper();
                    answers[questionNumber] = answer;
                }
            }

            return answers;
        }

        public async Task<ProcessingResult> ProcessUploadedFilesAsync(IFormFile answerKeyFile, IFormFile studentAnswersFile)
        {
            var answerKeyTask = ExtractAnswerKeyFromImageAsync(answerKeyFile.OpenReadStream());
            var studentAnswersTask = ExtractAnswersFromImageAsync(studentAnswersFile.OpenReadStream());

            await Task.WhenAll(answerKeyTask, studentAnswersTask);
            var answerKey = await answerKeyTask;
            var studentAnswers = await studentAnswersTask;

            var result = new ProcessingResult();
            foreach (var key in answerKey.Keys)
            {
                if (studentAnswers.TryGetValue(key, out var studentAnswer))
                {
                    if (string.Equals(studentAnswer, answerKey[key], StringComparison.OrdinalIgnoreCase))
                    {
                        result.CorrectAnswers.Add(key, studentAnswer);
                    }
                    else
                    {
                        result.IncorrectAnswers.Add(key, studentAnswer);
                    }
                }
                else
                {
                    result.UnansweredQuestions.Add(key);
                }
            }

            result.CorrectCount = result.CorrectAnswers.Count;
            result.IncorrectCount = result.IncorrectAnswers.Count;
            result.UnansweredCount = result.UnansweredQuestions.Count;
            result.Success = true;
            result.AnswerKey = answerKey;
            result.Message = "Answers processed successfully.";

            return result;
        }

       
    }

}
