using ExamReader.Core.Extensions;
using ExamReader.Web.Components;
using ExamReader.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddExamReaderCore(builder.Configuration);
builder.Services.AddScoped<ExamSessionService>();
builder.Services.AddScoped<DemoService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
