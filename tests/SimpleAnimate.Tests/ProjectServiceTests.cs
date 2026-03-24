using SimpleAnimate.Core.Services;

namespace SimpleAnimate.Tests;

public class ProjectServiceTests
{
    private readonly ProjectService _sut = new();

    [Fact]
    public void CreateNew_ReturnsProjectWithOneFrame()
    {
        var project = _sut.CreateNew();

        Assert.NotNull(project);
        Assert.Single(project.Frames);
        Assert.Equal(6, project.FramesPerSecond);
        Assert.Equal("My Animation", project.Name);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips()
    {
        var project = _sut.CreateNew();
        project.Name = "Test Project";
        project.FramesPerSecond = 12;

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.sanimate");
        try
        {
            await _sut.SaveAsync(project, tempFile);
            var loaded = await _sut.LoadAsync(tempFile);

            Assert.Equal("Test Project", loaded.Name);
            Assert.Equal(12, loaded.FramesPerSecond);
            Assert.Single(loaded.Frames);
            Assert.Equal(tempFile, loaded.FilePath);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
