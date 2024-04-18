using System;
namespace ExamReader.Models
{
    public class ProcessingResult
    {
        public int CorrectCount { get; set; }
        public int IncorrectCount { get; set; }
        public int UnansweredCount { get; set; }
        public Dictionary<int, string> CorrectAnswers { get; set; }
        public Dictionary<int, string> IncorrectAnswers { get; set; }
        public List<int> UnansweredQuestions { get; set; }
        public Dictionary<int, string> AnswerKey { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }

        public ProcessingResult()
        {
            Message = "";
            CorrectAnswers = new Dictionary<int, string>();
            IncorrectAnswers = new Dictionary<int, string>();
            UnansweredQuestions = new List<int>();
            AnswerKey = new Dictionary<int, string>();
        }
    }
}

