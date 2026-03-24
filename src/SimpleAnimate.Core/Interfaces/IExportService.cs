using SimpleAnimate.Core.Models;

namespace SimpleAnimate.Core.Interfaces;

public interface IExportService
{
    Task ExportAsync(Project project, string outputPath, ExportFormat format);
}

public enum ExportFormat
{
    Gif,
    Png,
    Video
}
