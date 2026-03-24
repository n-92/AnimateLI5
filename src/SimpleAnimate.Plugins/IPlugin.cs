using Microsoft.Extensions.DependencyInjection;

namespace SimpleAnimate.Plugins;

/// <summary>
/// Contract that all plugins must implement.
/// Plugins register their services/tools via Configure().
/// </summary>
public interface IPlugin
{
    string Name { get; }
    string Description { get; }

    /// <summary>
    /// Register plugin services into the app's DI container.
    /// </summary>
    void Configure(IServiceCollection services);
}
