using ExamReader.Services.Concrete;
using ExamReader.Services.Interfaces;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
var computerVisionConfig = builder.Configuration.GetSection("ComputerVision");
var endpoint = computerVisionConfig["Endpoint"];
var apiKey = computerVisionConfig["ApiKey"];
builder.Services.AddSingleton<IComputerVisionClient>(new ComputerVisionClient(new ApiKeyServiceClientCredentials(apiKey))
{
    Endpoint = endpoint
});

builder.Services.AddScoped<IAnswerProcessingService, AnswerProcessingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Upload}/{id?}");

app.Run();

