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
        private Process sshProcess;
        private StringBuilder outputBuffer = new StringBuilder();
        private string askpassPath;
        private string host;
        private int port;
        private string user;
        private string password;
        private bool connected;
        private object lockObj = new object();

        public bool IsConnected { get { return connected && sshProcess != null && !sshProcess.HasExited; } }

        public void Connect(string h, int p, string u, string pw)
        {
            Disconnect();
            host = h; port = p; user = u; password = pw;

            // Create askpass helper script
            askpassPath = Path.Combine(Path.GetTempPath(), "mysftp_ap_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".cmd");
            string escapedPw = pw.Replace("^", "^^").Replace("&", "^&").Replace("|", "^|")
                                 .Replace("<", "^<").Replace(">", "^>").Replace("%", "%%")
                                 .Replace("\"", "^\"");
            File.WriteAllText(askpassPath, "@echo off\r\necho " + escapedPw + "\r\n");

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = FindSshExe();
            psi.Arguments = "-o StrictHostKeyChecking=no -o UserKnownHostsFile=NUL -o ServerAliveInterval=30 -p " + port + " " + user + "@" + host;
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.EnvironmentVariables["SSH_ASKPASS"] = askpassPath;
            psi.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
            psi.EnvironmentVariables["DISPLAY"] = ":0";

            lock (lockObj) { outputBuffer.Clear(); }
            sshProcess = Process.Start(psi);

            Thread tOut = new Thread(() => ReadStream(sshProcess.StandardOutput));
            tOut.IsBackground = true;
            tOut.Start();
            Thread tErr = new Thread(() => ReadStream(sshProcess.StandardError));
            tErr.IsBackground = true;
            tErr.Start();

            connected = true;
        }

        private void ReadStream(StreamReader reader)
        {
            try
            {
                char[] buf = new char[4096];
                int n;
                while ((n = reader.Read(buf, 0, buf.Length)) > 0)
                {
                    string chunk = new string(buf, 0, n);
                    lock (lockObj) { outputBuffer.Append(chunk); }
                }
            }
            catch { }
        }

        public void SendInput(string input)
        {
            if (IsConnected)
            {
                try
                {
                    sshProcess.StandardInput.Write(input);
                    sshProcess.StandardInput.Flush();
                }
                catch { }
            }
        }

        public string GetOutput()
        {
            lock (lockObj)
            {
                string result = outputBuffer.ToString();
                outputBuffer.Clear();
                return result;
            }
        }

        public void Disconnect()
        {
            connected = false;
            if (sshProcess != null && !sshProcess.HasExited)
            {
                try { sshProcess.StandardInput.WriteLine("exit"); } catch { }
                Thread.Sleep(200);
                try { if (!sshProcess.HasExited) sshProcess.Kill(); } catch { }
            }
            sshProcess = null;
            CleanAskpass();
        }

        private void CleanAskpass()
        {
            if (!string.IsNullOrEmpty(askpassPath) && File.Exists(askpassPath))
            {
                try { File.Delete(askpassPath); } catch { }
            }
            askpassPath = null;
        }

        // Run a one-shot SSH command and return output
        public string RunOneShot(string command)
        {
            string apPath = Path.Combine(Path.GetTempPath(), "mysftp_os_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".cmd");
            string escapedPw = password.Replace("^", "^^").Replace("&", "^&").Replace("|", "^|")
                                       .Replace("<", "^<").Replace(">", "^>").Replace("%", "%%")
                                       .Replace("\"", "^\"");
            File.WriteAllText(apPath, "@echo off\r\necho " + escapedPw + "\r\n");

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = FindSshExe();
                psi.Arguments = "-o StrictHostKeyChecking=no -o UserKnownHostsFile=NUL -o ConnectTimeout=10 -p " + port + " " + user + "@" + host + " " + command;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                psi.EnvironmentVariables["SSH_ASKPASS"] = apPath;
                psi.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
                psi.EnvironmentVariables["DISPLAY"] = ":0";

                Process p = Process.Start(psi);
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(15000);
                if (!p.HasExited) try { p.Kill(); } catch { }

                return !string.IsNullOrEmpty(stdout) ? stdout : stderr;
            }
            finally
            {
                try { File.Delete(apPath); } catch { }
            }
        }

        // Run a one-shot command and pipe content to stdin
        public string RunOneShotWithStdin(string command, string stdinContent)
        {
            string apPath = Path.Combine(Path.GetTempPath(), "mysftp_ws_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".cmd");
            string escapedPw = password.Replace("^", "^^").Replace("&", "^&").Replace("|", "^|")
                                       .Replace("<", "^<").Replace(">", "^>").Replace("%", "%%")
                                       .Replace("\"", "^\"");
            File.WriteAllText(apPath, "@echo off\r\necho " + escapedPw + "\r\n");

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = FindSshExe();
                psi.Arguments = "-o StrictHostKeyChecking=no -o UserKnownHostsFile=NUL -o ConnectTimeout=10 -p " + port + " " + user + "@" + host + " " + command;
                psi.UseShellExecute = false;
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                psi.EnvironmentVariables["SSH_ASKPASS"] = apPath;
                psi.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
                psi.EnvironmentVariables["DISPLAY"] = ":0";

                Process p = Process.Start(psi);
                p.StandardInput.Write(stdinContent);
                p.StandardInput.Close();
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(15000);
                if (!p.HasExited) try { p.Kill(); } catch { }

                return !string.IsNullOrEmpty(stdout) ? stdout : stderr;
            }
            finally
            {
                try { File.Delete(apPath); } catch { }
            }
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
            return "ssh.exe"; // fall back to PATH
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
        // Store active connection info for remote operations
        private static string activeHost;
        private static int activePort;
        private static string activeUser;
        private static string activePassword;
        private static string activeProtocol;
        private static string activeName;
        private static string activeRemotePath = "/";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        static void Main(string[] args)
        {
            dataDir = AppDomain.CurrentDomain.BaseDirectory;
            profilesFile = Path.Combine(dataDir, "connections.json");

            TcpListener tcp = new TcpListener(IPAddress.Loopback, 0);
            tcp.Start();
            port = ((IPEndPoint)tcp.LocalEndpoint).Port;
            tcp.Stop();

            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
            listener.Start();

            Thread serverThread = new Thread(StartServer);
            serverThread.IsBackground = true;
            serverThread.Start();

            LaunchAppWindow("http://127.0.0.1:" + port + "/");

            if (browserProcess != null)
                browserProcess.WaitForExit();
            else
                while (isRunning) Thread.Sleep(1000);

            sshManager.Disconnect();
            try { listener.Stop(); } catch { }
        }

        private static void LaunchAppWindow(string url)
        {
            string edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
            if (!File.Exists(edgePath)) edgePath = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
            string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (!File.Exists(chromePath)) chromePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";

            string browser = File.Exists(edgePath) ? edgePath : (File.Exists(chromePath) ? chromePath : null);
            string userProfile = Path.Combine(Path.GetTempPath(), "MYSFTP_App_" + port);

            if (browser != null)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = browser;
                psi.Arguments = "--app=\"" + url + "\" --window-size=1300,850 --user-data-dir=\"" + userProfile + "\"";
                psi.UseShellExecute = false;
                browserProcess = Process.Start(psi);
            }
            else
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
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
                catch { break; }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest req = context.Request;
            HttpListenerResponse res = context.Response;

            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 200; res.Close(); return;
            }

            string path = req.Url.AbsolutePath;

            try
            {
                if (path == "/" || path == "/index.html")
                {
                    byte[] buf = Encoding.UTF8.GetBytes(HtmlUi);
                    res.ContentType = "text/html; charset=utf-8";
                    res.ContentLength64 = buf.Length;
                    res.OutputStream.Write(buf, 0, buf.Length);
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
                    File.WriteAllText(profilesFile, body, Encoding.UTF8);
                    SendJson(res, "{\"success\":true}");
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
                    activeRemotePath = "/";

                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost) && !string.IsNullOrEmpty(activePassword))
                    {
                        // Start interactive SSH session for terminal
                        sshManager.Connect(activeHost, activePort, activeUser, activePassword);
                        Thread.Sleep(1500); // Wait for SSH handshake

                        // Test connection by listing root
                        string testOut = sshManager.GetOutput();
                        bool hasError = testOut.Contains("Permission denied") || testOut.Contains("Connection refused") || testOut.Contains("No route to host");

                        if (hasError)
                        {
                            sshManager.Disconnect();
                            SendJson(res, "{\"success\":false,\"error\":\"" + EscapeJson(testOut) + "\"}");
                        }
                        else
                        {
                            SendJson(res, "{\"success\":true,\"protocol\":\"SFTP\",\"name\":\"" + EscapeJson(activeName) + "\",\"banner\":\"" + EscapeJson(testOut) + "\"}");
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
                else if (path == "/api/terminal/output")
                {
                    string output = sshManager.GetOutput();
                    SendJson(res, "{\"output\":\"" + EscapeJson(output) + "\"}");
                }
                else if (path == "/api/terminal/input" && req.HttpMethod == "POST")
                {
                    string body = ReadBody(req);
                    string input = ExtractVal(body, "input");
                    sshManager.SendInput(input + "\n");
                    SendJson(res, "{\"success\":true}");
                }
                else if (path == "/api/fs/list")
                {
                    string dir = req.QueryString["path"] ?? "/";
                    if (activeProtocol == "SFTP" && !string.IsNullOrEmpty(activeHost))
                    {
                        SendJson(res, ListRemoteDir(dir));
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
                    new Thread(() => { Thread.Sleep(500); Environment.Exit(0); }).Start();
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

        // ── Remote SFTP operations via SSH ──

        private static string ListRemoteDir(string dir)
        {
            try
            {
                if (string.IsNullOrEmpty(dir) || dir == ".") dir = "/root";
                string raw = sshManager.RunOneShot("\"ls -la --time-style=long-iso " + EscapeShell(dir) + " 2>&1\"");
                if (raw.Contains("No such file") || raw.Contains("Permission denied"))
                    return "{\"success\":false,\"error\":\"" + EscapeJson(raw.Trim()) + "\"}";

                List<string> items = new List<string>();
                string[] lines = raw.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (line.StartsWith("total ") || line.Length < 10) continue;

                    // Parse: drwxr-xr-x 2 root root 4096 2026-08-25 03:00 dirname
                    char firstChar = line[0];
                    bool isDir = firstChar == 'd' || firstChar == 'l';

                    // Split by whitespace
                    string[] parts = System.Text.RegularExpressions.Regex.Split(line, @"\s+");
                    if (parts.Length < 8) continue;

                    string name = "";
                    // Name is everything after the date+time (index 5=date, 6=time, 7+ = name)
                    for (int i = 7; i < parts.Length; i++)
                    {
                        if (i > 7) name += " ";
                        name += parts[i];
                    }
                    // Strip symlink target
                    int arrowIdx = name.IndexOf(" -> ");
                    if (arrowIdx >= 0) name = name.Substring(0, arrowIdx);

                    if (name == "." || name == "..") continue;

                    string size = parts[4];
                    string modified = parts[5] + " " + parts[6];
                    string fullPath = dir.TrimEnd('/') + "/" + name;

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
                string content = sshManager.RunOneShot("\"cat " + EscapeShell(path) + " 2>&1\"");
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
                sshManager.RunOneShotWithStdin("\"cat > " + EscapeShell(path) + "\"", content);
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
                sshManager.RunOneShot("\"rm -rf " + EscapeShell(path) + " 2>&1\"");
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
                    sshManager.RunOneShot("\"mkdir -p " + EscapeShell(path) + " 2>&1\"");
                else
                    sshManager.RunOneShot("\"touch " + EscapeShell(path) + " 2>&1\"");
                return "{\"success\":true}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        // ── Local file operations ──

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

        // ── Utilities ──

        private static string PingHost(string host, int p)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var result = client.BeginConnect(host, p, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                    sw.Stop();
                    if (success) return "{\"online\":true,\"latency\":" + sw.ElapsedMilliseconds + "}";
                }
            }
            catch { }
            return "{\"online\":false,\"latency\":0}";
        }

        private static string ReadBody(HttpListenerRequest req)
        {
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
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
        }

        private static string ExtractVal(string json, string key)
        {
            string search = "\"" + key + "\":\"";
            int idx = json.IndexOf(search);
            if (idx >= 0)
            {
                int start = idx + search.Length;
                // Handle escaped quotes
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
            // Try numeric/boolean
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

        #region Embedded HTML UI
        private const string HtmlUi = @"<!DOCTYPE html>
<html lang=""id"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>MYSFTP v1.6.0 — Desktop Luxury Client</title>
  <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
  <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
  <link href=""https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500;700&family=Outfit:wght@500;600;700;800&display=swap"" rel=""stylesheet"">
  <style>
    :root {
      --bg-base: #08090b;
      --bg-surface: #0f1014;
      --bg-card: #14151a;
      --bg-card-hover: #1b1c22;
      --bg-input: #0b0c0f;
      --border: rgba(255,255,255,0.06);
      --border-gold: rgba(205,189,148,0.3);
      --gold: #cdbd94;
      --gold-light: #e6dcbe;
      --gold-glow: rgba(205,189,148,0.15);
      --text: #eae6da;
      --text-dim: #7e7b73;
      --green: #6fcf7f;
      --red: #e06c75;
      --blue: #61afef;
      --cyan: #56c8d8;
      --r-sm: 8px; --r-md: 12px; --r-lg: 18px;
    }
    *{margin:0;padding:0;box-sizing:border-box;-webkit-font-smoothing:antialiased;}
    body,html{background:var(--bg-base);color:var(--text);font-family:'Inter',system-ui,sans-serif;font-size:13.5px;height:100%;overflow:hidden;}
    ::selection{background:var(--gold);color:#111;}
    ::-webkit-scrollbar{width:6px;height:6px;}
    ::-webkit-scrollbar-track{background:transparent;}
    ::-webkit-scrollbar-thumb{background:rgba(255,255,255,0.1);border-radius:3px;}
    ::-webkit-scrollbar-thumb:hover{background:rgba(255,255,255,0.2);}

    #root{display:flex;height:100vh;width:100vw;}

    /* ── Sidebar ── */
    .sb{width:220px;background:var(--bg-surface);border-right:1px solid var(--border);display:flex;flex-direction:column;flex-shrink:0;}
    .sb-brand{height:56px;padding:0 16px;display:flex;align-items:center;gap:10px;border-bottom:1px solid var(--border);}
    .sb-logo{width:30px;height:30px;border-radius:8px;background:linear-gradient(135deg,#cdbd94,#96865c);color:#111;display:flex;align-items:center;justify-content:center;font-family:'Outfit';font-weight:800;font-size:14px;}
    .sb-info{display:flex;flex-direction:column;}
    .sb-name{font-family:'Outfit';font-weight:800;font-size:14px;color:var(--gold-light);letter-spacing:.3px;}
    .sb-ver{font-size:9px;color:var(--text-dim);font-weight:600;text-transform:uppercase;letter-spacing:.5px;}
    .sb-nav{flex:1;padding:12px 8px;display:flex;flex-direction:column;gap:2px;overflow-y:auto;}
    .sb-cat{font-size:9.5px;font-weight:700;text-transform:uppercase;color:var(--text-dim);padding:12px 10px 4px;letter-spacing:.8px;}
    .sb-btn{display:flex;align-items:center;gap:10px;padding:9px 12px;border-radius:var(--r-sm);color:var(--text-dim);cursor:pointer;font-weight:600;font-size:12.5px;transition:all .15s;border:1px solid transparent;}
    .sb-btn:hover{background:var(--bg-card);color:var(--text);}
    .sb-btn.on{background:rgba(205,189,148,0.1);color:var(--gold-light);border-color:var(--border-gold);}
    .sb-btn .ic{font-size:15px;width:20px;text-align:center;}
    .sb-foot{padding:12px 14px;border-top:1px solid var(--border);display:flex;align-items:center;gap:8px;}
    .pulse{width:7px;height:7px;border-radius:50%;background:var(--green);box-shadow:0 0 6px var(--green);animation:pulse 2s infinite;}
    @keyframes pulse{0%,100%{opacity:1;}50%{opacity:.4;}}
    .sb-status{font-size:11px;font-weight:600;color:var(--text-dim);flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;}

    /* ── Main ── */
    .main{flex:1;display:flex;flex-direction:column;overflow:hidden;min-width:0;}
    .topbar{height:48px;background:var(--bg-surface);border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;padding:0 20px;flex-shrink:0;}
    .crumbs{display:flex;align-items:center;gap:6px;font-family:'JetBrains Mono';font-size:12px;color:var(--gold-light);font-weight:500;}
    .crumb{background:var(--bg-card);border:1px solid var(--border);padding:3px 8px;border-radius:5px;cursor:pointer;transition:border-color .15s;}
    .crumb:hover{border-color:var(--gold);}

    .btn{display:inline-flex;align-items:center;justify-content:center;gap:6px;padding:7px 14px;font-size:12px;font-weight:700;border-radius:var(--r-sm);border:none;cursor:pointer;font-family:'Inter';transition:all .15s;}
    .btn-g{background:linear-gradient(135deg,#cdbd94,#b5a477);color:#111;box-shadow:0 2px 10px rgba(205,189,148,.2);}
    .btn-g:hover{filter:brightness(1.12);transform:translateY(-1px);}
    .btn-d{background:var(--bg-card);color:var(--text);border:1px solid var(--border);}
    .btn-d:hover{background:var(--bg-card-hover);border-color:rgba(255,255,255,.15);}
    .btn-danger{background:rgba(224,108,117,.15);color:var(--red);border:1px solid rgba(224,108,117,.25);}
    .btn-danger:hover{background:rgba(224,108,117,.25);}
    .btn-sm{padding:5px 10px;font-size:11px;}

    /* ── Pages ── */
    .stage{flex:1;position:relative;overflow:hidden;}
    .page{position:absolute;inset:0;display:none;flex-direction:column;overflow-y:auto;padding:20px;}
    .page.on{display:flex;}

    /* ── Connection page ── */
    .sec-title{font-family:'Outfit';font-size:20px;font-weight:800;color:var(--text);margin-bottom:3px;}
    .sec-sub{font-size:12.5px;color:var(--text-dim);margin-bottom:16px;}
    .cards{display:grid;grid-template-columns:repeat(auto-fill,minmax(300px,1fr));gap:14px;}
    .card{background:var(--bg-card);border:1px solid var(--border);border-radius:var(--r-md);padding:16px;display:flex;flex-direction:column;gap:10px;transition:all .2s;position:relative;}
    .card:hover{border-color:var(--border-gold);box-shadow:0 8px 24px rgba(0,0,0,.3);transform:translateY(-2px);}
    .card-row{display:flex;justify-content:space-between;align-items:center;}
    .tag{font-size:10px;font-weight:800;font-family:'JetBrains Mono';padding:2px 7px;border-radius:5px;background:rgba(205,189,148,.1);color:var(--gold-light);border:1px solid var(--border-gold);}
    .card-name{font-family:'Outfit';font-size:16px;font-weight:700;color:var(--gold-light);}
    .card-ep{font-family:'JetBrains Mono';font-size:11.5px;color:var(--text-dim);}
    .card-acts{display:flex;gap:8px;margin-top:4px;padding-top:10px;border-top:1px solid var(--border);}
    .card-ping{font-family:'JetBrains Mono';font-size:10.5px;color:var(--text-dim);margin-top:2px;}

    /* ── Empty state ── */
    .empty-state{display:flex;flex-direction:column;align-items:center;justify-content:center;flex:1;gap:12px;opacity:.7;padding:40px 0;}
    .empty-icon{font-size:48px;opacity:.5;}
    .empty-text{font-size:14px;color:var(--text-dim);text-align:center;max-width:320px;}

    /* ── File table ── */
    .toolbar{display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;background:var(--bg-card);padding:8px 12px;border-radius:var(--r-sm);border:1px solid var(--border);}
    .ftbl{width:100%;border-collapse:collapse;background:var(--bg-card);border-radius:var(--r-md);overflow:hidden;border:1px solid var(--border);}
    .ftbl th{text-align:left;padding:10px 14px;font-size:10.5px;font-weight:700;text-transform:uppercase;color:var(--text-dim);border-bottom:1px solid var(--border);background:rgba(0,0,0,.2);}
    .ftbl td{padding:10px 14px;border-bottom:1px solid rgba(255,255,255,.02);font-size:12.5px;}
    .frow{cursor:pointer;transition:background .12s;}
    .frow:hover{background:var(--bg-card-hover);}
    .fname{display:flex;align-items:center;gap:8px;font-weight:600;color:var(--text);}
    .fname.dir{color:var(--gold-light);}
    .fmeta{font-family:'JetBrains Mono';color:var(--text-dim);font-size:11.5px;}
    .frow .del-btn{opacity:0;transition:opacity .15s;}
    .frow:hover .del-btn{opacity:1;}

    /* ── Editor ── */
    .editor-wrap{flex:1;display:flex;flex-direction:column;background:var(--bg-input);border:1px solid var(--border);border-radius:var(--r-md);overflow:hidden;}
    .editor-bar{height:40px;background:var(--bg-card);border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;padding:0 12px;flex-shrink:0;}
    .editor-tab{padding:4px 10px;background:var(--bg-input);border:1px solid var(--border-gold);border-radius:5px;color:var(--gold-light);font-weight:600;font-size:11.5px;font-family:'JetBrains Mono';}
    .code-area{flex:1;width:100%;background:transparent;color:#d8d4c6;caret-color:var(--gold);font-family:'JetBrains Mono';font-size:13px;line-height:1.65;padding:14px;border:none;outline:none;resize:none;white-space:pre;tab-size:2;}

    /* ── Terminal ── */
    .term-wrap{flex:1;display:flex;flex-direction:column;background:#020203;border:1px solid var(--border);border-radius:var(--r-md);overflow:hidden;}
    .term-bar{height:38px;background:var(--bg-card);border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;padding:0 12px;flex-shrink:0;}
    .term-title{font-family:'JetBrains Mono';font-weight:700;font-size:12px;color:var(--gold-light);}
    .chips{display:flex;gap:4px;overflow-x:auto;flex-shrink:0;}
    .chip{background:rgba(255,255,255,.05);border:1px solid var(--border);border-radius:5px;padding:2px 7px;font-family:'JetBrains Mono';font-size:10.5px;color:var(--gold-light);cursor:pointer;transition:all .12s;white-space:nowrap;}
    .chip:hover{background:var(--gold);color:#111;}
    .term-screen{flex:1;padding:14px;font-family:'JetBrains Mono';font-size:12.5px;color:#c8c4b8;overflow-y:auto;white-space:pre-wrap;word-break:break-all;line-height:1.55;user-select:text;}
    .term-input-row{display:flex;align-items:center;gap:8px;padding:8px 12px;background:var(--bg-card);border-top:1px solid var(--border);flex-shrink:0;}
    .term-prompt{font-family:'JetBrains Mono';font-weight:700;color:var(--green);font-size:12px;white-space:nowrap;}
    .term-inp{flex:1;background:transparent;border:none;outline:none;font-family:'JetBrains Mono';font-size:12.5px;color:var(--gold-light);}

    /* ── Modal ── */
    .overlay{position:fixed;inset:0;background:rgba(0,0,0,.7);backdrop-filter:blur(8px);display:none;align-items:center;justify-content:center;z-index:999;}
    .overlay.on{display:flex;}
    .modal{background:var(--bg-card);border:1px solid var(--border-gold);border-radius:var(--r-lg);width:100%;max-width:460px;box-shadow:0 20px 50px rgba(0,0,0,.5);overflow:hidden;}
    .modal-hd{padding:16px 20px;border-bottom:1px solid var(--border);display:flex;justify-content:space-between;align-items:center;}
    .modal-hd h3{font-family:'Outfit';font-size:17px;font-weight:800;color:var(--gold-light);}
    .modal-bd{padding:18px 20px;display:flex;flex-direction:column;gap:12px;}
    .lbl{font-size:11px;font-weight:700;color:var(--text-dim);text-transform:uppercase;letter-spacing:.4px;margin-bottom:3px;}
    .inp{width:100%;background:var(--bg-input);border:1px solid var(--border);border-radius:var(--r-sm);padding:9px 11px;color:var(--text);font-size:13px;outline:none;transition:border .2s;font-family:inherit;}
    .inp:focus{border-color:var(--gold);box-shadow:0 0 0 2px var(--gold-glow);}
    select.inp{appearance:none;cursor:pointer;}
    .modal-ft{padding:12px 20px;background:rgba(0,0,0,.15);border-top:1px solid var(--border);display:flex;justify-content:flex-end;gap:8px;}
    .row2{display:grid;grid-template-columns:1fr 1fr;gap:10px;}

    /* ── Toast ── */
    #toasts{position:fixed;top:14px;right:14px;z-index:9999;display:flex;flex-direction:column;gap:6px;pointer-events:none;}
    .toast{background:var(--bg-card);border:1px solid var(--border-gold);border-radius:var(--r-sm);padding:10px 16px;color:var(--gold-light);font-weight:600;font-size:12.5px;box-shadow:0 6px 20px rgba(0,0,0,.4);animation:tIn .25s ease;pointer-events:auto;}
    @keyframes tIn{from{transform:translateX(40px);opacity:0;}to{transform:none;opacity:1;}}

    /* ── Connect status bar ── */
    .conn-bar{height:32px;background:rgba(111,207,127,.08);border-bottom:1px solid rgba(111,207,127,.15);display:flex;align-items:center;justify-content:space-between;padding:0 20px;font-size:11.5px;font-weight:600;color:var(--green);flex-shrink:0;display:none;}
    .conn-bar.on{display:flex;}
  </style>
</head>
<body>
  <div id=""root"">
    <!-- Sidebar -->
    <aside class=""sb"">
      <div class=""sb-brand"">
        <div class=""sb-logo"">M</div>
        <div class=""sb-info"">
          <span class=""sb-name"">MYSFTP</span>
          <span class=""sb-ver"">v1.6.0 • Desktop Client</span>
        </div>
      </div>
      <div class=""sb-nav"">
        <div class=""sb-cat"">Koneksi</div>
        <div class=""sb-btn on"" data-v=""conn"" onclick=""go('conn')""><span class=""ic"">⚡</span>Server Profiles</div>
        <div class=""sb-cat"">Remote</div>
        <div class=""sb-btn"" data-v=""files"" onclick=""go('files')""><span class=""ic"">📁</span>File Explorer</div>
        <div class=""sb-btn"" data-v=""editor"" onclick=""go('editor')""><span class=""ic"">✏️</span>Code Editor</div>
        <div class=""sb-btn"" data-v=""term"" onclick=""go('term')""><span class=""ic"">💻</span>SSH Terminal</div>
      </div>
      <div class=""sb-foot"">
        <div class=""pulse"" id=""pulse-dot""></div>
        <span class=""sb-status"" id=""sb-lbl"">Tidak terhubung</span>
      </div>
    </aside>

    <!-- Main -->
    <div class=""main"">
      <div class=""conn-bar"" id=""conn-bar"">
        <span id=""conn-bar-text"">● Terhubung ke server</span>
        <button class=""btn btn-sm btn-danger"" onclick=""doDisconnect()"">Putuskan Koneksi</button>
      </div>

      <header class=""topbar"">
        <div class=""crumbs"" id=""crumbs""><span class=""crumb"">⚡ Server Profiles</span></div>
        <div style=""display:flex;gap:8px;"" id=""top-actions""></div>
      </header>

      <div class=""stage"">
        <!-- Connections -->
        <section class=""page on"" id=""p-conn"">
          <div style=""display:flex;justify-content:space-between;align-items:flex-end;margin-bottom:16px;"">
            <div>
              <h1 class=""sec-title"">Server Profiles</h1>
              <p class=""sec-sub"">Kelola koneksi SFTP/SSH ke server remote kamu.</p>
            </div>
            <button class=""btn btn-g"" onclick=""openModal()"">+ Tambah Server</button>
          </div>
          <div class=""cards"" id=""cards""></div>
          <div class=""empty-state"" id=""empty-conn"" style=""display:none;"">
            <div class=""empty-icon"">🖥️</div>
            <div class=""empty-text"">Belum ada profil server.<br>Klik <strong>+ Tambah Server</strong> untuk memulai koneksi SFTP pertamamu.</div>
          </div>
        </section>

        <!-- File Explorer -->
        <section class=""page"" id=""p-files"">
          <div class=""toolbar"">
            <div style=""display:flex;gap:6px;"">
              <button class=""btn btn-d btn-sm"" onclick=""fsUp()"">▲ Parent</button>
              <button class=""btn btn-d btn-sm"" onclick=""fsRefresh()"">↻ Refresh</button>
              <button class=""btn btn-d btn-sm"" onclick=""fsNew('file')"">+ File</button>
              <button class=""btn btn-d btn-sm"" onclick=""fsNew('folder')"">+ Folder</button>
            </div>
            <span id=""fs-info"" class=""fmeta""></span>
          </div>
          <table class=""ftbl"">
            <thead><tr><th style=""width:50%"">Nama</th><th style=""width:12%"">Ukuran</th><th style=""width:13%"">Tipe</th><th style=""width:15%"">Diubah</th><th style=""width:10%""></th></tr></thead>
            <tbody id=""ftbody""></tbody>
          </table>
          <div class=""empty-state"" id=""empty-fs"" style=""display:none;"">
            <div class=""empty-icon"">📂</div>
            <div class=""empty-text"">Hubungkan ke server terlebih dahulu untuk melihat file remote.</div>
          </div>
        </section>

        <!-- Editor -->
        <section class=""page"" id=""p-editor"" style=""padding:14px;"">
          <div class=""editor-wrap"">
            <div class=""editor-bar"">
              <div class=""editor-tab"" id=""ed-tab"">Belum ada file terbuka</div>
              <div style=""display:flex;gap:6px;"">
                <button class=""btn btn-g btn-sm"" onclick=""edSave()"">💾 Simpan (Ctrl+S)</button>
              </div>
            </div>
            <textarea class=""code-area"" id=""ed-area"" spellcheck=""false"" placeholder=""// Buka file dari File Explorer untuk mulai mengedit...""></textarea>
          </div>
        </section>

        <!-- Terminal -->
        <section class=""page"" id=""p-term"" style=""padding:14px;"">
          <div class=""term-wrap"">
            <div class=""term-bar"">
              <span class=""term-title"">💻 SSH Terminal</span>
              <div class=""chips"">
                <span class=""chip"" onclick=""tSend('ls -la')"">ls -la</span>
                <span class=""chip"" onclick=""tSend('df -h')"">df -h</span>
                <span class=""chip"" onclick=""tSend('free -m')"">free -m</span>
                <span class=""chip"" onclick=""tSend('pm2 status')"">pm2 status</span>
                <span class=""chip"" onclick=""tSend('top -bn1 | head -20')"">top</span>
                <span class=""chip"" onclick=""tSend('uptime')"">uptime</span>
                <span class=""chip"" onclick=""tClear()"">Clear</span>
              </div>
            </div>
            <div class=""term-screen"" id=""tscreen""></div>
            <div class=""term-input-row"">
              <span class=""term-prompt"" id=""tprompt"">$</span>
              <input type=""text"" class=""term-inp"" id=""tinp"" placeholder=""Ketik perintah..."" autocomplete=""off"">
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
          <button type=""submit"" class=""btn btn-g"">💾 Simpan</button>
        </div>
      </form>
    </div>
  </div>

  <!-- Connect modal (enter password) -->
  <div class=""overlay"" id=""ov-connect"">
    <div class=""modal"">
      <div class=""modal-hd"">
        <h3 id=""connect-title"">Hubungkan ke Server</h3>
        <button class=""btn btn-d btn-sm"" onclick=""closeOv('ov-connect')"">✕</button>
      </div>
      <form onsubmit=""event.preventDefault();doConnect();"">
        <div class=""modal-bd"">
          <div id=""connect-info"" style=""font-size:12.5px;color:var(--text-dim);""></div>
          <div>
            <div class=""lbl"">Password</div>
            <input type=""password"" id=""c-pass"" class=""inp"" placeholder=""Masukkan password server..."" required autofocus>
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

  <div id=""toasts""></div>

  <script>
    // ── State ──
    var profiles = [];
    var curView = 'conn';
    var connected = false;
    var connProfile = null;
    var fsPath = '/';
    var fsItems = [];
    var edFile = null;
    var termPoll = null;

    // ── Init ──
    window.onload = function() {
      loadProfiles();
      document.getElementById('tinp').addEventListener('keydown', function(e) {
        if (e.key === 'Enter') tExec();
      });
      window.addEventListener('keydown', function(e) {
        if (e.ctrlKey && e.key === 's') { e.preventDefault(); edSave(); }
      });
    };

    // ── Navigation ──
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
        cr.innerHTML = '<span class=""crumb"">⚡ Server Profiles</span>';
        acts.innerHTML = '<button class=""btn btn-g btn-sm"" onclick=""openModal()"">+ Tambah</button>';
      } else if (v === 'files') {
        cr.innerHTML = '<span class=""crumb"">📁 ' + esc(fsPath) + '</span>';
      } else if (v === 'editor') {
        cr.innerHTML = '<span class=""crumb"">✏️ ' + esc(edFile || 'Code Editor') + '</span>';
        acts.innerHTML = '<button class=""btn btn-g btn-sm"" onclick=""edSave()"">💾 Simpan</button>';
      } else if (v === 'term') {
        cr.innerHTML = '<span class=""crumb"">💻 SSH Terminal</span>';
      }
    }

    // ── Profiles ──
    function loadProfiles() {
      fetch('/api/profiles').then(function(r){return r.json();}).then(function(d) {
        profiles = Array.isArray(d) ? d : [];
        renderCards();
      });
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
        if (isConn) c.style.borderColor = 'rgba(111,207,127,.4)';
        c.innerHTML = '<div class=""card-row""><span class=""tag"">' + esc(p.protocol||'SFTP') + '</span>' +
          (isConn ? '<span style=""font-size:10px;color:var(--green);font-weight:700;"">● TERHUBUNG</span>' : '') +
          '</div>' +
          '<div class=""card-name"">' + esc(p.name) + '</div>' +
          '<div class=""card-ep"">' + esc(p.username||'') + '@' + esc(p.host||'') + ':' + (p.port||22) + '</div>' +
          '<div class=""card-ping"" id=""ping-' + p.id + '""></div>' +
          '<div class=""card-acts"">' +
          '<button class=""btn btn-d btn-sm"" style=""flex:1;"" onclick=""pingServer(\'' + esc(p.host) + '\',' + (p.port||22) + ',\'' + p.id + '\')"">⚡ Ping</button>' +
          '<button class=""btn btn-g btn-sm"" style=""flex:2;"" onclick=""promptConnect(\'' + p.id + '\')"">🚀 Hubungkan</button>' +
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
      fetch('/api/profiles', {
        method: 'POST',
        headers: {'Content-Type':'application/json'},
        body: JSON.stringify(profiles)
      }).then(function() {
        closeModal();
        renderCards();
        toast('Profil server berhasil disimpan!');
      });
    }

    function delProfile(id) {
      if (!confirm('Hapus profil ini?')) return;
      profiles = profiles.filter(function(x){return x.id!==id;});
      fetch('/api/profiles',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(profiles)}).then(function(){
        renderCards();
        toast('Profil dihapus.');
      });
    }

    function pingServer(host, port, id) {
      var el = document.getElementById('ping-' + id);
      if (el) el.innerHTML = '<span style=""color:var(--text-dim)"">Pinging...</span>';
      fetch('/api/ping?host=' + encodeURIComponent(host) + '&port=' + port)
        .then(function(r){return r.json();})
        .then(function(d) {
          if (el) {
            if (d.online) el.innerHTML = '<span style=""color:var(--green)"">● Online — ' + d.latency + 'ms</span>';
            else el.innerHTML = '<span style=""color:var(--red)"">● Offline / Port tertutup</span>';
          }
        });
    }

    // ── Connect flow ──
    var pendingConnId = null;

    function promptConnect(id) {
      var p = profiles.find(function(x){return x.id===id;});
      if (!p) return;

      if (p.protocol === 'LOCAL') {
        connProfile = p;
        connected = true;
        document.getElementById('sb-lbl').textContent = p.name;
        document.getElementById('conn-bar').classList.add('on');
        document.getElementById('conn-bar-text').textContent = '● Terhubung — ' + p.name + ' (Local)';
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

      fetch('/api/connect', {
        method: 'POST',
        headers: {'Content-Type':'application/json'},
        body: JSON.stringify({
          name: p.name,
          protocol: p.protocol,
          host: p.host,
          port: p.port,
          username: p.username,
          password: pw
        })
      }).then(function(r){return r.json();}).then(function(res) {
        btn.innerHTML = '🚀 Hubungkan';
        btn.disabled = false;

        if (res.success) {
          // Update password in profile
          p.password = pw;
          fetch('/api/profiles',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(profiles)});

          connected = true;
          connProfile = p;
          closeOv('ov-connect');

          document.getElementById('sb-lbl').textContent = p.name;
          document.getElementById('conn-bar').classList.add('on');
          document.getElementById('conn-bar-text').textContent = '● Terhubung ke ' + p.name + ' (' + p.host + ')';
          document.getElementById('tprompt').textContent = p.username + '@' + p.host + ':~$';
          document.getElementById('pulse-dot').style.background = 'var(--green)';

          renderCards();
          go('files');
          fsLoad(p.protocol === 'LOCAL' ? '/' : '/root');

          // Start terminal output polling
          startTermPoll();

          toast('Berhasil terhubung ke ' + p.name + '!');
        } else {
          var errEl = document.getElementById('connect-error');
          errEl.textContent = '❌ Gagal terhubung: ' + (res.error || 'Periksa kembali kredensial Anda');
          errEl.style.display = 'block';
        }
      }).catch(function(err) {
        btn.innerHTML = '🚀 Hubungkan';
        btn.disabled = false;
        document.getElementById('connect-error').textContent = '❌ Network error: ' + err.message;
        document.getElementById('connect-error').style.display = 'block';
      });
    }

    function doDisconnect() {
      fetch('/api/disconnect',{method:'POST'}).then(function() {
        connected = false;
        connProfile = null;
        stopTermPoll();
        document.getElementById('sb-lbl').textContent = 'Tidak terhubung';
        document.getElementById('conn-bar').classList.remove('on');
        document.getElementById('pulse-dot').style.background = 'var(--red)';
        document.getElementById('tprompt').textContent = '$';
        document.getElementById('tscreen').innerHTML = '';
        renderCards();
        go('conn');
        toast('Koneksi diputus.');
      });
    }

    // ── File Explorer ──
    function fsLoad(path) {
      if (!path) path = '/';
      fetch('/api/fs/list?path=' + encodeURIComponent(path))
        .then(function(r){return r.json();})
        .then(function(d) {
          if (d.success) {
            fsPath = d.currentPath;
            fsItems = d.items || [];
            renderFiles();
          } else {
            toast('Error: ' + (d.error || 'Gagal memuat direktori'));
          }
        });
    }

    function fsRefresh() { fsLoad(fsPath); }

    function fsUp() {
      var p = fsPath;
      if (p === '/') return;
      var idx = p.lastIndexOf('/');
      if (idx <= 0) fsLoad('/');
      else fsLoad(p.substring(0, idx));
    }

    function renderFiles() {
      var tb = document.getElementById('ftbody');
      var em = document.getElementById('empty-fs');
      tb.innerHTML = '';

      if (!connected && !fsItems.length) {
        em.style.display = 'flex';
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
        var isD = f.isDirectory;
        var icon = isD ? '📁' : getFileIcon(f.name);
        var sz = isD ? '—' : fmtSize(f.size);
        var type = isD ? 'Folder' : getExt(f.name);

        tr.innerHTML = '<td><div class=""fname ' + (isD?'dir':'') + '""><span>' + icon + '</span><span>' + esc(f.name) + '</span></div></td>' +
          '<td class=""fmeta"">' + sz + '</td>' +
          '<td class=""fmeta"">' + type + '</td>' +
          '<td class=""fmeta"">' + esc(f.modified||'') + '</td>' +
          '<td><button class=""btn btn-danger btn-sm del-btn"" onclick=""event.stopPropagation();fsDel(\'' + escAttr(f.path) + '\',\'' + esc(f.name) + '\')"">🗑</button></td>';

        tr.onclick = function() {
          if (isD) fsLoad(f.path);
          else edOpen(f.path, f.name);
        };
        tb.appendChild(tr);
      });
    }

    function fsNew(type) {
      var name = prompt(type === 'folder' ? 'Nama folder baru:' : 'Nama file baru:');
      if (!name) return;
      var fullPath = fsPath.replace(/\/$/,'') + '/' + name;
      fetch('/api/fs/create',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({path:fullPath,type:type})})
        .then(function(r){return r.json();})
        .then(function(d) {
          if (d.success) { fsRefresh(); toast(type==='folder' ? 'Folder dibuat!' : 'File dibuat!'); }
          else toast('Error: ' + (d.error || 'Gagal'));
        });
    }

    function fsDel(path, name) {
      if (!confirm('Hapus ' + name + '?')) return;
      fetch('/api/fs/delete',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({path:path})})
        .then(function(r){return r.json();})
        .then(function(d) {
          if (d.success) { fsRefresh(); toast('Berhasil dihapus!'); }
          else toast('Error: ' + (d.error || 'Gagal menghapus'));
        });
    }

    // Editor
    function edOpen(path, name) {
      edFile = path;
      go('editor');
      document.getElementById('ed-tab').textContent = String.fromCodePoint(0x1F4C4) + ' ' + (name || path.split('/').pop());
      document.getElementById('ed-area').value = 'Memuat file...';
      fetch('/api/fs/read?path=' + encodeURIComponent(path))
        .then(function(r){return r.json();})
        .then(function(d) {
          if (d.success) {
            document.getElementById('ed-area').value = d.content || '';
          } else {
            document.getElementById('ed-area').value = '// Error: ' + (d.error || 'Gagal memuat file');
          }
        });
    }

    function edSave() {
      if (!edFile) { toast('Tidak ada file yang terbuka.'); return; }
      var content = document.getElementById('ed-area').value;
      fetch('/api/fs/write',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({path:edFile,content:content})})
        .then(function(r){return r.json();})
        .then(function(d) {
          if (d.success) toast('File berhasil disimpan!');
          else toast('Error: ' + (d.error || 'Gagal menyimpan'));
        });
    }

    // Terminal
    function startTermPoll() {
      stopTermPoll();
      termPoll = setInterval(function() {
        fetch('/api/terminal/output')
          .then(function(r){return r.json();})
          .then(function(d) {
            if (d.output) tPrint(d.output);
          });
      }, 300);
    }

    function stopTermPoll() {
      if (termPoll) { clearInterval(termPoll); termPoll = null; }
    }

    function tSend(cmd) {
      document.getElementById('tinp').value = cmd;
      tExec();
    }

    function tExec() {
      var inp = document.getElementById('tinp');
      var cmd = inp.value.trim();
      if (!cmd) return;
      inp.value = '';

      if (cmd === 'clear') { tClear(); return; }

      if (!connected) {
        tPrint('Error: Belum terhubung ke server. Hubungkan dulu.\n');
        return;
      }

      fetch('/api/terminal/input',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({input:cmd})});
    }

    function tPrint(txt) {
      var box = document.getElementById('tscreen');
      var esc27 = String.fromCharCode(27);
      var html = esc(txt)
        .replace(new RegExp(esc(esc27) + '\\[0m', 'g'), '</span>')
        .replace(new RegExp(esc(esc27) + '\\[1m', 'g'), '<span style=""font-weight:bold"">')
        .replace(new RegExp(esc(esc27) + '\\[32m', 'g'), '<span style=""color:#6fcf7f"">')
        .replace(new RegExp(esc(esc27) + '\\[31m', 'g'), '<span style=""color:#e06c75"">')
        .replace(new RegExp(esc(esc27) + '\\[33m', 'g'), '<span style=""color:#e5c07b"">')
        .replace(new RegExp(esc(esc27) + '\\[34m', 'g'), '<span style=""color:#61afef"">')
        .replace(new RegExp(esc(esc27) + '\\[35m', 'g'), '<span style=""color:#c678dd"">')
        .replace(new RegExp(esc(esc27) + '\\[36m', 'g'), '<span style=""color:#56c8d8"">')
        .replace(new RegExp(esc(esc27) + '\\[[0-9;]+m', 'g'), '');
      box.innerHTML += html;
      box.scrollTop = box.scrollHeight;
    }

    function tClear() { document.getElementById('tscreen').innerHTML = ''; }

    // Utilities
    function toast(msg) {
      var box = document.getElementById('toasts');
      var t = document.createElement('div');
      t.className = 'toast';
      t.textContent = msg;
      box.appendChild(t);
      setTimeout(function(){t.remove();}, 3500);
    }

    function esc(s) {
      if (!s) return '';
      var d = document.createElement('div');
      d.appendChild(document.createTextNode(s));
      return d.innerHTML;
    }

    function escAttr(s) { return esc(s || ''); }

    function getFileIcon(name) {
      var ext = (name || '').split('.').pop().toLowerCase();
      var m = {js:'📜',ts:'📘',json:'📋',html:'🌐',css:'🎨',py:'🐍',kt:'🟣',java:'☕',md:'📝',txt:'📝',sh:'⚙️',yml:'📦',yaml:'📦',xml:'📦',env:'🔐',sql:'🗄️',log:'📊'};
      return m[ext] || '📄';
    }

    function getExt(name) {
      if (!name) return '';
      var parts = name.split('.');
      return parts.length > 1 ? parts.pop().toUpperCase() : 'FILE';
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
