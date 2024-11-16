# ClientGenerator

A .NET 8.0 command-line tool for automatically generating TypeScript API clients from ASP.NET Core projects using NSwag.

## Prerequisites

- .NET 8.0 SDK or later
- NSwag.ConsoleCore version 14.0.0 or later

### Installing NSwag.ConsoleCore

```bash
# If you have an older version, remove it first:
dotnet tool uninstall -g nswag.consolecore

# Install the latest version:
dotnet tool install -g nswag.consolecore
```

## Installation

1. Clone this repository
2. Build the project:
```bash
dotnet build
```

## Usage

```bash
ClientGenerator.exe [options]
```

### Options

- `--config <path>` - Path to the configuration file (default: config.json)
- `--fileLog` - Enable file logging to logs/clientgenerator-{Date}.txt
- `--help` - Display help message

### Examples

```bash
# Use default config.json
ClientGenerator.exe

# Use custom config file
ClientGenerator.exe --config custom-config.json

# Enable file logging
ClientGenerator.exe --fileLog

# Show help
ClientGenerator.exe --help
```

## Configuration

The application uses a JSON configuration file to specify input projects and output settings.

### Configuration File Structure

```json
{
  "solutionPaths": [
    "C:\\Path\\To\\Your\\Project.csproj",
    "C:\\Path\\To\\Another\\Solution.sln"
  ],
  "outputFileNameTemplate": "{Name}Client.ts",
  "outputDirectory": "C:\\Output\\Path\\Clients"
}
```

### Configuration Properties

- `solutionPaths` (required): Array of paths to .csproj or .sln files
- `outputFileNameTemplate` (required): Template for generated file names. Must contain {Name} placeholder
- `outputDirectory` (required): Directory where generated clients will be saved

### Example config.json

```json
{
  "solutionPaths": [
    "C:\\Projects\\MyApi\\MyApi.csproj"
  ],
  "outputFileNameTemplate": "{Name}Client.ts",
  "outputDirectory": "C:\\Projects\\Frontend\\src\\clients"
}
```

## Generated Output

The tool generates TypeScript API clients with the following features:

- TypeScript version 4.3 compatibility
- Fetch API-based HTTP client
- Promise-based async operations
- Strong typing for all API operations and models
- Controller-based client classes
- Optional parameters support
- Proper enum handling
- API_BASE_URL token for base URL configuration

### Output Example

```typescript
// Generated client example (UserClient.ts)
export class UserClient {
    private http: HttpClient;
    private baseUrl: string;

    constructor() {
        this.baseUrl = API_BASE_URL;
    }

    async getUsers(): Promise<User[]> {
        // Generated method implementation
    }
}
```
