using System.Text.Json;

namespace ClientGenerator.Models;

public class ClientGeneratorConfig
{
    public string[] SolutionPaths { get; set; } = [];
    public string OutputFileNameTemplate { get; set; } = "{Name}Client.ts";
    public string OutputDirectory { get; set; } = string.Empty;

    public static ClientGeneratorConfig FromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var config = JsonSerializer.Deserialize<ClientGeneratorConfig>(json, options);
            
        if (config == null)
            throw new JsonException("Failed to deserialize configuration");

        // Normalize all paths
        config.SolutionPaths = config.SolutionPaths
            .Select(Path.GetFullPath)
            .ToArray();

        if (!string.IsNullOrEmpty(config.OutputDirectory)) 
            config.OutputDirectory = Path.GetFullPath(config.OutputDirectory);

        return config;
    }

    public void Validate()
    {
        if (SolutionPaths == null || SolutionPaths.Length == 0)
            throw new ArgumentException("No solution paths provided in configuration");

        if (string.IsNullOrEmpty(OutputFileNameTemplate))
            throw new ArgumentException("OutputFileNameTemplate is required");

        if (!OutputFileNameTemplate.Contains("{Name}"))
            throw new ArgumentException("OutputFileNameTemplate must contain {Name} placeholder");

        if (string.IsNullOrEmpty(OutputDirectory))
            throw new ArgumentException("OutputDirectory is required");

        // Validate each path
        foreach (var path in SolutionPaths)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Path not found: {path}");

            // Check if it's a valid project or solution file
            var extension = Path.GetExtension(path).ToLower();
            if (extension != ".sln" && extension != ".csproj")
            {
                throw new ArgumentException($"Invalid file type for path: {path}. Must be .sln or .csproj");
            }
        }
    }
}