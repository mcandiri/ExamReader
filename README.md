# ExamReader

> Intelligent exam grading engine for .NET — OCR-powered answer sheet processing, automatic grading, and detailed analytics for educators.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![CI](https://github.com/mcandiri/ExamReader/actions/workflows/ci.yml/badge.svg)](https://github.com/mcandiri/ExamReader/actions/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/tests-100%2B%20passed-brightgreen)]()

## Why ExamReader?

Grading 100 multiple-choice exams by hand takes hours and is error-prone. ExamReader processes a stack of scanned answer sheets in seconds — with per-question analytics that reveal which topics your class struggles with.

## Demo — Try Without Setup

```bash
git clone https://github.com/mcandiri/ExamReader.git
cd ExamReader
dotnet run --project src/ExamReader.Web
# Open http://localhost:5000 → Click "Demo Mode"
```

Pre-loaded with 25 students and 30 questions. See the full dashboard, analytics, and export features — no API keys or database needed.

## Features

### Multi-Provider OCR
| Provider | Cost | Speed | Setup |
|----------|------|-------|-------|
| Azure Computer Vision | Pay-per-use | Fast | API key |
| Tesseract | Free | Medium | Local install |
| Demo Mode | Free | Instant | None |

### Answer Sheet Support
- Bubble sheets (A/B/C/D/E)
- Grid-based answer sheets
- Written answer extraction

### Smart Grading
- Configurable negative marking (-0.25 per wrong answer)
- Weighted questions (different point values)
- Partial credit for multi-select
- Custom letter grade scales (A/B/C/D/F with +/- variants)

### Class Analytics
- Score distribution histogram
- Per-question difficulty & discrimination index
- Automatic flagging of "bad questions" (low discrimination)
- Most common wrong answers per question

### Batch Processing
- Process entire classroom at once
- Real-time progress tracking
- Handles blank answers, multiple marks, smudges

### Export
- **HTML** report with charts (standalone, share via email)
- **JSON** for integration with other systems
- **CSV** for Excel/Google Sheets

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Blazor Server UI                     │
│  Home │ Exam Setup │ Processing │ Results │ Demo     │
├─────────────────────────────────────────────────────┤
│                  ExamReader.Core                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐          │
│  │ OCR      │  │ Grading  │  │ Analytics│          │
│  │ Provider │→ │ Engine   │→ │ Engine   │          │
│  └──────────┘  └──────────┘  └──────────┘          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐          │
│  │ Parsing  │  │ Batch    │  │ Reports  │          │
│  │ Engine   │  │ Processor│  │ Generator│          │
│  └──────────┘  └──────────┘  └──────────┘          │
├─────────────────────────────────────────────────────┤
│  OCR Providers: Azure CV │ Tesseract │ Demo          │
└─────────────────────────────────────────────────────┘
```

## Project Structure

```
ExamReader/
├── src/
│   ├── ExamReader.Core/          # Core library
│   │   ├── Models/               # Domain models
│   │   ├── Ocr/                  # OCR providers
│   │   ├── Parsing/              # Answer sheet parsers
│   │   ├── Grading/              # Grading engine
│   │   ├── Batch/                # Batch processing
│   │   ├── Analytics/            # Exam analytics
│   │   ├── Reports/              # Report generators
│   │   └── Demo/                 # Demo data
│   └── ExamReader.Web/           # Blazor Server app
│       ├── Components/Pages/     # Blazor pages
│       ├── Shared/               # Shared components
│       └── Services/             # Web services
├── tests/
│   ├── ExamReader.Core.Tests/    # Core library tests
│   └── ExamReader.Web.Tests/     # Web app tests
├── .github/workflows/ci.yml     # CI pipeline
└── ExamReader.sln
```

## Configuration

```json
{
  "OcrProvider": "Demo",
  "Azure": {
    "ComputerVision": {
      "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
      "ApiKey": "your-api-key"
    }
  }
}
```

## Born From Production

> ExamReader started as a simple OCR grading tool and evolved into a comprehensive exam analysis platform while managing assessments for an education platform serving 1,500+ students. The analytics features — difficulty index, discrimination index, distractor analysis — were added after teachers requested deeper insights into their exam quality.

## Security

- Scanned images processed in memory, never persisted to disk
- API keys stored in configuration only, never logged
- No student data sent to external services in demo mode

## Roadmap

- [ ] OMR (Optical Mark Recognition) with OpenCV for higher accuracy
- [ ] PDF answer sheet support
- [ ] Multi-page answer sheet support
- [ ] Integration with LMS platforms (Moodle, Canvas)
- [ ] Mobile app for scanning with phone camera
- [ ] AI-powered written answer grading (essay scoring)

## License

[MIT](LICENSE)
