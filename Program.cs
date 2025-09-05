
using code;
using MailKit.Net.Smtp;
using MimeKit;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// Load and parse the CV PDF at startup (look for it in the project root)
var cvPath = Path.Combine(Directory.GetCurrentDirectory(), "Associate CV.pdf");
CvData? cvData = null;
if (File.Exists(cvPath))
{
    cvData = CvParser.ParsePdf(cvPath);
}

var app = builder.Build();


// Simple endpoint to check Azure deployment
app.MapGet("/hello-azure", () => "hello azure");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Chat endpoint: use Gemini LLM to answer questions about the CV
app.MapPost("/api/chat", async (ChatRequest req, IConfiguration config) =>
{
    if (cvData == null)
    {
        Console.Error.WriteLine("CV data not loaded. Check if Associate CV.pdf exists and is readable.");
        return Results.Problem("CV data not loaded. Check if Associate CV.pdf exists and is readable.");
    }
    if (cvData.Name == null)
    {
        Console.Error.WriteLine("Name not found in CV. Check PDF content and parsing logic.");
        return Results.Problem("Name not found in CV. Check PDF content and parsing logic.");
    }
    var question = req.Question.ToLowerInvariant();
    // Direct field lookups
    if (question.Contains("name"))
        return Results.Ok(cvData.Name ?? "Not found in CV.");
    if (question.Contains("email"))
        return Results.Ok(cvData.Email ?? "Not found in CV.");
    if (question.Contains("skill"))
        return Results.Ok(cvData.Skills.Count > 0 ? string.Join(", ", cvData.Skills) : "Not found in CV.");
    if (question.Contains("education"))
        return Results.Ok(cvData.Educations.Count > 0 ? string.Join("; ", cvData.Educations.Select(e => $"{e.Degree} at {e.Institution} ({e.Period})")) : "Not found in CV.");
    if ((question.Contains("experience") || question.Contains("work") || question.Contains("ceylon electricity board")))
    {
        if (cvData.Experiences != null)
        {
            var exp = cvData.Experiences.FirstOrDefault(e => e.Company != null && e.Company.ToLowerInvariant().Contains("ceylon electricity board"));
            if (exp != null)
                return Results.Ok($"{exp.Role} at {exp.Company} ({exp.Period})");
            return Results.Ok(cvData.Experiences.Count > 0 ? string.Join("; ", cvData.Experiences.Select(e => $"{e.Role} at {e.Company} ({e.Period})")) : "Not found in CV.");
        }
        return Results.Ok("Not found in CV.");
    }
    if (question.Contains("competition"))
        return Results.Ok(cvData.Competitions.Count > 0 ? string.Join(", ", cvData.Competitions) : "Not found in CV.");
    if (question.Contains("project"))
        return Results.Ok(cvData.Projects.Count > 0 ? string.Join("; ", cvData.Projects.Select(p => p.Name)) : "Not found in CV.");

    // Otherwise, use OpenRouter for complex Q&A
    var model = req.Model ?? "openrouter/auto"; // Allow model selection, default to auto
    var openrouter = new code.OpenRouterClient(model);
    var cvJson = System.Text.Json.JsonSerializer.Serialize(cvData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    var systemPrompt = "You are an AI assistant. Answer ONLY using the information in the following CV data. If the answer is not present, reply exactly: 'Not found in CV.' Do NOT use any outside knowledge or make up information. The CV data is provided as a JSON array of experiences, each with 'Role', 'Company', and 'Period'.";
    var userPrompt = $"CV Data:\n{cvJson}\n\nUser question: {req.Question}\n\nExtract the answer directly from the CV data above.";
    var answer = await openrouter.AskAsync(systemPrompt, userPrompt);
    return Results.Ok(answer);
});

// MCP Email endpoint: send an email
app.MapPost("/mcp/email/send", async (EmailRequest req, IConfiguration config) =>
{
    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("CV MCP Server", req.From ?? "uhchewage23@gmail.com"));
    message.To.Add(new MailboxAddress(req.To, req.To));
    message.Subject = req.Subject;
    message.Body = new TextPart("plain") { Text = req.Body };

    // Read SMTP config from appsettings.json
    var smtpHost = config["Smtp:Host"] ?? "localhost";
    var smtpPort = int.TryParse(config["Smtp:Port"], out var port) ? port : 25;
    var smtpUser = config["Smtp:User"];
    var smtpPass = config["Smtp:Pass"];
    var useSsl = bool.TryParse(config["Smtp:Ssl"], out var ssl) && ssl;

    try
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, useSsl);
        if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
        {
            await client.AuthenticateAsync(smtpUser, smtpPass);
        }
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
        return Results.Ok(new { status = "success", message = "Email sent." });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to send email: {ex.Message}");
    }
});


app.Run();

public record ChatRequest(string Question, string? Model = null);
public record EmailRequest(string To, string Subject, string Body, string? From = null);
