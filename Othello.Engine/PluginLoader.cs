using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Othello.Contract;

namespace Othello.Engine;

public static class PluginLoader
{
    public static IOthelloAI? LoadPlugin(string dllPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(dllPath);
            var aiType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IOthelloAI).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (aiType != null)
            {
                return (IOthelloAI?)Activator.CreateInstance(aiType);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading plugin {dllPath}: {ex.Message}");
        }
        return null;
    }

    public static List<IOthelloAI> LoadPluginsFromDirectory(string directoryPath)
    {
        var plugins = new List<IOthelloAI>();
        if (!Directory.Exists(directoryPath)) return plugins;

        foreach (var file in Directory.GetFiles(directoryPath, "*.dll"))
        {
            var ai = LoadPlugin(file);
            if (ai != null) plugins.Add(ai);
        }
        return plugins;
    }
}
