using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace ClientGenerator;

public class NSwagValidator(ILogger<NSwagValidator> logger)
{
    private readonly ILogger<NSwagValidator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private static readonly Version MinimumVersion = new(14, 0, 0);

    public async Task<bool> ValidateNSwagVersionAsync()
    {
        try
        {
            var (version, error) = await GetNSwagVersionAsync();
                
            if (version == null)
            {
                _logger.LogError("Failed to get NSwag version: {Error}", error);
                _logger.LogError("Please install NSwag.ConsoleCore using: dotnet tool install -g nswag.consolecore --prerelease");
                return false;
            }

            if (version < MinimumVersion)
            {
                _logger.LogError("NSwag version {CurrentVersion} is not supported. Minimum required version is {MinVersion}", 
                    version, MinimumVersion);
                _logger.LogError("Please update NSwag.ConsoleCore using:");
                _logger.LogError("dotnet tool uninstall -g nswag.consolecore");
                _logger.LogError("dotnet tool install -g nswag.consolecore --prerelease");
                return false;
            }

            _logger.LogInformation("NSwag version {Version} validated successfully", version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating NSwag version");
            return false;
        }
    }

    private async Task<(Version? Version, string? Error)> GetNSwagVersionAsync()
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "nswag",
            Arguments = "version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
            {
                return (null, error);
            }

            // Extract version looking for the explicit "NSwag version: X.X.X.X" line
            var match = Regex.Match(output, @"NSwag version: (\d+\.\d+\.\d+\.\d+)");
            if (!match.Success)
            {
                _logger.LogWarning("Full NSwag output for debugging: {Output}", output);
                return (null, "Unable to parse NSwag version from output");
            }

            if (Version.TryParse(match.Groups[1].Value, out var version))
            {
                return (version, null);
            }

            return (null, "Failed to parse version number");
        }
        catch (Exception ex)
        {
            return (null, $"Error running NSwag: {ex.Message}");
        }
    }
}