# KubeNavigator

A modern, native Windows application for managing and interacting with Kubernetes clusters, built with WinUI 3 and .NET 10.

![KubeNavigator](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![WinUI](https://img.shields.io/badge/WinUI-3-green)

## 🚀 Features

### Core Kubernetes Management
- **Multi-Cluster Support**: Connect to and manage multiple Kubernetes clusters simultaneously
- **Resource Management**: View, edit, and delete Kubernetes resources across all standard resource types
- **Real-Time Updates**: Watch API with automatic UI updates when resources change
- **Namespace Filtering**: Filter resources by namespace for better organization

### Pod Management
- **Pod Logs**: Stream pod logs in real-time with a built-in terminal viewer
- **Pod Shell**: Execute interactive shell sessions directly within pods
- **Port Forwarding**: Forward ports from pods to your local machine for debugging
- **Container Details**: View detailed information about containers, environment variables, and configurations

### Advanced Features
- **Helm Integration**: View and manage Helm releases across your clusters
- **Resource Details**: Rich, contextual detail views for all Kubernetes resources
- **Resource Editing**: Edit resources with YAML support
- **Custom Resource Definitions**: Full support for CRDs
- **Application Logging**: Built-in log viewer for application diagnostics

### User Experience
- **Modern UI**: Native Windows 11 design with Fluent UI
- **Dark/Light Theme**: System-aware theming with manual override
- **Multi-Window Support**: Open resources in separate windows for multi-monitor setups
- **Workspace Management**: Organize your work with multiple workspaces and tabs
- **Terminal Integration**: WebView2-based terminal for logs and shell sessions with xterm.js

## 📋 Prerequisites

- **Operating System**: Windows 10 (version 1809) or later, Windows 11 recommended
- **Runtime**: .NET 10 Runtime
- **Kubernetes**: A valid kubeconfig file at the default location (`~/.kube/config`)

## 🔧 Installation

### From Source

1. **Clone the repository**
   ```bash
   git clone https://github.com/felixse/KubeNavigator.git
   cd KubeNavigator
   ```

2. **Install .NET 10 SDK**
   - Download from [Microsoft .NET Downloads](https://dotnet.microsoft.com/download/dotnet/10.0)

3. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

4. **Build the application**
   ```bash
   dotnet build --configuration Release
   ```

5. **Run the application**
   ```bash
   dotnet run --project KubeNavigator
   ```

### MSIX Package

For production deployment, build the MSIX package:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## 🎯 Usage

### Getting Started

1. **Launch KubeNavigator**
2. **Select a Cluster**: The application will automatically load clusters from your kubeconfig
3. **Connect**: Click "Open" on any cluster to connect and browse resources

### Key Features

#### Viewing Resources
- Navigate through resource types using the left sidebar
- Click on any resource to view detailed information
- Use namespace filters to narrow down results

#### Working with Pods
- **View Logs**: Click "Show Logs" on any pod to stream logs
- **Open Shell**: Click "Open Shell" to execute commands inside the pod
- **Port Forward**: Use the port forwarding feature to access services locally

#### Managing Resources
- **Edit**: Click the "Edit" button to modify resource YAML
- **Delete**: Select resources and delete with confirmation
- **Create**: Add new resources through YAML editing

#### Application Logs
- Access application logs through the shelf at the bottom
- View real-time logs with color-coded severity levels
- Search and filter log messages

## 🏗️ Architecture

### Technology Stack
- **Framework**: .NET 10
- **UI**: WinUI 3 with Windows App SDK
- **MVVM**: CommunityToolkit.Mvvm for clean architecture
- **Kubernetes Client**: Official KubernetesClient library
- **Logging**: Serilog with Microsoft.Extensions.Logging integration
- **Terminal**: xterm.js via WebView2

### Project Structure
```
KubeNavigator/
├── Model/                  # Domain models and business logic
│   ├── Details/           # Resource detail view models
│   ├── Helm/              # Helm-related models
│   └── TerminalMessages/  # Terminal communication models
├── Services/              # Application services
│   ├── KubernetesService.cs  # Kubernetes API interactions
│   ├── LoggingService.cs     # Application logging
│   └── SettingsService.cs    # User settings management
├── ViewModels/            # MVVM ViewModels
│   ├── Navigation/       # Navigation-related VMs
│   ├── Resources/        # Kubernetes resource VMs
│   └── Shelf/            # Bottom shelf VMs (logs, shell, etc.)
├── Views/                 # XAML views
├── Converters/           # Value converters for data binding
├── TemplateSelectors/    # DataTemplate selectors
└── WebViews/             # Web-based terminal implementation
```

## 🐛 Troubleshooting

### Common Issues

**Application won't start**
- Ensure .NET 10 Runtime is installed
- Check Windows version compatibility

**Can't connect to cluster**
- Verify kubeconfig file exists at `~/.kube/config`
- Test cluster connectivity with `kubectl cluster-info`
- Check cluster status in the application

**Logs not streaming**
- Ensure the pod is in Running state
- Check pod logs directly with kubectl to verify accessibility

**Port forwarding not working**
- Verify the port is not already in use
- Check firewall settings

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Install .NET 10 SDK
4. Open the solution in Visual Studio 2025 or later
5. Build and run the project
6. Make your changes
7. Commit your changes (`git commit -m 'Add amazing feature'`)
8. Push to the branch (`git push origin feature/amazing-feature`)
9. Open a Pull Request

### Code Style
- Follow C# coding conventions
- Use nullable reference types
- Add XML documentation for public APIs
- Use source-generated logging with `[LoggerMessage]` attributes
- Keep ViewModels clean and testable

## 📄 License

This project is licensed under the GPL v3 License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [KubernetesClient](https://github.com/kubernetes-client/csharp) - Official Kubernetes C# client
- [Windows Community Toolkit](https://github.com/CommunityToolkit/WindowsCommunityToolkit) - WinUI components and helpers
- [Serilog](https://serilog.net/) - Structured logging
- [xterm.js](https://xtermjs.org/) - Terminal emulator

## 📧 Contact

Felix - [@felixse](https://github.com/felixse)

Project Link: [https://github.com/felixse/KubeNavigator](https://github.com/felixse/KubeNavigator)

---

Made with ❤️ for the Kubernetes community
