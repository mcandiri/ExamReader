using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ExamReader.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using ExamReader.Services.Interfaces;
using ExamReader.Services.Concrete;

namespace ExamReader.Controllers;

public class HomeController : Controller
{
    private readonly IAnswerProcessingService _answerProcessingService;

    public HomeController(IAnswerProcessingService answerProcessingService)
    {
        _answerProcessingService = answerProcessingService;

       
    }

    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile answerKeyFile, IFormFile studentAnswersFile)
    {
        // Validate file uploads.
        if (answerKeyFile == null || answerKeyFile.Length == 0 || studentAnswersFile == null || studentAnswersFile.Length == 0)
        {
            return Json(new { success = false, message = "Please select both files to upload." });
        }

        // Process the uploaded files.
        var processingResult = await _answerProcessingService.ProcessUploadedFilesAsync(answerKeyFile, studentAnswersFile);

        // Return processing results as JSON.
        return Json(new
        {
            success = true,
            message = "Files have been successfully uploaded and processed.",
            results = processingResult
        });
    }

    public IActionResult Index()
    {
        return View();
    }


   
}

