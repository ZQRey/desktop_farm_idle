using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DesktopFarmIdle.Core.Logging;

namespace DesktopFarmIdle.Core;

public static class WindowsDesktopResolver
{
    private static bool _initialized = false;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string desktopRuntimesRoot = Path.Combine(programFiles, @"dotnet\shared\Microsoft.WindowsDesktop.App");

            string? runtimeDir = null;
            if (Directory.Exists(desktopRuntimesRoot))
            {
                var dirs = Directory.GetDirectories(desktopRuntimesRoot);
                // Prefer 8.0.* version
                runtimeDir = dirs.Where(d => Path.GetFileName(d).StartsWith("8.")).OrderByDescending(d => d).FirstOrDefault()
                             ?? dirs.OrderByDescending(d => d).FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(runtimeDir) && Directory.Exists(runtimeDir))
            {
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    try
                    {
                        string simpleName = new AssemblyName(args.Name).Name + ".dll";
                        string targetPath = Path.Combine(runtimeDir, simpleName);
                        if (File.Exists(targetPath))
                        {
                            return Assembly.LoadFrom(targetPath);
                        }
                    }
                    catch { }
                    return null;
                };

                GameLogger.Instance.Info($"WindowsDesktopResolver hooked into {runtimeDir}", "Resolver");
            }
            else
            {
                GameLogger.Instance.Warning("Microsoft.WindowsDesktop.App directory not found.", "Resolver");
            }
        }
        catch (Exception ex)
        {
            GameLogger.Instance.Error($"Error initializing WindowsDesktopResolver: {ex.Message}", ex, "Resolver");
        }
    }
}
