namespace ClientGenerator.Models;

public class ConfigGenerationResult
{
    public bool Success { get; set; }
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}