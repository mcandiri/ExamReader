
# EXAM GRADER

This ASP.NET Core application leverages the Azure Computer Vision API to automate the grading of multiple-choice tests. By processing scanned images of test sheets, it identifies student answers, compares them against a predefined answer key, and generates a summary of the grading outcome. This tool aims to enhance efficiency and accuracy in grading processes, making it an invaluable asset for educators and academic institutions.

## Getting Started

Follow these instructions to get a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

Before you begin, ensure you have the following installed:
- [.NET 5.0 SDK](https://dotnet.microsoft.com/download)
- Any IDE with C# support (e.g., [Visual Studio](https://visualstudio.microsoft.com/), [VS Code](https://code.visualstudio.com/))

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/mcandiri/ExamReader.git
   ```
2. Navigate to the project directory:
   ```
   cd ExamReader
   ```
3. Restore the project dependencies:
   ```
   dotnet restore
   ```
4. Build the project:
   ```
   dotnet build
   ```

## Running the Application

To start the application, run:
```
dotnet run --project ExamReader
```
Navigate to `http://localhost:5000/` in your web browser to view the application.

## Usage

1. **Scan Test Sheets**: Scan or take clear photos of the completed multiple-choice test sheets.
2. **Upload Images**: Through the application interface, upload the images for processing.
3. **Review Results**: After processing, the application will display the grading results, including correct, incorrect, and unanswered question counts.

## Contributing

We welcome contributions from the community! If you would like to contribute to the project, please follow these steps:

1. Fork the repository.
2. Create a new feature branch: `git checkout -b feature/AmazingFeature`.
3. Commit your changes: `git commit -m 'Add some AmazingFeature'`.
4. Push to the branch: `git push origin feature/AmazingFeature`.
5. Open a pull request.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Acknowledgments

- Special thanks to the Azure Computer Vision team for providing the API that made this project possible.
- Gratitude to all contributors for their invaluable input and feedback.
- This project was inspired by the need to streamline educational processes and improve grading accuracy.
