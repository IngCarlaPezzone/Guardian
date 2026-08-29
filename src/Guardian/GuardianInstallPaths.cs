using System;
using System.IO;
using System.Reflection;

namespace GuardianShared
{
    // This is the sole path authority for the client and updater. GUARDIAN_HOME
    // deliberately remains an explicit test/staging installation root.
    public static class GuardianInstallPaths
    {
        public static bool HasExplicitHome
        {
            get { return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GUARDIAN_HOME")); }
        }

        public static string InstallDirectory
        {
            get
            {
                var overrideDir = Environment.GetEnvironmentVariable("GUARDIAN_HOME");
                if (!string.IsNullOrWhiteSpace(overrideDir)) return Path.GetFullPath(overrideDir);
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Guardian");
            }
        }

        public static string GuardianExecutablePath { get { return Path.Combine(InstallDirectory, "Guardian.exe"); } }
        public static string UpdaterExecutablePath { get { return Path.Combine(InstallDirectory, "GuardianUpdater.exe"); } }
        public static string ExecutingExecutablePath { get { return Path.GetFullPath(Assembly.GetExecutingAssembly().Location); } }

        public static bool SamePath(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
            return string.Equals(Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }

        public static string RedactForTelemetry(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value ?? "";
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var position = string.IsNullOrWhiteSpace(profile) ? -1 : value.IndexOf(profile, StringComparison.OrdinalIgnoreCase);
            return position < 0 ? value : value.Substring(0, position) + "<windows-user>" + value.Substring(position + profile.Length);
        }
    }
}
