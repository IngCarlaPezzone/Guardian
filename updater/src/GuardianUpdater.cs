using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Threading;
using GuardianShared;

namespace GuardianUpdater
{
    public static class Program
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static int Main(string[] args)
        {
            var command = CommandLine.Parse(args);
            if (command.Has("--self-test")) return SelfTest.Run();

            var log = new UpdaterLog();
            GuardianConfig config = null;
            UpdaterTelemetry telemetry = null;
            string commandId = "";
            try
            {
                config = GuardianConfig.Load();
                telemetry = new UpdaterTelemetry(config);
                commandId = command.Value("--command-id");
                var releaseId = command.Value("--release-id");
                var version = command.Value("--version");
                var sha256 = command.Value("--sha256");
                if (string.IsNullOrWhiteSpace(commandId) || string.IsNullOrWhiteSpace(releaseId) || string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(sha256))
                {
                    throw new InvalidOperationException("missing update arguments");
                }
                var updateContext = UpdateContext.Load(commandId);
                var fromVersion = string.IsNullOrWhiteSpace(updateContext.from_version) ? AppInfo.Version : updateContext.from_version;
                var targetVersion = string.IsNullOrWhiteSpace(updateContext.target_version) ? version : updateContext.target_version;
                var direction = string.IsNullOrWhiteSpace(updateContext.direction) ? VersionDirection(fromVersion, targetVersion) : updateContext.direction;

                Report(config, commandId, "downloading", null);
                log.Write("UpdateStarted target=" + version);
                telemetry.Log("UpdateDownloadStarted", new Dictionary<string, object>
                {
                    { "commandId", commandId },
                    { "releaseId", releaseId },
                    { "previousVersion", fromVersion },
                    { "targetVersion", targetVersion },
                    { "from_version", fromVersion },
                    { "target_version", targetVersion },
                    { "direction", direction }
                });

                var tempRoot = Path.Combine(Path.GetTempPath(), "GuardianUpdate-" + Guid.NewGuid().ToString("N"));
                var zipPath = Path.Combine(tempRoot, "release.zip");
                var extractDir = Path.Combine(tempRoot, "extract");
                Directory.CreateDirectory(tempRoot);
                Directory.CreateDirectory(extractDir);

                Download(config, releaseId, zipPath);
                telemetry.Log("UpdateDownloadCompleted", new Dictionary<string, object>
                {
                    { "commandId", commandId },
                    { "releaseId", releaseId },
                    { "targetVersion", targetVersion },
                    { "from_version", fromVersion },
                    { "target_version", targetVersion },
                    { "direction", direction },
                    { "fileSize", new FileInfo(zipPath).Length }
                });
                var actual = Sha256(zipPath);
                if (!string.Equals(actual, sha256, StringComparison.OrdinalIgnoreCase))
                {
                    Report(config, commandId, "failed", "release hash mismatch");
                    log.Write("UpdateVerificationFailed expected=" + sha256 + " actual=" + actual);
                    telemetry.Log("UpdateFailed", new Dictionary<string, object>
                    {
                        { "commandId", commandId },
                        { "releaseId", releaseId },
                        { "targetVersion", targetVersion },
                        { "from_version", fromVersion },
                        { "target_version", targetVersion },
                        { "direction", direction },
                        { "result", "hash_mismatch" },
                        { "error", "release hash mismatch" }
                    });
                    return 2;
                }

                ZipFile.ExtractToDirectory(zipPath, extractDir);
                Report(config, commandId, "installing", null);
                telemetry.Log("UpdateInstallStarted", new Dictionary<string, object>
                {
                    { "commandId", commandId },
                    { "releaseId", releaseId },
                    { "previousVersion", fromVersion },
                    { "targetVersion", targetVersion },
                    { "from_version", fromVersion },
                    { "target_version", targetVersion },
                    { "direction", direction }
                });

                var appDir = AppInfo.AppDataDir;
                Directory.CreateDirectory(appDir);
                var backupDir = Path.Combine(appDir, "backup-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
                Directory.CreateDirectory(backupDir);

                BackupIfExists(appDir, backupDir, "Guardian.exe");
                BackupIfExists(appDir, backupDir, "Guardian.exe.config");
                BackupIfExists(appDir, backupDir, "GuardianUpdater.exe");
                CopyDirectoryIfExists(appDir, backupDir, "Assets");

                try
                {
                    StopGuardian(log);
                    CopyIfExists(extractDir, appDir, "Guardian.exe");
                    CopyIfExists(extractDir, appDir, "Guardian.exe.config");
                    CopyDirectoryIfExists(extractDir, appDir, "Assets");
                    ScheduleUpdaterReplacement(extractDir, appDir, log);
                    StartGuardian(appDir);
                    Thread.Sleep(5000);
                    if (Process.GetProcessesByName("Guardian").Length == 0)
                    {
                        throw new InvalidOperationException("Guardian did not start after update");
                    }
                    Report(config, commandId, "success", null);
                    log.Write("UpdateInstalled target=" + version);
                    telemetry.Log("UpdateCompleted", new Dictionary<string, object>
                    {
                        { "commandId", commandId },
                        { "releaseId", releaseId },
                        { "previousVersion", fromVersion },
                        { "targetVersion", targetVersion },
                        { "from_version", fromVersion },
                        { "target_version", targetVersion },
                        { "direction", direction },
                        { "result", "success" }
                    });
                    return 0;
                }
                catch (Exception ex)
                {
                    RestoreBackup(backupDir, appDir, log);
                    StartGuardian(appDir);
                    Report(config, commandId, "rolled_back", ex.Message);
                    log.Write("UpdateRolledBack " + ex.Message);
                    telemetry.Log("UpdateFailed", new Dictionary<string, object>
                    {
                        { "commandId", commandId },
                        { "releaseId", releaseId },
                        { "previousVersion", fromVersion },
                        { "targetVersion", targetVersion },
                        { "from_version", fromVersion },
                        { "target_version", targetVersion },
                        { "direction", direction },
                        { "result", "rolled_back" },
                        { "error", ex.Message }
                    });
                    return 3;
                }
            }
            catch (Exception ex)
            {
                if (config != null && !string.IsNullOrWhiteSpace(commandId))
                {
                    Report(config, commandId, "failed", ex.Message);
                }
                log.Write("UpdateFailed " + ex.Message);
                if (telemetry != null)
                {
                    telemetry.Log("UpdateFailed", new Dictionary<string, object>
                    {
                        { "commandId", commandId },
                        { "result", "failed" },
                        { "error", ex.Message }
                    });
                }
                return 1;
            }
        }

        private static void Download(GuardianConfig config, string releaseId, string destination)
        {
            var request = (HttpWebRequest)WebRequest.Create(config.GuardianServerUrl.TrimEnd('/') + "/api/v1/releases/" + releaseId + "/download");
            request.Method = "GET";
            request.Timeout = 30000;
            request.Headers["Authorization"] = "Bearer " + config.DeviceToken;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var input = response.GetResponseStream())
            using (var output = File.Create(destination))
            {
                input.CopyTo(output);
            }
        }

