# CV-MCP-Back

This project is a .NET 8 Minimal API backend for answering questions about a CV (PDF) using direct data extraction and LLM fallback. It also supports sending emails via SMTP.

## Getting Started

### 1. Clone the Repository
```
git clone <your-repo-url>
cd CV-MCP-Back/code
```

### 2. Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- The CV PDF file named `Associate CV.pdf` should be placed in the project root (`code/`).
- (Optional) SMTP server credentials for email sending (see below).

### 3. Install Dependencies
No manual package installation is needed; dependencies are managed via NuGet and restored automatically on build.

### 4. Build the Project
```
dotnet build
```

### 5. Run the Project
```
dotnet run
```
The API will be available at `http://localhost:5000` (or as shown in the console output).

### 6. API Endpoints

#### Chat Endpoint
- **POST** `/api/chat`
- **Body:**
  ```json
  {
    "question": "What is my email?",
    "model": "openrouter/auto" // optional
  }
  ```
- **Description:**
  - Answers questions about the CV using direct lookups or LLM fallback.
  - If the answer is not found, returns `"Not found in CV."`

#### Email Endpoint
- **POST** `/mcp/email/send`
- **Body:**
  ```json
  {
    "to": "recipient@example.com",
    "subject": "Subject here",
    "body": "Message body",
    "from": "optional-sender@example.com"
  }
  ```
- **Description:**
  - Sends an email using SMTP settings from `appsettings.json`.

### 7. Configuration

#### SMTP (for Email Sending)
Edit `appsettings.json` or `appsettings.Development.json`:
```json
"Smtp": {
  "Host": "smtp.example.com",
  "Port": 587,
  "User": "your-smtp-user",
  "Pass": "your-smtp-password",
  "Ssl": true
}
```

#### LLM API Keys
- Place your OpenRouter API key in the environment variable `OPENROUTER_API_KEY`.
- You can use a `.env` file or set it in your system environment.

### 8. Notes
- The CV PDF must be named `Associate CV.pdf` and placed in the project root.
- The backend answers questions strictly from the parsed CV data.
- For advanced Q&A, the backend uses OpenRouter LLM with a strict prompt to avoid hallucination.
- CORS is enabled for `http://localhost:3000` (frontend).
- Swagger UI is available in development mode at `/swagger`.

### 9. Troubleshooting
- If you get `CV data not loaded` errors, ensure the PDF exists and is readable.
- If you get `Name not found in CV`, check the PDF content and parsing logic.
- For email issues, verify your SMTP settings.

---

For further customization or issues, please refer to the code and comments.
