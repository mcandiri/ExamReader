using System;
using ExamReader.Models;

namespace ExamReader.Services.Interfaces
{
	public interface IAnswerProcessingService
	{
        Task<Dictionary<int, string>> ExtractAnswersFromImageAsync(Stream imageStream);
        Task<Dictionary<int, string>> ExtractAnswerKeyFromImageAsync(Stream imageStream);
        Task<ProcessingResult> ProcessUploadedFilesAsync(IFormFile answerKeyFile, IFormFile studentAnswersFile);

    }
}