        private static void Report(GuardianConfig config, string commandId, string status, string error)
        {
            if (string.IsNullOrWhiteSpace(config.GuardianServerUrl) || string.IsNullOrWhiteSpace(config.DeviceToken)) return;
            try
            {
                var payload = Serializer.Serialize(new Dictionary<string, object>
                {
                    { "status", status },
                    { "previous_version", AppInfo.Version },
                    { "error_message", error }
                });
                var request = (HttpWebRequest)WebRequest.Create(config.GuardianServerUrl.TrimEnd('/') + "/api/v1/devices/" + config.DeviceId + "/updates/" + commandId + "/status");
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 5000;
                request.Headers["Authorization"] = "Bearer " + config.DeviceToken;
                var bytes = Encoding.UTF8.GetBytes(payload);
                using (var stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length);
                using (var response = (HttpWebResponse)request.GetResponse()) { }
            }
            catch { }
        }

        private static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var input = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
            }
        }

        private static void StopGuardian(UpdaterLog log)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            foreach (var process in Process.GetProcessesByName("Guardian"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(10000);
                    log.Write("Stopped Guardian pid=" + process.Id);
                }
                catch (Exception ex)
                {
                    log.Write("Stop skipped " + ex.Message);
                }
            }
            while (DateTime.UtcNow < deadline)
            {
                if (Process.GetProcessesByName("Guardian").Length == 0) return;
                Thread.Sleep(250);
            }
            if (Process.GetProcessesByName("Guardian").Length != 0)
            {
                throw new InvalidOperationException("Guardian did not stop before update");
            }
        }

        private static void StartGuardian(string appDir)
        {
            var exe = GuardianInstallPaths.GuardianExecutablePath;
            if (!File.Exists(exe)) throw new FileNotFoundException("Guardian.exe not found", exe);
            Process.Start(new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        private static void BackupIfExists(string appDir, string backupDir, string fileName)
        {
            var source = Path.Combine(appDir, fileName);
            if (File.Exists(source)) File.Copy(source, Path.Combine(backupDir, fileName), true);
        }

        private static void CopyIfExists(string sourceDir, string appDir, string fileName)
        {
            var source = Path.Combine(sourceDir, fileName);
            if (File.Exists(source)) File.Copy(source, Path.Combine(appDir, fileName), true);
        }

        internal static void CopyDirectoryIfExists(string sourceRoot, string destinationRoot, string directoryName)
        {
            var source = Path.Combine(sourceRoot, directoryName);
            if (!Directory.Exists(source)) return;
            var destination = Path.Combine(destinationRoot, directoryName);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(Path.Combine(destination, directory.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
            }
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relative = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
        }

        private static void RestoreBackup(string backupDir, string appDir, UpdaterLog log)
        {
            foreach (var fileName in new[] { "Guardian.exe", "Guardian.exe.config" })
            {
                var source = Path.Combine(backupDir, fileName);
                if (File.Exists(source))
                {
                    File.Copy(source, Path.Combine(appDir, fileName), true);
                    log.Write("Restored " + fileName);
                }
            }
            CopyDirectoryIfExists(backupDir, appDir, "Assets");
        }

        private static void ScheduleUpdaterReplacement(string sourceDir, string appDir, UpdaterLog log)
        {
            var source = Path.Combine(sourceDir, "GuardianUpdater.exe");
            if (!File.Exists(source)) return;
            var pending = Path.Combine(appDir, "GuardianUpdater.exe.pending");
            var target = Path.Combine(appDir, "GuardianUpdater.exe");
            File.Copy(source, pending, true);
            var command = "/C ping 127.0.0.1 -n 3 > nul & move /Y " + Quote(pending) + " " + Quote(target) + " > nul";
            Process.Start(new ProcessStartInfo("cmd.exe", command)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            log.Write("UpdaterReplacementScheduled");
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string VersionDirection(string currentVersion, string targetVersion)
        {
            var comparison = CompareSemver(currentVersion, targetVersion);
            if (comparison < 0) return "upgrade";
            if (comparison > 0) return "downgrade";
            return "same";
        }

        private static int CompareSemver(string left, string right)
        {
            int[] a = ParseSemver(left);
            int[] b = ParseSemver(right);
            for (var i = 0; i < 3; i++)
            {
                if (a[i] < b[i]) return -1;
                if (a[i] > b[i]) return 1;
            }
            return 0;
        }

        private static int[] ParseSemver(string value)
        {
            var parts = (value ?? "").Split('.');
            var result = new[] { 0, 0, 0 };
            for (var i = 0; i < result.Length && i < parts.Length; i++)
            {
                int parsed;
                if (int.TryParse(parts[i], out parsed)) result[i] = parsed;
            }
            return result;
        }
    }

    public static class AppInfo
    {
        public static readonly string Version = VersionInfo.Version;
        public static readonly string AppDataDir = ResolveAppDataDir();
        public static string UpdateContextPath
        {
            get { return Path.Combine(AppDataDir, "update-context.json"); }
        }

        private static string ResolveAppDataDir()
        {
            return GuardianInstallPaths.InstallDirectory;
        }
    }

    public sealed class UpdateContext
    {
        public string command_id { get; set; }
        public string release_id { get; set; }
        public string from_version { get; set; }
        public string target_version { get; set; }
        public string direction { get; set; }

        public static UpdateContext Load(string commandId)
        {
            try
            {
                if (!File.Exists(AppInfo.UpdateContextPath)) return new UpdateContext();
                var context = new JavaScriptSerializer().Deserialize<UpdateContext>(File.ReadAllText(AppInfo.UpdateContextPath, Encoding.UTF8));
                if (context == null || !string.Equals(context.command_id, commandId, StringComparison.OrdinalIgnoreCase)) return new UpdateContext();
                return context;
            }
            catch
            {
                return new UpdateContext();
            }
        }
    }

    public sealed class GuardianConfig
    {
        public string DeviceId { get; set; }
        public string GuardianServerUrl { get; set; }
        public string DeviceToken { get; set; }

        public static GuardianConfig Load()
        {
            var path = Path.Combine(AppInfo.AppDataDir, "config.json");
            if (!File.Exists(path)) throw new FileNotFoundException("config.json not found", path);
            return new JavaScriptSerializer().Deserialize<GuardianConfig>(File.ReadAllText(path, Encoding.UTF8));
        }
    }

    public sealed class UpdaterLog
    {
        private readonly string _path;

        public UpdaterLog()
        {
            Directory.CreateDirectory(AppInfo.AppDataDir);
            _path = Path.Combine(AppInfo.AppDataDir, "updater.log");
        }

        public void Write(string message)
        {
            File.AppendAllText(_path, DateTimeOffset.UtcNow.ToString("o") + " " + message + Environment.NewLine, Encoding.UTF8);
        }
    }

    public sealed class UpdaterTelemetry
    {
        private readonly GuardianConfig _config;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public UpdaterTelemetry(GuardianConfig config)
        {
            _config = config;
            try { Directory.CreateDirectory(AppInfo.AppDataDir); } catch { }
        }

        public void Log(string eventType, Dictionary<string, object> payload)
        {
            string json;
            try
            {
                var now = DateTimeOffset.Now;
                var ev = new Dictionary<string, object>
                {
                    { "eventId", Guid.NewGuid().ToString() },
                    { "timestampLocal", now.ToString("o") },
                    { "timestampUtc", now.UtcDateTime.ToString("o") },
                    { "deviceId", _config.DeviceId },
                    { "machineName", Environment.MachineName },
                    { "windowsUser", Environment.UserName },
                    { "eventType", eventType },
                    { "clientVersion", AppInfo.Version },
                    { "payload", payload ?? new Dictionary<string, object>() }
                };
                json = _serializer.Serialize(ev);
            }
            catch
            {
                return;
            }
            var logPath = Path.Combine(AppInfo.AppDataDir, "events.jsonl");
            var pendingPath = Path.Combine(AppInfo.AppDataDir, "events-pending.jsonl");
            TelemetryFileStore.TryAppendLine(logPath, json);
            TelemetryFileStore.TryAppendLine(pendingPath, json);
            try { TrySend(json); } catch { }
        }

        private void TrySend(string eventJson)
        {
            if (string.IsNullOrWhiteSpace(_config.GuardianServerUrl) || string.IsNullOrWhiteSpace(_config.DeviceToken)) return;
            try
            {
                var ev = _serializer.Deserialize<Dictionary<string, object>>(eventJson);
                var payload = new Dictionary<string, object>
                {
                    { "device_id", _config.DeviceId },
                    { "events", new object[]
                        {
                            new Dictionary<string, object>
                            {
                                { "event_id", ev["eventId"] },
                                { "occurred_at", ev["timestampUtc"] },
                                { "event_type", ev["eventType"] },
                                { "client_version", ev["clientVersion"] },
                                { "payload", ev["payload"] }
                            }
                        }
                    }
                };
                var request = (HttpWebRequest)WebRequest.Create(_config.GuardianServerUrl.TrimEnd('/') + "/api/v1/events");
                request.Method = "POST";
                request.Accept = "application/json";
                request.ContentType = "application/json";
                request.Timeout = 5000;
                request.Headers["Authorization"] = "Bearer " + _config.DeviceToken;
                var bytes = Encoding.UTF8.GetBytes(_serializer.Serialize(payload));
                using (var stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length);
                using (var response = (HttpWebResponse)request.GetResponse()) { }
            }
            catch
            {
                // Guardian will retry pending events after restart.
            }
        }
    }

    public static class TelemetryFileStore
    {
        public static bool TryAppendLine(string path, string line)
        {
            try
            {
                WithFileMutex(path, delegate
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.WriteLine(line);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void WithFileMutex(string path, Action action)
        {
            var mutexName = "GuardianTelemetryFile-" + StableHash(Path.GetFullPath(path));
            using (var mutex = new Mutex(false, mutexName))
            {
                var acquired = false;
                try
                {
                    try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(2)); }
                    catch (AbandonedMutexException) { acquired = true; }
                    if (!acquired) throw new IOException("telemetry file is busy");
                    action();
                }
                finally
                {
                    if (acquired)
                    {
                        try { mutex.ReleaseMutex(); } catch { }
                    }
                }
            }
        }

        private static string StableHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes((value ?? "").ToLowerInvariant()));
                return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 24);
            }
        }
    }

    public sealed class CommandLine
    {
        private readonly List<string> _values;

        private CommandLine(List<string> values)
        {
            _values = values;
        }

        public static CommandLine Parse(string[] args)
        {
            return new CommandLine(new List<string>(args ?? new string[0]));
        }

        public bool Has(string name)
        {
            return _values.Exists(v => string.Equals(v, name, StringComparison.OrdinalIgnoreCase));
        }

        public string Value(string name)
        {
            for (var i = 0; i < _values.Count - 1; i++)
            {
                if (string.Equals(_values[i], name, StringComparison.OrdinalIgnoreCase)) return _values[i + 1];
            }
            return "";
        }
    }

    public static class SelfTest
    {
        public static int Run()
        {
            var temp = Path.Combine(Path.GetTempPath(), "GuardianUpdaterSelfTest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                var file = Path.Combine(temp, "sample.txt");
                File.WriteAllText(file, "guardian", Encoding.UTF8);
                var hash = typeof(Program).GetMethod("Main") == null ? "" : "";
                using (var sha = SHA256.Create())
                using (var input = File.OpenRead(file))
                {
                    hash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
                }
                if (hash.Length != 64) return 2;
                var sourceRoot = Path.Combine(temp, "source");
                var destinationRoot = Path.Combine(temp, "destination");
                var iconPath = Path.Combine(sourceRoot, "Assets", "Icons", "sample.png");
                Directory.CreateDirectory(Path.GetDirectoryName(iconPath));
                File.WriteAllText(iconPath, "sample-icon", Encoding.UTF8);
                Program.CopyDirectoryIfExists(sourceRoot, destinationRoot, "Assets");
                if (!File.Exists(Path.Combine(destinationRoot, "Assets", "Icons", "sample.png"))) return 3;
                return 0;
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }
    }
}
