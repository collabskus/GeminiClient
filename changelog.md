# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.0.4] - 2025-08-07

### Added
- Interactive console client for Google Gemini AI API
- Dynamic model discovery and selection with smart recommendations
- Real-time performance metrics with response time tracking and token speed analysis
- Session statistics tracking for all requests
- Support for multiple platform architectures (Windows x64/x86/ARM64, Linux x64/ARM/ARM64, macOS x64/ARM64)
- Automated GitHub Actions CI/CD pipeline for cross-platform builds and releases
- Configuration support via appsettings.json, environment variables, and user secrets
- Smart error handling with automatic fallback to stable models
- Visual performance indicators for response speeds
- Model categorization (Flash, Pro, Ultra, Experimental)

### Features
- **Model Management**: Automatic fetching of available Gemini models with detailed information
- **Performance Tracking**: Response time monitoring with tokens/second throughput analysis
- **Cross-Platform Support**: Self-contained executables for Windows, Linux, and macOS
- **Flexible Configuration**: Multiple configuration methods with proper precedence
- **Developer Experience**: Comprehensive error messages and user-friendly interface

### Technical
- Built with .NET 9.0 framework
- Self-contained, trimmed single-file executables
- Dependency injection with Microsoft.Extensions.Hosting
- Robust configuration management with validation
- Comprehensive logging support

## [0.0.3] - Previous Release
- Internal development version

## [0.0.2] - Previous Release  
- Internal development version

## [0.0.1] - Previous Release
- Internal development version

## [0.0.0] - Initial Release
- Repository initialization