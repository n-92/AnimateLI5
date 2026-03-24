using System.Text.Json;
using SimpleAnimate.Core.Interfaces;
using SimpleAnimate.Core.Models;

namespace SimpleAnimate.Core.Services;

public class ProjectService : IProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Project CreateNew()
    {
        return new Project();
    }

    public async Task SaveAsync(Project project, string filePath)
    {
        project.FilePath = filePath;
        var json = JsonSerializer.Serialize(project, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<Project> LoadAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var project = JsonSerializer.Deserialize<Project>(json, JsonOptions)
                      ?? throw new InvalidOperationException("Failed to deserialize project.");
        project.FilePath = filePath;
        return project;
    }
}
