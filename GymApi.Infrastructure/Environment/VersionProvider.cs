using System.Reflection;
using System.Runtime.InteropServices;

namespace GymApi.Infrastructure.Environment;

public class VersionProvider( (string? CodeVersion, string? LastCommitDate) version, string runtime)
{
    public string Runtime { get; } = runtime;
    public string CodeVersion { get; } = version.CodeVersion ?? "UnknownVersion";
    public string LastCommitDate { get; } = version.LastCommitDate ?? "UnknownDate";

    public static (string? Version, string? LastCommitDate) ReadVersionFromAssembly()
    {
        var assembly = typeof(VersionProvider).Assembly;
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var lastCommitDateAttribute = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
        return (versionAttribute?.InformationalVersion, lastCommitDateAttribute?.Description);
    }

    public static string GetRuntimeDescription()
    {
        var os = OperatingSystem.IsWindows() ? "Windows" :
            OperatingSystem.IsLinux() ? "Linux" :
            "Unknown OS";

        var cloud = DetectCloudEnvironment();

        var dotnetVersion = RuntimeInformation.FrameworkDescription;

        return $"{dotnetVersion} on {os} {(string.IsNullOrEmpty(cloud) ? "" : $" ({cloud})")} ";
    }

    private static string DetectCloudEnvironment()
    {
        if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
        {
            return "Microsoft Azure";
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("CODESPACES")))
        {
            return "GitHub Codespace";
        }

        return string.Empty;
    }
}
