using System;
using ExamReader.Models;

namespace ExamReader.Services.Interfaces
{
	public interface IAnswerProcessingService
	{
        Task<Dictionary<int, string>> ExtractAnswersFromImageAsync(Stream imageStream);
        Task<ProcessingResult> ProcessUploadedFileAsync(IFormFile fileUpload, string[] answerKey);

    }
}

