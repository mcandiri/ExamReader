using System;
namespace ExamReader.Models
{
	public class ProcessingResult
	{
        public int CorrectCount { get; set; }
        public int IncorrectCount { get; set; }
        public int UnansweredCount { get; set; }
        public string CorrectQuestions { get; set; }
        public string IncorrectQuestions { get; set; }
        public string UnansweredQuestions { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }
}

