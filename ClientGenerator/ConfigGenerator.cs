using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClientGenerator.Models;
using Microsoft.Extensions.Logging;

namespace ClientGenerator;

public class ConfigGenerator(ClientGeneratorConfig config, ILogger<ConfigGenerator> logger)
{
    private readonly ClientGeneratorConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly ILogger<ConfigGenerator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private static JsonObject CreateNSwagConfig(string projectPath, string outputPath)
    {
        return new JsonObject
        {
            ["runtime"] = "Net80",
            ["documentGenerator"] = new JsonObject
            {
                ["aspNetCoreToOpenApi"] = new JsonObject
                {
                    ["project"] = projectPath,
                    ["documentName"] = "v1",
                    ["configuration"] = "Debug",
                    ["targetFramework"] = "net8.0",
                    ["noBuild"] = true,
                    ["aspNetCoreEnvironment"] = "Development"
                }
            },
            ["codeGenerators"] = new JsonObject
            {
                ["openApiToTypeScriptClient"] = new JsonObject
                {
                    ["className"] = "{controller}Client",
                    ["typeScriptVersion"] = 4.3,
                    ["template"] = "Fetch",
                    ["promiseType"] = "Promise",
                    ["httpClass"] = "HttpClient",
                    ["generateClientClasses"] = true,
                    ["generateClientInterfaces"] = false,
                    ["generateOptionalParameters"] = true,
                    ["exportTypes"] = true,
                    ["generateResponseClasses"] = true,
                    ["operationGenerationMode"] = "MultipleClientsFromOperationId",
                    ["markOptionalProperties"] = true,
                    ["typeStyle"] = "Class",
                    ["enumStyle"] = "Enum",
                    ["generateDefaultValues"] = true,
                    ["baseUrlTokenName"] = "API_BASE_URL",
                    ["output"] = outputPath
                }
            }
        };
    }

    private async Task<(int ExitCode, string Error)> ExecuteNSwagCommandAsync(string configPath)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "nswag",
            Arguments = $"run \"{configPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var error = new StringBuilder();
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data)) 
                error.AppendLine(args.Data);
        };

        try
        {
            process.Start();
            process.BeginErrorReadLine();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            _logger.LogInformation("NSwag Output: {Output}", output);

            return (process.ExitCode, error.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute NSwag command");
            return (-1, ex.Message);
        }
    }

    public async Task<List<ConfigGenerationResult>> GenerateClientsAsync()
    {
        var results = new List<ConfigGenerationResult>();
            
        // Ensure output directory exists
        Directory.CreateDirectory(_config.OutputDirectory);

        foreach (var projectPath in _config.SolutionPaths)
        {
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var result = new ConfigGenerationResult { ProjectName = projectName };

            try
            {
                _logger.LogInformation("Generating client for project: {Project}", projectName);
                    
                result.OutputPath = Path.Combine(
                    _config.OutputDirectory,
                    _config.OutputFileNameTemplate.Replace("{Name}", projectName)
                );
                    
                var tempConfigPath = Path.Combine(
                    Path.GetTempPath(),
                    $"nswag_{Guid.NewGuid():N}.json"
                );

                var nswagConfig = CreateNSwagConfig(projectPath, result.OutputPath);
                await File.WriteAllTextAsync(
                    tempConfigPath,
                    JsonSerializer.Serialize(nswagConfig, new JsonSerializerOptions { WriteIndented = true })
                );

                _logger.LogDebug("Created temporary NSwag config at: {Path}", tempConfigPath);

                var nswagResult = await ExecuteNSwagCommandAsync(tempConfigPath);

                try
                {
                    File.Delete(tempConfigPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary config file: {Path}", tempConfigPath);
                }

                result.Success = nswagResult.ExitCode == 0;
                if (!result.Success)
                {
                    result.ErrorMessage = nswagResult.Error;
                    _logger.LogError("Failed to generate client: {Error}", nswagResult.Error);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error generating client for {Project}", projectName);
            }

            results.Add(result);
        }

        return results;
    }
}