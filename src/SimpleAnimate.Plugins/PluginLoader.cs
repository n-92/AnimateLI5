using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleAnimate.Plugins;

/// <summary>
/// Discovers and loads plugin assemblies from a folder at startup.
/// </summary>
public static class PluginLoader
{
    public static List<IPlugin> LoadPlugins(string pluginDirectory)
    {
        var plugins = new List<IPlugin>();

        if (!Directory.Exists(pluginDirectory))
            return plugins;

        foreach (var dll in Directory.GetFiles(pluginDirectory, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

                foreach (var type in pluginTypes)
                {
                    if (Activator.CreateInstance(type) is IPlugin plugin)
                        plugins.Add(plugin);
                }
            }
            catch (Exception ex)
            {
                // Log and skip bad plugins gracefully
                System.Diagnostics.Debug.WriteLine($"Failed to load plugin {dll}: {ex.Message}");
            }
        }

        return plugins;
    }

    public static void RegisterAll(IServiceCollection services, IEnumerable<IPlugin> plugins)
    {
        foreach (var plugin in plugins)
        {
            plugin.Configure(services);
        }
    }
}
