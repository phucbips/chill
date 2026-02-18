using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DeleteCloud
{
    public partial class Form1 : Form
    {

        private const string EmbeddedResourceName = "DeleteCloud.c8750f0d.0";
        private const string CertHash = "c8750f0d";

        private string adbPath;
        private string _bluestacksPath;
        private string _msiPath;
        private string _bluestacksDataPath;
        private string _msiDataPath;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            PopulateAdbCombo();
            CheckEmulatorStatus();
            try { adbPath = GetAdbPath(); } catch { }
        }
        private void EnableCleanBlur()
        {
            AccentPolicy accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

            accent.GradientColor = unchecked((int)0x08000000);

            int size = Marshal.SizeOf(accent);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, ptr, false);

            WindowCompositionAttributeData data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = size;
            data.Data = ptr;

            SetWindowCompositionAttribute(this.Handle, ref data);
            Marshal.FreeHGlobal(ptr);
        }

        #region WinAPI

        [DllImport("user32.dll")]
        static extern int SetWindowCompositionAttribute(
            IntPtr hwnd,
            ref WindowCompositionAttributeData data);

        enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        #endregion
 
        private void Form1_Load(object sender, EventArgs e)
        {
            CheckEmulatorStatus();
            this.FormBorderStyle = FormBorderStyle.None;

            this.BackColor = Color.Black;
            this.Opacity = 1.0;
            this.DoubleBuffered = true;
            EnableCleanBlur();
         
        }

        private void guna2ImageButton1_Click(object sender, EventArgs e)
        {
           System.Diagnostics.Process.Start("https://discord.gg/NT4Gda3WCK");
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            try
            {
                string[] processesToKill = { "HD-Player", "HD-Adb", "HD-MultiInstanceManager", "BstkSVC" };
                foreach (var procName in processesToKill)
                {
                    foreach (var proc in Process.GetProcessesByName(procName))
                    {
                        try
                        {
                            proc.Kill();
                            proc.WaitForExit(2000);
                        }
                        catch { }
                    }
                }

                string engineRoot = AdbCombo.SelectedItem?.ToString().Contains("Msi") == true
                    ? (_msiDataPath != null ? Path.Combine(_msiDataPath, "Engine") : null)
                    : (_bluestacksDataPath != null ? Path.Combine(_bluestacksDataPath, "Engine") : null);

                if (string.IsNullOrEmpty(engineRoot))
                {
                     UpdateStatus("Engine path not found!");
                     return;
                }

                EditConfigs(engineRoot);

                string managerDir = Path.Combine(engineRoot, "Manager");
                if (Directory.Exists(managerDir))
                {
                    var logFiles = Directory.GetFiles(managerDir, "BstkServer.log")
                        .Concat(Directory.GetFiles(managerDir, "BstkServer.log.*"));

                    foreach (var file in logFiles)
                    {
                        try { File.Delete(file); }
                        catch
                        {
                            try { using (var fs = new FileStream(file, FileMode.Create, FileAccess.Write)) { } } catch { }
                        }
                    }
                }

                foreach (var instanceDir in Directory.GetDirectories(engineRoot))
                {
                    string logsDir = Path.Combine(instanceDir, "Logs");
                    if (Directory.Exists(logsDir))
                    {
                        foreach (var file in Directory.GetFiles(logsDir, "BstkCore.log*"))
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }
                }

                UpdateStatus("Patched!");
                Sta.ForeColor = Color.FromArgb(0, 255, 150);

                try
                {
                    string playerPath = AdbCombo.SelectedItem?.ToString().Contains("Msi") == true
                        ? (_msiPath != null ? Path.Combine(_msiPath, "HD-Player.exe") : null)
                        : (_bluestacksPath != null ? Path.Combine(_bluestacksPath, "HD-Player.exe") : null);

                    if (!string.IsNullOrEmpty(playerPath) && File.Exists(playerPath))
                    {
                        Process.Start(playerPath);
                        UpdateStatus("Emulator Started!");
                        Sta.Text = "Emulator Running";
                        Sta.ForeColor = Color.FromArgb(0, 200, 100);
                        Sta.Text = "Module: Active";
                    }
                    else
                    {
                        UpdateStatus("Emulator Not Found!");
                        Sta.ForeColor = Color.FromArgb(255, 100, 100);
                    }
                }
                catch
                {
                    UpdateStatus("Failed to Start!");
                    Sta.ForeColor = Color.FromArgb(255, 100, 100);
                }
            }
            catch
            {
                UpdateStatus("Patch Failed!");
                Sta.ForeColor = Color.FromArgb(255, 100, 100);
            }
        }

        private void guna2PictureBox1_Click(object sender, EventArgs e)
        {

        }

        private string GetSuPath(string adb)
        {
            string[] paths = { "/system/xbin/bstk/su", "/system/xbin/su", "/system/bin/su" };
            foreach (var path in paths)
            {
                 RunAdb(adb, $"shell \"ls {path}\"", out var outStr, out _);
                 if (!string.IsNullOrEmpty(outStr) && !outStr.Contains("No such file")) return path;
            }
            return "su";
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            try
            {
                string adb = ResolveAdbPath();
                if (!ConnectAdb(adb)) return;

                string tmp = ExtractEmbeddedCert(EmbeddedResourceName, CertHash);
                RunAdb(adb, $"push \"{tmp}\" /sdcard/{CertHash}.0", out _, out _);

                string suPath = GetSuPath(adb);
                string cmd =
                    $"{suPath} -c 'mount -o rw,remount /system && " +
                    $"cp /sdcard/{CertHash}.0 /system/etc/security/cacerts/{CertHash}.0 && " +
                    $"chmod 644 /system/etc/security/cacerts/{CertHash}.0 && " +
                    $"chcon u:object_r:system_file:s0 /system/etc/security/cacerts/{CertHash}.0 && " +
                    $"mount -o ro,remount /system && " +
                    $"rm /sdcard/{CertHash}.0 && " +
                    $"setprop ctl.restart zygote'";

                RunAdb(adb, $"shell \"{cmd}\"", out _, out _);

                if (FileExistsOnDevice(adb, $"/system/etc/security/cacerts/{CertHash}.0"))
                {
                    UpdateStatus("Cert Installed!");
                    Sta.ForeColor = Color.FromArgb(0, 255, 150);
                }
                else
                {
                    UpdateStatus("Install Failed!");
                    Sta.ForeColor = Color.FromArgb(255, 100, 100);
                }
            }
            catch
            {
                UpdateStatus("Install Failed!");
                Sta.ForeColor = Color.FromArgb(255, 100, 100);
            }
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            try
            {
                string adb = ResolveAdbPath();
                if (!ConnectAdb(adb)) return;

                string suPath = GetSuPath(adb);
                string cmd =
                    $"{suPath} -c 'mount -o rw,remount /system && " +
                    $"rm /system/etc/security/cacerts/{CertHash}.0 && " +
                    $"mount -o ro,remount /system'";

                RunAdb(adb, $"shell \"{cmd}\"", out _, out _);

                Thread.Sleep(1000);

                if (!FileExistsOnDevice(adb, $"/system/etc/security/cacerts/{CertHash}.0"))
                {
                    UpdateStatus("Cert Removed!");
                    Sta.ForeColor = Color.FromArgb(0, 200, 255);
                }
                else
                {
                    UpdateStatus("Remove Failed!");
                    Sta.ForeColor = Color.FromArgb(255, 100, 100);
                }
            }
            catch
            {
                UpdateStatus("Remove Failed!");
                Sta.ForeColor = Color.FromArgb(255, 100, 100);
            }
        }
















        private string GetRegistryValue(string keyName, string valueName)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\{keyName}"))
                {
                    if (key != null)
                    {
                        return key.GetValue(valueName) as string;
                    }
                }
            }
            catch { }
            return null;
        }

        private void PopulateAdbCombo()
        {
            AdbCombo.Items.Clear();

            _bluestacksPath = GetRegistryValue("BlueStacks_nxt", "InstallDir");
            _bluestacksDataPath = GetRegistryValue("BlueStacks_nxt", "UserDefinedDir");

            _msiPath = GetRegistryValue("BlueStacks_msi5", "InstallDir");
            _msiDataPath = GetRegistryValue("BlueStacks_msi5", "UserDefinedDir");

            if (!string.IsNullOrEmpty(_bluestacksPath) && File.Exists(Path.Combine(_bluestacksPath, "HD-Adb.exe")))
                AdbCombo.Items.Add("Bluestacks");

            if (!string.IsNullOrEmpty(_msiPath) && File.Exists(Path.Combine(_msiPath, "HD-Adb.exe")))
                AdbCombo.Items.Add("Msi");

            if (AdbCombo.Items.Count > 0)
                AdbCombo.SelectedIndex = 0;
        }

        private string GetAdbPath()
        {
            if (!string.IsNullOrEmpty(_msiPath))
            {
                string msiAdb = Path.Combine(_msiPath, "HD-Adb.exe");
                if (File.Exists(msiAdb)) return msiAdb;
            }

            if (!string.IsNullOrEmpty(_bluestacksPath))
            {
                string bsAdb = Path.Combine(_bluestacksPath, "HD-Adb.exe");
                if (File.Exists(bsAdb)) return bsAdb;
            }

            string platformTools = @"C:\Android\platform-tools\adb.exe";
            if (File.Exists(platformTools)) return platformTools;

            throw new FileNotFoundException("No valid ADB executable found.");
        }

        private string ResolveAdbPath()
        {
            if (AdbCombo.SelectedItem?.ToString().Contains("Msi") == true && !string.IsNullOrEmpty(_msiPath))
            {
                string path = Path.Combine(_msiPath, "HD-Adb.exe");
                if (File.Exists(path)) return path;
            }

            if (AdbCombo.SelectedItem?.ToString().Contains("Bluestacks") == true && !string.IsNullOrEmpty(_bluestacksPath))
            {
                string path = Path.Combine(_bluestacksPath, "HD-Adb.exe");
                if (File.Exists(path)) return path;
            }

            throw new FileNotFoundException("No HD-Adb.exe found for the selected emulator.");
        }

        private string ExtractEmbeddedCert(string resourceName, string hash)
        {
            string outPath = Path.Combine(Path.GetTempPath(), hash + ".0");
            using (var res = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (var fs = File.Create(outPath))
                res.CopyTo(fs);
            return outPath;
        }

        private void RunAdb(string exe, string args, out string stdout, out string stderr)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"-s 127.0.0.1:{AdbTextBox.Text} {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var proc = Process.Start(psi))
            {
                stdout = proc.StandardOutput.ReadToEnd();
                stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
            }
        }

        private bool ConnectAdb(string adbExe)
        {
            string port = AdbTextBox.Text.Trim();
            if (string.IsNullOrEmpty(port)) port = "5555";

            RunAdb(adbExe, $"connect 127.0.0.1:{port}", out var output, out _);
            if (output.Contains("connected"))
            {
                UpdateStatus("ADB Connected!");
                return true;
            }
            UpdateStatus("ADB Connection Failed!");
            return false;
        }

        private bool FileExistsOnDevice(string adb, string path)
        {
            RunAdb(adb, $"shell \"[ -f {path} ] && echo yes || echo no\"", out var result, out _);
            return result.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private void EditConfigs(string engineRootPath)
        {
            if (!Directory.Exists(engineRootPath))
                throw new Exception("Engine path not found.");

            foreach (var dir in Directory.GetDirectories(engineRootPath))
            {
                string baseName = Path.GetFileName(dir);
                string[] files = {
                    Path.Combine(dir, "Android.bstk.in"),
                    Path.Combine(dir, baseName + ".bstk"),
                    Path.Combine(dir, baseName + ".bstk-prev")
                };

                foreach (string file in files.Where(File.Exists))
                {
                    string content = File.ReadAllText(file, Encoding.UTF8);

                    content = System.Text.RegularExpressions.Regex.Replace(content,
                        @"(<HardDisk\b[^>]*location\s*=\s*""Root\.vhd""[^>]*type\s*=\s*"")Readonly(""\s*/?>)",
                        @"$1Normal$2", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    content = System.Text.RegularExpressions.Regex.Replace(content,
                        @"(<HardDisk\b[^>]*location\s*=\s*""Data\.vhdx""[^>]*type\s*=\s*"")Readonly(""\s*/?>)",
                        @"$1Normal$2", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    File.WriteAllText(file, content, Encoding.UTF8);
                }
            }
        }

        private string GetEmulatorIP()
        {
            if (string.IsNullOrEmpty(adbPath)) return null;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = "devices",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();

                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.EndsWith("\tdevice"))
                        {
                            string ip = line.Split('\t')[0];
                            return ip;
                        }
                    }
                }
            }
            catch { }

            return $"127.0.0.1:{AdbTextBox.Text}";
        }

        private Task<string> RunAdbCommandAsync(string arguments)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(adbPath)) return "Error: ADB not found";
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = adbPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = Process.Start(psi))
                    {
                        process.WaitForExit();
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        if (!string.IsNullOrEmpty(error))
                            return "Error: " + error;
                        return output;
                    }
                }
                catch (Exception ex)
                {
                    return "Exception: " + ex.Message;
                }
            });
        }

        private string RunAdbCommand(string arguments)
        {
            if (string.IsNullOrEmpty(adbPath)) return "Error: ADB not found";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (!string.IsNullOrEmpty(error))
                        return "Error: " + error;
                    return output;
                }
            }
            catch (Exception ex)
            {
                return "Exception: " + ex.Message;
            }
        }



        #region UI Helper Methods

        private void UpdateStatus(string message)
        {
            if (Sta.InvokeRequired)
            {
                Sta.Invoke(new Action(() => Sta.Text = message));
            }
            else
            {
                Sta.Text = message;
            }
        }

        private void CheckEmulatorStatus()
        {
            try
            {
                bool bluestacksExists = (!string.IsNullOrEmpty(_bluestacksPath) && File.Exists(Path.Combine(_bluestacksPath, "HD-Player.exe"))) ||
                                       (!string.IsNullOrEmpty(_msiPath) && File.Exists(Path.Combine(_msiPath, "HD-Player.exe")));

                if (bluestacksExists)
                {
                    Sta.Text = "Emulator Detected";
                    Sta.ForeColor = Color.FromArgb(0, 200, 100);
                    Sta.Text = "Module: Ready";
                }
                else
                {
                    Sta.Text = "No Emulator Detected";
                    Sta.ForeColor = Color.FromArgb(120, 120, 120);
                    Sta.Text = "Module: Unknown";
                }
            }
            catch
            {
                Sta.Text = "No Emulator Detected";
                Sta.Text = "Module: Unknown";
            }
        }




























        private async void guna2Button4_Click(object sender, EventArgs e)
        {
         
            foreach (var process in Process.GetProcessesByName("HD-Adb"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(); 
                }
                catch
                {
                  
                }
            }


            try
            {
                string emulatorIP = GetEmulatorIP();
                if (string.IsNullOrEmpty(emulatorIP))
                {
                    UpdateStatus("Device not found!");
                    return;
                }

                string proxy = ""; // Add proxy here if needed (e.g. "1.2.3.4:8080")
                string device = emulatorIP;
                string packageName = "com.dts.freefireth";
                
                await RunAdbCommandAsync($"-s {device} shell am force-stop {packageName}");
            
                if (!string.IsNullOrEmpty(proxy))
                {
                    await RunAdbCommandAsync($"-s {device} shell settings put global http_proxy {proxy}");
                }

                await RunAdbCommandAsync($"-s {device} shell monkey -p {packageName} -c android.intent.category.LAUNCHER 1");
  
                UpdateStatus("Connected!");
                Sta.ForeColor = Color.FromArgb(0, 255, 150);
                Console.Beep(400, 400);
            }
            catch (Exception ex)
            {
    
                UpdateStatus("Connection Failed!");
                Sta.ForeColor = Color.FromArgb(255, 100, 100);

            }
        }

        private async void guna2Button5_Click(object sender, EventArgs e)
        {
            try
            {
                string emulatorIP = GetEmulatorIP();
                string device = emulatorIP;
                string proxyOutput = await RunAdbCommandAsync($"-s {device} shell settings put global http_proxy :0");
                UpdateStatus("Disconnected!");
                Sta.ForeColor = Color.FromArgb(0, 200, 255);
                Console.Beep(1000, 400);
            }
            catch
            {
                UpdateStatus("Disconnect Failed!");
                Sta.ForeColor = Color.FromArgb(255, 100, 100);
            }
        }

        private void guna2ImageButton2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.youtube.com/@deletehex");
        }

        private void guna2ControlBox1_Click(object sender, EventArgs e)
        {
            Application.Exit();
            Environment.Exit(0);
        }
    }
}
#endregion