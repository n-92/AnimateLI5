using SimpleAnimate.Core.Models;

namespace SimpleAnimate.Core.Interfaces;

public interface IProjectService
{
    Project CreateNew();
    Task SaveAsync(Project project, string filePath);
    Task<Project> LoadAsync(string filePath);
}
