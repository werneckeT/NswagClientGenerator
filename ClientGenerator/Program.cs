using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClientGenerator;
using ClientGenerator.Models;

// Define help text
const string helpText = """

                        ClientGenerator - TypeScript API Client Generator using NSwag

                        Usage: ClientGenerator.exe [options]

                        Options:
                          --config <path>    Path to the configuration file (default: config.json)
                          --fileLog         Enable file logging to logs/clientgenerator-{Date}.txt
                          --help            Display this help message

                        Example:
                          ClientGenerator.exe --config custom-config.json --fileLog
                          ClientGenerator.exe --help

                        """;

if (args.Contains("--help"))
{
    Console.WriteLine(helpText);
    return 0;
}

var configPath = "config.json";  // Default value
var enableFileLog = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--config" when i + 1 < args.Length:
            configPath = args[i + 1];
            i++; // Skip next argument
            break;
        case "--fileLog":
            enableFileLog = true;
            break;
        default:
            Console.WriteLine($"Unknown argument: {args[i]}");
            Console.WriteLine(helpText);
            return 1;
    }
}

try 
{
    var services = new ServiceCollection();
    
    services.AddLogging(builder =>
    {
        builder.AddConsole()
              .SetMinimumLevel(LogLevel.Information);
        
        if (enableFileLog)
        {
            builder.AddFile("logs/clientgenerator-{Date}.txt", LogLevel.Debug);
        }
    });

    services.AddTransient<NSwagValidator>();

    if (!File.Exists(configPath))
    {
        Console.Error.WriteLine($"Configuration file not found: {configPath}");
        Console.WriteLine("\nUse --help to see usage information.");
        return 1;
    }

    var configJson = await File.ReadAllTextAsync(configPath);
    var config = ClientGeneratorConfig.FromJson(configJson);
    
    config.Validate();

    services.AddSingleton(config);
    services.AddTransient<ConfigGenerator>();

    var serviceProvider = services.BuildServiceProvider();
    
    var nswagValidator = serviceProvider.GetRequiredService<NSwagValidator>();
    if (!await nswagValidator.ValidateNSwagVersionAsync())
    {
        Console.WriteLine(
            "Invalid NSwag consolecore version. Remove the existing version:");
        Console.WriteLine("    dotnet tool uninstall -g nswag.consolecore");
        Console.WriteLine("Then install the latest version:");
        Console.WriteLine("    dotnet tool install -g nswag.consolecore");
        return 1;
    }

    var generator = serviceProvider.GetRequiredService<ConfigGenerator>();
    var results = await generator.GenerateClientsAsync();

    Console.WriteLine();
    
    foreach (var result in results)
    {
        if (result.Success)
        {
            Console.WriteLine($"✅ Generated client for {result.ProjectName}");
            Console.WriteLine($"   Output: {result.OutputPath}");
        }
        else
        {
            Console.WriteLine($"❌ Failed to generate client for {result.ProjectName}");
            Console.WriteLine($"   Error: {result.ErrorMessage}");
        }
    }

    var exitCode = results.Any(r => !r.Success) ? 1 : 0;
    if (exitCode != 0)
    {
        Console.WriteLine("\nOne or more clients failed to generate. Check the errors above.");
    }
    return exitCode;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex.Message}");
    Console.WriteLine("\nUse --help to see usage information.");
    return 1;
}