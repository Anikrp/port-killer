using System.Reflection;

namespace PortKiller.Helpers;

/// <summary>Display version for Settings and tray tooltip (matches InformationalVersion / assembly).</summary>
public static class AppVersionInfo
{
    public static string DisplayVersion
    {
        get
        {
            var asm = Assembly.GetExecutingAssembly();
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
            {
                var plus = info.IndexOf('+');
                return plus > 0 ? info[..plus] : info;
            }

            return asm.GetName().Version?.ToString(3) ?? "4.1";
        }
    }
}
