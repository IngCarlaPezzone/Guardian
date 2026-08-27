using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using GuardianShared;

namespace Guardian
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            SingleInstanceGuard instanceGuard = null;
            try
            {
                var command = CommandLine.Parse(args);
                var requestedHome = command.Value("--home");
                if (!string.IsNullOrWhiteSpace(requestedHome))
                {
                    Environment.SetEnvironmentVariable("GUARDIAN_HOME", requestedHome, EnvironmentVariableTarget.Process);
                }
                if (command.Has("--self-test"))
                {
                    NativeConsole.Attach();
                    return SelfTest.Run();
                }
                if (command.Has("--install-startup"))
                {
                    NativeConsole.Attach();
                    CanonicalInstallation.EnsureCurrentBuild();
                    return StartupManager.Install();
                }
                if (command.Has("--uninstall-startup"))
                {
                    NativeConsole.Attach();
                    return StartupManager.Uninstall();
                }
                if (command.Has("--unmute-audio"))
                {
                    NativeConsole.Attach();
                    return AudioRecovery.Unmute();
                }
                if (command.Has("--reset-admin"))
                {
                    NativeConsole.Attach();
                    return AdminConfigReset.Reset();
                }
                if (command.Has("--configure-install"))
                {
                    return InstallConfigurator.Run();
                }
                if (command.Has("--watchdog")) return Watchdog.Run(command);

                if (!File.Exists(GuardianConfig.ConfigPath))
                {
                    var configured = InstallConfigurator.Run();
                    if (configured != 0) return configured;
                }

                var config = GuardianConfig.Load();
                var installation = CanonicalInstallation.EnsureCurrentBuild();
                var logger = new EventLogger(config);
                var startup = StartupManager.EnsureCanonicalRegistration(config.AutoStartEnabled);
                instanceGuard = SingleInstanceGuard.TryAcquire();
                if (!instanceGuard.IsAcquired)
                {
                    logger.Log("GuardianDuplicateInstanceSkipped", new Dictionary<string, object>
                    {
                        { "client_version", AppInfo.Version },
                        { "executable_path", GuardianInstallPaths.RedactForTelemetry(GuardianInstallPaths.ExecutingExecutablePath) },
                        { "canonical_executable_path", GuardianInstallPaths.RedactForTelemetry(GuardianInstallPaths.GuardianExecutablePath) },
                        { "startup_command", GuardianInstallPaths.RedactForTelemetry(startup.ConfiguredCommand) },
                        { "startup_repair_result", startup.Result },
                        { "process_id", Process.GetCurrentProcess().Id }
                    });
                    return 0;
                }
                logger.Log("GuardianStarted", new Dictionary<string, object>
                {
                    { "version", AppInfo.Version },
                    { "mode", command.Has("--minimized") ? "startup" : "manual" },
                    { "executable_path", GuardianInstallPaths.RedactForTelemetry(GuardianInstallPaths.ExecutingExecutablePath) },
                    { "canonical_executable_path", GuardianInstallPaths.RedactForTelemetry(GuardianInstallPaths.GuardianExecutablePath) },
                    { "installation_result", installation.Result },
                    { "startup_command", GuardianInstallPaths.RedactForTelemetry(startup.ConfiguredCommand) },
                    { "startup_repair_result", startup.Result },
                    { "process_id", Process.GetCurrentProcess().Id }
                });

                if (config.WatchdogEnabled && !command.Has("--no-watchdog"))
                {
                    Watchdog.StartForCurrentProcess(config, logger);
                }

                var app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var controller = new GuardianController(config, logger);
                app.MainWindow = controller.MainWindow;
                app.Exit += delegate
                {
                    logger.Log("GuardianStopped", new Dictionary<string, object> { { "reason", "application_exit" } });
                    controller.Dispose();
                };
                controller.Start();
                return app.Run();
            }
            catch (Exception ex)
            {
                try
                {
                    var config = GuardianConfig.Load();
                    new EventLogger(config).Log("UnhandledError", new Dictionary<string, object>
                    {
                        { "message", ex.Message },
                        { "stack", ex.ToString() }
                    });
                }
                catch { }
                MessageBox.Show("Guardian encontro un error: " + ex.Message, "Guardian", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
            finally
            {
                if (instanceGuard != null) instanceGuard.Dispose();
            }
        }
    }

    public static class NativeConsole
    {
        private const int AttachParentProcess = -1;

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        public static void Attach()
        {
            try { AttachConsole(AttachParentProcess); }
            catch { }
        }
    }

    public static class AppInfo
    {
        public const string Name = "Guardian";
        public static readonly string Version = VersionInfo.Version;
        // Do not cache this path: --home is applied at the beginning of Main
        // and must be honored even when the CLR initializes this type early.
        public static string AppDataDir
        {
            get { return ResolveAppDataDir(); }
        }
        public static string IntentionalExitFlagPath
        {
            get { return Path.Combine(AppDataDir, "intentional-exit.flag"); }
        }
        public static string UpdateContextPath
        {
            get { return Path.Combine(AppDataDir, "update-context.json"); }
        }

        private static string ResolveAppDataDir()
        {
            return GuardianInstallPaths.InstallDirectory;
        }
    }

    public sealed class CanonicalInstallationResult
    {
        public string Result { get; set; }
    }

    public static class CanonicalInstallation
    {
        // A release may be launched directly from an extracted ZIP. Copy its
        // binaries before enabling startup so reboot never depends on that ZIP.
        public static CanonicalInstallationResult EnsureCurrentBuild()
        {
            var result = new CanonicalInstallationResult { Result = "already_canonical" };
            try
            {
                var source = GuardianInstallPaths.ExecutingExecutablePath;
                var destination = GuardianInstallPaths.GuardianExecutablePath;
                if (GuardianInstallPaths.SamePath(source, destination)) return result;

                Directory.CreateDirectory(GuardianInstallPaths.InstallDirectory);
                File.Copy(source, destination, true);
                CopySiblingIfPresent(source + ".config", destination + ".config");
                CopySiblingIfPresent(Path.Combine(Path.GetDirectoryName(source), "GuardianUpdater.exe"), GuardianInstallPaths.UpdaterExecutablePath);
                result.Result = "migrated_to_canonical";
            }
            catch (Exception ex)
            {
                result.Result = "migration_failed:" + ex.GetType().Name;
            }
            return result;
        }

        private static void CopySiblingIfPresent(string source, string destination)
        {
            if (File.Exists(source)) File.Copy(source, destination, true);
        }
    }

    public sealed class GuardianConfig
    {
        public int IntervalSeconds { get; set; }
        public int TestIntervalSeconds { get; set; }
        public bool UseTestInterval { get; set; }
        public bool WatchdogEnabled { get; set; }
        public bool AutoStartEnabled { get; set; }
        public string Difficulty { get; set; }
        public string DeviceId { get; set; }
        public string MachineName { get; set; }
        public string DisplayName { get; set; }
        public string GuardianServerUrl { get; set; }
        public string DeviceBootstrapToken { get; set; }
        public string DeviceToken { get; set; }
        public int RemoteConfigVersion { get; set; }
        public string PendingUpdateCommandId { get; set; }
        public string LastDeviceCommandId { get; set; }
        public bool MonitoringEnabled { get; set; }
        public string UpdaterPath { get; set; }
        public string RemoteWebhookUrl { get; set; }
        public string RemoteAuthToken { get; set; }
        public string RemoteConfigUrl { get; set; }
        public int RemoteConfigPollSeconds { get; set; }
        public bool PauseMediaOnMission { get; set; }
        public bool AllowUnsafeMediaToggle { get; set; }
        public bool MuteSystemAudioDuringMission { get; set; }
        public bool ResumeMediaAfterMission { get; set; }
        public string AdminUsername { get; set; }
        public string AdminPasswordSha256 { get; set; }
        public int MaxSolvedMissionsBeforeAutoExit { get; set; }
        public MissionConfig MissionConfig { get; set; }
        public MissionRotationState MissionRotationState { get; set; }

        public static string ConfigPath
        {
            get { return Path.Combine(AppInfo.AppDataDir, "config.json"); }
        }

        public static GuardianConfig Default()
        {
            return new GuardianConfig
            {
                IntervalSeconds = 15 * 60,
                TestIntervalSeconds = 60,
                UseTestInterval = false,
                WatchdogEnabled = true,
                AutoStartEnabled = true,
                Difficulty = "9-11",
                DeviceId = Guid.NewGuid().ToString(),
                MachineName = Environment.MachineName,
                DisplayName = "",
                GuardianServerUrl = NormalizeServerUrlOrEmpty(Environment.GetEnvironmentVariable("GUARDIAN_SERVER_URL")),
                DeviceBootstrapToken = Environment.GetEnvironmentVariable("DEVICE_BOOTSTRAP_TOKEN") ?? "",
                DeviceToken = "",
                RemoteConfigVersion = 0,
                PendingUpdateCommandId = "",
                LastDeviceCommandId = "",
                MonitoringEnabled = true,
                UpdaterPath = "",
                RemoteWebhookUrl = "",
                RemoteAuthToken = "",
                RemoteConfigUrl = "",
                RemoteConfigPollSeconds = 60,
                PauseMediaOnMission = false,
                AllowUnsafeMediaToggle = false,
                MuteSystemAudioDuringMission = true,
                ResumeMediaAfterMission = false,
                AdminUsername = "admin",
                AdminPasswordSha256 = AdminAuth.HashPassword("guardian"),
                MaxSolvedMissionsBeforeAutoExit = 3,
                // Preserve the Stage 1 behavior for existing installations. Comprehension is opt-in.
                MissionConfig = MissionConfig.Default(),
                MissionRotationState = new MissionRotationState()
            };
        }

        public int EffectiveIntervalSeconds
        {
            get { return UseTestInterval ? TestIntervalSeconds : IntervalSeconds; }
        }

        public static GuardianConfig Load()
        {
            Directory.CreateDirectory(AppInfo.AppDataDir);
            if (!File.Exists(ConfigPath))
            {
                var created = Default();
                created.Save();
                return created;
            }

            var serializer = new JavaScriptSerializer();
            var loaded = serializer.Deserialize<GuardianConfig>(File.ReadAllText(ConfigPath, Encoding.UTF8));
            var defaults = Default();
            if (loaded.IntervalSeconds <= 0) loaded.IntervalSeconds = defaults.IntervalSeconds;
            if (loaded.TestIntervalSeconds <= 0) loaded.TestIntervalSeconds = defaults.TestIntervalSeconds;
            if (string.IsNullOrWhiteSpace(loaded.Difficulty)) loaded.Difficulty = defaults.Difficulty;
            loaded.MachineName = Environment.MachineName;
            if (!IsUuid(loaded.DeviceId)) loaded.DeviceId = defaults.DeviceId;
            if (loaded.DisplayName == null) loaded.DisplayName = defaults.DisplayName;
            if (string.IsNullOrWhiteSpace(loaded.GuardianServerUrl)) loaded.GuardianServerUrl = defaults.GuardianServerUrl;
            loaded.GuardianServerUrl = NormalizeServerUrlOrEmpty(loaded.GuardianServerUrl);
            if (string.IsNullOrWhiteSpace(loaded.DeviceBootstrapToken)) loaded.DeviceBootstrapToken = defaults.DeviceBootstrapToken;
            if (loaded.DeviceToken == null) loaded.DeviceToken = defaults.DeviceToken;
            if (loaded.PendingUpdateCommandId == null) loaded.PendingUpdateCommandId = defaults.PendingUpdateCommandId;
            if (loaded.LastDeviceCommandId == null) loaded.LastDeviceCommandId = defaults.LastDeviceCommandId;
            if (loaded.UpdaterPath == null) loaded.UpdaterPath = defaults.UpdaterPath;
            if (loaded.RemoteConfigPollSeconds <= 0) loaded.RemoteConfigPollSeconds = defaults.RemoteConfigPollSeconds;
            if (string.IsNullOrWhiteSpace(loaded.AdminUsername)) loaded.AdminUsername = defaults.AdminUsername;
            if (string.IsNullOrWhiteSpace(loaded.AdminPasswordSha256)) loaded.AdminPasswordSha256 = defaults.AdminPasswordSha256;
            if (loaded.MaxSolvedMissionsBeforeAutoExit < 0) loaded.MaxSolvedMissionsBeforeAutoExit = defaults.MaxSolvedMissionsBeforeAutoExit;
            if (loaded.MissionConfig == null) loaded.MissionConfig = MissionConfig.Default();
            if (loaded.MissionConfig.EnabledSkills == null) loaded.MissionConfig.EnabledSkills = MissionConfig.Default().EnabledSkills;
            if (loaded.MissionConfig.PrivateProfile == null) loaded.MissionConfig.PrivateProfile = new PrivateMissionProfile();
            if (loaded.MissionRotationState == null) loaded.MissionRotationState = new MissionRotationState();
            if (loaded.MissionRotationState.UsedSkillsInCycle == null) loaded.MissionRotationState.UsedSkillsInCycle = new List<string>();
            if (loaded.MissionRotationState.LastVariantBySkill == null) loaded.MissionRotationState.LastVariantBySkill = new Dictionary<string, string>();
            EnsureBooleanDefaults(loaded, File.ReadAllText(ConfigPath, Encoding.UTF8));
            loaded.Save();
            return loaded;
        }

        private static bool IsUuid(string value)
        {
            Guid parsed;
            return Guid.TryParse(value, out parsed);
        }

        private static string NormalizeServerUrlOrEmpty(string value)
        {
            string normalized;
            string error;
            return InstallConfigurator.TryNormalizeGuardianServerUrl(value, out normalized, out error) ? normalized : "";
        }

        private static void EnsureBooleanDefaults(GuardianConfig loaded, string rawJson)
        {
            if (rawJson.IndexOf("\"PauseMediaOnMission\"", StringComparison.OrdinalIgnoreCase) < 0)
            {
                loaded.PauseMediaOnMission = false;
            }
            if (rawJson.IndexOf("\"AllowUnsafeMediaToggle\"", StringComparison.OrdinalIgnoreCase) < 0)
            {
                loaded.AllowUnsafeMediaToggle = false;
            }
            if (rawJson.IndexOf("\"MuteSystemAudioDuringMission\"", StringComparison.OrdinalIgnoreCase) < 0)
            {
                loaded.MuteSystemAudioDuringMission = true;
            }
            if (rawJson.IndexOf("\"ResumeMediaAfterMission\"", StringComparison.OrdinalIgnoreCase) < 0)
            {
                loaded.ResumeMediaAfterMission = false;
            }
            if (rawJson.IndexOf("\"MonitoringEnabled\"", StringComparison.OrdinalIgnoreCase) < 0)
            {
                loaded.MonitoringEnabled = true;
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(AppInfo.AppDataDir);
            var serializer = new JavaScriptSerializer();
            File.WriteAllText(ConfigPath, serializer.Serialize(this), Encoding.UTF8);
        }
    }

    public static class InstallConfigurator
    {
        public static int Run()
        {
            try
            {
                Directory.CreateDirectory(AppInfo.AppDataDir);
                if (File.Exists(GuardianConfig.ConfigPath)) return 0;

                var serverUrl = Environment.GetEnvironmentVariable("GUARDIAN_SERVER_URL") ?? "";
                var bootstrapToken = Environment.GetEnvironmentVariable("DEVICE_BOOTSTRAP_TOKEN") ?? "";
                var interactive = string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(bootstrapToken);

                if (interactive && !PromptForInstallConfig(ref serverUrl, ref bootstrapToken))
                {
                    return 1;
                }

                string normalizedUrl;
                string urlError;
                if (!TryNormalizeGuardianServerUrl(serverUrl, out normalizedUrl, out urlError))
                {
                    ShowInstallMessage(urlError, true, interactive);
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(bootstrapToken))
                {
                    ShowInstallMessage("Falta el bootstrap token.", true, interactive);
                    return 1;
                }

                var config = GuardianConfig.Default();
                config.GuardianServerUrl = normalizedUrl;
                config.DeviceBootstrapToken = bootstrapToken.Trim();
                config.DeviceToken = "";
                config.Save();
                ShowInstallMessage("Configuracion inicial creada. Guardian registrara esta PC al iniciar.", false, interactive);
                return 0;
            }
            catch (Exception ex)
            {
                ShowInstallMessage("No se pudo crear la configuracion inicial:\n\n" + ex.Message, true, true);
                return 1;
            }
        }

        private static bool PromptForInstallConfig(ref string serverUrl, ref string bootstrapToken)
        {
            WinForms.Application.EnableVisualStyles();
            using (var form = new WinForms.Form())
            using (var urlBox = new WinForms.TextBox())
            using (var tokenBox = new WinForms.TextBox())
            using (var startupBox = new WinForms.CheckBox())
            using (var ok = new WinForms.Button())
            using (var cancel = new WinForms.Button())
            {
                form.Text = "Guardian - primera instalacion";
                form.Width = 520;
                form.Height = 260;
                form.FormBorderStyle = WinForms.FormBorderStyle.FixedDialog;
                form.StartPosition = WinForms.FormStartPosition.CenterScreen;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var intro = new WinForms.Label();
                intro.Left = 16;
                intro.Top = 14;
                intro.Width = 470;
                intro.Height = 34;
                intro.Text = "Ingresar los datos provistos por el administrador para registrar esta PC.";

                var urlLabel = new WinForms.Label();
                urlLabel.Left = 16;
                urlLabel.Top = 58;
                urlLabel.Width = 160;
                urlLabel.Text = "URL del servidor";

                urlBox.Left = 180;
                urlBox.Top = 54;
                urlBox.Width = 300;
                urlBox.Text = serverUrl;

                var tokenLabel = new WinForms.Label();
                tokenLabel.Left = 16;
                tokenLabel.Top = 96;
                tokenLabel.Width = 160;
                tokenLabel.Text = "Bootstrap token";

                tokenBox.Left = 180;
                tokenBox.Top = 92;
                tokenBox.Width = 300;
                tokenBox.UseSystemPasswordChar = true;
                tokenBox.Text = bootstrapToken;

                startupBox.Left = 180;
                startupBox.Top = 126;
                startupBox.Width = 300;
                startupBox.Checked = true;
                startupBox.Text = "Iniciar automaticamente con Windows";

                ok.Text = "Instalar";
                ok.Left = 300;
                ok.Top = 166;
                ok.Width = 85;
                ok.DialogResult = WinForms.DialogResult.OK;

                cancel.Text = "Cancelar";
                cancel.Left = 395;
                cancel.Top = 166;
                cancel.Width = 85;
                cancel.DialogResult = WinForms.DialogResult.Cancel;

                form.Controls.Add(intro);
                form.Controls.Add(urlLabel);
                form.Controls.Add(urlBox);
                form.Controls.Add(tokenLabel);
                form.Controls.Add(tokenBox);
                form.Controls.Add(startupBox);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;

                while (true)
                {
                    if (form.ShowDialog() != WinForms.DialogResult.OK) return false;
                    string normalizedUrl;
                    string urlError;
                    if (!TryNormalizeGuardianServerUrl(urlBox.Text, out normalizedUrl, out urlError))
                    {
                        WinForms.MessageBox.Show(urlError, "Guardian", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                        urlBox.Focus();
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(tokenBox.Text))
                    {
                        WinForms.MessageBox.Show("Ingresar el bootstrap token provisto por el administrador.", "Guardian", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                        tokenBox.Focus();
                        continue;
                    }
                    serverUrl = normalizedUrl;
                    bootstrapToken = tokenBox.Text;
                    break;
                }
                if (startupBox.Checked)
                {
                    string error;
                    StartupManager.TryInstall(out error);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        WinForms.MessageBox.Show(
                            "Guardian quedo configurado, pero Windows no permitio registrar el autoarranque:\n\n" + error,
                            "Guardian",
                            WinForms.MessageBoxButtons.OK,
                            WinForms.MessageBoxIcon.Warning);
                    }
                }
                return true;
            }
        }

        public static bool TryNormalizeGuardianServerUrl(string value, out string normalized, out string error)
        {
            normalized = "";
            error = "";
            var trimmed = (value ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                error = "Ingresar la URL del Guardian Server, por ejemplo http://servidor:8080.";
                return false;
            }
            if (trimmed.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
            {
                error = "La URL del Guardian Server no puede contener espacios. Revisar el hostname y volver a intentar.";
                return false;
            }

            Uri uri;
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrWhiteSpace(uri.Host))
            {
                error = "La URL del Guardian Server debe ser una URL HTTP o HTTPS valida, por ejemplo http://servidor:8080.";
                return false;
            }

            normalized = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            return true;
        }

        private static void ShowInstallMessage(string message, bool error, bool interactive)
        {
            if (!interactive) return;
            WinForms.MessageBox.Show(
                message,
                "Guardian",
                WinForms.MessageBoxButtons.OK,
                error ? WinForms.MessageBoxIcon.Error : WinForms.MessageBoxIcon.Information);
        }
    }

    public sealed class GuardianEvent
    {
        public string eventId { get; set; }
        public string timestampLocal { get; set; }
        public string timestampUtc { get; set; }
        public string deviceId { get; set; }
        public string machineName { get; set; }
        public string windowsUser { get; set; }
        public string eventType { get; set; }
        public string clientVersion { get; set; }
        public Dictionary<string, object> payload { get; set; }
    }

    public sealed class SingleInstanceGuard : IDisposable
    {
        private readonly Mutex _mutex;
        private bool _ownsMutex;

        private SingleInstanceGuard(Mutex mutex, bool ownsMutex)
        {
            _mutex = mutex;
            _ownsMutex = ownsMutex;
        }

        public bool IsAcquired
        {
            get { return _ownsMutex; }
        }

        public static SingleInstanceGuard TryAcquire()
        {
            var mutex = new Mutex(false, "GuardianSingleInstance-" + StableHash(AppInfo.AppDataDir));
            try
            {
                var acquired = mutex.WaitOne(0);
                return new SingleInstanceGuard(mutex, acquired);
            }
            catch (AbandonedMutexException)
            {
                return new SingleInstanceGuard(mutex, true);
            }
        }

        public void Dispose()
        {
            if (_ownsMutex)
            {
                try { _mutex.ReleaseMutex(); } catch { }
                _ownsMutex = false;
            }
            try { _mutex.Dispose(); } catch { }
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

    public sealed class EventLogger
    {
        private readonly GuardianConfig _config;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        public EventLogger(GuardianConfig config)
        {
            _config = config;
            try { Directory.CreateDirectory(AppInfo.AppDataDir); } catch { }
        }

        public static string LogPath
        {
            get { return Path.Combine(AppInfo.AppDataDir, "events.jsonl"); }
        }

        public static string PendingPath
        {
            get { return Path.Combine(AppInfo.AppDataDir, "events-pending.jsonl"); }
        }

        public void Log(string eventType, Dictionary<string, object> payload)
        {
            string json;
            try
            {
                var now = DateTimeOffset.Now;
                var ev = new GuardianEvent
                {
                    eventId = Guid.NewGuid().ToString(),
                    timestampLocal = now.ToString("o"),
                    timestampUtc = now.UtcDateTime.ToString("o"),
                    deviceId = _config.DeviceId,
                    machineName = Environment.MachineName,
                    windowsUser = Environment.UserName,
                    eventType = eventType,
                    clientVersion = AppInfo.Version,
                    payload = payload ?? new Dictionary<string, object>()
                };
                json = _serializer.Serialize(ev);
            }
            catch
            {
                return;
            }

            TelemetryFileStore.TryAppendLine(LogPath, json);
            TelemetryFileStore.TryAppendLine(PendingPath, json);

            try { RemoteReporter.TrySendAsync(_config, json); } catch { }
            try { TelemetrySync.TryFlushAsync(_config); } catch { }
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

        public static List<string> ReadAllLines(string path)
        {
            return WithFileMutex(path, delegate
            {
                if (!File.Exists(path)) return new List<string>();
                return new List<string>(File.ReadAllLines(path, Encoding.UTF8));
            });
        }

        public static void RemoveAcceptedEvents(string path, HashSet<string> accepted, JavaScriptSerializer serializer)
        {
            WithFileMutex(path, delegate
            {
                if (!File.Exists(path)) return;
                var current = new List<string>(File.ReadAllLines(path, Encoding.UTF8));
                var remaining = new List<string>();
                foreach (var line in current)
                {
                    GuardianEvent ev = null;
                    try { ev = serializer.Deserialize<GuardianEvent>(line); } catch { }
                    if (ev == null || string.IsNullOrWhiteSpace(ev.eventId) || !accepted.Contains(ev.eventId))
                    {
                        remaining.Add(line);
                    }
                }

                var temp = path + ".tmp";
                File.WriteAllLines(temp, remaining.ToArray(), Encoding.UTF8);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
            });
        }

        private static void WithFileMutex(string path, Action action)
        {
            WithFileMutex<object>(path, delegate
            {
                action();
                return null;
            });
        }

        private static T WithFileMutex<T>(string path, Func<T> action)
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
                    return action();
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

    public sealed class EventBatchResponse
    {
        public bool ok { get; set; }
        public List<string> accepted_event_ids { get; set; }
    }

    public static class TelemetrySync
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();
        private static readonly object SyncLock = new object();
        private static bool _running;
        private static int _failureCount;
        private static DateTimeOffset _nextAttemptUtc = DateTimeOffset.MinValue;

        public static void TryFlushAsync(GuardianConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.GuardianServerUrl) || string.IsNullOrWhiteSpace(config.DeviceToken)) return;
            if (DateTimeOffset.UtcNow < _nextAttemptUtc) return;
            lock (SyncLock)
            {
                if (_running) return;
                _running = true;
            }

            Task.Run(delegate
            {
                try
                {
                    Flush(config);
                    _failureCount = 0;
                    _nextAttemptUtc = DateTimeOffset.MinValue;
                }
                catch
                {
                    _failureCount = Math.Min(_failureCount + 1, 5);
                    _nextAttemptUtc = DateTimeOffset.UtcNow.AddSeconds(15 * _failureCount);
                }
                finally
                {
                    lock (SyncLock) _running = false;
                }
            });
        }

        private static void Flush(GuardianConfig config)
        {
            var path = EventLogger.PendingPath;

            var lines = TelemetryFileStore.ReadAllLines(path);
            if (lines.Count == 0) return;

            var batchLines = lines.Count > 100 ? lines.GetRange(0, 100) : new List<string>(lines);
            var events = new List<Dictionary<string, object>>();
            foreach (var line in batchLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                GuardianEvent ev;
                try { ev = Serializer.Deserialize<GuardianEvent>(line); }
                catch { continue; }
                if (ev == null || string.IsNullOrWhiteSpace(ev.eventId) || string.IsNullOrWhiteSpace(ev.eventType)) continue;
                events.Add(new Dictionary<string, object>
                {
                    { "event_id", ev.eventId },
                    { "occurred_at", string.IsNullOrWhiteSpace(ev.timestampUtc) ? DateTimeOffset.UtcNow.ToString("o") : ev.timestampUtc },
                    { "event_type", ev.eventType },
                    { "client_version", string.IsNullOrWhiteSpace(ev.clientVersion) ? AppInfo.Version : ev.clientVersion },
                    { "payload", ev.payload ?? new Dictionary<string, object>() }
                });
            }
            if (events.Count == 0) return;

            var payload = new Dictionary<string, object>
            {
                { "device_id", config.DeviceId },
                { "events", events }
            };
            var response = PostEvents(config, payload);
            if (response == null || response.accepted_event_ids == null || response.accepted_event_ids.Count == 0) return;
            var accepted = new HashSet<string>(response.accepted_event_ids, StringComparer.OrdinalIgnoreCase);
            TelemetryFileStore.RemoveAcceptedEvents(path, accepted, Serializer);
        }

        private static EventBatchResponse PostEvents(GuardianConfig config, Dictionary<string, object> payload)
        {
            var request = (HttpWebRequest)WebRequest.Create(config.GuardianServerUrl.TrimEnd('/') + "/api/v1/events");
            request.Method = "POST";
            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Timeout = 5000;
            request.Headers["Authorization"] = "Bearer " + config.DeviceToken;
            var bytes = Encoding.UTF8.GetBytes(Serializer.Serialize(payload));
            using (var stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length);
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return Serializer.Deserialize<EventBatchResponse>(reader.ReadToEnd());
            }
        }
    }

    public static class RemoteReporter
    {
        public static void TrySendAsync(GuardianConfig config, string json)
        {
            if (string.IsNullOrWhiteSpace(config.RemoteWebhookUrl)) return;

            Task.Run(delegate
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(config.RemoteWebhookUrl);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Timeout = 5000;
                    if (!string.IsNullOrWhiteSpace(config.RemoteAuthToken))
                    {
                        request.Headers["Authorization"] = "Bearer " + config.RemoteAuthToken;
                    }
                    var bytes = Encoding.UTF8.GetBytes(json);
                    using (var stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length);
                    using (var response = (HttpWebResponse)request.GetResponse()) { }
                }
                catch
                {
                    // Guardian must keep working offline. Failed sends remain available in the local JSONL log.
                }
            });
        }
    }

    public sealed class GuardianController : IDisposable
    {
        private readonly GuardianConfig _config;
        private readonly EventLogger _logger;
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _remoteConfigTimer;
        private readonly MissionCatalog _missionCatalog = new MissionCatalog();
        private readonly MissionSelector _missionSelector;
        private readonly UsageCounter _counter;
        private bool _sessionUnlocked = true;
        private bool _suspended;
        private bool _missionActive;
        private LockWindow _lockWindow;
        private MediaInterruptionSession _mediaSession;
        private GuardianTray _tray;
        private bool _monitoringEnabled;
        private readonly MissionUnavailableDeduplicator _missionUnavailableDeduplicator = new MissionUnavailableDeduplicator();

        public Window MainWindow { get; private set; }

        public GuardianController(GuardianConfig config, EventLogger logger)
        {
            _config = config;
            _logger = logger;
            _missionSelector = new MissionSelector(config, _missionCatalog);
            _counter = new UsageCounter(config.EffectiveIntervalSeconds);
            MainWindow = new StatusWindow(config);
            MainWindow.Closing += delegate(object sender, System.ComponentModel.CancelEventArgs e)
            {
                e.Cancel = true;
                MainWindow.Hide();
            };

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTick;

            _remoteConfigTimer = new DispatcherTimer();
            _remoteConfigTimer.Interval = TimeSpan.FromSeconds(Math.Max(60, config.RemoteConfigPollSeconds));
            _remoteConfigTimer.Tick += OnRemoteConfigTick;
        }

        public void Start()
        {
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            _tray = new GuardianTray(this, _config, _logger);
            StartRemotePolling();
            if (_config.MonitoringEnabled) StartMonitoring("app_start");
            else _tray.SetMonitoringState(false);
            MainWindow.Hide();
        }

        private void StartRemotePolling()
        {
            if (!string.IsNullOrWhiteSpace(_config.RemoteConfigUrl) || !string.IsNullOrWhiteSpace(_config.GuardianServerUrl))
            {
                _remoteConfigTimer.Start();
                OnRemoteConfigTick(this, EventArgs.Empty);
            }
        }

        public void StartMonitoring(string reason)
        {
            if (_monitoringEnabled) return;
            _monitoringEnabled = true;
            _config.MonitoringEnabled = true;
            _config.Save();
            _logger.Log("UsageCounterStarted", new Dictionary<string, object>
            {
                { "intervalSeconds", _config.EffectiveIntervalSeconds },
                { "reason", reason }
            });
            _logger.Log("MonitoringResumed", new Dictionary<string, object>
            {
                { "source", MonitoringSource(reason) },
                { "reason", reason }
            });
            _timer.Start();
            if (_tray != null) _tray.SetMonitoringState(true);
        }

        public void StopMonitoring(string reason)
        {
            _timer.Stop();
            _monitoringEnabled = false;
            _config.MonitoringEnabled = false;
            _config.Save();
            _counter.Reset();
            _missionActive = false;
            if (_mediaSession != null)
            {
                _mediaSession.Dispose();
                _mediaSession = null;
            }
            if (_lockWindow != null)
            {
                _lockWindow.AllowProgrammaticClose();
                _lockWindow.Close();
                _lockWindow = null;
            }
            _logger.Log("UsageCounterPaused", new Dictionary<string, object> { { "reason", reason } });
            _logger.Log("MonitoringPaused", new Dictionary<string, object>
            {
                { "source", MonitoringSource(reason) },
                { "reason", reason }
            });
            if (_tray != null) _tray.SetMonitoringState(false);
        }

        private static string MonitoringSource(string reason)
        {
            if ((reason ?? "").StartsWith("remote_", StringComparison.OrdinalIgnoreCase)) return "remote";
            if ((reason ?? "").StartsWith("tray_", StringComparison.OrdinalIgnoreCase)) return "tray";
            return "local";
        }

        public void ExitCompletely(string reason)
        {
            try
            {
                Directory.CreateDirectory(AppInfo.AppDataDir);
                File.WriteAllText(AppInfo.IntentionalExitFlagPath, DateTimeOffset.UtcNow.ToString("o"), Encoding.UTF8);
                _logger.Log("GuardianExitRequested", new Dictionary<string, object> { { "reason", reason } });
            }
            catch (Exception ex)
            {
                _logger.Log("Error", new Dictionary<string, object>
                {
                    { "source", "intentional_exit_flag" },
                    { "message", ex.Message }
                });
            }
            Application.Current.Shutdown();
        }

        public bool TriggerMissionNow()
        {
            if (_missionActive) return false;
            ShowMission(MissionTrigger.Manual);
            return true;
        }

        public bool AuthenticateAdmin(string username, string password)
        {
            return AdminAuth.Verify(_config, username, password);
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_counter.Tick(_sessionUnlocked && !_suspended && !_missionActive)) return;
            if (_counter.ShouldTriggerMission)
            {
                ShowMission(MissionTrigger.Timer);
            }
        }

        private void ShowMission(MissionTrigger trigger)
        {
            var mission = _missionSelector.Next();
            var availabilitySignature = MissionAvailabilitySignature();
            if (mission == null)
            {
                if (_missionUnavailableDeduplicator.ShouldLog(false, availabilitySignature)) _logger.Log("MissionUnavailable", new Dictionary<string, object>
                {
                    { "reason", "no_effective_skills" }
                });
                return;
            }
            _missionUnavailableDeduplicator.ShouldLog(true, availabilitySignature);
            _missionActive = true;
            _mediaSession = MediaInterruptionSession.Start(_config, _logger, mission.Id, trigger);
            var startedPayload = MissionTelemetry.Payload(mission, 1);
            startedPayload["elapsedSeconds"] = _counter.ElapsedSeconds;
            startedPayload["trigger"] = trigger.ToString().ToLowerInvariant();
            _logger.Log("MissionStarted", startedPayload);
            _logger.Log("DeviceLocked", new Dictionary<string, object> { { "missionId", mission.Id } });

            _lockWindow = new LockWindow(mission, _config, _logger);
            _lockWindow.UnlockRequested += delegate
            {
                _counter.Reset();
                _missionActive = false;
                _logger.Log("DeviceUnlocked", new Dictionary<string, object> { { "missionId", mission.Id } });
                if (_mediaSession != null)
                {
                    _mediaSession.Dispose();
                    _mediaSession = null;
                }
                _lockWindow = null;
            };
            _lockWindow.AdminShutdownRequested += delegate
            {
                _logger.Log("AdminShutdownRequested", new Dictionary<string, object> { { "missionId", mission.Id }, { "behavior", "stop_monitoring" } });
                StopMonitoring("admin_exit_from_lock");
            };
            _lockWindow.Show();
            _lockWindow.Activate();
        }

        private string MissionAvailabilitySignature()
        {
            var missionConfig = _config.MissionConfig;
            var skills = missionConfig == null || missionConfig.EnabledSkills == null ? "" : string.Join("|", missionConfig.EnabledSkills.ToArray());
            var profile = missionConfig == null ? null : missionConfig.PrivateProfile;
            return skills + "|" + (profile != null && !string.IsNullOrWhiteSpace(profile.PreferredName)) + "|" + (profile != null && !string.IsNullOrWhiteSpace(profile.FirstName)) + "|" + (profile != null && !string.IsNullOrWhiteSpace(profile.LastName)) + "|" + (profile != null && !string.IsNullOrWhiteSpace(profile.BirthDate));
        }

        private void OnRemoteConfigTick(object sender, EventArgs e)
        {
            GuardianServerClient.TrySyncAsync(_config, _logger, delegate { return _monitoringEnabled; }, delegate(GuardianRemoteConfig remote)
            {
                ApplyRemoteInterval(remote);
            }, delegate(UpdateCommandInfo update)
            {
                LaunchUpdater(update);
            }, delegate(DeviceCommandInfo command)
            {
                ProcessRemoteCommand(command);
            });

            RemoteConfigClient.TryFetchAsync(_config, delegate(GuardianConfig remote)
            {
                if (remote == null) return;
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    ApplyLegacyRemoteConfig(remote);
                }));
            }, delegate(Exception ex)
            {
                _logger.Log("Error", new Dictionary<string, object>
                {
                    { "source", "remote_config" },
                    { "message", ex.Message }
                });
            });
        }

        private void ProcessRemoteCommand(DeviceCommandInfo command)
        {
            if (command == null || !command.pending || string.IsNullOrWhiteSpace(command.command_id) || string.IsNullOrWhiteSpace(command.command_type)) return;
            if (string.Equals(_config.LastDeviceCommandId, command.command_id, StringComparison.OrdinalIgnoreCase))
            {
                GuardianServerClient.ReportDeviceCommandStatus(_config, command.command_id, "success", null);
                return;
            }

            GuardianServerClient.ReportDeviceCommandStatus(_config, command.command_id, "acknowledged", null);
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                try
                {
                    if (command.command_type == "pause_monitoring")
                    {
                        _logger.Log("MonitoringPauseCommandReceived", new Dictionary<string, object> { { "commandId", command.command_id } });
                        StopMonitoring("remote_pause");
                    }
                    else if (command.command_type == "resume_monitoring")
                    {
                        _logger.Log("MonitoringResumeCommandReceived", new Dictionary<string, object> { { "commandId", command.command_id } });
                        StartMonitoring("remote_resume");
                    }
                    else if (command.command_type == "trigger_mission_now")
                    {
                        _logger.Log("TriggerMissionCommandReceived", new Dictionary<string, object> { { "commandId", command.command_id } });
                        if (!TriggerMissionNow()) throw new InvalidOperationException("a mission is already active");
                        _logger.Log("RemoteMissionTriggered", new Dictionary<string, object> { { "commandId", command.command_id }, { "monitoringEnabled", _monitoringEnabled } });
                    }
                    else
                    {
                        throw new InvalidOperationException("unsupported remote command");
                    }
                    _config.LastDeviceCommandId = command.command_id;
                    _config.Save();
                    GuardianServerClient.ReportDeviceCommandStatus(_config, command.command_id, "success", null);
                }
                catch (Exception ex)
                {
                    _logger.Log("Error", new Dictionary<string, object> { { "source", "remote_command" }, { "message", ex.Message } });
                    GuardianServerClient.ReportDeviceCommandStatus(_config, command.command_id, "failed", ex.Message);
                }
            }));
        }

        private void ApplyRemoteInterval(GuardianRemoteConfig remote)
        {
            if (remote == null) return;
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (remote.interval_seconds < 60 || remote.interval_seconds > 14400)
                {
                    _logger.Log("RemoteConfigFailed", new Dictionary<string, object> { { "message", "interval out of range" } });
                    return;
                }
                if (remote.version <= _config.RemoteConfigVersion && _config.IntervalSeconds == remote.interval_seconds && !_config.UseTestInterval) return;
                var oldInterval = _config.EffectiveIntervalSeconds;
                _config.IntervalSeconds = remote.interval_seconds;
                _config.UseTestInterval = false;
                _config.RemoteConfigVersion = remote.version;
                if (remote.mission_config != null)
                {
                    _config.MissionConfig = remote.mission_config;
                    if (_config.MissionConfig.EnabledSkills == null) _config.MissionConfig.EnabledSkills = new List<string>();
                    if (_config.MissionConfig.PrivateProfile == null) _config.MissionConfig.PrivateProfile = new PrivateMissionProfile();
                }
                _config.Save();
                _counter.UpdateInterval(_config.EffectiveIntervalSeconds);
                _logger.Log("RemoteConfigApplied", new Dictionary<string, object>
                {
                    { "oldIntervalSeconds", oldInterval },
                    { "newIntervalSeconds", _config.EffectiveIntervalSeconds },
                    { "configVersion", remote.version },
                    { "missionConfigChanged", remote.mission_config != null },
                    { "profileConfigured", remote.mission_config != null && remote.mission_config.PrivateProfile != null && remote.mission_config.PrivateProfile.IsConfigured }
                });
            }));
        }

        private void ApplyLegacyRemoteConfig(GuardianConfig remote)
        {
            var oldInterval = _config.EffectiveIntervalSeconds;
            if (remote.IntervalSeconds > 0) _config.IntervalSeconds = remote.IntervalSeconds;
            if (remote.TestIntervalSeconds > 0) _config.TestIntervalSeconds = remote.TestIntervalSeconds;
            _config.UseTestInterval = remote.UseTestInterval;
            if (!string.IsNullOrWhiteSpace(remote.Difficulty)) _config.Difficulty = remote.Difficulty;
            if (!string.IsNullOrWhiteSpace(remote.RemoteWebhookUrl)) _config.RemoteWebhookUrl = remote.RemoteWebhookUrl;
            if (!string.IsNullOrWhiteSpace(remote.RemoteConfigUrl)) _config.RemoteConfigUrl = remote.RemoteConfigUrl;
            if (remote.RemoteConfigPollSeconds > 0) _config.RemoteConfigPollSeconds = remote.RemoteConfigPollSeconds;
            _config.Save();
            _counter.UpdateInterval(_config.EffectiveIntervalSeconds);
            _logger.Log("RemoteConfigApplied", new Dictionary<string, object>
            {
                { "oldIntervalSeconds", oldInterval },
                { "newIntervalSeconds", _config.EffectiveIntervalSeconds },
                { "difficulty", _config.Difficulty }
            });
        }

        private void LaunchUpdater(UpdateCommandInfo update)
        {
            if (update == null || !update.pending) return;
            if (string.Equals(update.version, AppInfo.Version, StringComparison.OrdinalIgnoreCase)) return;
            if (string.Equals(_config.PendingUpdateCommandId, update.command_id, StringComparison.OrdinalIgnoreCase)) return;

            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                try
                {
                    var updater = GuardianInstallPaths.UpdaterExecutablePath;
                    if (!string.IsNullOrWhiteSpace(_config.UpdaterPath) && !GuardianInstallPaths.SamePath(_config.UpdaterPath, updater))
                    {
                        _logger.Log("UpdaterPathMigrated", new Dictionary<string, object>
                        {
                            { "configured_updater_path", GuardianInstallPaths.RedactForTelemetry(_config.UpdaterPath) },
                            { "canonical_updater_path", GuardianInstallPaths.RedactForTelemetry(updater) }
                        });
                        _config.UpdaterPath = "";
                        _config.Save();
                    }
                    if (!File.Exists(updater))
                    {
                        _logger.Log("UpdateFailed", new Dictionary<string, object> { { "message", "GuardianUpdater.exe not found" } });
                        return;
                    }
                    _config.PendingUpdateCommandId = update.command_id;
                    _config.Save();
                    try
                    {
                        var updateContext = new Dictionary<string, object>
                        {
                            { "command_id", update.command_id },
                            { "release_id", update.release_id },
                            { "from_version", AppInfo.Version },
                            { "target_version", update.version },
                            { "direction", VersionDirection(AppInfo.Version, update.version) }
                        };
                        File.WriteAllText(AppInfo.UpdateContextPath, new JavaScriptSerializer().Serialize(updateContext), Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log("Error", new Dictionary<string, object> { { "source", "update_context" }, { "message", ex.Message } });
                    }
                    GuardianServerClient.ReportUpdateStatus(_config, update.command_id, "acknowledged", null);
                    _logger.Log("UpdateCommandReceived", new Dictionary<string, object>
                    {
                        { "commandId", update.command_id },
                        { "releaseId", update.release_id },
                        { "previousVersion", AppInfo.Version },
                        { "targetVersion", update.version },
                        { "from_version", AppInfo.Version },
                        { "target_version", update.version },
                        { "direction", VersionDirection(AppInfo.Version, update.version) },
                        { "sha256", update.sha256 },
                        { "fileSize", update.file_size }
                    });
                    Process.Start(new ProcessStartInfo(updater)
                    {
                        Arguments = "--command-id " + QuoteArg(update.command_id) + " --release-id " + QuoteArg(update.release_id) + " --version " + QuoteArg(update.version) + " --sha256 " + QuoteArg(update.sha256),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    _logger.Log("UpdateFailed", new Dictionary<string, object> { { "message", ex.Message } });
                }
            }));
        }

        private static string QuoteArg(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\\\"") + "\"";
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

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                _sessionUnlocked = false;
                _logger.Log("DeviceLocked", new Dictionary<string, object> { { "reason", "windows_session_lock" } });
                _logger.Log("UsageCounterPaused", new Dictionary<string, object> { { "reason", "windows_session_lock" } });
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                _sessionUnlocked = true;
                _logger.Log("DeviceUnlocked", new Dictionary<string, object> { { "reason", "windows_session_unlock" } });
                _logger.Log("UsageCounterStarted", new Dictionary<string, object>
                {
                    { "reason", "windows_session_unlock" },
                    { "elapsedSeconds", _counter.ElapsedSeconds }
                });
            }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                _suspended = true;
                _logger.Log("UsageCounterPaused", new Dictionary<string, object> { { "reason", "system_suspend" } });
            }
            else if (e.Mode == PowerModes.Resume)
            {
                _suspended = false;
                _logger.Log("UsageCounterStarted", new Dictionary<string, object>
                {
                    { "reason", "system_resume" },
                    { "elapsedSeconds", _counter.ElapsedSeconds }
                });
            }
        }

        public void Dispose()
        {
            if (_mediaSession != null)
            {
                _mediaSession.Dispose();
                _mediaSession = null;
            }
            if (_tray != null)
            {
                _tray.Dispose();
                _tray = null;
            }
            _timer.Stop();
            _remoteConfigTimer.Stop();
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        }
    }

    public enum MissionTrigger
    {
        Timer,
        Manual
    }

    public static class MediaInterruptionPolicy
    {
        public static bool ShouldRequestPlayPause(GuardianConfig config, MissionTrigger trigger)
        {
            return config != null && config.PauseMediaOnMission && config.AllowUnsafeMediaToggle && trigger == MissionTrigger.Timer;
        }

        public static bool ShouldResumeMedia(GuardianConfig config)
        {
            return config != null && config.ResumeMediaAfterMission;
        }
    }

    public sealed class MediaInterruptionSession : IDisposable
    {
        private readonly GuardianConfig _config;
        private readonly EventLogger _logger;
        private readonly string _missionId;
        private readonly SystemAudioMuteScope _muteScope;
        private bool _disposed;

        private MediaInterruptionSession(GuardianConfig config, EventLogger logger, string missionId, SystemAudioMuteScope muteScope)
        {
            _config = config;
            _logger = logger;
            _missionId = missionId;
            _muteScope = muteScope;
        }

        public static MediaInterruptionSession Start(GuardianConfig config, EventLogger logger, string missionId, MissionTrigger trigger)
        {
            if (MediaInterruptionPolicy.ShouldRequestPlayPause(config, trigger))
            {
                try
                {
                    MediaKeys.SendPlayPause();
                    logger.Log("MediaPauseRequested", new Dictionary<string, object> { { "missionId", missionId } });
                }
                catch (Exception ex)
                {
                    logger.Log("Error", new Dictionary<string, object>
                    {
                        { "source", "media_pause" },
                        { "missionId", missionId },
                        { "message", ex.Message }
                    });
                }
            }
            else
            {
                logger.Log("MediaPauseSkipped", new Dictionary<string, object>
                {
                    { "missionId", missionId },
                    { "trigger", trigger.ToString().ToLowerInvariant() }
                });
            }

            SystemAudioMuteScope muteScope = null;
            if (config.MuteSystemAudioDuringMission)
            {
                try
                {
                    muteScope = SystemAudioMuteScope.Mute(logger, missionId);
                }
                catch (Exception ex)
                {
                    logger.Log("Error", new Dictionary<string, object>
                    {
                        { "source", "system_audio_mute" },
                        { "missionId", missionId },
                        { "message", ex.Message }
                    });
                }
            }

            return new MediaInterruptionSession(config, logger, missionId, muteScope);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_muteScope != null) _muteScope.Dispose();

            if (MediaInterruptionPolicy.ShouldResumeMedia(_config))
            {
                try
                {
                    MediaKeys.SendPlayPause();
                    _logger.Log("MediaResumeRequested", new Dictionary<string, object> { { "missionId", _missionId } });
                }
                catch (Exception ex)
                {
                    _logger.Log("Error", new Dictionary<string, object>
                    {
                        { "source", "media_resume" },
                        { "missionId", _missionId },
                        { "message", ex.Message }
                    });
                }
            }
        }
    }

    public static class MediaKeys
    {
        private const byte VkMediaPlayPause = 0xB3;
        private const int KeyeventfKeyup = 0x0002;

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, UIntPtr dwExtraInfo);

        public static void SendPlayPause()
        {
            keybd_event(VkMediaPlayPause, 0, 0, UIntPtr.Zero);
            Thread.Sleep(40);
            keybd_event(VkMediaPlayPause, 0, KeyeventfKeyup, UIntPtr.Zero);
        }
    }

    public sealed class SystemAudioMuteScope : IDisposable
    {
        private readonly IAudioEndpointVolume _endpoint;
        private readonly bool _previousMute;
        private readonly EventLogger _logger;
        private readonly string _missionId;
        private bool _disposed;

        private SystemAudioMuteScope(IAudioEndpointVolume endpoint, bool previousMute, EventLogger logger, string missionId)
        {
            _endpoint = endpoint;
            _previousMute = previousMute;
            _logger = logger;
            _missionId = missionId;
        }

        public static SystemAudioMuteScope Mute(EventLogger logger, string missionId)
        {
            var endpoint = CoreAudio.GetDefaultEndpointVolume();
            bool previousMute;
            endpoint.GetMute(out previousMute);
            if (!previousMute)
            {
                var context = Guid.Empty;
                endpoint.SetMute(true, ref context);
            }
            logger.Log("SystemAudioMuted", new Dictionary<string, object>
            {
                { "missionId", missionId },
                { "previousMute", previousMute }
            });
            return new SystemAudioMuteScope(endpoint, previousMute, logger, missionId);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                var context = Guid.Empty;
                _endpoint.SetMute(_previousMute, ref context);
                _logger.Log("SystemAudioRestored", new Dictionary<string, object>
                {
                    { "missionId", _missionId },
                    { "restoredMute", _previousMute }
                });
            }
            catch (Exception ex)
            {
                _logger.Log("Error", new Dictionary<string, object>
                {
                    { "source", "system_audio_restore" },
                    { "missionId", _missionId },
                    { "message", ex.Message }
                });
            }
            finally
            {
                Marshal.ReleaseComObject(_endpoint);
            }
        }
    }

    public static class CoreAudio
    {
        private static readonly Guid IAudioEndpointVolumeGuid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
        private const int ClsctxAll = 23;

        public static IAudioEndpointVolume GetDefaultEndpointVolume()
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            IMMDevice device;
            enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);
            object endpoint;
            var iid = IAudioEndpointVolumeGuid;
            device.Activate(ref iid, ClsctxAll, IntPtr.Zero, out endpoint);
            Marshal.ReleaseComObject(device);
            Marshal.ReleaseComObject(enumerator);
            return (IAudioEndpointVolume)endpoint;
        }
    }

    public static class AudioRecovery
    {
        public static int Unmute()
        {
            IAudioEndpointVolume endpoint = null;
            try
            {
                endpoint = CoreAudio.GetDefaultEndpointVolume();
                var context = Guid.Empty;
                endpoint.SetMute(false, ref context);
                Console.WriteLine("Audio del sistema desmuteado.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("No se pudo desmutear audio: " + ex.Message);
                return 1;
            }
            finally
            {
                if (endpoint != null) Marshal.ReleaseComObject(endpoint);
            }
        }
    }

    public enum EDataFlow
    {
        eRender = 0,
        eCapture = 1,
        eAll = 2
    }

    public enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    public interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, IntPtr ppDevices);
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
        int GetDevice(string pwstrId, out IMMDevice ppDevice);
        int RegisterEndpointNotificationCallback(IntPtr pClient);
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    public interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        int OpenPropertyStore(int stgmAccess, IntPtr ppProperties);
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        int GetState(out int pdwState);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    public interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr pNotify);
        int UnregisterControlChangeNotify(IntPtr pNotify);
        int GetChannelCount(out uint pnChannelCount);
        int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        int GetMasterVolumeLevel(out float pfLevelDB);
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
        int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);
        int VolumeStepUp(ref Guid pguidEventContext);
        int VolumeStepDown(ref Guid pguidEventContext);
        int QueryHardwareSupport(out uint pdwHardwareSupportMask);
        int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }

    public sealed class UsageCounter
    {
        private int _intervalSeconds;

        public UsageCounter(int intervalSeconds)
        {
            if (intervalSeconds <= 0) throw new ArgumentOutOfRangeException("intervalSeconds");
            _intervalSeconds = intervalSeconds;
        }

        public int ElapsedSeconds { get; private set; }

        public bool ShouldTriggerMission
        {
            get { return ElapsedSeconds >= _intervalSeconds; }
        }

        public bool Tick(bool shouldCount)
        {
            if (!shouldCount) return false;
            ElapsedSeconds++;
            return true;
        }

        public void Reset()
        {
            ElapsedSeconds = 0;
        }

        public void UpdateInterval(int intervalSeconds)
        {
            if (intervalSeconds <= 0) throw new ArgumentOutOfRangeException("intervalSeconds");
            _intervalSeconds = intervalSeconds;
        }
    }

    public sealed class GuardianRemoteConfig
    {
        public int version { get; set; }
        public int interval_seconds { get; set; }
        public string updated_at { get; set; }
        public MissionConfig mission_config { get; set; }
    }

    public sealed class RegisterDeviceResponse
    {
        public string device_id { get; set; }
        public string device_token { get; set; }
    }

    public sealed class UpdateCommandInfo
    {
        public bool pending { get; set; }
        public string command_id { get; set; }
        public string release_id { get; set; }
        public string version { get; set; }
        public string sha256 { get; set; }
        public long file_size { get; set; }
        public string download_url { get; set; }
    }

    public sealed class DeviceCommandInfo
    {
        public bool pending { get; set; }
        public string command_id { get; set; }
        public string command_type { get; set; }
    }

    public static class GuardianServerClient
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static void TrySyncAsync(GuardianConfig config, EventLogger logger, Func<bool> monitoringEnabled, Action<GuardianRemoteConfig> onConfig, Action<UpdateCommandInfo> onUpdate, Action<DeviceCommandInfo> onDeviceCommand)
        {
            if (string.IsNullOrWhiteSpace(config.GuardianServerUrl)) return;

            Task.Run(delegate
            {
                try
                {
                    EnsureRegistered(config, logger);
                    if (string.IsNullOrWhiteSpace(config.DeviceToken)) return;
                    SendHeartbeat(config, logger, monitoringEnabled == null ? config.MonitoringEnabled : monitoringEnabled());
                    TelemetrySync.TryFlushAsync(config);
                    var remoteConfig = GetJson<GuardianRemoteConfig>(config, "/api/v1/devices/" + config.DeviceId + "/config");
                    logger.Log("RemoteConfigReceived", new Dictionary<string, object>
                    {
                        { "configVersion", remoteConfig.version },
                        { "intervalSeconds", remoteConfig.interval_seconds }
                    });
                    logger.Log("RemoteConfigFetched", new Dictionary<string, object>
                    {
                        { "configVersion", remoteConfig.version },
                        { "intervalSeconds", remoteConfig.interval_seconds }
                    });
                    if (onConfig != null) onConfig(remoteConfig);
                    var update = GetJson<UpdateCommandInfo>(config, "/api/v1/devices/" + config.DeviceId + "/updates/pending");
                    if (update != null && update.pending && onUpdate != null) onUpdate(update);
                    var deviceCommand = GetJson<DeviceCommandInfo>(config, "/api/v1/devices/" + config.DeviceId + "/commands/pending");
                    if (deviceCommand != null && deviceCommand.pending && onDeviceCommand != null) onDeviceCommand(deviceCommand);
                }
                catch (Exception ex)
                {
                    logger.Log("HeartbeatFailed", new Dictionary<string, object> { { "message", ex.Message } });
                }
            });
        }

        public static void ReportUpdateStatus(GuardianConfig config, string commandId, string status, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(config.GuardianServerUrl) || string.IsNullOrWhiteSpace(config.DeviceToken) || string.IsNullOrWhiteSpace(commandId)) return;
            try
            {
                var payload = new Dictionary<string, object>
                {
                    { "status", status },
                    { "previous_version", AppInfo.Version },
                    { "error_message", errorMessage }
                };
                PostJson<object>(config, "/api/v1/devices/" + config.DeviceId + "/updates/" + commandId + "/status", payload, true);
            }
            catch { }
        }

        public static void ReportDeviceCommandStatus(GuardianConfig config, string commandId, string status, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(config.GuardianServerUrl) || string.IsNullOrWhiteSpace(config.DeviceToken) || string.IsNullOrWhiteSpace(commandId)) return;
            try
            {
                var payload = new Dictionary<string, object>
                {
                    { "status", status },
                    { "error_message", errorMessage }
                };
                PostJson<object>(config, "/api/v1/devices/" + config.DeviceId + "/commands/" + commandId + "/status", payload, true);
            }
            catch { }
        }

        private static void EnsureRegistered(GuardianConfig config, EventLogger logger)
        {
            if (!string.IsNullOrWhiteSpace(config.DeviceToken)) return;
            if (string.IsNullOrWhiteSpace(config.DeviceBootstrapToken)) return;
            var payload = new Dictionary<string, object>
            {
                { "device_id", config.DeviceId },
                { "machine_name", Environment.MachineName },
                { "client_version", AppInfo.Version },
                { "bootstrap_token", config.DeviceBootstrapToken }
            };
            var response = PostJson<RegisterDeviceResponse>(config, "/api/v1/devices/register", payload, false);
            if (response == null || string.IsNullOrWhiteSpace(response.device_token)) return;
            config.DeviceToken = response.device_token;
            config.DeviceBootstrapToken = "";
            config.Save();
            logger.Log("DeviceRegistered", new Dictionary<string, object> { { "deviceId", config.DeviceId } });
            TelemetrySync.TryFlushAsync(config);
        }

        private static void SendHeartbeat(GuardianConfig config, EventLogger logger, bool monitoringEnabled)
        {
            var payload = new Dictionary<string, object>
            {
                { "machine_name", Environment.MachineName },
                { "client_version", AppInfo.Version },
                { "effective_interval_seconds", config.EffectiveIntervalSeconds },
                { "monitoring_enabled", monitoringEnabled }
            };
            PostJson<object>(config, "/api/v1/devices/" + config.DeviceId + "/heartbeat", payload, true);
            logger.Log("HeartbeatSent", new Dictionary<string, object>
            {
                { "version", AppInfo.Version },
                { "monitoring_enabled", monitoringEnabled }
            });
        }

        private static T GetJson<T>(GuardianConfig config, string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(BuildUrl(config, path));
            request.Method = "GET";
            request.Accept = "application/json";
            request.Timeout = 5000;
            AddDeviceAuth(config, request);
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return Serializer.Deserialize<T>(reader.ReadToEnd());
            }
        }

        private static T PostJson<T>(GuardianConfig config, string path, Dictionary<string, object> payload, bool auth)
        {
            var request = (HttpWebRequest)WebRequest.Create(BuildUrl(config, path));
            request.Method = "POST";
            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Timeout = 5000;
            if (auth) AddDeviceAuth(config, request);
            var bytes = Encoding.UTF8.GetBytes(Serializer.Serialize(payload));
            using (var stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length);
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var text = reader.ReadToEnd();
                if (typeof(T) == typeof(object)) return default(T);
                return Serializer.Deserialize<T>(text);
            }
        }

        private static void AddDeviceAuth(GuardianConfig config, HttpWebRequest request)
        {
            request.Headers["Authorization"] = "Bearer " + config.DeviceToken;
        }

        private static string BuildUrl(GuardianConfig config, string path)
        {
            return config.GuardianServerUrl.TrimEnd('/') + path;
        }
    }

    public static class RemoteConfigClient
    {
        public static void TryFetchAsync(GuardianConfig current, Action<GuardianConfig> onSuccess, Action<Exception> onError)
        {
            if (string.IsNullOrWhiteSpace(current.RemoteConfigUrl)) return;

            Task.Run(delegate
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(current.RemoteConfigUrl);
                    request.Method = "GET";
                    request.Accept = "application/json";
                    request.Timeout = 5000;
                    if (!string.IsNullOrWhiteSpace(current.RemoteAuthToken))
                    {
                        request.Headers["Authorization"] = "Bearer " + current.RemoteAuthToken;
                    }
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var json = reader.ReadToEnd();
                        var config = new JavaScriptSerializer().Deserialize<GuardianConfig>(json);
                        if (onSuccess != null) onSuccess(config);
                    }
                }
                catch (Exception ex)
                {
                    if (onError != null) onError(ex);
                }
            });
        }
    }

    public sealed class StatusWindow : Window
    {
        public StatusWindow(GuardianConfig config)
        {
            Title = "Guardian";
            Width = 360;
            Height = 180;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = false;
            var panel = new StackPanel { Margin = new Thickness(18) };
            panel.Children.Add(new TextBlock
            {
                Text = "Guardian esta activo.",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Intervalo: " + config.EffectiveIntervalSeconds + " segundos",
                TextWrapping = TextWrapping.Wrap
            });
            Content = panel;
        }
    }

    public sealed class LockWindow : Window
    {
        private readonly GuardianConfig _config;
        private readonly EventLogger _logger;
        private readonly Random _random = new Random();
        private readonly TextBox _answerBox;
        private readonly TextBlock _feedback;
        private readonly TextBlock _promptText;
        private readonly StackPanel _routine;
        private readonly Image _feedbackIcon;
        private readonly StackPanel _helpPanel;
        private readonly Button _helpButton;
        private readonly Button _exitButton;
        private readonly Grid _adminOverlay;
        private readonly TextBox _adminUserBox;
        private readonly PasswordBox _adminPasswordBox;
        private readonly TextBlock _adminFeedback;
        private Mission _mission;
        private bool _canExit;
        private bool _unlockRequested;
        private int _attempt = 1;
        private int _maxHelpLevelUsed;
        private int _helpRequestsCount;
        private bool _helpLevel2Shown;
        private bool _helpLevel3Unlocked;
        private bool _hadOrthographicError;
        private int _writingCorrectionCount;
        private bool _writingAnswerRevealed;

        public event Action UnlockRequested;
        public event Action AdminShutdownRequested;

        public LockWindow(Mission mission, GuardianConfig config, EventLogger logger)
        {
            _mission = mission;
            _config = config;
            _logger = logger;
            Title = "Guardian";
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            Topmost = true;
            ShowInTaskbar = true;
            Background = new SolidColorBrush(Color.FromRgb(16, 24, 35));
            Foreground = Brushes.White;

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var card = new Border
            {
                MaxWidth = 720,
                Padding = new Thickness(34),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(95, 121, 151)),
                BorderThickness = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "MISI\u00d3N R\u00c1PIDA",
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 18)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Para continuar, resolve:",
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                FontSize = 20,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            });
            _promptText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
                FontSize = 42,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 640,
                Margin = new Thickness(0, 0, 0, 18)
            };
            RenderRichText(_promptText, mission.Prompt, mission.PromptBoldTerms);
            panel.Children.Add(_promptText);
            _answerBox = new TextBox
            {
                FontSize = 30,
                Width = 460,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };
            _answerBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) CheckAnswer();
            };
            panel.Children.Add(_answerBox);

            var button = new Button
            {
                Content = "Comprobar",
                FontSize = 18,
                Padding = new Thickness(18, 8, 18, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };
            button.Click += delegate { CheckAnswer(); };
            panel.Children.Add(button);

            _feedback = new TextBlock
            {
                Text = "",
                Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27)),
                FontSize = 16,
                TextAlignment = TextAlignment.Center,
                MinHeight = 28
            };
            _feedbackIcon = CreateIcon("spelling.png", 24);
            _feedbackIcon.Visibility = Visibility.Collapsed;
            var feedbackPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 2) };
            feedbackPanel.Children.Add(_feedbackIcon);
            feedbackPanel.Children.Add(_feedback);
            panel.Children.Add(feedbackPanel);

            _routine = CreateRoutinePanel();
            panel.Children.Add(_routine);
            _helpPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            panel.Children.Add(_helpPanel);
            _helpButton = new Button { FontSize = 16, Padding = new Thickness(14, 7, 14, 7), HorizontalAlignment = HorizontalAlignment.Center, Visibility = Visibility.Collapsed };
            _helpButton.FontFamily = new FontFamily("Segoe UI Emoji");
            _helpButton.Background = new SolidColorBrush(Color.FromRgb(219, 234, 254));
            _helpButton.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            _helpButton.Foreground = new SolidColorBrush(Color.FromRgb(30, 64, 175));
            _helpButton.Click += delegate { RequestNextHelp(); };
            panel.Children.Add(_helpButton);
            UpdateHelpButton();

            card.Child = panel;
            Grid.SetRow(card, 1);
            root.Children.Add(card);

            _exitButton = new Button
            {
                Content = "X",
                Width = 34,
                Height = 34,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Opacity = 0.82,
                Visibility = Visibility.Hidden,
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 18, 18, 0),
                Padding = new Thickness(0),
                ToolTip = "Cerrar"
            };
            _exitButton.Click += delegate { RequestUnlock(); };
            root.Children.Add(_exitButton);

            var adminButton = new Button
            {
                Content = "Admin",
                Width = 86,
                Height = 34,
                FontSize = 12,
                Opacity = 0.72,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 18, 18),
                ToolTip = "Salida de emergencia para adulto"
            };
            adminButton.Click += delegate { ShowAdminOverlay(); };
            root.Children.Add(adminButton);

            _adminOverlay = new Grid
            {
                Visibility = Visibility.Hidden,
                Background = new SolidColorBrush(Color.FromArgb(190, 16, 24, 35))
            };
            var adminCard = new Border
            {
                Width = 360,
                Padding = new Thickness(24),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var adminPanel = new StackPanel();
            adminPanel.Children.Add(new TextBlock
            {
                Text = "Salida admin",
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 14)
            });
            adminPanel.Children.Add(new TextBlock { Text = "Usuario", Foreground = Brushes.Black });
            _adminUserBox = new TextBox { FontSize = 16, Margin = new Thickness(0, 4, 0, 12) };
            adminPanel.Children.Add(_adminUserBox);
            adminPanel.Children.Add(new TextBlock { Text = "Contrase\u00f1a", Foreground = Brushes.Black });
            _adminPasswordBox = new PasswordBox { FontSize = 16, Margin = new Thickness(0, 4, 0, 14) };
            _adminPasswordBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) TryAdminExit();
            };
            adminPanel.Children.Add(_adminPasswordBox);
            var adminButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancelAdmin = new Button { Content = "Cancelar", Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 8, 0) };
            cancelAdmin.Click += delegate { HideAdminOverlay(); };
            var exitAdmin = new Button { Content = "Cerrar Guardian", Padding = new Thickness(12, 6, 12, 6) };
            exitAdmin.Click += delegate { TryAdminExit(); };
            adminButtons.Children.Add(cancelAdmin);
            adminButtons.Children.Add(exitAdmin);
            adminPanel.Children.Add(adminButtons);
            _adminFeedback = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27)),
                FontSize = 13,
                MinHeight = 24,
                Margin = new Thickness(0, 12, 0, 0)
            };
            adminPanel.Children.Add(_adminFeedback);
            adminCard.Child = adminPanel;
            _adminOverlay.Children.Add(adminCard);
            root.Children.Add(_adminOverlay);
            Content = root;

            Loaded += delegate
            {
                Activate();
                _answerBox.Focus();
                Keyboard.Focus(_answerBox);
            };
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_unlockRequested)
            {
                e.Cancel = true;
                Topmost = true;
                Activate();
            }
            base.OnClosing(e);
        }

        public void AllowProgrammaticClose()
        {
            _unlockRequested = true;
        }

        private void CheckAnswer()
        {
            var analysis = MissionValidator.Analyze(_answerBox.Text, _mission);
            var result = analysis.Result;
            if (result == MissionAnswerResult.Invalid)
            {
                _feedbackIcon.Visibility = Visibility.Collapsed;
                _feedback.Text = "Revis\u00e1 la respuesta e intent\u00e1 de nuevo.";
                var invalidPayload = TelemetryPayload();
                invalidPayload["reason"] = "invalid_input";
                _logger.Log("MissionFailed", invalidPayload);
                _attempt++;
                return;
            }

            if (result == MissionAnswerResult.OrthographicNearMatch)
            {
                _hadOrthographicError = true;
                _writingCorrectionCount++;
                var stage = _writingCorrectionCount >= 3 ? 3 : _writingCorrectionCount;
                _feedbackIcon.Visibility = Visibility.Visible;
                if (stage < 3) _feedback.Text = MissionContent.WritingFeedback(MissionValidator.DescribeDifference(_answerBox.Text, analysis.MatchedAcceptedAnswer));
                else { _writingAnswerRevealed = true; _feedback.Text = MissionContent.WritingAnswerRevealed(analysis.MatchedAcceptedAnswer); }
                _answerBox.SelectAll();
                var spellingPayload = TelemetryPayload(); spellingPayload["reason"] = "orthographic_error";
                _logger.Log("MissionFailed", spellingPayload);
                var hintPayload = TelemetryPayload(); hintPayload["writing_hint_stage"] = stage;
                _logger.Log("MissionWritingHintShown", hintPayload);
                _attempt++;
                return;
            }

            if (result == MissionAnswerResult.Wrong)
            {
                _feedbackIcon.Visibility = Visibility.Collapsed;
                _feedback.Text = "";
                _routine.Visibility = Visibility.Visible;
                if (_helpLevel2Shown) _helpLevel3Unlocked = true;
                UpdateHelpButton();
                _answerBox.SelectAll();
                var failedPayload = TelemetryPayload();
                failedPayload["reason"] = "wrong_answer";
                _logger.Log("MissionFailed", failedPayload);
                _attempt++;
                return;
            }

            _logger.Log("MissionSolved", TelemetryPayload());

            if (!_canExit)
            {
                _canExit = true;
                _exitButton.Visibility = Visibility.Visible;
                _logger.Log("ExitAvailable", TelemetryPayload());
            }
            RequestUnlock();
        }

        private Dictionary<string, object> TelemetryPayload() { return MissionTelemetry.Payload(_mission, _attempt, _maxHelpLevelUsed, _helpRequestsCount, _hadOrthographicError, _writingCorrectionCount, _writingAnswerRevealed); }

        private void RequestNextHelp()
        {
            var next = _maxHelpLevelUsed + 1;
            if (next == 3 && !_helpLevel3Unlocked) return;
            MissionHelpStep step = null;
            if (_mission.HelpSteps != null) foreach (var candidate in _mission.HelpSteps) if (candidate.HelpLevel == next) { step = candidate; break; }
            if (step == null) return;
            var text = new TextBlock { Foreground = new SolidColorBrush(Color.FromRgb(30, 64, 175)), FontSize = 17, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(7, 4, 0, 4), MaxWidth = 560 };
            RenderRichText(text, step.Text, step.BoldTerms);
            var helpRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            helpRow.Children.Add(CreateIcon(next == 1 ? "rephrase.png" : next == 2 ? "hint.png" : "guided.png", 28));
            helpRow.Children.Add(text);
            _helpPanel.Children.Add(helpRow);
            _maxHelpLevelUsed = next; _helpRequestsCount++; if (next == 2) _helpLevel2Shown = true;
            var payload = TelemetryPayload(); payload["help_level"] = next;
            _logger.Log("MissionHelpRequested", payload);
            UpdateHelpButton();
        }

        private void UpdateHelpButton()
        {
            if (_mission == null || _mission.HelpSteps == null || _mission.HelpSteps.Count == 0 || _maxHelpLevelUsed >= 3) { _helpButton.Visibility = Visibility.Collapsed; return; }
            var next = _maxHelpLevelUsed + 1;
            if (next == 3 && !_helpLevel3Unlocked) { _helpButton.Visibility = Visibility.Collapsed; return; }
            _helpButton.Content = CreateIconButtonContent(next == 1 ? "rephrase.png" : next == 2 ? "hint.png" : "guided.png", next == 1 ? MissionContent.RephraseButton : next == 2 ? MissionContent.HintButton : MissionContent.GuidedButton);
            _helpButton.Visibility = Visibility.Visible;
        }

        private static StackPanel CreateIconButtonContent(string iconName, string label)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var image = CreateIcon(iconName, 28); image.Margin = new Thickness(0, 0, 8, 0); panel.Children.Add(image);
            panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
            return panel;
        }

        private static StackPanel CreateRoutinePanel()
        {
            var panel = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 4, 0, 10) };
            var steps = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
            AddRoutineStep(steps, "look.png", "MIRO"); AddRoutineStep(steps, "think.png", "PIENSO"); AddRoutineStep(steps, "write.png", "RESPONDO"); panel.Children.Add(steps);
            return panel;
        }

        private static void AddRoutineStep(StackPanel panel, string iconName, string label)
        {
            if (panel.Children.Count > 0) panel.Children.Add(new TextBlock { Text = "  →  ", VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)) });
            panel.Children.Add(CreateIcon(iconName, 32)); panel.Children.Add(new TextBlock { Text = " " + label, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)), FontSize = 16 });
        }

        private static Image CreateIcon(string iconName, double size)
        {
            var image = new Image { Width = size, Height = size, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center };
            var assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", "Icons", iconName);
            if (!File.Exists(assetPath)) return image;
            var source = new BitmapImage(); source.BeginInit(); source.UriSource = new Uri(assetPath, UriKind.Absolute); source.CacheOption = BitmapCacheOption.OnLoad; source.EndInit(); image.Source = source;
            return image;
        }

        private static string PrefixHint(string value) { if (string.IsNullOrWhiteSpace(value)) return "la palabra"; var count = value.Length >= 4 ? 3 : 1; return value.Substring(0, count); }

        internal static void RenderRichText(TextBlock block, string text, IList<string> terms)
        {
            block.Inlines.Clear(); if (string.IsNullOrEmpty(text)) return;
            var matches = new List<Tuple<int, int>>();
            if (terms != null) foreach (var term in terms) { if (string.IsNullOrWhiteSpace(term)) continue; var start = 0; while (start < text.Length) { var index = text.IndexOf(term, start, StringComparison.OrdinalIgnoreCase); if (index < 0) break; matches.Add(Tuple.Create(index, term.Length)); start = index + term.Length; } }
            matches.Sort(delegate(Tuple<int,int> a, Tuple<int,int> b) { return a.Item1.CompareTo(b.Item1); }); int cursor = 0;
            foreach (var match in matches) { if (match.Item1 < cursor) continue; if (match.Item1 > cursor) block.Inlines.Add(new Run(text.Substring(cursor, match.Item1-cursor))); block.Inlines.Add(new Run(text.Substring(match.Item1, match.Item2)) { FontWeight = FontWeights.Bold }); cursor = match.Item1 + match.Item2; }
            if (cursor < text.Length) block.Inlines.Add(new Run(text.Substring(cursor)));
        }

        private void RequestUnlock()
        {
            if (!_canExit) return;
            _unlockRequested = true;
            _logger.Log("ExitClicked", new Dictionary<string, object>
            {
                { "missionId", _mission.Id },
                { "attempt", _attempt }
            });
            if (UnlockRequested != null) UnlockRequested();
            Close();
        }

        private void ShowAdminOverlay()
        {
            _adminFeedback.Text = "";
            _adminUserBox.Text = _config.AdminUsername;
            _adminPasswordBox.Password = "";
            _adminOverlay.Visibility = Visibility.Visible;
            _adminPasswordBox.Focus();
            Keyboard.Focus(_adminPasswordBox);
            _logger.Log("AdminExitPromptOpened", new Dictionary<string, object>
            {
                { "missionId", _mission.Id }
            });
        }

        private void HideAdminOverlay()
        {
            _adminOverlay.Visibility = Visibility.Hidden;
            _answerBox.Focus();
            Keyboard.Focus(_answerBox);
        }

        private void TryAdminExit()
        {
            if (AdminAuth.Verify(_config, _adminUserBox.Text, _adminPasswordBox.Password))
            {
                _unlockRequested = true;
                _logger.Log("AdminExitSucceeded", new Dictionary<string, object>
                {
                    { "missionId", _mission.Id },
                    { "username", _adminUserBox.Text }
                });
                if (AdminShutdownRequested != null) AdminShutdownRequested();
                Close();
                return;
            }

            _adminFeedback.Text = "Usuario o contrase\u00f1a incorrectos.";
            _adminPasswordBox.SelectAll();
            _adminPasswordBox.Focus();
            _logger.Log("AdminExitFailed", new Dictionary<string, object>
            {
                { "missionId", _mission.Id },
                { "username", _adminUserBox.Text }
            });
        }

    }

    public enum MissionAnswerResult
    {
        Invalid,
        Wrong,
        OrthographicNearMatch,
        Correct
    }

    public enum WritingDifference { Unknown, ExtraLetter, MissingLetter, TransposedLetters, SubstitutedLetter }

    public sealed class MissionAnswerAnalysis { public MissionAnswerResult Result { get; set; } public string MatchedAcceptedAnswer { get; set; } public int EditDistance { get; set; } }

    public static class MissionValidator
    {
        public static MissionAnswerResult Validate(string input, Mission mission)
        {
            return Analyze(input, mission).Result;
        }
        public static MissionAnswerAnalysis Analyze(string input, Mission mission)
        {
            var analysis = new MissionAnswerAnalysis { Result = MissionAnswerResult.Invalid };
            if (string.IsNullOrWhiteSpace(input) || mission == null || mission.AcceptedAnswers == null) return analysis;
            var normalized = MissionText.Normalize(input);
            foreach (var answer in mission.AcceptedAnswers)
            {
                if (normalized == MissionText.Normalize(answer)) { analysis.Result = MissionAnswerResult.Correct; return analysis; }
            }
            var best = Int32.MaxValue; string accepted = null; bool ambiguous = false;
            foreach (var answer in mission.AcceptedAnswers)
            {
                var expected = MissionText.Normalize(answer);
                if (expected.Length < 4 || !IsText(expected) || !IsText(normalized)) continue;
                var distance = DamerauLevenshtein(normalized, expected); var limit = expected.Length <= 5 ? 1 : 2;
                if (distance > limit || (expected.Length >= 6 && ((double)distance / expected.Length) > 0.30)) continue;
                if (distance < best) { best = distance; accepted = answer; ambiguous = false; } else if (distance == best && MissionText.Normalize(answer) != MissionText.Normalize(accepted)) ambiguous = true;
            }
            if (accepted != null && !ambiguous) { analysis.Result=MissionAnswerResult.OrthographicNearMatch; analysis.MatchedAcceptedAnswer=accepted; analysis.EditDistance=best; return analysis; }
            analysis.Result = MissionAnswerResult.Wrong; return analysis;
        }

        public static WritingDifference DescribeDifference(string input, string expected)
        {
            var actual = MissionText.Normalize(input); var target = MissionText.Normalize(expected);
            if (actual.Length == target.Length + 1 && IsSingleInsertion(target, actual)) return WritingDifference.ExtraLetter;
            if (target.Length == actual.Length + 1 && IsSingleInsertion(actual, target)) return WritingDifference.MissingLetter;
            if (actual.Length == target.Length)
            {
                var first = -1; var second = -1;
                for (var i = 0; i < actual.Length; i++) if (actual[i] != target[i]) { if (first < 0) first = i; else if (second < 0) second = i; else return WritingDifference.Unknown; }
                if (first >= 0 && second == first + 1 && actual[first] == target[second] && actual[second] == target[first]) return WritingDifference.TransposedLetters;
                if (first >= 0 && second < 0) return WritingDifference.SubstitutedLetter;
            }
            return WritingDifference.Unknown;
        }

        private static bool IsSingleInsertion(string shorter, string longer)
        {
            var left = 0; while (left < shorter.Length && shorter[left] == longer[left]) left++;
            var shortIndex = left; var longIndex = left + 1;
            while (shortIndex < shorter.Length && shorter[shortIndex] == longer[longIndex]) { shortIndex++; longIndex++; }
            return shortIndex == shorter.Length;
        }

        private static bool IsText(string text) { foreach (var c in text) if (!char.IsLetter(c) && c != ' ') return false; return text.Length > 0; }
        internal static int DamerauLevenshtein(string left, string right) { var d = new int[left.Length + 1, right.Length + 1]; for (var i=0;i<=left.Length;i++) d[i,0]=i; for (var j=0;j<=right.Length;j++) d[0,j]=j; for (var i=1;i<=left.Length;i++) for (var j=1;j<=right.Length;j++) { var cost=left[i-1]==right[j-1]?0:1; d[i,j]=Math.Min(Math.Min(d[i-1,j]+1,d[i,j-1]+1),d[i-1,j-1]+cost); if(i>1&&j>1&&left[i-1]==right[j-2]&&left[i-2]==right[j-1]) d[i,j]=Math.Min(d[i,j],d[i-2,j-2]+cost); } return d[left.Length,right.Length]; }

        public static MissionAnswerResult Validate(string input, int expected, out int answer)
        {
            if (!int.TryParse((input ?? "").Trim(), out answer)) return MissionAnswerResult.Invalid;
            return answer == expected ? MissionAnswerResult.Correct : MissionAnswerResult.Wrong;
        }
    }

    public static class AdminAuth
    {
        public static bool Verify(GuardianConfig config, string username, string password)
        {
            if (config == null) return false;
            if (!string.Equals((username ?? "").Trim(), config.AdminUsername, StringComparison.OrdinalIgnoreCase)) return false;
            return string.Equals(HashPassword((password ?? "").Trim()), config.AdminPasswordSha256, StringComparison.OrdinalIgnoreCase);
        }

        public static string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }

    public sealed class GuardianTray : IDisposable
    {
        private readonly GuardianController _controller;
        private readonly GuardianConfig _config;
        private readonly EventLogger _logger;
        private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
        private readonly System.Drawing.Icon _trayIcon;
        private readonly System.Windows.Forms.ToolStripMenuItem _startItem;
        private readonly System.Windows.Forms.ToolStripMenuItem _stopItem;
        private AdminControlWindow _adminWindow;
        private bool _disposed;

        public GuardianTray(GuardianController controller, GuardianConfig config, EventLogger logger)
        {
            _controller = controller;
            _config = config;
            _logger = logger;

            var openPanelItem = new System.Windows.Forms.ToolStripMenuItem("Abrir panel admin...", null, delegate { OpenAdminPanel(); });
            _startItem = new System.Windows.Forms.ToolStripMenuItem("Iniciar Guardian", null, delegate { RunOnDispatcher(delegate { _controller.StartMonitoring("tray_start"); }); });
            _stopItem = new System.Windows.Forms.ToolStripMenuItem("Detener Guardian...", null, delegate { RequestAdminAction("Detener Guardian", delegate { _controller.StopMonitoring("tray_admin_stop"); }); });
            var testItem = new System.Windows.Forms.ToolStripMenuItem("Probar misi\u00f3n ahora", null, delegate { RunOnDispatcher(delegate { _controller.TriggerMissionNow(); }); });
            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Salir de Guardian...", null, delegate { RequestAdminAction("Salir de Guardian", delegate { _controller.ExitCompletely("tray_admin_exit"); }); });

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add(openPanelItem);
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menu.Items.Add(_startItem);
            menu.Items.Add(_stopItem);
            menu.Items.Add(testItem);
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menu.Items.Add(exitItem);

            _trayIcon = TrayIconFactory.Create();
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = _trayIcon,
                Text = "Guardian",
                ContextMenuStrip = menu,
                Visible = true
            };
            _notifyIcon.DoubleClick += delegate { OpenAdminPanel(); };
            SetMonitoringState(false);
        }

        public void SetMonitoringState(bool running)
        {
            _startItem.Enabled = !running;
            _stopItem.Enabled = running;
            _notifyIcon.Text = running ? "Guardian activo" : "Guardian detenido";
        }

        private void RequestAdminAction(string title, Action action)
        {
            RunOnDispatcher(delegate
            {
                if (AdminPromptWindow.TryAuthenticate(_config, title))
                {
                    _logger.Log("TrayAdminActionSucceeded", new Dictionary<string, object> { { "title", title } });
                    action();
                }
                else
                {
                    _logger.Log("TrayAdminActionCancelled", new Dictionary<string, object> { { "title", title } });
                }
            });
        }

        private void OpenAdminPanel()
        {
            RunOnDispatcher(delegate
            {
                if (_adminWindow != null)
                {
                    _adminWindow.Activate();
                    return;
                }

                _adminWindow = new AdminControlWindow(_controller, _config, _logger);
                _adminWindow.Closed += delegate { _adminWindow = null; };
                _adminWindow.Show();
                _adminWindow.Activate();
            });
        }

        private static void RunOnDispatcher(Action action)
        {
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _trayIcon.Dispose();
            if (_adminWindow != null)
            {
                _adminWindow.Close();
                _adminWindow = null;
            }
        }
    }

    public static class TrayIconFactory
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static System.Drawing.Icon Create()
        {
            using (var bitmap = new System.Drawing.Bitmap(32, 32))
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            using (var navy = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(30, 64, 119)))
            using (var face = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(252, 211, 177)))
            using (var visor = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(15, 23, 42)))
            using (var white = new System.Drawing.SolidBrush(System.Drawing.Color.White))
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(15, 23, 42), 2))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                g.FillEllipse(navy, 3, 3, 26, 26);
                g.FillPie(face, 8, 10, 16, 17, 0, 180);
                g.FillEllipse(face, 8, 12, 16, 14);
                g.FillRectangle(visor, 7, 7, 18, 6);
                g.FillRectangle(navy, 9, 5, 14, 6);
                g.FillEllipse(white, 12, 16, 2, 2);
                g.FillEllipse(white, 18, 16, 2, 2);
                g.DrawArc(pen, 12, 17, 8, 6, 20, 140);

                var handle = bitmap.GetHicon();
                try
                {
                    return (System.Drawing.Icon)System.Drawing.Icon.FromHandle(handle).Clone();
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }
    }

    public sealed class AdminControlWindow : Window
    {
        private readonly GuardianController _controller;
        private readonly GuardianConfig _config;
        private readonly EventLogger _logger;
        private readonly Grid _root;
        private readonly TextBox _userBox;
        private readonly PasswordBox _passwordBox;
        private readonly TextBlock _feedback;

        public AdminControlWindow(GuardianController controller, GuardianConfig config, EventLogger logger)
        {
            _controller = controller;
            _config = config;
            _logger = logger;
            Title = "Guardian Admin";
            Width = 380;
            Height = 300;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;

            _root = new Grid { Margin = new Thickness(22) };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "Guardian Admin",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 14)
            });
            panel.Children.Add(new TextBlock { Text = "Usuario" });
            _userBox = new TextBox { Text = config.AdminUsername, FontSize = 16, Margin = new Thickness(0, 4, 0, 12) };
            panel.Children.Add(_userBox);
            panel.Children.Add(new TextBlock { Text = "Contrase\u00f1a" });
            _passwordBox = new PasswordBox { FontSize = 16, Margin = new Thickness(0, 4, 0, 14) };
            _passwordBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) Authenticate();
            };
            panel.Children.Add(_passwordBox);
            var login = new Button { Content = "Entrar", Padding = new Thickness(14, 7, 14, 7), HorizontalAlignment = HorizontalAlignment.Right };
            login.Click += delegate { Authenticate(); };
            panel.Children.Add(login);
            _feedback = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27)),
                MinHeight = 24,
                Margin = new Thickness(0, 12, 0, 0)
            };
            panel.Children.Add(_feedback);
            _root.Children.Add(panel);
            Content = _root;

            Loaded += delegate
            {
                _passwordBox.Focus();
                Keyboard.Focus(_passwordBox);
            };
        }

        private void Authenticate()
        {
            if (!AdminAuth.Verify(_config, _userBox.Text, _passwordBox.Password))
            {
                _feedback.Text = "Usuario o contrase\u00f1a incorrectos.";
                _passwordBox.SelectAll();
                _passwordBox.Focus();
                _logger.Log("AdminPanelLoginFailed", new Dictionary<string, object> { { "username", _userBox.Text } });
                return;
            }

            _logger.Log("AdminPanelLoginSucceeded", new Dictionary<string, object> { { "username", _userBox.Text } });
            ShowControls();
        }

        private void ShowControls()
        {
            _root.Children.Clear();
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "Guardian Admin",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 18)
            });
            panel.Children.Add(MakeButton("Activar Guardian", delegate { _controller.StartMonitoring("admin_panel_start"); }));
            panel.Children.Add(MakeButton("Pausar hasta nuevo aviso", delegate { _controller.StopMonitoring("admin_panel_pause"); }));
            panel.Children.Add(MakeButton("Probar misi\u00f3n ahora", delegate { _controller.TriggerMissionNow(); }));
            panel.Children.Add(MakeButton("Salir completamente", delegate { _controller.ExitCompletely("admin_panel_exit"); }));
            panel.Children.Add(new TextBlock
            {
                Text = "Si paus\u00e1s Guardian, no volver\u00e1 a saltar hasta activarlo desde este panel o desde la bandeja.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 14, 0, 0)
            });
            _root.Children.Add(panel);
        }

        private Button MakeButton(string text, Action action)
        {
            var button = new Button
            {
                Content = text,
                Height = 36,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 0, 12, 0)
            };
            button.Click += delegate
            {
                action();
                Close();
            };
            return button;
        }
    }

    public static class AdminConfigReset
    {
        public static int Reset()
        {
            try
            {
                var config = GuardianConfig.Load();
                config.AdminUsername = "admin";
                config.AdminPasswordSha256 = AdminAuth.HashPassword("guardian");
                config.Save();
                Console.WriteLine("Credenciales admin restauradas: admin / guardian");
                Console.WriteLine("Config: " + GuardianConfig.ConfigPath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("No se pudieron restaurar credenciales admin: " + ex.Message);
                return 1;
            }
        }
    }

    public sealed class AdminPromptWindow : Window
    {
        private readonly GuardianConfig _config;
        private readonly TextBox _userBox;
        private readonly PasswordBox _passwordBox;
        private readonly TextBlock _feedback;
        private bool _authenticated;

        private AdminPromptWindow(GuardianConfig config, string titleText)
        {
            _config = config;
            Title = titleText;
            Width = 360;
            Height = 245;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;

            var panel = new StackPanel { Margin = new Thickness(22) };
            panel.Children.Add(new TextBlock
            {
                Text = titleText,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 14)
            });
            panel.Children.Add(new TextBlock { Text = "Usuario" });
            _userBox = new TextBox { Text = config.AdminUsername, FontSize = 16, Margin = new Thickness(0, 4, 0, 12) };
            panel.Children.Add(_userBox);
            panel.Children.Add(new TextBlock { Text = "Contrase\u00f1a" });
            _passwordBox = new PasswordBox { FontSize = 16, Margin = new Thickness(0, 4, 0, 14) };
            _passwordBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) TrySubmit();
            };
            panel.Children.Add(_passwordBox);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = new Button { Content = "Cancelar", Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 8, 0) };
            cancel.Click += delegate { DialogResult = false; Close(); };
            var ok = new Button { Content = "Aceptar", Padding = new Thickness(12, 6, 12, 6) };
            ok.Click += delegate { TrySubmit(); };
            buttons.Children.Add(cancel);
            buttons.Children.Add(ok);
            panel.Children.Add(buttons);
            _feedback = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27)),
                MinHeight = 24,
                Margin = new Thickness(0, 10, 0, 0)
            };
            panel.Children.Add(_feedback);
            Content = panel;

            Loaded += delegate
            {
                _passwordBox.Focus();
                Keyboard.Focus(_passwordBox);
            };
        }

        public static bool TryAuthenticate(GuardianConfig config, string title)
        {
            var window = new AdminPromptWindow(config, title);
            window.ShowDialog();
            return window._authenticated;
        }

        private void TrySubmit()
        {
            if (AdminAuth.Verify(_config, _userBox.Text, _passwordBox.Password))
            {
                _authenticated = true;
                DialogResult = true;
                Close();
                return;
            }

            _feedback.Text = "Usuario o contrase\u00f1a incorrectos.";
            _passwordBox.SelectAll();
            _passwordBox.Focus();
        }
    }

    public sealed class LegacyMission
    {
        public string Id { get; set; }
        public string Prompt { get; set; }
        public int Answer { get; set; }
        public string Operation { get; set; }
    }

    public sealed class LegacyMissionGenerator
    {
        private readonly Random _random = new Random();

        public LegacyMission Next(string difficulty)
        {
            var kind = _random.Next(0, 3);
            if (kind == 0) return Sum();
            if (kind == 1) return Subtract();
            return Multiply();
        }

        private LegacyMission Sum()
        {
            var a = _random.Next(20, 100);
            var b = _random.Next(10, 90);
            return Create(a + " + " + b + " = ?", a + b, "sum");
        }

        private LegacyMission Subtract()
        {
            var a = _random.Next(40, 130);
            var b = _random.Next(10, Math.Min(90, a));
            return Create(a + " - " + b + " = ?", a - b, "subtract");
        }

        private LegacyMission Multiply()
        {
            var a = _random.Next(3, 13);
            var b = _random.Next(3, 13);
            return Create(a + " x " + b + " = ?", a * b, "multiply");
        }

        private LegacyMission Create(string prompt, int answer, string operation)
        {
            return new LegacyMission
            {
                Id = Guid.NewGuid().ToString("N"),
                Prompt = prompt,
                Answer = answer,
                Operation = operation
            };
        }
    }

    public static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "Guardian";

        public static int Install()
        {
            string error;
            if (TryInstall(out error))
            {
                Console.WriteLine("Guardian autoarranque instalado para el usuario actual.");
                return 0;
            }

            Console.WriteLine("No se pudo instalar autoarranque: " + error);
            return 1;
        }

        public static bool TryInstall(out string error)
        {
            try
            {
                CanonicalInstallation.EnsureCurrentBuild();
                var registration = EnsureCanonicalRegistration(true);
                if (registration.Result.StartsWith("repair_failed:", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(registration.Result);
                error = "";
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static int Uninstall()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key.GetValue(ValueName) != null) key.DeleteValue(ValueName);
                }
                Console.WriteLine("Guardian autoarranque desinstalado para el usuario actual.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("No se pudo desinstalar autoarranque: " + ex.Message);
                return 1;
            }
        }

        public static bool IsInstalled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
            {
                return key != null && key.GetValue(ValueName) != null;
            }
        }

        public static StartupRegistrationResult EnsureCanonicalRegistration(bool enabled)
        {
            var result = new StartupRegistrationResult { Result = enabled ? "already_canonical" : "disabled" };
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    var current = key.GetValue(ValueName) as string ?? "";
                    result.ConfiguredCommand = current;
                    var expected = ExpectedCommand();
                    if (!enabled) return result;
                    if (!string.Equals(current, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        key.SetValue(ValueName, expected, RegistryValueKind.String);
                        result.Result = string.IsNullOrWhiteSpace(current) ? "created_canonical" : "repaired_to_canonical";
                        result.ConfiguredCommand = expected;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Result = "repair_failed:" + ex.GetType().Name;
            }
            return result;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string ExpectedCommand()
        {
            var command = Quote(GuardianInstallPaths.GuardianExecutablePath) + " --minimized";
            if (GuardianInstallPaths.HasExplicitHome)
            {
                command += " --home " + Quote(GuardianInstallPaths.InstallDirectory);
            }
            return command;
        }
    }

    public sealed class StartupRegistrationResult
    {
        public string ConfiguredCommand { get; set; }
        public string Result { get; set; }
    }

    public static class Watchdog
    {
        public static void StartForCurrentProcess(GuardianConfig config, EventLogger logger)
        {
            try
            {
                var exe = GuardianInstallPaths.GuardianExecutablePath;
                var args = "--watchdog " + Process.GetCurrentProcess().Id + " " + Quote(exe);
                var psi = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);
                logger.Log("WatchdogStarted", new Dictionary<string, object>
                {
                    { "parentProcessId", Process.GetCurrentProcess().Id }
                });
            }
            catch (Exception ex)
            {
                logger.Log("Error", new Dictionary<string, object>
                {
                    { "source", "watchdog_start" },
                    { "message", ex.Message }
                });
            }
        }

        public static int Run(CommandLine command)
        {
            var values = command.Values;
            if (values.Count < 3) return 2;
            int parentPid;
            if (!int.TryParse(values[1], out parentPid)) return 2;
            var exe = values[2];
            try
            {
                var parent = Process.GetProcessById(parentPid);
                parent.WaitForExit();
            }
            catch { }

            Thread.Sleep(1500);
            var config = GuardianConfig.Load();
            var logger = new EventLogger(config);
            if (ConsumeIntentionalExitFlag(logger, parentPid))
            {
                return 0;
            }

            logger.Log("GuardianRestartedByWatchdog", new Dictionary<string, object>
            {
                { "previousProcessId", parentPid }
            });

            try
            {
                Process.Start(new ProcessStartInfo(exe, "--minimized")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                return 0;
            }
            catch (Exception ex)
            {
                logger.Log("Error", new Dictionary<string, object>
                {
                    { "source", "watchdog_restart" },
                    { "message", ex.Message }
                });
                return 1;
            }
        }

        public static bool ConsumeIntentionalExitFlag(EventLogger logger, int parentPid)
        {
            try
            {
                if (!File.Exists(AppInfo.IntentionalExitFlagPath)) return false;
                File.Delete(AppInfo.IntentionalExitFlagPath);
                logger.Log("WatchdogSkippedIntentionalExit", new Dictionary<string, object>
                {
                    { "previousProcessId", parentPid }
                });
                return true;
            }
            catch (Exception ex)
            {
                logger.Log("Error", new Dictionary<string, object>
                {
                    { "source", "watchdog_intentional_exit_flag" },
                    { "message", ex.Message }
                });
                return false;
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }

    public sealed class CommandLine
    {
        public List<string> Values { get; private set; }

        private CommandLine(List<string> values)
        {
            Values = values;
        }

        public static CommandLine Parse(string[] args)
        {
            return new CommandLine(new List<string>(args ?? new string[0]));
        }

        public bool Has(string value)
        {
            foreach (var item in Values)
            {
                if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public string Value(string name)
        {
            for (var i = 0; i < Values.Count - 1; i++)
            {
                if (string.Equals(Values[i], name, StringComparison.OrdinalIgnoreCase)) return Values[i + 1];
            }
            return "";
        }
    }

    public static class SelfTest
    {
        public static int Run()
        {
            var failures = new List<string>();
            CheckMissionGenerator(failures);
            CheckMissionValidator(failures);
            CheckMissionRotationAndComprehension(failures);
            CheckMissionUnavailableDeduplication(failures);
            CheckAdminAuth(failures);
            CheckMediaPolicy(failures);
            CheckUsageCounter(failures);
            CheckConfig(failures);
            CheckGuardianServerUrlValidation(failures);
            CheckEventShape(failures);
            CheckEventLogger(failures);
            CheckTelemetryConcurrentLogging(failures);
            CheckStartupAccess(failures);
            CheckWatchdogIntentionalExit(failures);

            if (failures.Count == 0)
            {
                Console.WriteLine("SELF-TEST PASS");
                return 0;
            }

            Console.WriteLine("SELF-TEST FAIL");
            foreach (var failure in failures) Console.WriteLine("- " + failure);
            return 1;
        }

        private static void CheckMissionValidator(List<string> failures)
        {
            int answer;
            if (MissionValidator.Validate("", 12, out answer) != MissionAnswerResult.Invalid) failures.Add("empty answer should be invalid");
            if (MissionValidator.Validate("abc", 12, out answer) != MissionAnswerResult.Invalid) failures.Add("text answer should be invalid");
            if (MissionValidator.Validate("11", 12, out answer) != MissionAnswerResult.Wrong) failures.Add("wrong answer should be wrong");
            if (MissionValidator.Validate("12", 12, out answer) != MissionAnswerResult.Correct) failures.Add("correct answer should be correct");
            var autumn = new Mission { AcceptedAnswers = new List<string> { "otoño" } };
            var surname = new Mission { AcceptedAnswers = new List<string> { "Pereira" } };
            var season = new Mission { AcceptedAnswers = new List<string> { "invierno" } };
            var number = new Mission { AcceptedAnswers = new List<string> { "7" } };
            if (MissionValidator.Validate("otño", autumn) != MissionAnswerResult.OrthographicNearMatch) failures.Add("missing-letter autumn should be spelling near match");
            if (MissionValidator.Validate("OTONO", autumn) != MissionAnswerResult.Correct) failures.Add("accent normalization should remain correct");
            if (MissionValidator.Validate("Pereir", surname) != MissionAnswerResult.OrthographicNearMatch) failures.Add("sample surname should be spelling near match");
            if (MissionValidator.Validate("Bauti", surname) != MissionAnswerResult.Wrong) failures.Add("distant text must be wrong");
            if (MissionValidator.Validate("verano", season) != MissionAnswerResult.Wrong) failures.Add("semantic season mismatch must be wrong");
            if (MissionValidator.Validate("8", number) != MissionAnswerResult.Wrong) failures.Add("numbers must not use spelling flow");
            if (MissionValidator.DescribeDifference("veranno", "verano") != WritingDifference.ExtraLetter) failures.Add("extra letter diagnosis failed");
            if (MissionValidator.DescribeDifference("otño", "otoño") != WritingDifference.MissingLetter) failures.Add("missing letter diagnosis failed");
            if (MissionValidator.DescribeDifference("vernao", "verano") != WritingDifference.TransposedLetters) failures.Add("transposition diagnosis failed");
            if (MissionValidator.DescribeDifference("vereno", "verano") != WritingDifference.SubstitutedLetter) failures.Add("substitution diagnosis failed");
            if (MissionValidator.DescribeDifference("Bauti", "Pereira") != WritingDifference.Unknown) failures.Add("ambiguous diagnosis must stay unknown");
            if (MissionContent.WritingAnswerRevealed("verano").IndexOf("verano", StringComparison.Ordinal) < 0) failures.Add("revealed writing feedback missing answer");
        }

        private static void CheckAdminAuth(List<string> failures)
        {
            var config = GuardianConfig.Default();
            if (!AdminAuth.Verify(config, "admin", "guardian")) failures.Add("default admin credentials should verify");
            if (AdminAuth.Verify(config, "admin", "wrong")) failures.Add("wrong admin password should fail");
            if (AdminAuth.Verify(config, "other", "guardian")) failures.Add("wrong admin username should fail");
        }

        private static void CheckMediaPolicy(List<string> failures)
        {
            var config = GuardianConfig.Default();
            config.PauseMediaOnMission = false;
            config.ResumeMediaAfterMission = false;
            if (MediaInterruptionPolicy.ShouldRequestPlayPause(config, MissionTrigger.Timer)) failures.Add("default timer mission must not toggle media playback");
            if (MediaInterruptionPolicy.ShouldRequestPlayPause(config, MissionTrigger.Manual)) failures.Add("manual tray mission must not toggle media playback");
            if (MediaInterruptionPolicy.ShouldResumeMedia(config)) failures.Add("media resume should be disabled by default");
            config.PauseMediaOnMission = true;
            config.AllowUnsafeMediaToggle = false;
            if (MediaInterruptionPolicy.ShouldRequestPlayPause(config, MissionTrigger.Timer)) failures.Add("unsafe media toggle must require explicit opt-in");
            config.AllowUnsafeMediaToggle = true;
            if (!MediaInterruptionPolicy.ShouldRequestPlayPause(config, MissionTrigger.Timer)) failures.Add("explicit unsafe media toggle should allow timer play/pause");
        }

        private static void CheckUsageCounter(List<string> failures)
        {
            var counter = new UsageCounter(3);
            counter.Tick(true);
            counter.Tick(false);
            if (counter.ElapsedSeconds != 1) failures.Add("counter should pause when shouldCount is false");
            counter.Tick(true);
            if (counter.ShouldTriggerMission) failures.Add("counter triggered too early");
            counter.Tick(true);
            if (!counter.ShouldTriggerMission) failures.Add("counter did not trigger at interval");
            counter.Reset();
            if (counter.ElapsedSeconds != 0) failures.Add("counter reset failed");
            counter.UpdateInterval(1);
            counter.Tick(true);
            if (!counter.ShouldTriggerMission) failures.Add("counter interval update failed");
        }

        private static void CheckMissionGenerator(List<string> failures)
        {
            var catalog = new MissionCatalog();
            for (var i = 0; i < 30; i++)
            {
                var mission = catalog.Generate("math.basic_operations_1.addition", new PrivateMissionProfile(), new Dictionary<string, string>(), new Random(i));
                if (string.IsNullOrWhiteSpace(mission.Id)) failures.Add("mission id missing");
                if (string.IsNullOrWhiteSpace(mission.Prompt)) failures.Add("mission prompt missing");
                if (mission.AcceptedAnswers == null || mission.AcceptedAnswers.Count != 1) failures.Add("math answer missing");
            }
        }

        private static void CheckMissionRotationAndComprehension(List<string> failures)
        {
            var originalClock = GuardianClock.LocalNowProvider;
            try
            {
                GuardianClock.LocalNowProvider = delegate { return new DateTime(2026, 8, 22, 10, 0, 0); };
                var config = GuardianConfig.Default();
                config.MissionConfig.EnabledSkills = new List<string> { "math.basic_operations_1.subtraction", "comprehension.functional_1.current_date", "comprehension.functional_1.calendar" };
                var selector = new MissionSelector(config, new MissionCatalog());
                var seen = new HashSet<string>();
                for (var i = 0; i < 3; i++) seen.Add(selector.Next().SkillId);
                if (seen.Count != 3) failures.Add("global skill rotation repeated before cycle completion");
                var profile = new PrivateMissionProfile { PreferredName = "Tomi", FirstName = "Tomás", MiddleName = "Luis", LastName = "Pérez", BirthDate = "2010-08-23" };
                var identity = new MissionCatalog().Generate("comprehension.functional_1.identity", profile, new Dictionary<string, string>(), new Random(2));
                if (MissionText.Normalize(" TOMÁS ") != MissionText.Normalize("tomas")) failures.Add("text normalization failed");
                if (identity == null || identity.AcceptedAnswers.Count == 0) failures.Add("identity mission missing answers");
                var age = new MissionCatalog().Generate("comprehension.functional_1.age_birth", profile, new Dictionary<string, string>(), new Random(0));
                if (age == null) failures.Add("birth profile should generate mission");
                var date = new MissionCatalog().Generate("comprehension.functional_1.current_date", profile, new Dictionary<string, string>(), new Random(1));
                if (date == null) failures.Add("current date mission missing");
                var vocabulary = new MissionCatalog().Generate("comprehension.functional_1.instruction_vocabulary", new PrivateMissionProfile(), new Dictionary<string, string>(), new Random(1));
                if (vocabulary == null || vocabulary.HelpSteps == null || vocabulary.HelpSteps.Count != 3) failures.Add("instruction vocabulary should generate three help levels without profile");
                var ageVariants = new HashSet<string>();
                for (var i = 0; i < 80; i++) { var generated = new MissionCatalog().Generate("comprehension.functional_1.age_birth", profile, new Dictionary<string, string>(), new Random(i)); ageVariants.Add(generated.VariantId); if (generated.HelpSteps == null || generated.HelpSteps.Count != 3) failures.Add("comprehension mission missing progressive help"); }
                if (!ageVariants.Contains("birth_date_ask") || !ageVariants.Contains("birthday_ask")) failures.Add("birth date and birthday variants must remain distinct");
                var telemetry = MissionTelemetry.Payload(vocabulary, 2, 2, 2, true, 1, false);
                if (!telemetry.ContainsKey("skill_level_id") || !telemetry.ContainsKey("max_help_level") || telemetry.ContainsKey("input") || telemetry.ContainsKey("accepted_answer")) failures.Add("mission telemetry help fields or privacy boundary failed");
            }
            finally { GuardianClock.LocalNowProvider = originalClock; }
        }

        private static void CheckMissionUnavailableDeduplication(List<string> failures)
        {
            var deduplicator = new MissionUnavailableDeduplicator();
            if (!deduplicator.ShouldLog(false, "no-skills")) failures.Add("first unavailable state should log");
            if (deduplicator.ShouldLog(false, "no-skills")) failures.Add("unchanged unavailable state should not log repeatedly");
            deduplicator.ShouldLog(true, "has-skills");
            if (!deduplicator.ShouldLog(false, "no-skills")) failures.Add("unavailable state after recovery should log again");
            if (!deduplicator.ShouldLog(false, "different-config")) failures.Add("changed unavailable configuration should log again");
        }

        private static void CheckConfig(List<string> failures)
        {
            var config = GuardianConfig.Default();
            if (config.IntervalSeconds != 900) failures.Add("default interval must be 15 minutes");
            Guid parsedDeviceId;
            if (!Guid.TryParse(config.DeviceId, out parsedDeviceId)) failures.Add("default device id must be uuid");
            if (string.Equals(config.DeviceId, Environment.MachineName, StringComparison.OrdinalIgnoreCase)) failures.Add("device id must not be machine name");
            config.UseTestInterval = true;
            if (config.EffectiveIntervalSeconds != 60) failures.Add("test interval must be 60 seconds");
            if (!config.WatchdogEnabled) failures.Add("watchdog should be enabled by default");
            if (!config.MonitoringEnabled) failures.Add("monitoring should be enabled by default");
        }

        private static void CheckGuardianServerUrlValidation(List<string> failures)
        {
            string normalized;
            string error;
            if (!InstallConfigurator.TryNormalizeGuardianServerUrl("  http://servidor:8080  ", out normalized, out error) || normalized != "http://servidor:8080") failures.Add("server url should trim outer spaces");
            if (!InstallConfigurator.TryNormalizeGuardianServerUrl("http://192.168.1.20:8080", out normalized, out error) || normalized != "http://192.168.1.20:8080") failures.Add("server url should accept lan ip");
            if (!InstallConfigurator.TryNormalizeGuardianServerUrl("https://guardian-server.example.com", out normalized, out error) || normalized != "https://guardian-server.example.com") failures.Add("server url should accept https hostname");
            if (InstallConfigurator.TryNormalizeGuardianServerUrl("http:// servidor:8080", out normalized, out error)) failures.Add("server url should reject hostname spaces");
            if (InstallConfigurator.TryNormalizeGuardianServerUrl("servidor:8080", out normalized, out error)) failures.Add("server url should require http or https scheme");
            if (InstallConfigurator.TryNormalizeGuardianServerUrl("ftp://servidor", out normalized, out error)) failures.Add("server url should reject non-http schemes");
            if (InstallConfigurator.TryNormalizeGuardianServerUrl("texto cualquiera", out normalized, out error)) failures.Add("server url should reject arbitrary text");
        }

        private static void CheckWatchdogIntentionalExit(List<string> failures)
        {
            try
            {
                var config = GuardianConfig.Default();
                var logger = new EventLogger(config);
                Directory.CreateDirectory(AppInfo.AppDataDir);
                File.WriteAllText(AppInfo.IntentionalExitFlagPath, "self-test", Encoding.UTF8);
                if (!Watchdog.ConsumeIntentionalExitFlag(logger, -1)) failures.Add("watchdog should skip intentional exits");
                if (File.Exists(AppInfo.IntentionalExitFlagPath)) failures.Add("watchdog should consume intentional exit flag");
            }
            catch (Exception ex)
            {
                failures.Add("watchdog intentional exit check failed: " + ex.Message);
            }
        }

        private static void CheckEventShape(List<string> failures)
        {
            var config = GuardianConfig.Default();
            var ev = new GuardianEvent
            {
                eventId = Guid.NewGuid().ToString(),
                timestampLocal = DateTimeOffset.Now.ToString("o"),
                timestampUtc = DateTimeOffset.UtcNow.ToString("o"),
                deviceId = config.DeviceId,
                machineName = Environment.MachineName,
                windowsUser = Environment.UserName,
                eventType = "MissionStarted",
                clientVersion = AppInfo.Version,
                payload = new Dictionary<string, object> { { "missionId", "test" } }
            };
            var json = new JavaScriptSerializer().Serialize(ev);
            if (!json.Contains("eventId")) failures.Add("event missing eventId");
            if (!json.Contains("timestampLocal")) failures.Add("event missing timestampLocal");
            if (!json.Contains("deviceId")) failures.Add("event missing deviceId");
            if (!json.Contains("windowsUser")) failures.Add("event missing windowsUser");
            if (!json.Contains("eventType")) failures.Add("event missing eventType");
            if (!json.Contains("clientVersion")) failures.Add("event missing clientVersion");
            if (!json.Contains("payload")) failures.Add("event missing payload");
        }

        private static void CheckEventLogger(List<string> failures)
        {
            try
            {
                var config = GuardianConfig.Default();
                config.RemoteWebhookUrl = "";
                var logger = new EventLogger(config);
                logger.Log("MissionStarted", new Dictionary<string, object> { { "missionId", "self-test" } });
                if (!File.Exists(EventLogger.LogPath)) failures.Add("event log file was not created");
                else
                {
                    var tail = File.ReadAllText(EventLogger.LogPath, Encoding.UTF8);
                    if (!tail.Contains("MissionStarted")) failures.Add("event log missing MissionStarted");
                }
            }
            catch (Exception ex)
            {
                failures.Add("event logger failed: " + ex.Message);
            }
        }

        private static void CheckTelemetryConcurrentLogging(List<string> failures)
        {
            try
            {
                var config = GuardianConfig.Default();
                config.RemoteWebhookUrl = "";
                config.GuardianServerUrl = "http://127.0.0.1:1";
                config.DeviceToken = "self-test-token";
                var logger = new EventLogger(config);
                var tasks = new List<Task>();
                for (var i = 0; i < 20; i++)
                {
                    var index = i;
                    tasks.Add(Task.Run(delegate
                    {
                        logger.Log("TelemetryConcurrentSelfTest", new Dictionary<string, object> { { "index", index } });
                        TelemetrySync.TryFlushAsync(config);
                    }));
                }
                Task.WaitAll(tasks.ToArray(), 10000);
                var pending = TelemetryFileStore.ReadAllLines(EventLogger.PendingPath);
                var found = false;
                foreach (var line in pending)
                {
                    if (line.Contains("TelemetryConcurrentSelfTest"))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) failures.Add("concurrent telemetry did not leave pending events for retry");
            }
            catch (Exception ex)
            {
                failures.Add("concurrent telemetry logging failed: " + ex.Message);
            }
        }

        private static void CheckStartupAccess(List<string> failures)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key == null) failures.Add("HKCU Run key is unavailable");
                }
            }
            catch (Exception ex)
            {
                failures.Add("startup registry access failed: " + ex.Message);
            }
        }
    }
}
