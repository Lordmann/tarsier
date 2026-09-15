using System.Collections.Generic;
using System.Diagnostics;
using Tarsier.Core.Models;

namespace Tarsier.App.Interop;

public sealed record RunningApplication(string DisplayName, string ExecutableName, string ExecutablePath);

/// <summary>Lists the visible top-level windows so a profile can be created by pointing at a running application.</summary>
public static class RunningApplications
{
    public static IReadOnlyList<RunningApplication> Enumerate()
    {
        var ownProcessId = (uint)Environment.ProcessId;
        var found = new Dictionary<string, RunningApplication>(StringComparer.Ordinal);

        NativeMethods.EnumWindows((window, _) =>
        {
            if (!NativeMethods.IsWindowVisible(window))
            {
                return true;
            }

            var title = WindowInspector.GetTitle(window);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            NativeMethods.GetWindowThreadProcessId(window, out var processId);
            if (processId == ownProcessId)
            {
                return true;
            }

            var path = WindowInspector.GetExecutablePath(window);
            if (path is null)
            {
                return true;
            }

            var key = ExecutableKey.Normalize(path);
            if (!found.ContainsKey(key))
            {
                found.Add(key, new RunningApplication(title, key, path));
            }

            return true;
        }, IntPtr.Zero);

        return found.Values.OrderBy(app => app.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
