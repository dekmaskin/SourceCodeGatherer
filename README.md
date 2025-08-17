# Source Code Gatherer

A professional Windows desktop application for collecting and exporting source code files from directory structures. Perfect for creating context files for AI models, code reviews, or documentation purposes.

![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=flat-square&logo=dotnet)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

## 🚀 Features
- **Automatic Directory Scanning**: Instantly scans directories when selected
- **Smart File Type Detection**: Automatically identifies 40+ source code file types
- **Selective Export**: Choose exactly which file types to include
- **Multiple Export Options**:
  - Export to file with customizable location
  - Export directly to clipboard for quick sharing
- **Enhanced Performance**:
  - Streaming export for large codebases
  - Progress tracking with detailed feedback
  - Memory-efficient processing
- **Advanced Filtering**:
  - Exclude common directories (.git, node_modules, bin, obj, etc.)
  - File size limits to skip huge files
  - Binary file detection
- **User Experience**:
  - Drag & drop folder selection
  - Recent folders dropdown
  - Export statistics (file count, size, estimated lines)
  - Persistent settings and preferences
  - Detailed progress reporting
- **Detailed Output Format**: Each file includes their relative location

## 📋 Prerequisites

- Windows 10 or later
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) or later

## 🛠️ Installation

### Option 1: Build from Source

1. Clone the repository:
```bash
git clone https://github.com/dekmaskin/SourceCodeGatherer.git
cd source-code-gatherer
```

2. Build the project:
```bash
dotnet build -c Release
```

3. Run the application:
```bash
dotnet run -c Release
```

### Option 2: Download Release

Download the latest release from the [Releases](https://github.com/dekmaskin/SourceCodeGatherer/releases) page.

## 📖 Usage

1. **Select Source Directory**
   - Click "Browse..." next to Root Path
   - Choose the directory containing your source code
   - The application automatically scans for file types

2. **Choose File Types**
   - Check the file extensions you want to include
   - Only text-based source files are shown (no binaries)

3. **Export Your Code**
   - **Export to File**: Saves to your Downloads folder by default (customizable)
   - **Export to Clipboard**: Instantly copies all code to clipboard

## 📁 Output Format

The exported file contains all selected source files in a structured format:

```
=== FILE: src/models/User.cs ===

[File contents here]

=== END OF FILE ===
```

## 🔧 Supported File Types

The application recognizes 40+ file extensions including:

**Languages**: `.cs`, `.py`, `.js`, `.ts`, `.java`, `.cpp`, `.go`, `.rs`, `.swift`, `.php`, `.rb`

**Web**: `.html`, `.css`, `.scss`, `.jsx`, `.tsx`, `.vue`, `.svelte`

**Data**: `.json`, `.xml`, `.yaml`, `.toml`

**Config**: `.config`, `.ini`, `.env`, `.gitignore`

**Scripts**: `.sh`, `.bat`, `.ps1`

And many more...

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

If you encounter any issues or have questions:

- Open an [Issue](https://github.com/yourusername/SourceCodeGatherer/issues)
- Check existing issues for solutions
- Ensure you have the latest .NET runtime installed

---

**Made with ❤️ for developers who work with AI**
