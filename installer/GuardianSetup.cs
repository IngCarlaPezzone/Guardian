using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace GuardianInstaller
{
    internal static class Program
    {
        private const string Version = "0.2.0";
        private const string AppName = "Guardian";
        private static readonly string AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Guardian");
        private static readonly string GuardianExe = Path.Combine(AppDir, "Guardian.exe");
        private static readonly string UpdaterExe = Path.Combine(AppDir, "GuardianUpdater.exe");
        private static readonly string SetupExe = Path.Combine(AppDir, "GuardianSetup.exe");
        private static readonly string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private static readonly string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Guardian";

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                if (Has(args, "--self-test")) return SelfTest();
                if (Has(args, "--uninstall")) return Uninstall();
                return Install();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Guardian no pudo completar la operacion:\n\n" + ex.Message, "Guardian", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static int Install()
        {
            Directory.CreateDirectory(AppDir);
            StopGuardian();
            Extract("Guardian.exe", GuardianExe);
            Extract("Guardian.exe.config", GuardianExe + ".config");
            Extract("GuardianUpdater.exe", UpdaterExe);
            CopySelf();
            WriteConfig();
            RegisterStartup();
            RegisterUninstall();
            StartGuardian();

            MessageBox.Show(
                "Guardian quedo instalado para este usuario de Windows.\n\n" +
                "Version final: la mision aparece cada 15 minutos.\n" +
                "Modo prueba de 60 segundos disponible desde config.json.\n\n" +
                "Usuario admin inicial: admin\n" +
                "Contrasena inicial: guardian\n\n" +
                "Podes desinstalarlo desde Configuracion > Aplicaciones.",
                "Guardian instalado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return 0;
        }

        private static int Uninstall()
        {
            Directory.CreateDirectory(AppDir);
            File.WriteAllText(Path.Combine(AppDir, "intentional-exit.flag"), DateTimeOffset.UtcNow.ToString("o"), Encoding.UTF8);
            StopGuardian();
            RemoveStartup();
            RemoveUninstall();
            ScheduleBinaryRemoval();
            MessageBox.Show("Guardian fue desinstalado para este usuario.\n\nSe conservaron config.json y events.jsonl.", "Guardian desinstalado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }

        private static int SelfTest()
        {
            if (GetResource("Guardian.exe") == null) return 2;
            if (GetResource("Guardian.exe.config") == null) return 3;
            if (GetResource("GuardianUpdater.exe") == null) return 4;
            return 0;
        }

        private static void Extract(string resourceName, string destination)
        {
            using (var input = GetResource(resourceName))
            {
                if (input == null) throw new InvalidOperationException("Falta recurso embebido: " + resourceName);
                using (var output = File.Create(destination))
                {
                    input.CopyTo(output);
                }
            }
        }

        private static Stream GetResource(string resourceName)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        }

        private static void CopySelf()
        {
            var current = Assembly.GetExecutingAssembly().Location;
            if (!string.Equals(current, SetupExe, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(current, SetupExe, true);
            }
        }

        private static void WriteConfig()
        {
            var configPath = Path.Combine(AppDir, "config.json");
            if (File.Exists(configPath)) return;
            var serverUrl = Environment.GetEnvironmentVariable("GUARDIAN_SERVER_URL") ?? "";
            var bootstrapToken = Environment.GetEnvironmentVariable("DEVICE_BOOTSTRAP_TOKEN") ?? "";
            if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(bootstrapToken))
            {
                throw new InvalidOperationException("Primera instalacion requiere GUARDIAN_SERVER_URL y DEVICE_BOOTSTRAP_TOKEN. Para el flujo normal use Instalar-Guardian.bat.");
            }

            var json = "{\"IntervalSeconds\":900,\"TestIntervalSeconds\":60,\"UseTestInterval\":false,\"WatchdogEnabled\":true,\"AutoStartEnabled\":true,\"MonitoringEnabled\":true,\"Difficulty\":\"9-11\",\"DeviceId\":\"\",\"MachineName\":\"" + Escape(Environment.MachineName) + "\",\"DisplayName\":\"\",\"GuardianServerUrl\":\"" + Escape(serverUrl) + "\",\"DeviceBootstrapToken\":\"" + Escape(bootstrapToken) + "\",\"DeviceToken\":\"\",\"RemoteConfigVersion\":0,\"PendingUpdateCommandId\":\"\",\"LastDeviceCommandId\":\"\",\"UpdaterPath\":\"\",\"RemoteWebhookUrl\":\"\",\"RemoteAuthToken\":\"\",\"RemoteConfigUrl\":\"\",\"RemoteConfigPollSeconds\":60,\"PauseMediaOnMission\":false,\"AllowUnsafeMediaToggle\":false,\"MuteSystemAudioDuringMission\":true,\"ResumeMediaAfterMission\":false,\"AdminUsername\":\"admin\",\"AdminPasswordSha256\":\"dde6e8974b46a1eddcd7ea3bbb899342f48cad896b47275a6f806062ec5ca14c\",\"MaxSolvedMissionsBeforeAutoExit\":3}";
            File.WriteAllText(configPath, json, Encoding.UTF8);
        }

        private static void RegisterStartup()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                key.SetValue("Guardian", Quote(GuardianExe), RegistryValueKind.String);
            }
        }

        private static void RemoveStartup()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
            {
                if (key != null) key.DeleteValue("Guardian", false);
            }
        }

        private static void RegisterUninstall()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(UninstallKeyPath))
            {
                key.SetValue("DisplayName", "Guardian", RegistryValueKind.String);
                key.SetValue("DisplayVersion", Version, RegistryValueKind.String);
                key.SetValue("Publisher", "Guardian", RegistryValueKind.String);
                key.SetValue("InstallLocation", AppDir, RegistryValueKind.String);
                key.SetValue("DisplayIcon", GuardianExe, RegistryValueKind.String);
                key.SetValue("UninstallString", Quote(SetupExe) + " --uninstall", RegistryValueKind.String);
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }

        private static void RemoveUninstall()
        {
            Registry.CurrentUser.DeleteSubKeyTree(UninstallKeyPath, false);
        }

        private static void StartGuardian()
        {
            Process.Start(new ProcessStartInfo(GuardianExe)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        private static void StopGuardian()
        {
            foreach (var process in Process.GetProcessesByName("Guardian"))
            {
                try { process.Kill(); process.WaitForExit(3000); }
                catch { }
            }
        }

        private static void ScheduleBinaryRemoval()
        {
            var command = "/C ping 127.0.0.1 -n 3 > nul & del /F /Q " + Quote(GuardianExe) + " " + Quote(GuardianExe + ".config") + " " + Quote(UpdaterExe) + " " + Quote(SetupExe);
            Process.Start(new ProcessStartInfo("cmd.exe", command)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        private static bool Has(string[] args, string value)
        {
            foreach (var arg in args ?? new string[0])
            {
                if (string.Equals(arg, value, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
