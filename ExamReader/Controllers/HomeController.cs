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
    private readonly string[] _answerKey;

    public HomeController(IAnswerProcessingService answerProcessingService)
    {
        _answerProcessingService = answerProcessingService;

        //Normally it will be equal to the value in the database. This is just a sample
        _answerKey = new string[]{
        "C", "A", "B", "C", "C", "E", "D", "C", "B", "B",
        "B", "C", "B", "E", "A", "D", "C", "B", "B", "E",
        "D", "B", "B", "D", "D", "A", "D", "E", "E", "C",
        "C", "C", "E", "B", "E", "C", "D", "A", "D", "C"
        };
    }

    public IActionResult Upload()
    {
        return View();
    }

    // POST action to handle the file upload and processing.
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile fileUpload)
    {
        // Validate file upload.
        if (fileUpload == null || fileUpload.Length == 0)
        {
            return Json(new { success = false, message = "Please select a file to upload." });
        }

        // Process the uploaded file.
        var processingResult = await _answerProcessingService.ProcessUploadedFileAsync(fileUpload, _answerKey);

        // Return processing results as JSON.
        return Json(new
        {
            success = true,
            message = "File has been successfully uploaded and processed.",
            fileName = fileUpload.FileName,
            results = processingResult
        });
    }


    public IActionResult Index()
    {
        return View();
    }


   
}

