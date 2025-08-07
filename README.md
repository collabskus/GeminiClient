# 🤖 Gemini Client Console

A powerful, interactive command-line client for Google's Gemini AI API with real-time model selection, performance metrics, and session statistics.

![GitHub release (latest by date)](https://img.shields.io/github/v/release/yourusername/GeminiClient)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/yourusername/GeminiClient/build-and-release.yml)
![Platform Support](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)
![License](https://img.shields.io/badge/license-AGPL--3.0--or--later-blue)

## 🔑 Quick Start - API Key Required!

> **⚠️ IMPORTANT: You need a Google Gemini API key to use this application!**

### Getting Your API Key

1. **Get a FREE API key** from Google AI Studio: [https://aistudio.google.com/apikey](https://aistudio.google.com/apikey)
2. Click "Get API Key" and follow the instructions
3. Copy your API key (starts with `AIza...`)

### Setting Your API Key (3 Methods)

#### Method 1: Configuration File (Recommended)
Create an `appsettings.json` file in the same directory as the executable:

```json
{
  "GeminiSettings": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "BaseUrl": "https://generativelanguage.googleapis.com/",
    "DefaultModel": "gemini-2.5-flash"
  }
}
```

#### Method 2: Environment Variable
```bash
# Linux/macOS
export GeminiSettings__ApiKey="YOUR_API_KEY_HERE"

# Windows Command Prompt
set GeminiSettings__ApiKey=YOUR_API_KEY_HERE

# Windows PowerShell
$env:GeminiSettings__ApiKey="YOUR_API_KEY_HERE"
```

#### Method 3: User Secrets (Development)
```bash
dotnet user-secrets set "GeminiSettings:ApiKey" "YOUR_API_KEY_HERE"
```

> **🔒 Security Note**: Never commit your API key to version control! The `appsettings.json` file is gitignored by default.

## 📥 Installation

### Download Pre-built Binaries

Download the latest release for your platform from the [Releases page](https://github.com/yourusername/GeminiClient/releases).

| Platform | Download | Architecture |
|----------|----------|--------------|
| **Windows** | `gemini-client-win-x64.zip` | 64-bit Intel/AMD |
| | `gemini-client-win-x86.zip` | 32-bit Intel/AMD |
| | `gemini-client-win-arm64.zip` | ARM64 |
| **Linux** | `gemini-client-linux-x64.tar.gz` | 64-bit Intel/AMD |
| | `gemini-client-linux-arm64.tar.gz` | ARM64 (Raspberry Pi 4+) |
| | `gemini-client-linux-musl-x64.tar.gz` | Alpine Linux |
| **macOS** | `gemini-client-osx-x64.tar.gz` | Intel Macs |
| | `gemini-client-osx-arm64.tar.gz` | Apple Silicon (M1/M2/M3) |

### Running the Application

#### Windows
```powershell
# Extract the ZIP file
# Double-click gemini-client-win-x64.exe
# OR run from command line:
.\gemini-client-win-x64.exe
```

#### Linux/macOS
```bash
# Extract the archive
tar -xzf gemini-client-linux-x64.tar.gz

# Make executable
chmod +x gemini-client-linux-x64

# Run
./gemini-client-linux-x64
```

## 🚀 Features

### Interactive Model Selection
- **Dynamic Model Discovery**: Automatically fetches all available Gemini models
- **Smart Recommendations**: Suggests optimal models based on your needs
- **Model Categories**:
  - ⚡ **Flash Models**: Fast, cost-effective for most tasks
  - 💎 **Pro Models**: Advanced capabilities for complex tasks
  - 🚀 **Ultra Models**: Maximum performance (when available)
  - 🧪 **Experimental Models**: Cutting-edge features in testing

### Real-time Performance Metrics
- **Response Time Tracking**: See exactly how long each request takes
- **Token Speed Analysis**: Monitors tokens/second throughput
- **Visual Speed Indicators**:
  - 🐌 Slow (< 10 tokens/s)
  - 🚶 Normal (10-30 tokens/s)
  - 🏃 Fast (30-50 tokens/s)
  - 🚀 Very Fast (50-100 tokens/s)
  - ⚡ Lightning (100+ tokens/s)

### Session Statistics
- Track all requests in your session
- View average response times
- Compare model performance
- See total tokens processed

### Smart Error Handling
- Automatic fallback to stable models
- Clear error messages with suggested fixes
- Graceful handling of API limits and server issues

## 💻 Usage

### Basic Commands

| Command | Description |
|---------|-------------|
| `exit` | Quit the application |
| `model` | Change the selected AI model |
| `stats` | View session statistics |

### Example Session

```
╔═══ Available Gemini Models ═══╗
  1. ⚡ Gemini 2.5 Flash [RECOMMENDED]
     Stable version of Gemini 2.5 Flash, our mid-size multimodal...
     Tokens: Input 1,048,576 | Output 65,536
  2. 💎 Gemini 2.5 Pro
     Stable release of Gemini 2.5 Pro
     Tokens: Input 1,048,576 | Output 65,536
  ...

Enter selection (number), or press Enter for recommended:
> [Press Enter]

✓ Selected: Gemini 2.5 Flash

📝 Enter prompt ('exit' to quit, 'model' to change model, 'stats' for session stats):
> Explain quantum computing in simple terms

⠙ Generating response... [00:01.23]

╭─── Response ─── ⏱ 1.23s ───╮
Quantum computing is like having a magical computer that can try many solutions 
at once instead of one at a time...
╰────────────────╯

📊 Performance Metrics:
   └─ Response Time: 1.23s
   └─ Words: 127 | Characters: 823
   └─ Est. Tokens: ~206 | Speed: 167.5 tokens/s [██████████] ⚡
```

## ⚙️ Configuration

### Full Configuration Options

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "GeminiClient": "Information",
      "GeminiClientConsole": "Information"
    }
  },
  "GeminiSettings": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "BaseUrl": "https://generativelanguage.googleapis.com/",
    "DefaultModel": "gemini-2.5-flash",
    "ModelPreference": "Fastest",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "EnableDetailedLogging": false
  }
}
```

### Configuration Priority

The application loads configuration in this order (later sources override earlier ones):
1. Default values
2. `appsettings.json` file
3. User secrets (development only)
4. Environment variables
5. Command line arguments (if applicable)

### Model Preferences

Set `ModelPreference` to control automatic model selection:
- `"Fastest"` - Prefers Flash models for quick responses
- `"MostCapable"` - Prefers Pro/Ultra models for complex tasks
- `"Balanced"` - Balances speed and capability

## 🛠️ Building from Source

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Git

### Build Steps

```bash
# Clone the repository
git clone https://github.com/yourusername/GeminiClient.git
cd GeminiClient

# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release

# Run
dotnet run --project GeminiClientConsole
```

### Creating a Release Build

```bash
# Windows PowerShell
./build-release.ps1 -Version 1.0.0

# Linux/macOS
chmod +x build-release.sh
./build-release.sh 1.0.0
```

## 📦 Project Structure

```
GeminiClient/
├── GeminiClient/                 # Core library
│   ├── GeminiApiClient.cs       # Main API client
│   ├── ModelService.cs          # Model management
│   └── Models/                  # Data models
├── GeminiClientConsole/          # Console application
│   ├── Program.cs               # Entry point
│   ├── AppRunner.cs             # Main application logic
│   └── ConsoleModelSelector.cs  # Interactive model selection
├── .github/workflows/            # CI/CD pipelines
│   ├── build-and-release.yml   # Release automation
│   └── ci.yml                   # Continuous integration
└── README.md                     # This file
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Development Setup

```bash
# Clone your fork
git clone https://github.com/yourusername/GeminiClient.git
cd GeminiClient

# Create a new branch
git checkout -b feature/your-feature

# Set up user secrets for development
dotnet user-secrets set "GeminiSettings:ApiKey" "YOUR_API_KEY"

# Run tests
dotnet test

# Run the application
dotnet run --project GeminiClientConsole
```

## 🐛 Troubleshooting

### Common Issues

#### "API Key not configured"
- Make sure you've set your API key using one of the three methods above
- Check that your `appsettings.json` is in the same directory as the executable
- Verify environment variables are set correctly

#### "500 Internal Server Error"
- Some experimental models may be unstable
- Switch to a stable model like `gemini-2.5-flash`
- Check [Google's status page](https://status.cloud.google.com/) for outages

#### "Rate limit exceeded"
- Free tier has usage limits
- Wait a few minutes and try again
- Consider upgrading your API plan

#### Application won't start on macOS
```bash
# Remove quarantine attribute
xattr -d com.apple.quarantine ./gemini-client-osx-arm64

# Make executable
chmod +x ./gemini-client-osx-arm64
```

#### Application won't start on Linux
```bash
# Check if executable permission is set
chmod +x ./gemini-client-linux-x64

# If using Alpine Linux, use the musl version
./gemini-client-linux-musl-x64
```

## 📊 Performance

### Binary Sizes (Approximate)

| Platform | Size | Notes |
|----------|------|-------|
| Windows x64 | ~35 MB | Self-contained, trimmed |
| Linux x64 | ~38 MB | Self-contained, trimmed |
| macOS ARM64 | ~36 MB | Self-contained, trimmed |

### System Requirements

- **Memory**: 128 MB RAM minimum
- **Disk Space**: 50 MB for application
- **Network**: Internet connection required
- **.NET Runtime**: Not required (self-contained)

## 📝 API Usage and Limits

### Free Tier Limits (as of 2025)

- **Requests**: 60 requests per minute
- **Daily Tokens**: Varies by model
- **Rate Limits**: Automatically handled with retry logic

### Tips for Optimal Usage

1. **Use Flash models for most tasks** - They're fast and cost-effective
2. **Switch to Pro models for complex reasoning** - When you need advanced capabilities
3. **Monitor your usage** - Check your [Google AI Studio dashboard](https://aistudio.google.com/)
4. **Cache responses when possible** - Avoid repeated identical requests

## 📜 License

This project is licensed under the GNU Affero General Public License v3.0 or later (AGPL-3.0-or-later) - see the [LICENSE](LICENSE) file for details.

### What this means:

- ✅ **You can**: Use, modify, distribute, and use commercially
- ⚠️ **You must**: Disclose source, include license and copyright notice, state changes, and use the same license
- 🌐 **Network use**: If you run a modified version on a server, you must provide source code to users of that server
- 🚫 **You cannot**: Hold the authors liable or remove the license terms

For more information, see the [full AGPL-3.0 license text](https://www.gnu.org/licenses/agpl-3.0.en.html).

## 🙏 Acknowledgments

- Google for the Gemini AI API
- The .NET team for the excellent framework
- All contributors and users of this project

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/GeminiClient/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/GeminiClient/discussions)
- **API Documentation**: [Google AI Documentation](https://ai.google.dev/gemini-api/docs)

## 🗺️ Roadmap

- [ ] Add support for image inputs
- [ ] Implement conversation history
- [ ] Add export functionality for responses
- [ ] Create a web UI version
- [ ] Add support for streaming responses
- [ ] Implement prompt templates
- [ ] Add batch processing mode

---

<div align="center">
Made with ❤️ using .NET and Google Gemini AI

⭐ Star this repo if you find it useful!
</div>


---
*Notice: This project contains code generated by Large Language Models such as Claude and Gemini. All code is experimental whether explicitly stated or not.*