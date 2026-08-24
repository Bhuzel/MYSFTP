using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MYSFTP
{
    class SshManager
    {
        private Process activeStreamingProcess;
        private string askpassPath;
        private string host;
        private int port;
        private string user;
        private string password;
        private bool connected;
        private object streamLock = new object();
        private StringBuilder streamOutput = new StringBuilder();

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        public bool IsConnected { get { return connected; } }
        public string Host { get { return host; } }
        public int Port { get { return port; } }
        public string User { get { return user; } }

        public void Connect(string h, int p, string u, string pw)
        {
            Disconnect();
            host = h; port = p; user = u; password = pw;

            askpassPath = Path.Combine(Path.GetTempPath(), "mysftp_ap_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".cmd");
            string escapedPw = pw.Replace("^", "^^").Replace("&", "^&").Replace("|", "^|")
                                 .Replace("<", "^<").Replace(">", "^>").Replace("%", "%%")
                                 .Replace("\"", "^\"");
            File.WriteAllText(askpassPath, "@echo off\r\necho " + escapedPw + "\r\n");

            connected = true;
        }

        public string RunCommand(string command, int timeoutMs = 8000)
        {
            if (!connected || string.IsNullOrEmpty(host)) return "Not connected";

            string apPath = CreateAskPass();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = FindSshExe();
                psi.Arguments = "-o StrictHostKeyChecking=no -o UserKnownHostsFile=NUL -o ConnectTimeout=6 -o BatchMode=no -p " + port + " " + user + "@" + host + " " + command;
                psi.UseShellExecute = false;
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                psi.StandardOutputEncoding = Encoding.UTF8;
                psi.StandardErrorEncoding = Encoding.UTF8;
                psi.EnvironmentVariables["SSH_ASKPASS"] = apPath;
                psi.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
                psi.EnvironmentVariables["DISPLAY"] = ":0";

                Process p = Process.Start(psi);
                try { p.StandardInput.Close(); } catch { }

                StringBuilder outSb = new StringBuilder();
                StringBuilder errSb = new StringBuilder();
                p.OutputDataReceived += (s, ev) => { if (ev.Data != null) outSb.AppendLine(ev.Data); };
                p.ErrorDataReceived += (s, ev) => { if (ev.Data != null) errSb.AppendLine(ev.Data); };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch { }
                    return "Koneksi SSH Timeout (Server tidak merespons dalam " + (timeoutMs / 1000) + " detik)";
                }

                string stdout = outSb.ToString().Trim();
                string stderr = errSb.ToString().Trim();
                return !string.IsNullOrEmpty(stdout) ? stdout : stderr;
            }
            catch (Exception ex)
            {
                return "SSH Error: " + ex.Message;
            }
            finally
            {
                try { File.Delete(apPath); } catch { }
            }
        }

        public void StartStreamingCommand(string command)
        {
            StopStreaming();

            string apPath = CreateAskPass();
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = FindSshExe();
            psi.Arguments = "-o StrictHostKeyChecking=no -o UserKnownHostsFile=NUL -o ConnectTimeout=8 -p " + port + " " + user + "@" + host + " " + command;
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;
            psi.EnvironmentVariables["SSH_ASKPASS"] = apPath;
            psi.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
            psi.EnvironmentVariables["DISPLAY"] = ":0";
            psi.EnvironmentVariables["TERM"] = "xterm-256color";

            lock (streamLock) { streamOutput.Clear(); }

            activeStreamingProcess = Process.Start(psi);

            Thread tOut = new Thread(() => {
                try {
                    char[] buf = new char[2048];
                    int n;
                    while (activeStreamingProcess != null && !activeStreamingProcess.HasExited && (n = activeStreamingProcess.StandardOutput.Read(buf, 0, buf.Length)) > 0) {
                        string s = new string(buf, 0, n);
                        lock (streamLock) { streamOutput.Append(s); }
                    }
                } catch { }
            });
            tOut.IsBackground = true;
            tOut.Start();

            Thread tErr = new Thread(() => {
                try {
                    char[] buf = new char[2048];
                    int n;
                    while (activeStreamingProcess != null && !activeStreamingProcess.HasExited && (n = activeStreamingProcess.StandardError.Read(buf, 0, buf.Length)) > 0) {
                        string s = new string(buf, 0, n);
                        lock (streamLock) { streamOutput.Append(s); }
                    }
                } catch { }
            });
            tErr.IsBackground = true;
            tErr.Start();
        }

        public string GetStreamOutput()
        {
            lock (streamLock)
            {
                string result = streamOutput.ToString();
                streamOutput.Clear();
                return result;
            }
        }

        public void StopStreaming()
        {
            if (activeStreamingProcess != null && !activeStreamingProcess.HasExited)
            {
                try { activeStreamingProcess.Kill(); } catch { }
            }
            activeStreamingProcess = null;
        }

        // ── Persistent interactive shell (real Termius-style session) ──
        private Process interactiveProcess;
        private StreamWriter interactiveStdin;
        private StringBuilder interactiveBuffer = new StringBuilder();
        private object interactiveLock = new object();

        public bool IsInteractiveAlive
        {
            get { return interactiveProcess != null && !interactiveProcess.HasExited; }
        }

        public void StartInteractiveShell()
        {
            StopInteractiveShell();
            if (!connected || string.IsNullOrEmpty(host)) return;

            string apPath = CreateAskPass();
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = FindSshExe();
            psi.Arguments = "-o StrictHostKeyChecking=no -o UserKnownHostsFile=NUL -o ConnectTimeout=10 -o ServerAliveInterval=15 -o ServerAliveCountMax=4 -p " + port + " " + user + "@" + host + " \"/bin/bash -i 2>&1 || sh -i 2>&1\"";
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;
            psi.EnvironmentVariables["SSH_ASKPASS"] = apPath;
            psi.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
            psi.EnvironmentVariables["DISPLAY"] = ":0";

            lock (interactiveLock) { interactiveBuffer.Clear(); }

            interactiveProcess = Process.Start(psi);
            interactiveStdin = interactiveProcess.StandardInput;

            Thread tOut = new Thread(() => {
                try {
                    char[] buf = new char[4096];
                    int n;
                    while (interactiveProcess != null && !interactiveProcess.HasExited && (n = interactiveProcess.StandardOutput.Read(buf, 0, buf.Length)) > 0) {
                        lock (interactiveLock) { interactiveBuffer.Append(buf, 0, n); }
                    }
                } catch { }
                finally { try { File.Delete(apPath); } catch { } }
            });
            tOut.IsBackground = true;
            tOut.Start();

            Thread tErr = new Thread(() => {
                try {
                    char[] buf = new char[4096];
                    int n;
                    while (interactiveProcess != null && !interactiveProcess.HasExited && (n = interactiveProcess.StandardError.Read(buf, 0, buf.Length)) > 0) {
                        lock (interactiveLock) { interactiveBuffer.Append(buf, 0, n); }
                    }
                } catch { }
            });
            tErr.IsBackground = true;
            tErr.Start();
        }

        public void SendInteractiveLine(string text)
        {
            if (!IsInteractiveAlive) return;
            try { interactiveStdin.Write(text + "\n"); interactiveStdin.Flush(); } catch { }
        }

        public void SendInteractiveRaw(char c)
        {
            if (!IsInteractiveAlive) return;
            try { interactiveStdin.Write(c); interactiveStdin.Flush(); } catch { }
        }

        public string PollInteractiveOutput()
        {
            lock (interactiveLock)
            {
                string result = interactiveBuffer.ToString();
                interactiveBuffer.Clear();
                return result;
            }
        }

        public void StopInteractiveShell()
        {
            if (interactiveProcess != null)
            {
                try { if (!interactiveProcess.HasExited) interactiveProcess.Kill(); } catch { }
            }
            interactiveProcess = null;
            interactiveStdin = null;
        }

        public void BreakInteractive()
        {
            try
            {
                if (IsInteractiveAlive)
                {
                    try { interactiveStdin.Write("\x03\n"); interactiveStdin.Flush(); } catch { }
                }
                StopInteractiveShell();
                StartInteractiveShell();
            }
            catch { }
        }

        public byte[] ReadRemoteBytes(string remotePath)
        {
            if (!connected || string.IsNullOrEmpty(host)) return new byte[0];
            string apPath = CreateAskPass();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = FindSshExe();
                psi.Arguments = "-o StrictHostKeyChecking=no -o UserKnownHostsFile=NUL -o ConnectTimeout=10 -p " + port + " " + user + "@" + host + " \"base64 -w 0 " + EscapeShell(remotePath) + "\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                psi.EnvironmentVariables["SSH_ASKPASS"] = apPath;
                psi.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
                psi.EnvironmentVariables["DISPLAY"] = ":0";

                Process p = Process.Start(psi);
                string b64 = p.StandardOutput.ReadToEnd();
                p.WaitForExit(30000);
                if (!p.HasExited) try { p.Kill(); } catch { }

                try
                {
                    return Convert.FromBase64String(b64.Trim());
                }
                catch
                {
                    return Encoding.UTF8.GetBytes(b64);
                }
            }
            catch
            {
                return new byte[0];
            }
            finally
            {
                try { File.Delete(apPath); } catch { }
            }
        }

        public byte[] ArchiveRemoteItems(List<string> paths)
        {
            if (!connected || string.IsNullOrEmpty(host) || paths == null || paths.Count == 0) return new byte[0];
            string apPath = CreateAskPass();
            try
            {
                StringBuilder tarCmd = new StringBuilder("tar -czf -");
                foreach (string itemPath in paths)
                {
                    if (!string.IsNullOrEmpty(itemPath)) tarCmd.Append(" " + EscapeShell(itemPath));
                }
                tarCmd.Append(" 2>/dev/null | base64 -w 0");

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = FindSshExe();
                psi.Arguments = "-o StrictHostKeyChecking=no -o UserKnownHostsFile=NUL -o ConnectTimeout=10 -p " + port + " " + user + "@" + host + " \"" + tarCmd.ToString() + "\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                psi.EnvironmentVariables["SSH_ASKPASS"] = apPath;
                psi.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
                psi.EnvironmentVariables["DISPLAY"] = ":0";

                Process proc = Process.Start(psi);
                string b64 = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(60000);
                if (!proc.HasExited) try { proc.Kill(); } catch { }

                try
                {
                    return Convert.FromBase64String(b64.Trim());
                }
                catch
                {
                    return new byte[0];
                }
            }
            catch
            {
                return new byte[0];
            }
            finally
            {
                try { File.Delete(apPath); } catch { }
            }
        }

        public string WriteRemoteBytes(string remotePath, byte[] data)
        {
            if (!connected || string.IsNullOrEmpty(host)) return "Not connected";

            string b64 = Convert.ToBase64String(data);
            string apPath = CreateAskPass();
            try
            {
                string rPath = remotePath.Replace('\\', '/');
                int lastSlash = rPath.LastIndexOf('/');
                string rDir = lastSlash > 0 ? rPath.Substring(0, lastSlash) : "/";

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = FindSshExe();
                string remoteCmd = "mkdir -p " + EscapeShell(rDir) + " && base64 -d > " + EscapeShell(rPath);
                psi.Arguments = "-o StrictHostKeyChecking=no -o UserKnownHostsFile=NUL -o ConnectTimeout=10 -p " + port + " " + user + "@" + host + " \"" + remoteCmd + "\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                psi.EnvironmentVariables["SSH_ASKPASS"] = apPath;
                psi.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
                psi.EnvironmentVariables["DISPLAY"] = ":0";

                Process p = Process.Start(psi);
                using (StreamWriter sw = new StreamWriter(p.StandardInput.BaseStream, Encoding.ASCII))
                {
                    sw.Write(b64);
                    sw.Flush();
                }
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(15000);
                if (!p.HasExited) try { p.Kill(); } catch { }

                return stderr;
            }
            finally
            {
                try { File.Delete(apPath); } catch { }
            }
        }

        private string CreateAskPass()
        {
            string p = Path.Combine(Path.GetTempPath(), "mysftp_cmd_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".cmd");
            string escapedPw = (password ?? "").Replace("^", "^^").Replace("&", "^&").Replace("|", "^|")
                                              .Replace("<", "^<").Replace(">", "^>").Replace("%", "%%")
                                              .Replace("\"", "^\"");
            File.WriteAllText(p, "@echo off\r\necho " + escapedPw + "\r\n");
            return p;
        }

        public void Disconnect()
        {
            connected = false;
            StopStreaming();
            StopInteractiveShell();
            if (!string.IsNullOrEmpty(askpassPath) && File.Exists(askpassPath))
            {
                try { File.Delete(askpassPath); } catch { }
            }
            askpassPath = null;
        }

        private static string FindSshExe()
        {
            string[] paths = new string[] {
                @"C:\Windows\System32\OpenSSH\ssh.exe",
                @"C:\Program Files\OpenSSH\ssh.exe",
                @"C:\Program Files (x86)\OpenSSH\ssh.exe"
            };
            foreach (string p in paths)
            {
                if (File.Exists(p)) return p;
            }
            return "ssh.exe";
        }

        private static string EscapeShell(string s)
        {
            if (s == null) return "";
            return s.Replace("'", "'\\''");
        }
    }

    class Program
    {
        private static HttpListener listener;
        private static int port;
        private static string dataDir;
        private static string profilesFile;
        private static bool isRunning = true;
        private static Process browserProcess;
        private static SshManager sshManager = new SshManager();
        private static string activeHost;
        private static int activePort;
        private static string activeUser;
        private static string activePassword;
        private static string activeProtocol;
        private static string activeName;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        private const uint MB_ICONERROR = 0x00000010;
        private const uint MB_OK = 0x00000000;

        [STAThread]
        static void Main(string[] args)
        {
            // Wrap absolutely everything: any unhandled exception here (a missing
            // BCL method, a locked port, a permissions error, etc.) used to kill the
            // process silently or dump a raw ".NET error" the user cannot act on.
            // Now the person always gets a real, readable dialog instead of a dead app.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    Exception ex = e.ExceptionObject as Exception;
                    MessageBoxW(IntPtr.Zero,
                        "MYSFTP mengalami kesalahan tak terduga dan harus ditutup.\n\n" +
                        (ex != null ? ex.Message : "Unknown error") +
                        "\n\nSilakan buka kembali MYSFTP. Jika masalah berlanjut, install ulang aplikasi.",
                        "MYSFTP - Kesalahan", MB_ICONERROR | MB_OK);
                }
                catch { }
            };

            try
            {
                RunApp();
            }
            catch (Exception ex)
            {
                MessageBoxW(IntPtr.Zero,
                    "MYSFTP gagal dijalankan.\n\n" + ex.Message +
                    "\n\nCoba tutup aplikasi ini lalu buka lagi. Jika tetap gagal, install ulang MYSFTP.",
                    "MYSFTP - Gagal Memulai", MB_ICONERROR | MB_OK);
            }
        }

        private static void RunApp()
        {
            try
            {
                SshManager.SetCurrentProcessExplicitAppUserModelID("ZellRayy.MYSFTP.Desktop.v200");
            }
            catch { }

            dataDir = AppDomain.CurrentDomain.BaseDirectory;

            // ── Fix: "gagal menyimpan profil VPS" ──
            // connections.json used to be written next to the exe, which on a
            // default install lives under C:\Program Files\MYSFTP — a folder
            // normal (non-admin) users cannot write to. Saving a profile would
            // silently fail there. User data now always lives in the per-user,
            // always-writable LocalAppData folder instead, regardless of where
            // the app itself is installed.
            string userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MYSFTP");
            try { Directory.CreateDirectory(userDataDir); } catch { }
            profilesFile = Path.Combine(userDataDir, "connections.json");

            // One-time migration: if an older install already saved profiles
            // next to the exe (from before this fix) and the new location is
            // still empty, bring the old data over so nobody loses their saved
            // servers just because of this change.
            try
            {
                string legacyProfilesFile = Path.Combine(dataDir, "connections.json");
                if (!File.Exists(profilesFile) && File.Exists(legacyProfilesFile))
                {
                    File.Copy(legacyProfilesFile, profilesFile, false);
                }
            }
            catch { }

            // Find a free TCP port. Retry a few times: on a freshly-installed
            // machine antivirus/EDR sometimes holds a just-picked ephemeral port
            // for a split second, which used to make HttpListener.Start() below
            // throw on the very first run.
            int freePort = 0;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    TcpListener tcp = new TcpListener(IPAddress.Loopback, 0);
                    tcp.Start();
                    freePort = ((IPEndPoint)tcp.LocalEndpoint).Port;
                    tcp.Stop();

                    listener = new HttpListener();
                    listener.Prefixes.Add("http://127.0.0.1:" + freePort + "/");
                    listener.Start();
                    port = freePort;
                    break;
                }
                catch
                {
                    listener = null;
                    Thread.Sleep(300);
                }
            }

            if (listener == null)
            {
                MessageBoxW(IntPtr.Zero,
                    "MYSFTP tidak bisa membuka server lokal (port terpakai atau diblokir).\n\n" +
                    "Coba tutup aplikasi lain yang mungkin bentrok, matikan sementara antivirus, lalu buka MYSFTP lagi.",
                    "MYSFTP - Gagal Memulai Server", MB_ICONERROR | MB_OK);
                return;
            }

            Thread serverThread = new Thread(StartServer);
            serverThread.IsBackground = true;
            serverThread.Start();

            // ── Fix for "harus tutup lalu buka lagi baru bisa connect" ──
            // We used to launch the browser straight at http://127.0.0.1:PORT/ and
            // only *hope* the local HttpListener was already answering. On a fresh
            // install, Windows Defender/SmartScreen scanning the new, unsigned exe
            // (or a cold disk cache) can delay the first real response by several
            // seconds — longer than the old wait — so the browser showed
            // ERR_CONNECTION_REFUSED and the user had to close and reopen manually.
            //
            // Instead, we now launch the browser at a tiny local HTML file that has
            // NOTHING to do with our HttpListener (so it always opens instantly),
            // and that page polls the real server itself with retries until it
            // answers, then redirects. The user only ever sees a friendly "Menyiapkan
            // aplikasi..." loading screen — never a browser error page — and it
            // will keep retrying for up to a minute instead of failing once.
            string loadingUrl = WriteLoadingBootstrap(port);

            LaunchNativeAppWindow(loadingUrl);

            if (browserProcess != null)
                browserProcess.WaitForExit();
            else
                while (isRunning) Thread.Sleep(1000);

            sshManager.Disconnect();
            try { listener.Stop(); } catch { }
        }

        private static string WriteLoadingBootstrap(int listenPort)
        {
            string html = @"<!DOCTYPE html>
<html lang=""id""><head><meta charset=""UTF-8"">
<title>MYSFTP</title>
<link rel=""icon"" href=""data:,"">
<style>
  html,body{height:100%;margin:0;background:#060709;color:#eae6db;font-family:Segoe UI,Inter,system-ui,sans-serif;}
  body{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:18px;}
  .logo{width:52px;height:52px;border-radius:14px;background:linear-gradient(135deg,#cdbd94,#96865c);color:#111;display:flex;align-items:center;justify-content:center;font-weight:800;font-size:24px;box-shadow:0 6px 20px rgba(205,189,148,.3);}
  .title{font-weight:800;font-size:17px;letter-spacing:.5px;color:#f0e6cf;}
  .spin{width:30px;height:30px;border-radius:50%;border:3px solid rgba(205,189,148,.2);border-top-color:#cdbd94;animation:sp .8s linear infinite;}
  @keyframes sp{to{transform:rotate(360deg)}}
  .msg{font-size:12.5px;color:#8a877c;}
  .err{color:#e06c75;text-align:center;max-width:340px;line-height:1.6;font-size:12.5px;}
  .retrybtn{margin-top:6px;background:#cdbd94;color:#111;border:none;border-radius:8px;padding:8px 18px;font-weight:700;font-size:12.5px;cursor:pointer;display:none;}
</style></head>
<body>
  <div class=""logo"">M</div>
  <div class=""title"">MYSFTP</div>
  <div class=""spin"" id=""sp""></div>
  <div class=""msg"" id=""m"">Menyiapkan aplikasi...</div>
  <div class=""err"" id=""e"" style=""display:none""></div>
  <button class=""retrybtn"" id=""rb"" onclick=""tries=0;document.getElementById('rb').style.display='none';document.getElementById('sp').style.display='block';document.getElementById('e').style.display='none';check();"">Coba Lagi</button>
<script>
  var target = 'http://127.0.0.1:__PORT__/';
  var tries = 0;
  var maxTries = 300; // ~60s of retrying, covers slow first-run AV scans
  function check(){
    tries++;
    fetch(target, {cache:'no-store', mode:'cors'}).then(function(r){
      if (r.ok) { location.replace(target); }
      else { scheduleRetry(); }
    }).catch(function(){ scheduleRetry(); });
  }
  function scheduleRetry(){
    if (tries >= maxTries) {
      document.getElementById('sp').style.display = 'none';
      document.getElementById('m').style.display = 'none';
      document.getElementById('e').style.display = 'block';
      document.getElementById('e').textContent = 'Server lokal MYSFTP belum merespons. Ini biasa terjadi sekali saja di percobaan pertama (Windows sedang memeriksa aplikasi baru). Klik Coba Lagi, atau tutup dan buka ulang MYSFTP.';
      document.getElementById('rb').style.display = 'inline-block';
      return;
    }
    document.getElementById('m').textContent = 'Menyiapkan aplikasi...';
    setTimeout(check, 200);
  }
  check();
</script>
</body></html>";

            html = html.Replace("__PORT__", listenPort.ToString());

            string bootstrapPath = Path.Combine(dataDir, "loading.html");
            try
            {
                File.WriteAllText(bootstrapPath, html, Encoding.UTF8);
                return new Uri(bootstrapPath).AbsoluteUri;
            }
            catch
            {
                // If we can't write next to the exe (locked-down folder), fall back
                // to the OS temp directory, which is always writable.
                bootstrapPath = Path.Combine(Path.GetTempPath(), "mysftp_loading.html");
                File.WriteAllText(bootstrapPath, html, Encoding.UTF8);
                return new Uri(bootstrapPath).AbsoluteUri;
            }
        }

        private static string FindAppBrowser(out bool isEdge)
        {
            string edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
            if (!File.Exists(edgePath)) edgePath = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
            string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (!File.Exists(chromePath)) chromePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";

            if (File.Exists(chromePath)) { isEdge = false; return chromePath; }
            if (File.Exists(edgePath)) { isEdge = true; return edgePath; }
            isEdge = false;
            return null;
        }

        private static void CleanStaleProfileLocks(string userProfile)
        {
            // A profile dir left behind by a crashed/killed previous run can hold a
            // "SingletonLock" that makes the NEXT launch silently fail to open a
            // window on the first click, forcing the user to click twice.
            try
            {
                if (!Directory.Exists(userProfile)) return;
                string[] lockNames = { "SingletonLock", "SingletonCookie", "SingletonSocket", "lockfile" };
                foreach (string ln in lockNames)
                {
                    string lp = Path.Combine(userProfile, ln);
                    if (File.Exists(lp)) { try { File.Delete(lp); } catch { } }
                }
            }
            catch { }
        }

        private static void LaunchNativeAppWindow(string url)
        {
            bool isEdge;
            string browser = FindAppBrowser(out isEdge);
            string userProfile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MYSFTP_Desktop_Profile");

            if (browser == null)
            {
                // No Chromium-based browser found at all — this is the only case where
                // we truly cannot open an isolated app window, so fall back to the
                // system default browser rather than failing silently.
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return;
            }

            for (int attempt = 0; attempt < 2; attempt++)
            {
                CleanStaleProfileLocks(userProfile);

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = browser;
                psi.Arguments = "--app=\"" + url + "\" --app-id=\"MYSFTP_Client_v200\" --window-size=1340,880 --window-name=\"MYSFTP\" --user-data-dir=\"" + userProfile + "\" --disable-extensions --disable-component-extensions-with-background-pages --disable-background-networking --no-default-browser-check --no-first-run --disable-session-crashed-bubble --no-crash-upload";
                psi.UseShellExecute = false;

                try
                {
                    browserProcess = Process.Start(psi);
                }
                catch
                {
                    browserProcess = null;
                }

                if (browserProcess == null)
                {
                    continue; // retry once with a cleaned profile
                }

                // Detect an immediate crash (e.g. locked profile) and retry once
                // automatically instead of leaving the user with nothing on screen.
                Thread.Sleep(700);
                if (!browserProcess.HasExited) return; // window is up, we're done

                // process died almost instantly — clean up and try again with a
                // fresh, timestamped profile directory so a corrupted profile can't
                // repeat the failure.
                userProfile = userProfile + "_" + DateTime.Now.Ticks;
            }
        }

        private static void StartServer()
        {
            while (isRunning && listener.IsListening)
            {
                try
                {
                    HttpListenerContext ctx = listener.GetContext();
                    ThreadPool.QueueUserWorkItem((o) => HandleRequest(ctx));
                }
                catch
                {
                    // ── Fix: "Failed to fetch" on every request after the first hiccup ──
                    // GetContext() can throw for all sorts of harmless, transient
                    // reasons — a client aborting mid-request, a malformed request
                    // line, momentary AV interference, etc. The old code treated
                    // ANY such exception as fatal and permanently stopped accepting
                    // new connections, which silently killed the whole local server
                    // for the rest of the app's life (every fetch() in the UI then
                    // failed with "Failed to fetch" until the app was restarted).
                    // Now we only stop the loop if the listener was actually shut
                    // down (app closing); anything else is just skipped and the
                    // server keeps serving.
                    if (!isRunning || !listener.IsListening) break;
                    Thread.Sleep(50);
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest req = context.Request;
            HttpListenerResponse res = context.Response;

            try
            {
                res.AddHeader("Access-Control-Allow-Origin", "*");
                res.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                res.AddHeader("Access-Control-Allow-Headers", "*");
                res.AddHeader("Access-Control-Max-Age", "86400");

                if (req.HttpMethod == "OPTIONS")
                {
                    res.StatusCode = 200;
                    res.ContentLength64 = 0;
                    return;
                }

                string path = req.Url.AbsolutePath;
                if (path == "/" || path == "/index.html")
                {
                    byte[] buf = Encoding.UTF8.GetBytes(HtmlUi);
                    res.ContentType = "text/html; charset=utf-8";
                    res.ContentLength64 = buf.Length;
                    res.OutputStream.Write(buf, 0, buf.Length);
                }
                else if (path == "/icon.jpg")
                {
                    string iconFile = Path.Combine(dataDir, "Icon.jpg");
                    if (File.Exists(iconFile))
                    {
                        byte[] iconBuf = File.ReadAllBytes(iconFile);
                        res.ContentType = "image/jpeg";
                        res.ContentLength64 = iconBuf.Length;
                        res.OutputStream.Write(iconBuf, 0, iconBuf.Length);
                    }
                    else
                    {
                        res.StatusCode = 404;
                    }
                }
                else if (path == "/favicon.ico")
                {
                    // Serve a real .ico so the app-mode window picks it up as its OWN
                    // taskbar/title-bar icon instead of falling back to the browser's
                    // generic icon (which is what made it look like "Edge opened").
                    string icoFile = Path.Combine(dataDir, "app.ico");
                    if (File.Exists(icoFile))
                    {
                        byte[] icoBuf = File.ReadAllBytes(icoFile);
                        res.ContentType = "image/x-icon";
                        res.ContentLength64 = icoBuf.Length;
                        res.OutputStream.Write(icoBuf, 0, icoBuf.Length);
                    }
                    else
                    {
                        res.StatusCode = 404;
                    }
                }
                else if (path == "/api/profiles" && req.HttpMethod == "GET")
                {
                    string json = "[]";
                    if (File.Exists(profilesFile))
                        json = File.ReadAllText(profilesFile, Encoding.UTF8);
                    SendJson(res, json);
                }
                else if (path == "/api/profiles" && req.HttpMethod == "POST")
                {
                    string body = ReadBody(req);
                    try
                    {
                        File.WriteAllText(profilesFile, body, Encoding.UTF8);
                        SendJson(res, "{\"success\":true}");
                    }
                    catch (Exception exWrite)
                    {
                        // Surface a real, readable reason (permission denied, disk
                        // full, path missing, etc.) instead of a generic 500 the
                        // client used to ignore anyway.
                        SendJson(res, "{\"success\":false,\"error\":\"" + EscapeJson(exWrite.Message) + "\"}");
                    }
                }
                else if (path == "/api/connect" && req.HttpMethod == "POST")
                {
                    string body = ReadBody(req);
                    activeHost = ExtractVal(body, "host");
                    int.TryParse(ExtractVal(body, "port"), out activePort);
                    if (activePort <= 0) activePort = 22;
                    activeUser = ExtractVal(body, "username");
                    activePassword = ExtractVal(body, "password");
                    activeProtocol = ExtractVal(body, "protocol");
                    activeName = ExtractVal(body, "name");

                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost) && !string.IsNullOrEmpty(activePassword))
                    {
                        sshManager.Connect(activeHost, activePort, activeUser, activePassword);
                        string test = sshManager.RunCommand("echo MYSFTP_OK", 6000);

                        if (test.Contains("MYSFTP_OK"))
                        {
                            sshManager.StartInteractiveShell();
                            SendJson(res, "{\"success\":true,\"protocol\":\"SFTP\",\"name\":\"" + EscapeJson(activeName) + "\"}");
                        }
                        else
                        {
                            sshManager.Disconnect();
                            SendJson(res, "{\"success\":false,\"error\":\"" + EscapeJson(test.Trim()) + "\"}");
                        }
                    }
                    else if (activeProtocol == "LOCAL")
                    {
                        SendJson(res, "{\"success\":true,\"protocol\":\"LOCAL\",\"name\":\"" + EscapeJson(activeName) + "\"}");
                    }
                    else
                    {
                        SendJson(res, "{\"success\":false,\"error\":\"Host, Username, dan Password wajib diisi\"}");
                    }
                }
                else if (path == "/api/disconnect" && req.HttpMethod == "POST")
                {
                    sshManager.Disconnect();
                    activeProtocol = null;
                    SendJson(res, "{\"success\":true}");
                }
                else if (path == "/api/status")
                {
                    bool conn = sshManager.IsConnected;
                    SendJson(res, "{\"connected\":" + (conn ? "true" : "false") + ",\"protocol\":\"" + EscapeJson(activeProtocol ?? "") + "\",\"name\":\"" + EscapeJson(activeName ?? "") + "\",\"host\":\"" + EscapeJson(activeHost ?? "") + "\"}");
                }
                else if (path == "/api/terminal/exec" && req.HttpMethod == "POST")
                {
                    string body = ReadBody(req);
                    string cmd = ExtractVal(body, "command");

                    if (activeProtocol == "SFTP" && sshManager.IsConnected)
                    {
                        if (sshManager.IsInteractiveAlive)
                        {
                            sshManager.SendInteractiveLine(cmd);
                            SendJson(res, "{\"success\":true}");
                        }
                        else
                        {
                            string output = sshManager.RunCommand(cmd, 15000);
                            SendJson(res, "{\"success\":true,\"output\":\"" + EscapeJson(output) + "\"}");
                        }
                    }
                    else
                    {
                        ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + cmd)
                        {
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8,
                            StandardErrorEncoding = Encoding.UTF8
                        };
                        Process p = Process.Start(psi);
                        string outStr = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                        p.WaitForExit(5000);
                        SendJson(res, "{\"success\":true,\"output\":\"" + EscapeJson(outStr) + "\"}");
                    }
                }
                else if (path == "/api/terminal/poll")
                {
                    string stream = sshManager.PollInteractiveOutput();
                    SendJson(res, "{\"output\":\"" + EscapeJson(stream) + "\",\"alive\":" + (sshManager.IsInteractiveAlive ? "true" : "false") + "}");
                }
                else if (path == "/api/terminal/break" && req.HttpMethod == "POST")
                {
                    sshManager.BreakInteractive();
                    SendJson(res, "{\"success\":true}");
                }
                else if (path == "/api/fs/list")
                {
                    string dir = req.QueryString["path"] ?? "/root";
                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost))
                    {
                        SendJson(res, ListRemoteDirFast(dir));
                    }
                    else
                    {
                        SendJson(res, ListLocalDir(dir));
                    }
                }
                else if (path == "/api/fs/read")
                {
                    string fPath = req.QueryString["path"] ?? "";
                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost))
                    {
                        SendJson(res, ReadRemoteFile(fPath));
                    }
                    else
                    {
                        SendJson(res, ReadLocalFile(fPath));
                    }
                }
                else if (path == "/api/fs/write" && req.HttpMethod == "POST")
                {
                    string body = ReadBody(req);
                    string fp = ExtractVal(body, "path");
                    string content = ExtractVal(body, "content");
                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost))
                    {
                        SendJson(res, WriteRemoteFile(fp, content));
                    }
                    else
                    {
                        SendJson(res, WriteLocalFile(fp, content));
                    }
                }
                else if (path == "/api/fs/upload" && req.HttpMethod == "POST")
                {
                    string body = ReadBody(req);
                    string fp = ExtractVal(body, "path");
                    string b64 = ExtractVal(body, "data");
                    byte[] bytes = Convert.FromBase64String(b64);

                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost))
                    {
                        string err = sshManager.WriteRemoteBytes(fp, bytes);
                        if (string.IsNullOrEmpty(err) || !err.Contains("Permission denied"))
                            SendJson(res, "{\"success\":true,\"message\":\"Berkas berhasil diunggah!\"}");
                        else
                            SendJson(res, "{\"success\":false,\"error\":\"" + EscapeJson(err) + "\"}");
                    }
                    else
                    {
                        string localPath = fp.Replace('/', '\\');
                        string dir = Path.GetDirectoryName(localPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        File.WriteAllBytes(localPath, bytes);
                        SendJson(res, "{\"success\":true,\"message\":\"Berkas berhasil disimpan!\"}");
                    }
                }
                else if (path == "/api/fs/delete" && req.HttpMethod == "POST")
                {
                    string body = ReadBody(req);
                    string fp = ExtractVal(body, "path");
                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost))
                    {
                        SendJson(res, DeleteRemoteItem(fp));
                    }
                    else
                    {
                        SendJson(res, DeleteLocalItem(fp));
                    }
                }
                else if (path == "/api/fs/batch-delete" && req.HttpMethod == "POST")
                {
                    string body = ReadBody(req);
                    List<string> paths = ParseJsonStringArray(body);
                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost))
                    {
                        if (paths.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder("rm -rf");
                            foreach (string p in paths)
                            {
                                if (!string.IsNullOrEmpty(p)) sb.Append(" " + EscapeShell(p));
                            }
                            sshManager.RunCommand(sb.ToString(), 15000);
                        }
                        SendJson(res, "{\"success\":true}");
                    }
                    else
                    {
                        foreach (string p in paths)
                        {
                            if (!string.IsNullOrEmpty(p)) DeleteLocalItem(p);
                        }
                        SendJson(res, "{\"success\":true}");
                    }
                }
                else if (path == "/api/fs/download")
                {
                    string fPath = req.QueryString["path"] ?? "";
                    string isDirStr = req.QueryString["isDir"] ?? "false";
                    bool isDir = isDirStr.ToLower() == "true";

                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost))
                    {
                        if (isDir)
                        {
                            List<string> pList = new List<string>();
                            pList.Add(fPath);
                            byte[] archive = sshManager.ArchiveRemoteItems(pList);
                            string dirName = Path.GetFileName(fPath.TrimEnd('/'));
                            if (string.IsNullOrEmpty(dirName)) dirName = "folder";
                            res.ContentType = "application/gzip";
                            res.AddHeader("Content-Disposition", "attachment; filename=\"" + dirName + ".tar.gz\"");
                            res.ContentLength64 = archive.Length;
                            res.OutputStream.Write(archive, 0, archive.Length);
                            try { res.OutputStream.Flush(); } catch { }
                        }
                        else
                        {
                            byte[] fileBytes = sshManager.ReadRemoteBytes(fPath);
                            string fileName = Path.GetFileName(fPath);
                            res.ContentType = "application/octet-stream";
                            res.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
                            res.ContentLength64 = fileBytes.Length;
                            res.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                            try { res.OutputStream.Flush(); } catch { }
                        }
                    }
                    else
                    {
                        string localPath = fPath.Replace('/', '\\');
                        if (File.Exists(localPath))
                        {
                            byte[] fileBytes = File.ReadAllBytes(localPath);
                            string fileName = Path.GetFileName(localPath);
                            res.ContentType = "application/octet-stream";
                            res.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
                            res.ContentLength64 = fileBytes.Length;
                            res.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                            try { res.OutputStream.Flush(); } catch { }
                        }
                    }
                }
                else if (path == "/api/fs/batch-download" && req.HttpMethod == "POST")
                {
                    string body = ReadBody(req);
                    List<string> paths = ParseJsonStringArray(body);

                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost))
                    {
                        byte[] archive = sshManager.ArchiveRemoteItems(paths);
                        res.ContentType = "application/gzip";
                        res.AddHeader("Content-Disposition", "attachment; filename=\"MYSFTP_Archive_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".tar.gz\"");
                        res.ContentLength64 = archive.Length;
                        res.OutputStream.Write(archive, 0, archive.Length);
                        try { res.OutputStream.Flush(); } catch { }
                    }
                }
                else if (path == "/api/fs/create" && req.HttpMethod == "POST")
                {
                    string body = ReadBody(req);
                    string fp = ExtractVal(body, "path");
                    string type = ExtractVal(body, "type");
                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost))
                    {
                        SendJson(res, CreateRemoteItem(fp, type));
                    }
                    else
                    {
                        SendJson(res, CreateLocalItem(fp, type));
                    }
                }
                else if (path == "/api/ping")
                {
                    string h = req.QueryString["host"] ?? "127.0.0.1";
                    int p = 22;
                    int.TryParse(req.QueryString["port"], out p);
                    SendJson(res, PingHost(h, p > 0 ? p : 22));
                }
                else if (path == "/api/exit")
                {
                    sshManager.Disconnect();
                    SendJson(res, "{\"success\":true}");
                    isRunning = false;
                    new Thread(() => { Thread.Sleep(300); Environment.Exit(0); }).Start();
                }
                else
                {
                    res.StatusCode = 404;
                }
            }
            catch (Exception ex)
            {
                res.StatusCode = 500;
                byte[] err = Encoding.UTF8.GetBytes("{\"error\":\"" + EscapeJson(ex.Message) + "\"}");
                try { res.OutputStream.Write(err, 0, err.Length); } catch { }
            }
            finally
            {
                try { res.Close(); } catch { }
            }
        }

        private static string ListRemoteDirFast(string dir)
        {
            try
            {
                if (string.IsNullOrEmpty(dir) || dir == ".") dir = "/root";
                // NOTE: TrimEnd(char) — the single-char overload — only exists on
                // newer .NET runtimes. Using the char[] overload explicitly (which
                // has existed since .NET 1.1) avoids a "Method not found:
                // System.String.TrimEnd(Char)" crash on machines/CLRs that only
                // have the classic TrimEnd(params char[]) overload.
                dir = dir.TrimEnd(new char[] { '/' });
                if (string.IsNullOrEmpty(dir)) dir = "/";

                string raw = sshManager.RunCommand("\"ls -la --time-style=long-iso " + EscapeShell(dir) + " 2>&1\"", 7000);
                if (raw.Contains("No such file") || raw.Contains("cannot access") || raw.Contains("Permission denied"))
                {
                    return "{\"success\":false,\"error\":\"" + EscapeJson(raw.Trim()) + "\"}";
                }

                List<string> items = new List<string>();
                string[] lines = raw.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (line.StartsWith("total ") || line.Length < 10) continue;

                    char firstChar = line[0];
                    bool isDir = firstChar == 'd' || firstChar == 'l';

                    string[] parts = System.Text.RegularExpressions.Regex.Split(line, @"\s+");
                    if (parts.Length < 8) continue;

                    string name = "";
                    for (int i = 7; i < parts.Length; i++)
                    {
                        if (i > 7) name += " ";
                        name += parts[i];
                    }
                    int arrowIdx = name.IndexOf(" -> ");
                    if (arrowIdx >= 0) name = name.Substring(0, arrowIdx);

                    if (name == "." || name == "..") continue;

                    string size = parts[4];
                    string modified = parts[5] + " " + parts[6];
                    string fullPath = (dir == "/" ? "" : dir) + "/" + name;

                    items.Add("{\"name\":\"" + EscapeJson(name) + "\",\"path\":\"" + EscapeJson(fullPath) + "\",\"isDirectory\":" + (isDir ? "true" : "false") + ",\"size\":" + size + ",\"modified\":\"" + EscapeJson(modified) + "\"}");
                }

                string parent = dir == "/" ? "/" : dir.Substring(0, dir.LastIndexOf('/'));
                if (string.IsNullOrEmpty(parent)) parent = "/";

                return "{\"success\":true,\"currentPath\":\"" + EscapeJson(dir) + "\",\"parentPath\":\"" + EscapeJson(parent) + "\",\"items\":[" + string.Join(",", items.ToArray()) + "]}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string ReadRemoteFile(string path)
        {
            try
            {
                string content = sshManager.RunCommand("\"cat " + EscapeShell(path) + " 2>&1\"", 8000);
                return "{\"success\":true,\"path\":\"" + EscapeJson(path) + "\",\"content\":\"" + EscapeJson(content) + "\"}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string WriteRemoteFile(string path, string content)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                sshManager.WriteRemoteBytes(path, bytes);
                return "{\"success\":true,\"message\":\"Berkas berhasil disimpan ke server!\"}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string DeleteRemoteItem(string path)
        {
            try
            {
                sshManager.RunCommand("\"rm -rf " + EscapeShell(path) + " 2>&1\"", 6000);
                return "{\"success\":true}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string CreateRemoteItem(string path, string type)
        {
            try
            {
                if (type == "folder")
                    sshManager.RunCommand("\"mkdir -p " + EscapeShell(path) + " 2>&1\"", 5000);
                else
                    sshManager.RunCommand("\"touch " + EscapeShell(path) + " 2>&1\"", 5000);
                return "{\"success\":true}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string ListLocalDir(string p)
        {
            try
            {
                if (string.IsNullOrEmpty(p) || p == "." || p == "/") p = dataDir;
                p = p.Replace('/', '\\');
                if (!Directory.Exists(p)) p = dataDir;

                List<string> items = new List<string>();
                foreach (string d in Directory.GetDirectories(p))
                {
                    string name = Path.GetFileName(d);
                    string full = d.Replace('\\', '/');
                    DateTime dt = Directory.GetLastWriteTime(d);
                    items.Add("{\"name\":\"" + EscapeJson(name) + "\",\"path\":\"" + EscapeJson(full) + "\",\"isDirectory\":true,\"size\":0,\"modified\":\"" + dt.ToString("yyyy-MM-dd HH:mm") + "\"}");
                }
                foreach (string f in Directory.GetFiles(p))
                {
                    string name = Path.GetFileName(f);
                    string full = f.Replace('\\', '/');
                    long sz = 0;
                    try { sz = new FileInfo(f).Length; } catch { }
                    DateTime dt = File.GetLastWriteTime(f);
                    items.Add("{\"name\":\"" + EscapeJson(name) + "\",\"path\":\"" + EscapeJson(full) + "\",\"isDirectory\":false,\"size\":" + sz + ",\"modified\":\"" + dt.ToString("yyyy-MM-dd HH:mm") + "\"}");
                }

                string parent = "";
                DirectoryInfo pInfo = Directory.GetParent(p);
                if (pInfo != null) parent = pInfo.FullName.Replace('\\', '/');

                return "{\"success\":true,\"currentPath\":\"" + EscapeJson(p.Replace('\\', '/')) + "\",\"parentPath\":\"" + EscapeJson(parent) + "\",\"items\":[" + string.Join(",", items.ToArray()) + "]}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string ReadLocalFile(string p)
        {
            try
            {
                p = (p ?? "").Replace('/', '\\');
                if (!File.Exists(p)) return "{\"success\":false,\"error\":\"File tidak ditemukan\"}";
                string content = File.ReadAllText(p, Encoding.UTF8);
                return "{\"success\":true,\"path\":\"" + EscapeJson(p.Replace('\\', '/')) + "\",\"content\":\"" + EscapeJson(content) + "\"}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string WriteLocalFile(string fp, string content)
        {
            try
            {
                string p = fp.Replace('/', '\\');
                string dir = Path.GetDirectoryName(p);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(p, content, Encoding.UTF8);
                return "{\"success\":true,\"message\":\"Berkas berhasil disimpan!\"}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string DeleteLocalItem(string fp)
        {
            try
            {
                string p = fp.Replace('/', '\\');
                if (Directory.Exists(p)) Directory.Delete(p, true);
                else if (File.Exists(p)) File.Delete(p);
                return "{\"success\":true}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string CreateLocalItem(string fp, string type)
        {
            try
            {
                string p = fp.Replace('/', '\\');
                if (type == "folder") Directory.CreateDirectory(p);
                else
                {
                    string dir = Path.GetDirectoryName(p);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(p, "", Encoding.UTF8);
                }
                return "{\"success\":true}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string PingHost(string host, int p)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var result = client.BeginConnect(host, p, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                    sw.Stop();
                    if (success) return "{\"online\":true,\"latency\":" + sw.ElapsedMilliseconds + "}";
                }
            }
            catch { }
            return "{\"online\":false,\"latency\":0}";
        }

        private static string ReadBody(HttpListenerRequest req)
        {
            Encoding enc = req.ContentEncoding ?? Encoding.UTF8;
            using (var reader = new StreamReader(req.InputStream, enc))
            {
                return reader.ReadToEnd();
            }
        }

        private static void SendJson(HttpListenerResponse res, string json)
        {
            byte[] buf = Encoding.UTF8.GetBytes(json);
            res.ContentType = "application/json; charset=utf-8";
            res.ContentLength64 = buf.Length;
            res.OutputStream.Write(buf, 0, buf.Length);
            try { res.OutputStream.Flush(); } catch { }
        }

        private static string ExtractVal(string json, string key)
        {
            string search = "\"" + key + "\":\"";
            int idx = json.IndexOf(search);
            if (idx >= 0)
            {
                int start = idx + search.Length;
                StringBuilder sb = new StringBuilder();
                for (int i = start; i < json.Length; i++)
                {
                    if (json[i] == '\\' && i + 1 < json.Length)
                    {
                        char next = json[i + 1];
                        if (next == '"') { sb.Append('"'); i++; }
                        else if (next == 'n') { sb.Append('\n'); i++; }
                        else if (next == 'r') { sb.Append('\r'); i++; }
                        else if (next == 't') { sb.Append('\t'); i++; }
                        else if (next == '\\') { sb.Append('\\'); i++; }
                        else { sb.Append(json[i]); }
                    }
                    else if (json[i] == '"')
                    {
                        break;
                    }
                    else
                    {
                        sb.Append(json[i]);
                    }
                }
                return sb.ToString();
            }
            search = "\"" + key + "\":";
            idx = json.IndexOf(search);
            if (idx >= 0)
            {
                int start = idx + search.Length;
                int end = json.IndexOfAny(new char[] { ',', '}', ' ' }, start);
                if (end < 0) end = json.Length;
                return json.Substring(start, end - start).Trim();
            }
            return "";
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private static string EscapeShell(string s)
        {
            if (s == null) return "";
            return s.Replace("'", "'\\''");
        }

        private static List<string> ParseJsonStringArray(string json)
        {
            List<string> list = new List<string>();
            if (string.IsNullOrEmpty(json)) return list;
            int idx = 0;
            while (true)
            {
                int quoteStart = json.IndexOf('"', idx);
                if (quoteStart < 0) break;
                StringBuilder sb = new StringBuilder();
                int i = quoteStart + 1;
                for (; i < json.Length; i++)
                {
                    if (json[i] == '\\' && i + 1 < json.Length)
                    {
                        char next = json[i + 1];
                        if (next == '"') { sb.Append('"'); i++; }
                        else if (next == '\\') { sb.Append('\\'); i++; }
                        else if (next == 'n') { sb.Append('\n'); i++; }
                        else if (next == 'r') { sb.Append('\r'); i++; }
                        else if (next == 't') { sb.Append('\t'); i++; }
                        else { sb.Append(json[i]); }
                    }
                    else if (json[i] == '"')
                    {
                        break;
                    }
                    else
                    {
                        sb.Append(json[i]);
                    }
                }
                list.Add(sb.ToString());
                idx = i + 1;
            }
            return list;
        }

        #region Embedded Luxury HTML UI
        private const string HtmlUi = @"<!DOCTYPE html>
<html lang=""id"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>MYSFTP</title>
  <link rel=""icon"" type=""image/x-icon"" href=""/favicon.ico"">
  <link rel=""shortcut icon"" type=""image/x-icon"" href=""/favicon.ico"">
  <meta name=""theme-color"" content=""#060709"">
  <meta name=""application-name"" content=""MYSFTP"">
  <meta name=""msapplication-TileColor"" content=""#060709"">
  <meta name=""msapplication-TileImage"" content=""/favicon.ico"">
  <meta name=""apple-mobile-web-app-title"" content=""MYSFTP"">
  <link rel=""apple-touch-icon"" href=""/icon.jpg"">
  <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
  <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
  <link href=""https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=JetBrains+Mono:ital,wght@0,400;0,500;0,600;0,700;1,400&family=Outfit:wght@500;600;700;800;900&display=swap"" rel=""stylesheet"">
  <style>
    :root {
      --bg-base: #060709;
      --bg-surface: #0c0d12;
      --bg-card: #12141a;
      --bg-card-hover: #181b22;
      --bg-input: #08090c;
      --border: rgba(255,255,255,0.07);
      --border-gold: rgba(205,189,148,0.35);
      --gold: #cdbd94;
      --gold-light: #f0e6cf;
      --gold-glow: rgba(205,189,148,0.22);
      --text: #eae6db;
      --text-dim: #7f7c72;
      --green: #73d285;
      --red: #e06c75;
      --blue: #6ab0f3;
      --yellow: #e5c07b;
      --cyan: #56c8d8;
      --purple: #c678dd;
      --r-sm: 8px; --r-md: 14px; --r-lg: 20px;
    }
    * { margin:0; padding:0; box-sizing:border-box; -webkit-font-smoothing:antialiased; }
    body, html { background:var(--bg-base); color:var(--text); font-family:'Inter',system-ui,sans-serif; font-size:13.5px; height:100%; overflow:hidden; }
    ::selection { background:var(--gold); color:#111; }
    ::-webkit-scrollbar { width:6px; height:6px; }
    ::-webkit-scrollbar-track { background:transparent; }
    ::-webkit-scrollbar-thumb { background:rgba(255,255,255,0.12); border-radius:3px; }
    ::-webkit-scrollbar-thumb:hover { background:rgba(255,255,255,0.25); }

    #root { display:flex; height:100vh; width:100vw; background:radial-gradient(circle at 85% 15%, rgba(205,189,148,0.04), transparent 50%), var(--bg-base); position:relative; }

    /* ── Progress Bar ── */
    #top-loader { position:absolute; top:0; left:0; width:100%; height:3px; background:linear-gradient(90deg, transparent, var(--gold), var(--gold-light), transparent); background-size:200% 100%; z-index:99999; display:none; animation:loadMove 1.2s infinite linear; }
    @keyframes loadMove { 0%{background-position:200% 0;} 100%{background-position:-200% 0;} }
    #load-badge { display:none; align-items:center; gap:7px; font-size:11.5px; font-weight:700; color:var(--gold-light); background:rgba(205,189,148,0.1); border:1px solid var(--border-gold); border-radius:20px; padding:4px 12px; }
    #load-badge.on { display:inline-flex; }
    #p-files.drag-on { outline:2px dashed var(--gold); outline-offset:-6px; background:rgba(205,189,148,0.05); border-radius:var(--r-md); }
    .spin-dot { width:11px; height:11px; border-radius:50%; border:2px solid rgba(205,189,148,0.25); border-top-color:var(--gold); animation:spinDot .7s linear infinite; }
    @keyframes spinDot { to { transform:rotate(360deg); } }

    /* ── Sidebar ── */
    .sb { width:230px; background:rgba(12,13,18,0.96); backdrop-filter:blur(24px); border-right:1px solid var(--border); display:flex; flex-direction:column; flex-shrink:0; z-index:10; }
    .sb-brand { height:62px; padding:0 18px; display:flex; align-items:center; gap:12px; border-bottom:1px solid var(--border); }
    .sb-logo { width:34px; height:34px; border-radius:10px; background:linear-gradient(135deg,#cdbd94,#96865c); color:#111; display:flex; align-items:center; justify-content:center; font-family:'Outfit'; font-weight:900; font-size:16px; box-shadow:0 4px 14px rgba(205,189,148,0.3); }
    .sb-info { display:flex; flex-direction:column; }
    .sb-name { font-family:'Outfit'; font-weight:800; font-size:15px; color:var(--gold-light); letter-spacing:.5px; }
    .sb-ver { font-size:9px; color:var(--text-dim); font-weight:700; text-transform:uppercase; letter-spacing:.6px; }
    .sb-nav { flex:1; padding:14px 10px; display:flex; flex-direction:column; gap:3px; overflow-y:auto; }
    .sb-cat { font-size:9.5px; font-weight:800; text-transform:uppercase; color:var(--text-dim); padding:12px 10px 4px; letter-spacing:.9px; }
    .sb-btn { display:flex; align-items:center; gap:11px; padding:9px 12px; border-radius:var(--r-sm); color:var(--text-dim); cursor:pointer; font-weight:600; font-size:12.5px; transition:all .15s; border:1px solid transparent; }
    .sb-btn:hover { background:var(--bg-card); color:var(--text); }
    .sb-btn.on { background:rgba(205,189,148,0.12); color:var(--gold-light); border-color:var(--border-gold); font-weight:700; }
    .sb-btn .ic { font-size:16px; width:20px; text-align:center; }
    .sb-foot { padding:14px 16px; border-top:1px solid var(--border); display:flex; align-items:center; gap:10px; }
    .pulse { width:8px; height:8px; border-radius:50%; background:var(--red); box-shadow:0 0 8px var(--red); transition:all .3s; }
    .pulse.live { background:var(--green); box-shadow:0 0 8px var(--green); animation:pGlow 2s infinite; }
    @keyframes pGlow { 0%,100%{opacity:1;} 50%{opacity:.5;} }
    .sb-status { font-size:11.5px; font-weight:600; color:var(--text-dim); flex:1; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }

    /* ── Main Area ── */
    .main { flex:1; display:flex; flex-direction:column; overflow:hidden; min-width:0; }
    .conn-bar { height:36px; background:rgba(115,210,133,0.08); border-bottom:1px solid rgba(115,210,133,0.2); display:none; align-items:center; justify-content:space-between; padding:0 22px; font-size:12px; font-weight:600; color:var(--green); flex-shrink:0; }
    .conn-bar.on { display:flex; }
    .topbar { height:52px; background:var(--bg-surface); border-bottom:1px solid var(--border); display:flex; align-items:center; justify-content:space-between; padding:0 22px; flex-shrink:0; }
    .crumbs { display:flex; align-items:center; gap:8px; font-family:'JetBrains Mono'; font-size:12.5px; color:var(--gold-light); font-weight:500; }
    .crumb { background:var(--bg-card); border:1px solid var(--border); padding:4px 10px; border-radius:6px; cursor:pointer; transition:border-color .15s; }
    .crumb:hover { border-color:var(--gold); }

    .btn { display:inline-flex; align-items:center; justify-content:center; gap:7px; padding:7px 15px; font-size:12.5px; font-weight:700; border-radius:var(--r-sm); border:none; cursor:pointer; font-family:'Inter'; transition:all .18s; user-select:none; }
    .btn-g { background:linear-gradient(135deg,#cdbd94,#b5a477); color:#111; box-shadow:0 3px 12px rgba(205,189,148,.25); }
    .btn-g:hover { filter:brightness(1.1); transform:translateY(-1px); }
    .btn-d { background:var(--bg-card); color:var(--text); border:1px solid var(--border); }
    .btn-d:hover { background:var(--bg-card-hover); border-color:rgba(255,255,255,.18); }
    .btn-upload { background:rgba(205,189,148,0.15); color:var(--gold-light); border:1px solid var(--border-gold); }
    .btn-upload:hover { background:var(--gold); color:#111; }
    .btn-danger { background:rgba(224,108,117,0.12); color:var(--red); border:1px solid rgba(224,108,117,0.25); }
    .btn-danger:hover { background:rgba(224,108,117,0.22); }
    .btn-break { background:rgba(224,108,117,0.18); color:#f28b93; border:1px solid var(--red); font-family:'JetBrains Mono'; font-weight:700; }
    .btn-break:hover { background:var(--red); color:#fff; }
    .btn-sm { padding:5px 11px; font-size:11.5px; }

    /* ── Pages ── */
    .stage { flex:1; position:relative; overflow:hidden; }
    .page { position:absolute; inset:0; display:none; flex-direction:column; overflow-y:auto; padding:22px; }
    .page.on { display:flex; }

    /* ── Connections Page ── */
    .sec-title { font-family:'Outfit'; font-size:22px; font-weight:800; color:var(--text); margin-bottom:4px; }
    .sec-sub { font-size:13px; color:var(--text-dim); margin-bottom:18px; }
    .cards { display:grid; grid-template-columns:repeat(auto-fill,minmax(310px,1fr)); gap:16px; }
    .card { background:var(--bg-card); border:1px solid var(--border); border-radius:var(--r-md); padding:18px; display:flex; flex-direction:column; gap:11px; transition:all .2s; position:relative; }
    .card:hover { border-color:var(--border-gold); box-shadow:0 10px 28px rgba(0,0,0,.4); transform:translateY(-2px); }
    .card-row { display:flex; justify-content:space-between; align-items:center; }
    .tag { font-size:10.5px; font-weight:800; font-family:'JetBrains Mono'; padding:2px 8px; border-radius:6px; background:rgba(205,189,148,.12); color:var(--gold-light); border:1px solid var(--border-gold); }
    .card-name { font-family:'Outfit'; font-size:17px; font-weight:700; color:var(--gold-light); }
    .card-ep { font-family:'JetBrains Mono'; font-size:12px; color:var(--text-dim); }
    .card-acts { display:flex; gap:8px; margin-top:4px; padding-top:12px; border-top:1px solid var(--border); }
    .card-ping { font-family:'JetBrains Mono'; font-size:11px; color:var(--text-dim); }

    .empty-state { display:flex; flex-direction:column; align-items:center; justify-content:center; flex:1; gap:14px; opacity:.75; padding:60px 0; }
    .empty-icon { font-size:52px; opacity:.6; }
    .empty-text { font-size:14px; color:var(--text-dim); text-align:center; max-width:340px; line-height:1.6; }

    /* ── File Explorer ── */
    .toolbar { display:flex; justify-content:space-between; align-items:center; margin-bottom:12px; background:var(--bg-card); padding:8px 14px; border-radius:var(--r-sm); border:1px solid var(--border); flex-shrink:0; }
    .ftbl-wrap { flex:1; overflow-y:auto; overflow-x:auto; min-height:0; border-radius:var(--r-md); border:1px solid var(--border); background:var(--bg-card); position:relative; }
    .ftbl { width:100%; border-collapse:collapse; background:transparent; }
    .ftbl thead { position:sticky; top:0; z-index:10; background:#0e1017; }
    .ftbl th { text-align:left; padding:11px 16px; font-size:11px; font-weight:700; text-transform:uppercase; color:var(--text-dim); border-bottom:1px solid var(--border); background:#0e1017; }
    .ftbl td { padding:11px 16px; border-bottom:1px solid rgba(255,255,255,.03); font-size:13px; }
    .frow { cursor:pointer; transition:background .12s; }
    .frow:hover { background:var(--bg-card-hover); }
    .fname { display:flex; align-items:center; gap:10px; font-weight:600; color:var(--text); }
    .fname.dir { color:var(--gold-light); }
    .fmeta { font-family:'JetBrains Mono'; color:var(--text-dim); font-size:12px; }
    .frow .row-acts { opacity:0; transition:opacity .15s; display:flex; gap:6px; }
    .frow:hover .row-acts { opacity:1; }
    .chk-box { width:15px; height:15px; accent-color:var(--gold); cursor:pointer; vertical-align:middle; }
    .batch-bar { display:none; align-items:center; justify-content:space-between; background:rgba(205,189,148,0.1); border:1px solid var(--border-gold); padding:9px 16px; border-radius:var(--r-sm); margin-bottom:12px; animation:tIn .2s ease; flex-shrink:0; }
    .batch-bar.on { display:flex; }
    .dropzone-overlay { position:absolute; inset:0; background:rgba(6,7,9,0.88); backdrop-filter:blur(8px); border:2px dashed var(--gold); border-radius:var(--r-md); display:none; flex-direction:column; align-items:center; justify-content:center; gap:10px; z-index:90; pointer-events:none; }
    .dropzone-overlay.on { display:flex; }
    .dropzone-icon { font-size:44px; animation:pGlow 1.5s infinite ease-in-out; }
    .dropzone-title { font-family:'Outfit'; font-size:18px; font-weight:800; color:var(--gold-light); }
    .dropzone-sub { font-size:13px; color:var(--text-dim); }
    .frow.selected { background:rgba(205,189,148,0.08); }

    /* ── Editor ── */
    .editor-wrap { flex:1; display:flex; flex-direction:column; background:var(--bg-input); border:1px solid var(--border); border-radius:var(--r-md); overflow:hidden; }
    .editor-bar { height:44px; background:var(--bg-card); border-bottom:1px solid var(--border); display:flex; align-items:center; justify-content:space-between; padding:0 14px; flex-shrink:0; }
    .editor-tab { padding:5px 12px; background:var(--bg-input); border:1px solid var(--border-gold); border-radius:6px; color:var(--gold-light); font-weight:600; font-size:12px; font-family:'JetBrains Mono'; }
    .code-area { flex:1; width:100%; background:transparent; color:#eae6db; caret-color:var(--gold); font-family:'JetBrains Mono',monospace; font-size:13px; line-height:1.65; padding:16px; border:none; outline:none; resize:none; white-space:pre; tab-size:2; }

    /* ── Termius Terminal ── */
    .term-wrap { flex:1; display:flex; flex-direction:column; background:#06070a; border:1px solid var(--border-gold); border-radius:var(--r-md); overflow:hidden; box-shadow:0 8px 30px rgba(0,0,0,0.5); }
    .term-bar { height:44px; background:#0d0e14; border-bottom:1px solid var(--border); display:flex; align-items:center; justify-content:space-between; padding:0 14px; flex-shrink:0; }
    .term-title { font-family:'JetBrains Mono',monospace; font-weight:700; font-size:12.5px; color:var(--gold-light); display:flex; align-items:center; gap:8px; }
    .chips { display:flex; gap:6px; overflow-x:auto; flex-shrink:0; }
    .chip { background:rgba(255,255,255,.06); border:1px solid var(--border); border-radius:6px; padding:4px 9px; font-family:'JetBrains Mono',monospace; font-size:11px; color:var(--gold-light); cursor:pointer; transition:all .14s; white-space:nowrap; }
    .chip:hover { background:var(--gold); color:#111; font-weight:700; }
    .term-screen { flex:1; padding:16px; font-family:'JetBrains Mono','Cascadia Code',monospace; font-size:12.5px; line-height:1.45; letter-spacing:0px; color:#d6d3c9; overflow-y:auto; white-space:pre-wrap; word-break:break-all; user-select:text; background:#06070a; }
    .term-input-row { display:flex; align-items:center; gap:10px; padding:10px 14px; background:#0d0e14; border-top:1px solid var(--border); flex-shrink:0; }
    .term-prompt { font-family:'JetBrains Mono',monospace; font-weight:700; color:var(--green); font-size:12.5px; white-space:nowrap; }
    .term-inp { flex:1; background:transparent; border:none; outline:none; font-family:'JetBrains Mono',monospace; font-size:13px; color:var(--gold-light); caret-color:var(--gold); }

    /* ── Modal ── */
    .overlay { position:fixed; inset:0; background:rgba(0,0,0,.75); backdrop-filter:blur(10px); display:none; align-items:center; justify-content:center; z-index:999; }
    .overlay.on { display:flex; }
    .modal { background:var(--bg-card); border:1px solid var(--border-gold); border-radius:var(--r-lg); width:100%; max-width:480px; box-shadow:0 24px 60px rgba(0,0,0,.6); overflow:hidden; }
    .modal-hd { padding:18px 22px; border-bottom:1px solid var(--border); display:flex; justify-content:space-between; align-items:center; }
    .modal-hd h3 { font-family:'Outfit'; font-size:18px; font-weight:800; color:var(--gold-light); }
    .modal-bd { padding:20px 22px; display:flex; flex-direction:column; gap:13px; }
    .lbl { font-size:11.5px; font-weight:700; color:var(--text-dim); text-transform:uppercase; letter-spacing:.5px; margin-bottom:4px; }
    .inp { width:100%; background:var(--bg-input); border:1px solid var(--border); border-radius:var(--r-sm); padding:10px 12px; color:var(--text); font-size:13px; outline:none; transition:border .2s; font-family:inherit; }
    .inp:focus { border-color:var(--gold); box-shadow:0 0 0 2px var(--gold-glow); }
    .modal-ft { padding:14px 22px; background:rgba(0,0,0,.2); border-top:1px solid var(--border); display:flex; justify-content:flex-end; gap:10px; }
    .row2 { display:grid; grid-template-columns:1fr 1fr; gap:12px; }

    /* ── Toasts ── */
    #toasts { position:fixed; top:16px; right:16px; z-index:9999; display:flex; flex-direction:column; gap:8px; pointer-events:none; }
    .toast { background:var(--bg-card); border:1px solid var(--border-gold); border-radius:var(--r-sm); padding:11px 18px; color:var(--gold-light); font-weight:600; font-size:13px; box-shadow:0 8px 24px rgba(0,0,0,.5); animation:tIn .22s ease; pointer-events:auto; display:flex; align-items:center; gap:8px; }
    @keyframes tIn { from { transform:translateX(40px); opacity:0; } to { transform:none; opacity:1; } }
  </style>
</head>
<body>
  <div id=""top-loader""></div>
  <input type=""file"" id=""file-upload-input"" multiple style=""display:none"" onchange=""handleFileSelected(event)"">
  <input type=""file"" id=""folder-upload-input"" webkitdirectory directory multiple style=""display:none"" onchange=""handleFolderSelected(event)"">

  <div id=""root"">
    <!-- Sidebar -->
    <aside class=""sb"">
      <div class=""sb-brand"">
        <div class=""sb-logo"">M</div>
        <div class=""sb-info"">
          <span class=""sb-name"">MYSFTP</span>
          <span class=""sb-ver"">v2.0.0 • Dedicated Suite</span>
        </div>
      </div>
      <div class=""sb-nav"">
        <div class=""sb-cat"">Koneksi Server</div>
        <div class=""sb-btn on"" data-v=""conn"" onclick=""go('conn')""><span class=""ic"">⚡</span>Profil Server</div>
        <div class=""sb-cat"">Remote File & Tools</div>
        <div class=""sb-btn"" data-v=""files"" onclick=""go('files')""><span class=""ic"">📁</span>File Explorer</div>
        <div class=""sb-btn"" data-v=""editor"" onclick=""go('editor')""><span class=""ic"">✏️</span>Pro Code Editor</div>
        <div class=""sb-btn"" data-v=""term"" onclick=""go('term')""><span class=""ic"">💻</span>SSH Termius</div>
      </div>
      <div class=""sb-foot"">
        <div class=""pulse"" id=""pulse-dot""></div>
        <span class=""sb-status"" id=""sb-lbl"">Offline</span>
      </div>
    </aside>

    <!-- Main -->
    <div class=""main"">
      <div class=""conn-bar"" id=""conn-bar"">
        <span id=""conn-bar-text"">● Terhubung ke server</span>
        <button class=""btn btn-sm btn-danger"" onclick=""doDisconnect()"">Putuskan Koneksi</button>
      </div>

      <header class=""topbar"">
        <div class=""crumbs"" id=""crumbs""><span class=""crumb"">⚡ Profil Server</span></div>
        <div style=""display:flex;gap:10px;align-items:center;"">
          <span id=""load-badge""><span class=""spin-dot""></span> Memuat...</span>
          <div style=""display:flex;gap:8px;"" id=""top-actions""></div>
        </div>
      </header>

      <div class=""stage"">
        <!-- 1. Connections Page -->
        <section class=""page on"" id=""p-conn"">
          <div style=""display:flex;justify-content:space-between;align-items:flex-end;margin-bottom:18px;"">
            <div>
              <h1 class=""sec-title"">Profil Koneksi Server</h1>
              <p class=""sec-sub"">Kelola server SFTP/SSH VPS Anda dengan koneksi aman dan instan.</p>
            </div>
            <button class=""btn btn-g"" onclick=""openModal()"">+ Tambah Server Baru</button>
          </div>
          <div class=""cards"" id=""cards""></div>
          <div class=""empty-state"" id=""empty-conn"" style=""display:none;"">
            <div class=""empty-icon"">🖥️</div>
            <div class=""empty-text"">Belum ada profil server tersimpan.<br>Klik <strong>+ Tambah Server Baru</strong> untuk menambahkan VPS pertama Anda.</div>
          </div>
        </section>

        <!-- 2. File Explorer Page -->
        <section class=""page"" id=""p-files"">
          <div class=""dropzone-overlay"" id=""dropzone-overlay"">
            <div class=""dropzone-icon"">📥</div>
            <div class=""dropzone-title"">Lepaskan Berkas atau Folder di Sini</div>
            <div class=""dropzone-sub"">Unggah otomatis ke direktori remote yang sedang dibuka</div>
          </div>

          <div class=""batch-bar"" id=""batch-bar"">
            <span id=""batch-count"" style=""font-weight:700;color:var(--gold-light);font-size:13px;"">0 item terpilih</span>
            <div style=""display:flex;gap:8px;"">
              <button class=""btn btn-upload btn-sm"" onclick=""downloadSelected()"">📥 Download Terpilih</button>
              <button class=""btn btn-danger btn-sm"" onclick=""deleteSelected()"">🗑 Hapus Terpilih</button>
              <button class=""btn btn-d btn-sm"" onclick=""clearSelection()"">✕ Batal</button>
            </div>
          </div>

          <div class=""toolbar"">
            <div style=""display:flex;gap:8px;flex-wrap:wrap;"">
              <button class=""btn btn-d btn-sm nav-btn"" onclick=""fsUp()"">◀ Kembali</button>
              <button class=""btn btn-d btn-sm nav-btn"" onclick=""fsRefresh()"">🔄 Refresh</button>
              <button class=""btn btn-upload btn-sm"" onclick=""triggerUpload()"">📤 Upload File</button>
              <button class=""btn btn-upload btn-sm"" onclick=""triggerFolderUpload()"">📁 Upload Folder</button>
              <button class=""btn btn-d btn-sm"" onclick=""fsNew('file')"">+ File Baru</button>
              <button class=""btn btn-d btn-sm"" onclick=""fsNew('folder')"">+ Folder</button>
            </div>
            <span id=""fs-info"" class=""fmeta""></span>
          </div>

          <div class=""ftbl-wrap"">
            <table class=""ftbl"">
              <thead>
                <tr>
                  <th style=""width:38px;text-align:center;""><input type=""checkbox"" id=""chk-all"" class=""chk-box"" onchange=""toggleSelectAll(this.checked)""></th>
                  <th style=""width:48%"">Nama Berkas / Folder</th>
                  <th style=""width:13%"">Ukuran</th>
                  <th style=""width:12%"">Tipe</th>
                  <th style=""width:17%"">Terakhir Diubah</th>
                  <th style=""width:8%"">Aksi</th>
                </tr>
              </thead>
              <tbody id=""ftbody""></tbody>
            </table>
          </div>

          <div class=""empty-state"" id=""empty-fs"" style=""display:none;"">
            <div class=""empty-icon"">📂</div>
            <div class=""empty-text"">Silakan hubungkan ke server di tab <strong>Profil Server</strong> terlebih dahulu.</div>
          </div>
        </section>

        <!-- 3. Pro Code Editor Page -->
        <section class=""page"" id=""p-editor"" style=""padding:14px;"">
          <div class=""editor-wrap"">
            <div class=""editor-bar"">
              <div class=""editor-tab"" id=""ed-tab"">📄 Belum ada berkas</div>
              <div style=""display:flex;gap:8px;"">
                <button class=""btn btn-g btn-sm"" onclick=""edSave()"">💾 Simpan Berkas (Ctrl+S)</button>
              </div>
            </div>
            <textarea class=""code-area"" id=""ed-area"" spellcheck=""false"" placeholder=""// Pilih berkas dari File Explorer untuk mulai mengedit...""></textarea>
          </div>
        </section>

        <!-- 4. SSH Termius Terminal Page -->
        <section class=""page"" id=""p-term"" style=""padding:14px;"">
          <div class=""term-wrap"">
            <div class=""term-bar"">
              <div class=""term-title"">
                <span>💻 SSH Termius Console</span>
              </div>
              <div class=""chips"">
                <button class=""btn btn-sm btn-break"" onclick=""tBreak()"" title=""Hentikan proses streaming log / monitoring (SIGINT)"">🛑 Ctrl+C</button>
                <span class=""chip"" onclick=""tSend('pm2 status')"">📊 pm2 status</span>
                <span class=""chip"" onclick=""tSend('pm2 logs')"">📜 pm2 logs</span>
                <span class=""chip"" onclick=""tSend('ls -la')"">📁 ls -la</span>
                <span class=""chip"" onclick=""tSend('df -h')"">💾 df -h</span>
                <span class=""chip"" onclick=""tSend('free -m')"">🧠 free -m</span>
                <span class=""chip"" onclick=""tSend('uptime')"">⏱️ uptime</span>
                <span class=""chip"" onclick=""tClear()"">🧹 Clear</span>
              </div>
            </div>
            <div class=""term-screen"" id=""tscreen""></div>
            <div class=""term-input-row"">
              <span class=""term-prompt"" id=""tprompt"">root@server:~#</span>
              <input type=""text"" class=""term-inp"" id=""tinp"" placeholder=""Ketik perintah Linux di sini... (contoh: pm2 status, ls -la, htop)"" autocomplete=""off"" onkeydown=""if(event.key==='Enter'){tExec();}else if(event.key==='ArrowUp'){event.preventDefault();tHistoryNav(-1);}else if(event.key==='ArrowDown'){event.preventDefault();tHistoryNav(1);}"">
              <button class=""btn btn-g btn-sm"" onclick=""tExec()"">Kirim</button>
            </div>
          </div>
        </section>
      </div>
    </div>
  </div>

  <!-- Add/Edit Server Modal -->
  <div class=""overlay"" id=""ov-conn"">
    <div class=""modal"">
      <div class=""modal-hd"">
        <h3 id=""modal-title"">Tambah Server Baru</h3>
        <button class=""btn btn-d btn-sm"" onclick=""closeModal()"">✕</button>
      </div>
      <form onsubmit=""event.preventDefault();saveProfile();"">
        <div class=""modal-bd"">
          <div>
            <div class=""lbl"">Nama Profil</div>
            <input type=""text"" id=""f-name"" class=""inp"" placeholder=""Contoh: VPS Produksi"" required>
          </div>
          <div class=""row2"">
            <div>
              <div class=""lbl"">Protokol</div>
              <select id=""f-proto"" class=""inp"">
                <option value=""SFTP"">SFTP (SSH)</option>
                <option value=""LOCAL"">Local File System</option>
              </select>
            </div>
            <div>
              <div class=""lbl"">Port</div>
              <input type=""number"" id=""f-port"" class=""inp"" value=""22"" required>
            </div>
          </div>
          <div>
            <div class=""lbl"">Host / IP Server</div>
            <input type=""text"" id=""f-host"" class=""inp"" placeholder=""163.172.110.146"" required>
          </div>
          <div class=""row2"">
            <div>
              <div class=""lbl"">Username</div>
              <input type=""text"" id=""f-user"" class=""inp"" placeholder=""root"">
            </div>
            <div>
              <div class=""lbl"">Password</div>
              <input type=""password"" id=""f-pass"" class=""inp"" placeholder=""••••••••"">
            </div>
          </div>
        </div>
        <div class=""modal-ft"">
          <button type=""button"" class=""btn btn-d"" onclick=""closeModal()"">Batal</button>
          <button type=""submit"" class=""btn btn-g"">💾 Simpan Profil</button>
        </div>
      </form>
    </div>
  </div>

  <!-- Connect Modal -->
  <div class=""overlay"" id=""ov-connect"">
    <div class=""modal"">
      <div class=""modal-hd"">
        <h3>Hubungkan ke Server</h3>
        <button class=""btn btn-d btn-sm"" onclick=""closeOv('ov-connect')"">✕</button>
      </div>
      <form onsubmit=""event.preventDefault();doConnect();"">
        <div class=""modal-bd"">
          <div id=""connect-info"" style=""font-size:13px;color:var(--text-dim);line-height:1.5;""></div>
          <div>
            <div class=""lbl"">Password Server</div>
            <input type=""password"" id=""c-pass"" class=""inp"" placeholder=""Masukkan password..."" required autofocus>
          </div>
          <div id=""connect-error"" style=""color:var(--red);font-size:12px;font-weight:600;display:none;""></div>
        </div>
        <div class=""modal-ft"">
          <button type=""button"" class=""btn btn-d"" onclick=""closeOv('ov-connect')"">Batal</button>
          <button type=""submit"" class=""btn btn-g"" id=""connect-btn"">🚀 Hubungkan</button>
        </div>
      </form>
    </div>
  </div>

  <!-- Custom Confirm Modal (Replaces browser default confirm) -->
  <div class=""overlay"" id=""ov-confirm"">
    <div class=""modal"" style=""max-width:430px;"">
      <div class=""modal-hd"">
        <h3 id=""conf-title"">Konfirmasi</h3>
        <button class=""btn btn-d btn-sm"" onclick=""closeOv('ov-confirm')"">✕</button>
      </div>
      <div class=""modal-bd"">
        <p id=""conf-msg"" style=""font-size:13.5px;color:var(--text);line-height:1.6;""></p>
      </div>
      <div class=""modal-ft"">
        <button type=""button"" class=""btn btn-d"" onclick=""closeOv('ov-confirm')"">Batal</button>
        <button type=""button"" class=""btn btn-danger"" id=""conf-ok-btn"">OK</button>
      </div>
    </div>
  </div>

  <!-- Custom Prompt Modal (Replaces browser default prompt) -->
  <div class=""overlay"" id=""ov-prompt"">
    <div class=""modal"" style=""max-width:440px;"">
      <div class=""modal-hd"">
        <h3 id=""prompt-title"">Input</h3>
        <button class=""btn btn-d btn-sm"" onclick=""closeOv('ov-prompt')"">✕</button>
      </div>
      <form onsubmit=""event.preventDefault();submitCustomPrompt();"">
        <div class=""modal-bd"">
          <div class=""lbl"" id=""prompt-lbl"">Nama:</div>
          <input type=""text"" id=""prompt-inp"" class=""inp"" autocomplete=""off"" required>
        </div>
        <div class=""modal-ft"">
          <button type=""button"" class=""btn btn-d"" onclick=""closeOv('ov-prompt')"">Batal</button>
          <button type=""submit"" class=""btn btn-g"" id=""prompt-ok-btn"">Lanjutkan</button>
        </div>
      </form>
    </div>
  </div>

  <div id=""toasts""></div>

  <script>
    var profiles = [];
    var curView = 'conn';
    var connected = false;
    var connProfile = null;
    var fsPath = '/root';
    var fsItems = [];
    var fsCache = {};
    var isNavigating = false;
    var edFile = null;
    var termStreamPoll = null;

    window.onload = function() {
      loadProfiles();
      document.getElementById('tinp').addEventListener('keydown', function(e) {
        if (e.key === 'Enter') tExec();
        else if (e.key === 'ArrowUp') { e.preventDefault(); tHistoryNav(-1); }
        else if (e.key === 'ArrowDown') { e.preventDefault(); tHistoryNav(1); }
      });
      var dropZone = document.getElementById('p-files');
      ['dragenter','dragover'].forEach(function(ev) {
        dropZone.addEventListener(ev, function(e) {
          e.preventDefault(); e.stopPropagation();
          if (curView === 'files' && connected) dropZone.classList.add('drag-on');
        });
      });
      ['dragleave','drop'].forEach(function(ev) {
        dropZone.addEventListener(ev, function(e) {
          e.preventDefault(); e.stopPropagation();
          dropZone.classList.remove('drag-on');
        });
      });
      dropZone.addEventListener('drop', function(e) {
        if (curView !== 'files' || !connected) return;
        var files = e.dataTransfer && e.dataTransfer.files;
        if (files && files.length) uploadFileList(files);
      });

      window.addEventListener('keydown', function(e) {
        if (e.ctrlKey && e.key === 's') {
          e.preventDefault();
          edSave();
        } else if (e.ctrlKey && e.key === 'c') {
          if (window.getSelection().toString().length === 0 && curView === 'term') {
            e.preventDefault();
            tBreak();
          }
        }
      });
    };

    function showLoader(show) {
      document.getElementById('top-loader').style.display = show ? 'block' : 'none';
      document.getElementById('load-badge').classList.toggle('on', !!show);
    }

    function go(v) {
      curView = v;
      document.querySelectorAll('.sb-btn').forEach(function(b) { b.classList.remove('on'); });
      document.querySelectorAll('.page').forEach(function(p) { p.classList.remove('on'); });
      var btn = document.querySelector('[data-v=""' + v + '""]');
      if (btn) btn.classList.add('on');
      var pg = document.getElementById('p-' + v);
      if (pg) pg.classList.add('on');

      var cr = document.getElementById('crumbs');
      var acts = document.getElementById('top-actions');
      acts.innerHTML = '';
      if (v === 'conn') {
        cr.innerHTML = '<span class=""crumb"">⚡ Profil Server</span>';
        acts.innerHTML = '<button class=""btn btn-g btn-sm"" onclick=""openModal()"">+ Tambah</button>';
      } else if (v === 'files') {
        cr.innerHTML = '<span class=""crumb"">📁 ' + esc(fsPath) + '</span>';
        acts.innerHTML = '<button class=""btn btn-upload btn-sm"" onclick=""triggerUpload()"">📤 Upload File</button>';
      } else if (v === 'editor') {
        cr.innerHTML = '<span class=""crumb"">✏️ ' + esc(edFile || 'Pro Code Editor') + '</span>';
        acts.innerHTML = '<button class=""btn btn-g btn-sm"" onclick=""edSave()"">💾 Simpan</button>';
      } else if (v === 'term') {
        cr.innerHTML = '<span class=""crumb"">💻 SSH Termius</span>';
        acts.innerHTML = '<button class=""btn btn-sm btn-break"" onclick=""tBreak()"">🛑 Ctrl+C</button>';
        startTermPoll();
        setTimeout(function(){ var el = document.getElementById('tinp'); if(el) el.focus(); }, 60);
      }
    }

    function loadProfiles() {
      try {
        var cached = localStorage.getItem('mysftp_profiles');
        if (cached) {
          profiles = JSON.parse(cached) || [];
          renderCards();
        }
      } catch (e) {}

      fetch('/api/profiles')
        .then(function(r){ return r.json(); })
        .then(function(d) {
          if (Array.isArray(d)) {
            profiles = d;
            try { localStorage.setItem('mysftp_profiles', JSON.stringify(profiles)); } catch(e){}
            renderCards();
          }
        }).catch(function(){});
    }

    function renderCards() {
      var g = document.getElementById('cards');
      var em = document.getElementById('empty-conn');
      g.innerHTML = '';
      if (profiles.length === 0) { em.style.display = 'flex'; return; }
      em.style.display = 'none';

      profiles.forEach(function(p) {
        var c = document.createElement('div');
        c.className = 'card';
        var isConn = connected && connProfile && connProfile.id === p.id;
        if (isConn) c.style.borderColor = 'rgba(115,210,133,.45)';
        c.innerHTML = '<div class=""card-row""><span class=""tag"">' + esc(p.protocol||'SFTP') + '</span>' +
          (isConn ? '<span style=""font-size:10.5px;color:var(--green);font-weight:800;"">● ONLINE</span>' : '') +
          '</div>' +
          '<div class=""card-name"">' + esc(p.name) + '</div>' +
          '<div class=""card-ep"">' + esc(p.username||'') + '@' + esc(p.host||'') + ':' + (p.port||22) + '</div>' +
          '<div class=""card-ping"" id=""ping-' + p.id + '""></div>' +
          '<div class=""card-acts"">' +
          '<button class=""btn btn-d btn-sm"" style=""flex:1;"" onclick=""pingServer(\'' + esc(p.host) + '\',' + (p.port||22) + ',\'' + p.id + '\')"">⚡ Ping</button>' +
          '<button class=""btn btn-g btn-sm"" style=""flex:2;"" onclick=""promptConnect(\'' + p.id + '\')"">🚀 Buka</button>' +
          '<button class=""btn btn-danger btn-sm"" onclick=""delProfile(\'' + p.id + '\')"">🗑</button>' +
          '</div>';
        g.appendChild(c);
      });
    }

    function openModal() {
      document.getElementById('f-name').value = '';
      document.getElementById('f-host').value = '';
      document.getElementById('f-port').value = '22';
      document.getElementById('f-user').value = 'root';
      document.getElementById('f-pass').value = '';
      document.getElementById('f-proto').value = 'SFTP';
      document.getElementById('ov-conn').classList.add('on');
      document.getElementById('f-name').focus();
    }

    function closeModal() { document.getElementById('ov-conn').classList.remove('on'); }
    function closeOv(id) { document.getElementById(id).classList.remove('on'); }

    function saveProfile() {
      var item = {
        id: 'c' + Date.now(),
        name: document.getElementById('f-name').value,
        protocol: document.getElementById('f-proto').value,
        host: document.getElementById('f-host').value,
        port: parseInt(document.getElementById('f-port').value) || 22,
        username: document.getElementById('f-user').value,
        password: document.getElementById('f-pass').value
      };
      
      profiles.unshift(item);
      try { localStorage.setItem('mysftp_profiles', JSON.stringify(profiles)); } catch(e){}
      closeModal();
      renderCards();
      toast('Profil server berhasil disimpan!');

      fetch('/api/profiles', {
        method: 'POST',
        headers: {'Content-Type':'application/json; charset=utf-8'},
        body: JSON.stringify(profiles)
      }).catch(function(err) {
        console.warn('Backend sync:', err);
      });
    }

    function delProfile(id) {
      customConfirm('Hapus Profil Server', 'Apakah Anda yakin ingin menghapus profil server ini?', '🗑 Hapus Profil', true, function() {
        profiles = profiles.filter(function(x){return x.id!==id;});
        try { localStorage.setItem('mysftp_profiles', JSON.stringify(profiles)); } catch(e){}
        renderCards();
        toast('Profil dihapus.');

        fetch('/api/profiles',{
          method:'POST',
          headers:{'Content-Type':'application/json; charset=utf-8'},
          body:JSON.stringify(profiles)
        }).catch(function(err) {
          console.warn('Backend sync:', err);
        });
      });
    }

    function pingServer(host, port, id) {
      var el = document.getElementById('ping-' + id);
      if (el) el.innerHTML = '<span style=""color:var(--text-dim)"">Pinging...</span>';
      fetch('/api/ping?host=' + encodeURIComponent(host) + '&port=' + port)
        .then(function(r){return r.json();})
        .then(function(d) {
          if (el) {
            if (d.online) el.innerHTML = '<span style=""color:var(--green)"">● Online (' + d.latency + ' ms)</span>';
            else el.innerHTML = '<span style=""color:var(--red)"">● Host Offline / Port tertutup</span>';
          }
        });
    }

    var pendingConnId = null;

    function promptConnect(id) {
      var p = profiles.find(function(x){return x.id===id;});
      if (!p) return;

      if (p.protocol === 'LOCAL') {
        connProfile = p;
        connected = true;
        document.getElementById('sb-lbl').textContent = p.name;
        document.getElementById('pulse-dot').classList.add('live');
        document.getElementById('conn-bar').classList.add('on');
        document.getElementById('conn-bar-text').textContent = '● Terhubung — ' + p.name + ' (Local Drive)';
        renderCards();
        go('files');
        fsLoad('/');
        toast('Terhubung ke ' + p.name);
        return;
      }

      pendingConnId = id;
      var inf = document.getElementById('connect-info');
      inf.innerHTML = 'Server: <strong>' + esc(p.name) + '</strong><br>Endpoint: <code>' + esc(p.username) + '@' + esc(p.host) + ':' + p.port + '</code>';
      document.getElementById('c-pass').value = p.password || '';
      document.getElementById('connect-error').style.display = 'none';
      document.getElementById('ov-connect').classList.add('on');
      document.getElementById('c-pass').focus();
    }

    function doConnect() {
      var p = profiles.find(function(x){return x.id===pendingConnId;});
      if (!p) return;

      var pw = document.getElementById('c-pass').value;
      var btn = document.getElementById('connect-btn');
      btn.innerHTML = '⏳ Menghubungkan...';
      btn.disabled = true;
      showLoader(true);
      document.getElementById('connect-error').style.display = 'none';

      var ctrl = (typeof AbortController !== 'undefined') ? new AbortController() : null;
      var timer = ctrl ? setTimeout(function(){ ctrl.abort(); }, 14000) : null;

      fetch('/api/connect', {
        method: 'POST',
        headers: {'Content-Type':'application/json; charset=utf-8'},
        body: JSON.stringify({
          name: p.name,
          protocol: p.protocol,
          host: p.host,
          port: p.port,
          username: p.username,
          password: pw
        }),
        signal: ctrl ? ctrl.signal : undefined
      }).then(function(r){
        if (timer) clearTimeout(timer);
        return r.json();
      }).then(function(res) {
        btn.innerHTML = '🚀 Hubungkan';
        btn.disabled = false;
        showLoader(false);

        if (res.success) {
          p.password = pw;
          try { localStorage.setItem('mysftp_profiles', JSON.stringify(profiles)); } catch(e){}
          fetch('/api/profiles',{method:'POST',headers:{'Content-Type':'application/json; charset=utf-8'},body:JSON.stringify(profiles)}).catch(function(){});

          connected = true;
          connProfile = p;
          closeOv('ov-connect');

          document.getElementById('sb-lbl').textContent = p.name;
          document.getElementById('pulse-dot').classList.add('live');
          document.getElementById('conn-bar').classList.add('on');
          document.getElementById('conn-bar-text').textContent = '● Terhubung ke ' + p.name + ' (' + p.host + ')';
          document.getElementById('tprompt').textContent = p.username + '@' + p.host + ':~#';
          tClear();
          if (p.protocol === 'SFTP') startTermPoll();

          renderCards();
          go('files');
          fsLoad(p.protocol === 'LOCAL' ? '/' : '/root');
          toast('Berhasil terhubung ke ' + p.name + '!');
        } else {
          var errEl = document.getElementById('connect-error');
          errEl.textContent = '❌ Gagal: ' + (res.error || 'Periksa username & password');
          errEl.style.display = 'block';
        }
      }).catch(function(err) {
        if (timer) clearTimeout(timer);
        btn.innerHTML = '🚀 Hubungkan';
        btn.disabled = false;
        showLoader(false);
        var msg = (err.name === 'AbortError') 
          ? 'Koneksi timeout — pastikan Host IP, Port, dan firewall server mengizinkan SSH' 
          : err.message;
        document.getElementById('connect-error').textContent = '❌ Kesalahan: ' + msg;
        document.getElementById('connect-error').style.display = 'block';
      });
    }

    function doDisconnect() {
      fetch('/api/disconnect',{method:'POST'}).then(function() {
        connected = false;
        connProfile = null;
        fsCache = {};
        stopTermPoll();
        document.getElementById('sb-lbl').textContent = 'Offline';
        document.getElementById('pulse-dot').classList.remove('live');
        document.getElementById('conn-bar').classList.remove('on');
        document.getElementById('tprompt').textContent = 'root@server:~#';
        document.getElementById('tscreen').innerHTML = '';
        renderCards();
        go('conn');
        toast('Koneksi diputuskan.');
      });
    }

    // ── File Explorer with Debounced Caching ──
    function setNavButtonsBusy(busy) {
      document.querySelectorAll('.nav-btn').forEach(function(b) {
        b.disabled = busy;
        b.style.opacity = busy ? '0.5' : '1';
        b.style.pointerEvents = busy ? 'none' : 'auto';
      });
    }

    function fsLoad(path) {
      if (isNavigating) return; // hard guard: a second click while loading does nothing
      if (!path) path = '/root';
      isNavigating = true;
      showLoader(true);
      setNavButtonsBusy(true);

      // If we already have this folder cached, show it instantly — but only
      // when it's actually a DIFFERENT folder than what's on screen, so a
      // double-click on the same button never looks like it jumped back —
      // there's nothing to jump back to because the content doesn't change
      // until the real, fresh data arrives.
      if (fsCache[path] && path !== fsPath) {
        fsPath = path;
        fsItems = fsCache[path];
        renderFiles();
      }

      fetch('/api/fs/list?path=' + encodeURIComponent(path))
        .then(function(r){return r.json();})
        .then(function(d) {
          isNavigating = false;
          showLoader(false);
          setNavButtonsBusy(false);
          if (d.success) {
            fsPath = d.currentPath;
            fsItems = d.items || [];
            fsCache[fsPath] = fsItems;
            renderFiles();
          } else {
            toast('❌ Gagal: ' + (d.error || 'Folder tidak dapat diakses'));
          }
        }).catch(function(err) {
          isNavigating = false;
          showLoader(false);
          setNavButtonsBusy(false);
          toast('❌ Error: ' + err.message);
        });
    }

    function fsRefresh() {
      delete fsCache[fsPath];
      fsLoad(fsPath);
    }

    function fsUp() {
      if (isNavigating) return;
      if (fsPath === '/' || !fsPath) return;
      var idx = fsPath.lastIndexOf('/');
      var target = idx <= 0 ? '/' : fsPath.substring(0, idx);
      fsLoad(target);
    }

    var confirmCallback = null;
    function customConfirm(title, msg, btnText, isDanger, onOk) {
      document.getElementById('conf-title').textContent = title || 'Konfirmasi';
      document.getElementById('conf-msg').textContent = msg || '';
      var btn = document.getElementById('conf-ok-btn');
      btn.textContent = btnText || 'OK';
      btn.className = isDanger ? 'btn btn-danger' : 'btn btn-g';
      confirmCallback = onOk;
      document.getElementById('ov-confirm').classList.add('on');
    }
    document.getElementById('conf-ok-btn').onclick = function() {
      closeOv('ov-confirm');
      if (confirmCallback) { var cb = confirmCallback; confirmCallback = null; cb(); }
    };

    var promptCallback = null;
    function customPrompt(title, lbl, placeholder, defVal, btnText, onOk) {
      document.getElementById('prompt-title').textContent = title || 'Input';
      document.getElementById('prompt-lbl').textContent = lbl || 'Nama:';
      var inp = document.getElementById('prompt-inp');
      inp.placeholder = placeholder || '';
      inp.value = defVal || '';
      document.getElementById('prompt-ok-btn').textContent = btnText || 'Simpan';
      promptCallback = onOk;
      document.getElementById('ov-prompt').classList.add('on');
      setTimeout(function(){ inp.focus(); inp.select(); }, 60);
    }
    function submitCustomPrompt() {
      var val = document.getElementById('prompt-inp').value.trim();
      closeOv('ov-prompt');
      if (promptCallback) { var cb = promptCallback; promptCallback = null; cb(val); }
    }

    var selectedPaths = [];

    function updateBatchBar() {
      var bar = document.getElementById('batch-bar');
      var cnt = document.getElementById('batch-count');
      var chkAll = document.getElementById('chk-all');
      
      if (selectedPaths.length > 0) {
        bar.classList.add('on');
        cnt.textContent = selectedPaths.length + ' item terpilih';
      } else {
        bar.classList.remove('on');
      }

      if (chkAll) {
        chkAll.checked = fsItems.length > 0 && selectedPaths.length === fsItems.length;
        chkAll.indeterminate = selectedPaths.length > 0 && selectedPaths.length < fsItems.length;
      }
    }

    function toggleSelectAll(checked) {
      selectedPaths = [];
      if (checked) {
        fsItems.forEach(function(f) { selectedPaths.push(f.path); });
      }
      document.querySelectorAll('.row-chk').forEach(function(chk) {
        chk.checked = checked;
        var row = chk.closest('tr');
        if (row) row.classList.toggle('selected', checked);
      });
      updateBatchBar();
    }

    function toggleSelectRow(path, checked, evt) {
      if (evt) evt.stopPropagation();
      var idx = selectedPaths.indexOf(path);
      if (checked && idx < 0) selectedPaths.push(path);
      else if (!checked && idx >= 0) selectedPaths.splice(idx, 1);

      document.querySelectorAll('.row-chk').forEach(function(chk) {
        if (chk.getAttribute('data-path') === path) {
          chk.checked = checked;
          var row = chk.closest('tr');
          if (row) row.classList.toggle('selected', checked);
        }
      });
      updateBatchBar();
    }

    function clearSelection() {
      selectedPaths = [];
      document.querySelectorAll('.row-chk').forEach(function(chk) {
        chk.checked = false;
        var row = chk.closest('tr');
        if (row) row.classList.remove('selected');
      });
      var chkAll = document.getElementById('chk-all');
      if (chkAll) { chkAll.checked = false; chkAll.indeterminate = false; }
      updateBatchBar();
    }

    function deleteSelected() {
      if (!selectedPaths.length) return;
      var count = selectedPaths.length;
      customConfirm('Hapus ' + count + ' Item Terpilih', 'Apakah Anda yakin ingin menghapus ' + count + ' berkas/folder yang dipilih secara permanen?', '🗑 Hapus Semua (' + count + ')', true, function() {
        showLoader(true);
        var toDelete = selectedPaths.slice();
        fetch('/api/fs/batch-delete', {
          method: 'POST',
          headers: {'Content-Type':'application/json; charset=utf-8'},
          body: JSON.stringify(toDelete)
        }).then(function(r){ return r.json(); }).then(function(d) {
          showLoader(false);
          clearSelection();
          fsRefresh();
          toast('✔ ' + count + ' item berhasil dihapus!');
        }).catch(function(err) {
          showLoader(false);
          toast('❌ Gagal menghapus: ' + err.message);
        });
      });
    }

    function fsDownload(path, name, isDir) {
      toast('⏳ Mengunduh ' + (isDir ? 'folder: ' : 'berkas: ') + name);
      var url = '/api/fs/download?path=' + encodeURIComponent(path) + '&isDir=' + (isDir ? 'true' : 'false');
      var a = document.createElement('a');
      a.href = url;
      a.download = isDir ? (name + '.tar.gz') : name;
      document.body.appendChild(a);
      a.click();
      a.remove();
    }

    function downloadSelected() {
      if (!selectedPaths.length) return;
      if (selectedPaths.length === 1) {
        var p = selectedPaths[0];
        var item = fsItems.find(function(f){ return f.path === p; });
        var isD = item ? item.isDirectory : false;
        var name = item ? item.name : p.split('/').pop();
        fsDownload(p, name, isD);
        return;
      }
      var count = selectedPaths.length;
      toast('⏳ Mengompresi ' + count + ' item terpilih untuk diunduh...');
      showLoader(true);
      fetch('/api/fs/batch-download', {
        method: 'POST',
        headers: {'Content-Type':'application/json; charset=utf-8'},
        body: JSON.stringify(selectedPaths)
      }).then(function(r) { return r.blob(); }).then(function(blob) {
        showLoader(false);
        var url = window.URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = url;
        a.download = 'MYSFTP_Archive_' + Date.now() + '.tar.gz';
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);
        toast('✔ Unduhan berhasil!');
      }).catch(function(err) {
        showLoader(false);
        toast('❌ Gagal mengunduh: ' + err.message);
      });
    }

    function renderFiles() {
      var tb = document.getElementById('ftbody');
      var em = document.getElementById('empty-fs');
      tb.innerHTML = '';

      if (!connected && !fsItems.length) {
        em.style.display = 'flex';
        clearSelection();
        return;
      }
      em.style.display = 'none';

      document.getElementById('fs-info').textContent = fsPath + ' (' + fsItems.length + ' items)';
      if (curView === 'files') {
        document.getElementById('crumbs').innerHTML = '<span class=""crumb"">📁 ' + esc(fsPath) + '</span>';
      }

      fsItems.forEach(function(f) {
        var tr = document.createElement('tr');
        tr.className = 'frow';
        var isSelected = selectedPaths.indexOf(f.path) >= 0;
        if (isSelected) tr.classList.add('selected');

        var isD = f.isDirectory;
        var icon = isD ? '📁' : getFileIcon(f.name);
        var sz = isD ? '—' : fmtSize(f.size);
        var type = isD ? 'Folder' : getExt(f.name);

        tr.innerHTML = '<td style=""text-align:center;"" onclick=""event.stopPropagation();"">' +
          '<input type=""checkbox"" class=""row-chk chk-box"" data-path=""' + esc(f.path) + '"" ' + (isSelected ? 'checked' : '') + ' onchange=""toggleSelectRow(\'' + esc(f.path) + '\', this.checked, event)"">' +
          '</td>' +
          '<td><div class=""fname ' + (isD?'dir':'') + '""><span>' + icon + '</span><span>' + esc(f.name) + '</span></div></td>' +
          '<td class=""fmeta"">' + sz + '</td>' +
          '<td class=""fmeta"">' + type + '</td>' +
          '<td class=""fmeta"">' + esc(f.modified||'') + '</td>' +
          '<td><div class=""row-acts"">' +
          '<button class=""btn btn-d btn-sm"" title=""Download"" onclick=""event.stopPropagation();fsDownload(\'' + esc(f.path) + '\',\'' + esc(f.name) + '\',' + (isD?'true':'false') + ')"">📥</button>' +
          '<button class=""btn btn-danger btn-sm"" title=""Hapus"" onclick=""event.stopPropagation();fsDel(\'' + esc(f.path) + '\',\'' + esc(f.name) + '\')"">🗑</button>' +
          '</div></td>';

        tr.onclick = function(e) {
          if (e.target.tagName === 'INPUT' || e.target.tagName === 'BUTTON') return;
          if (isD) fsLoad(f.path);
          else edOpen(f.path, f.name);
        };
        tb.appendChild(tr);
      });
      updateBatchBar();
    }

    function triggerUpload() {
      var inp = document.getElementById('file-upload-input');
      inp.value = '';
      inp.click();
    }

    function triggerFolderUpload() {
      var inp = document.getElementById('folder-upload-input');
      inp.value = '';
      inp.click();
    }

    function handleFileSelected(e) {
      var files = e.target.files;
      if (!files || files.length === 0) return;
      uploadFileList(files);
    }

    function handleFolderSelected(e) {
      var files = e.target.files;
      if (!files || !files.length) return;
      var list = [];
      for (var i = 0; i < files.length; i++) {
        var f = files[i];
        var relPath = f.webkitRelativePath || f.name;
        list.push({ file: f, relativePath: relPath });
      }
      uploadFileListWithPaths(list);
    }

    function handleDragOver(e) {
      e.preventDefault();
      e.stopPropagation();
      if (curView === 'files' && connected) {
        document.getElementById('dropzone-overlay').classList.add('on');
      }
    }

    function handleDragLeave(e) {
      e.preventDefault();
      e.stopPropagation();
      var overlay = document.getElementById('dropzone-overlay');
      if (e.target === overlay || !overlay.contains(e.relatedTarget)) {
        overlay.classList.remove('on');
      }
    }

    function handleDrop(e) {
      e.preventDefault();
      e.stopPropagation();
      document.getElementById('dropzone-overlay').classList.remove('on');
      if (curView !== 'files' || !connected) return;

      var items = e.dataTransfer && e.dataTransfer.items;
      if (items && items.length > 0 && typeof items[0].webkitGetAsEntry === 'function') {
        var entries = [];
        for (var i = 0; i < items.length; i++) {
          var entry = items[i].webkitGetAsEntry();
          if (entry) entries.push(entry);
        }
        scanAndUploadEntries(entries);
      } else if (e.dataTransfer && e.dataTransfer.files) {
        uploadFileList(e.dataTransfer.files);
      }
    }

    function scanAndUploadEntries(entries) {
      var fileEntries = [];
      var pending = 0;

      function scanEntry(entry, path) {
        path = path || '';
        if (entry.isFile) {
          pending++;
          entry.file(function(file) {
            fileEntries.push({ file: file, relativePath: (path ? path + '/' : '') + file.name });
            pending--;
            if (pending === 0) uploadFileListWithPaths(fileEntries);
          }, function() {
            pending--;
            if (pending === 0) uploadFileListWithPaths(fileEntries);
          });
        } else if (entry.isDirectory) {
          pending++;
          var reader = entry.createReader();
          var readNext = function() {
            reader.readEntries(function(results) {
              if (results.length > 0) {
                results.forEach(function(child) {
                  scanEntry(child, (path ? path + '/' : '') + entry.name);
                });
                readNext();
              } else {
                pending--;
                if (pending === 0) uploadFileListWithPaths(fileEntries);
              }
            }, function() {
              pending--;
              if (pending === 0) uploadFileListWithPaths(fileEntries);
            });
          };
          readNext();
        }
      }

      if (entries.length === 0) return;
      showLoader(true);
      toast('⚡ Membaca struktur folder & berkas...');
      entries.forEach(function(entry) { scanEntry(entry, ''); });
    }

    function uploadFileListWithPaths(list) {
      if (!list || !list.length) return;
      showLoader(true);
      toast('⚡ Mengunggah ' + list.length + ' item ke ' + fsPath + ' ...');

      var i = 0, okCount = 0, failCount = 0;
      function next() {
        if (i >= list.length) {
          showLoader(false);
          fsRefresh();
          if (failCount === 0) toast('✔ ' + okCount + ' item berhasil diunggah!');
          else toast('⚠ ' + okCount + ' berhasil, ' + failCount + ' gagal diunggah.');
          return;
        }
        var item = list[i++];
        uploadOneRelativeFile(item.file, item.relativePath).then(function(res) {
          if (res.ok) okCount++;
          else { failCount++; toast('❌ Gagal: ' + item.relativePath); }
          next();
        });
      }
      next();
    }

    function uploadOneRelativeFile(file, relPath) {
      return new Promise(function(resolve) {
        var reader = new FileReader();
        reader.onload = function(evt) {
          var b64 = evt.target.result.split(',')[1];
          var base = fsPath === '/' ? '' : fsPath;
          var cleanRel = relPath.replace(/^[\\\/]+/, '');
          var remoteDest = base + '/' + cleanRel;
          fetch('/api/fs/upload', {
            method: 'POST',
            headers: {'Content-Type':'application/json; charset=utf-8'},
            body: JSON.stringify({ path: remoteDest, data: b64 })
          }).then(function(r){return r.json();}).then(function(res) {
            resolve({ ok: !!res.success, error: res.error });
          }).catch(function(err) {
            resolve({ ok: false, error: err.message });
          });
        };
        reader.onerror = function() { resolve({ ok: false, error: 'Gagal membaca berkas lokal' }); };
        reader.readAsDataURL(file);
      });
    }

    function uploadOneFile(file) {
      return uploadOneRelativeFile(file, file.name);
    }

    function uploadFileList(fileList) {
      var files = Array.prototype.slice.call(fileList);
      if (!files.length) return;
      var list = files.map(function(f) { return { file: f, relativePath: f.name }; });
      uploadFileListWithPaths(list);
    }

    function fsNew(type) {
      var isFolder = type === 'folder';
      customPrompt(
        isFolder ? 'Buat Folder Baru' : 'Buat Berkas Baru',
        isFolder ? 'Nama folder baru:' : 'Nama berkas baru:',
        isFolder ? 'Contoh: Project' : 'Contoh: app.js',
        '',
        '✨ Buat ' + (isFolder ? 'Folder' : 'Berkas'),
        function(name) {
          if (!name) return;
          var fullPath = (fsPath === '/' ? '' : fsPath) + '/' + name;
          showLoader(true);
          fetch('/api/fs/create',{
            method:'POST',
            headers:{'Content-Type':'application/json; charset=utf-8'},
            body:JSON.stringify({path:fullPath,type:type})
          }).then(function(r){return r.json();})
            .then(function(d) {
              showLoader(false);
              if (d.success) { fsRefresh(); toast(isFolder ? 'Folder dibuat!' : 'Berkas dibuat!'); }
              else toast('Gagal: ' + (d.error||'Error'));
            });
        }
      );
    }

    function fsDel(path, name) {
      customConfirm('Hapus Berkas / Folder', 'Apakah Anda yakin ingin menghapus ' + name + ' secara permanen dari server?', '🗑 Hapus', true, function() {
        showLoader(true);
        fetch('/api/fs/delete',{
          method:'POST',
          headers:{'Content-Type':'application/json; charset=utf-8'},
          body:JSON.stringify({path:path})
        }).then(function(r){return r.json();})
          .then(function(d) {
            showLoader(false);
            if (d.success) { fsRefresh(); toast('Item dihapus.'); }
            else toast('Gagal menghapus: ' + (d.error||'Error'));
          });
      });
    }

    function edOpen(path, name) {
      edFile = path;
      go('editor');
      document.getElementById('ed-tab').textContent = '📄 ' + (name || path.split('/').pop());
      document.getElementById('ed-area').value = '// Memuat berkas...';
      showLoader(true);
      fetch('/api/fs/read?path=' + encodeURIComponent(path))
        .then(function(r){return r.json();})
        .then(function(d) {
          showLoader(false);
          if (d.success) {
            document.getElementById('ed-area').value = d.content || '';
          } else {
            document.getElementById('ed-area').value = '// Gagal memuat berkas: ' + (d.error || 'Unknown error');
          }
        });
    }

    function edSave() {
      if (!edFile) { toast('Tidak ada berkas yang terbuka.'); return; }
      var content = document.getElementById('ed-area').value;
      showLoader(true);
      fetch('/api/fs/write',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({path:edFile,content:content})})
        .then(function(r){return r.json();})
        .then(function(d) {
          showLoader(false);
          if (d.success) toast('✔ Berkas berhasil disimpan ke server!');
          else toast('Gagal menyimpan: ' + (d.error||'Error'));
        });
    }

    // ── SSH Termius Terminal Engine (real persistent PTY session) ──
    // Commands are written to a single long-lived remote shell. The remote
    // shell itself echoes the input and prints its own real prompt, so what
    // you see is exactly what a real SSH terminal shows — cd, history, nano,
    // htop, pm2 logs, all work like a genuine session, not one-off commands.
    var termHistory = [];
    var termHistPos = -1;

    function tSend(cmd) {
      document.getElementById('tinp').value = cmd;
      tExec();
    }

    function tStream(cmd) {
      document.getElementById('tinp').value = cmd;
      tExec();
    }

    function tExec() {
      var inp = document.getElementById('tinp');
      var cmd = inp.value;
      if (cmd === '') return;
      inp.value = '';

      if (cmd.trim() === 'clear') { tClear(); return; }

      if (!connected) {
        tPrint('\r\n\x1b[31m[!] Belum terhubung ke server. Buka tab Profil Server lalu klik Hubungkan.\x1b[0m\r\n');
        return;
      }

      termHistory.push(cmd);
      termHistPos = termHistory.length;

      fetch('/api/terminal/exec', {
        method: 'POST',
        headers: {'Content-Type':'application/json; charset=utf-8'},
        body: JSON.stringify({ command: cmd })
      }).then(function(r){ return r.json(); }).then(function(d) {
        if (d && d.output) {
          tPrint(d.output + '\r\n');
        }
      }).catch(function(err) {
        tPrint('\r\n\x1b[31m[Error] ' + err.message + '\x1b[0m\r\n');
      });
    }

    function startTermPoll() {
      stopTermPoll();
      termStreamPoll = setInterval(function() {
        fetch('/api/terminal/poll')
          .then(function(r){return r.json();})
          .then(function(d) {
            if (d.output) tPrint(d.output);
          }).catch(function() {});
      }, 120);
    }

    function stopTermPoll() {
      if (termStreamPoll) { clearInterval(termStreamPoll); termStreamPoll = null; }
    }

    function tBreak() {
      fetch('/api/terminal/break',{method:'POST',headers:{'Content-Type':'application/json; charset=utf-8'}});
      tPrint('\r\n\x1b[1;31m^C (Proses dihentikan)\x1b[0m\r\n');
      toast('🛑 Ctrl+C terkirim (SIGINT).');
    }

    function tHistoryNav(dir) {
      if (!termHistory.length) return;
      termHistPos += dir;
      if (termHistPos < 0) termHistPos = 0;
      if (termHistPos >= termHistory.length) { termHistPos = termHistory.length; document.getElementById('tinp').value = ''; return; }
      document.getElementById('tinp').value = termHistory[termHistPos];
    }

    function tPrint(txt) {
      if (!txt) return;
      var clean = txt.replace(/Warning: Permanently added[^\r\n]*\r?\n?/g, '')
                     .replace(/bash: cannot set terminal process group[^\r\n]*\r?\n?/g, '')
                     .replace(/bash: no job control in this shell\r?\n?/g, '');
      if (!clean) return;
      var box = document.getElementById('tscreen');
      var html = parseAnsi(clean);
      box.innerHTML += html;
      box.scrollTop = box.scrollHeight;
    }

    function parseAnsi(str) {
      if (!str) return '';
      var s = str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
      
      s = s.replace(/\x1b\[\?25[hl]/g, '')
           .replace(/\x1b\[[0-9;]*[HfABCDJK]/g, '');

      s = s.replace(/\x1b\[0m/g, '</span>')
           .replace(/\x1b\[1m/g, '<span style=""font-weight:700;"">')
           .replace(/\x1b\[2m/g, '<span style=""opacity:0.6;"">')
           .replace(/\x1b\[3m/g, '<span style=""font-style:italic;"">')
           .replace(/\x1b\[4m/g, '<span style=""text-decoration:underline;"">')
           .replace(/\x1b\[30m/g, '<span style=""color:#4b5263;"">')
           .replace(/\x1b\[31m/g, '<span style=""color:#e06c75;"">')
           .replace(/\x1b\[32m/g, '<span style=""color:#73d285;"">')
           .replace(/\x1b\[33m/g, '<span style=""color:#e5c07b;"">')
           .replace(/\x1b\[34m/g, '<span style=""color:#6ab0f3;"">')
           .replace(/\x1b\[35m/g, '<span style=""color:#c678dd;"">')
           .replace(/\x1b\[36m/g, '<span style=""color:#56c8d8;"">')
           .replace(/\x1b\[37m/g, '<span style=""color:#e6e3da;"">')
           .replace(/\x1b\[90m/g, '<span style=""color:#636d83;"">')
           .replace(/\x1b\[91m/g, '<span style=""color:#f28b93;"">')
           .replace(/\x1b\[92m/g, '<span style=""color:#8be39b;"">')
           .replace(/\x1b\[93m/g, '<span style=""color:#f5d18d;"">')
           .replace(/\x1b\[94m/g, '<span style=""color:#88c3f7;"">')
           .replace(/\x1b\[95m/g, '<span style=""color:#d898ec;"">')
           .replace(/\x1b\[96m/g, '<span style=""color:#78dcee;"">')
           .replace(/\x1b\[97m/g, '<span style=""color:#ffffff;"">')
           .replace(/\x1b\[([0-9;]+)m/g, '');

      return s;
    }

    function tClear() { document.getElementById('tscreen').innerHTML = ''; }

    function toast(msg) {
      var box = document.getElementById('toasts');
      var t = document.createElement('div');
      t.className = 'toast';
      t.textContent = msg;
      box.appendChild(t);
      setTimeout(function(){ t.remove(); }, 3200);
    }

    function esc(s) {
      if (!s) return '';
      var d = document.createElement('div');
      d.appendChild(document.createTextNode(s));
      return d.innerHTML;
    }

    function getFileIcon(name) {
      var ext = (name || '').split('.').pop().toLowerCase();
      var m = {js:'📜',ts:'📘',json:'📋',html:'🌐',css:'🎨',py:'🐍',kt:'🟣',java:'☕',md:'📝',txt:'📝',sh:'⚙️',yml:'📦',yaml:'📦',xml:'📦',env:'🔐',sql:'🗄️',log:'📊',jpg:'🖼️',png:'🖼️',svg:'🖼️'};
      return m[ext] || '📄';
    }

    function getExt(name) {
      if (!name) return '';
      var parts = name.split('.');
      return parts.length > 1 ? parts.pop().toUpperCase() : 'BERKAS';
    }

    function fmtSize(bytes) {
      if (bytes == null || bytes === 0) return '0 B';
      if (bytes < 1024) return bytes + ' B';
      if (bytes < 1048576) return (bytes/1024).toFixed(1) + ' KB';
      return (bytes/1048576).toFixed(1) + ' MB';
    }
  </script>
</body>
</html>";
        #endregion
    }
}
