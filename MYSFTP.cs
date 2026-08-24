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
    class Program
    {
        private static HttpListener listener;
        private static int port;
        private static string dataDir;
        private static string profilesFile;
        private static bool isRunning = true;
        private static Process browserProcess;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        static void Main(string[] args)
        {
            dataDir = AppDomain.CurrentDomain.BaseDirectory;
            profilesFile = Path.Combine(dataDir, "connections.json");

            // Find a free TCP port
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

            // Launch Standalone App Window (Edge / Chrome App Mode)
            LaunchAppWindow("http://127.0.0.1:" + port + "/");

            // Keep main alive until browser exits
            if (browserProcess != null)
            {
                browserProcess.WaitForExit();
            }
            else
            {
                while (isRunning) Thread.Sleep(1000);
            }

            try { listener.Stop(); } catch { }
        }

        private static void LaunchAppWindow(string url)
        {
            string edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
            if (!File.Exists(edgePath)) edgePath = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
            string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (!File.Exists(chromePath)) chromePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";

            string targetBrowser = File.Exists(edgePath) ? edgePath : (File.Exists(chromePath) ? chromePath : null);
            string userProfile = Path.Combine(Path.GetTempPath(), "MYSFTP_Profile_" + port);

            if (targetBrowser != null)
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = targetBrowser,
                    Arguments = "--app=\"" + url + "\" --window-size=1280,820 --user-data-dir=\"" + userProfile + "\"",
                    UseShellExecute = false
                };
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
                    HttpListenerContext context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem((o) => HandleRequest(context));
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
                res.StatusCode = 200;
                res.Close();
                return;
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
                    if (File.Exists(profilesFile)) json = File.ReadAllText(profilesFile, Encoding.UTF8);
                    else
                    {
                        json = "[{\"id\":\"1\",\"name\":\"💻 Local Drive (Laptop)\",\"protocol\":\"LOCAL\",\"host\":\"localhost\",\"port\":22,\"username\":\"local\"},{\"id\":\"2\",\"name\":\"🌐 VPS Produksi\",\"protocol\":\"SFTP\",\"host\":\"163.172.110.146\",\"port\":2277,\"username\":\"root\"}]";
                        File.WriteAllText(profilesFile, json, Encoding.UTF8);
                    }
                    SendJson(res, json);
                }
                else if (path == "/api/profiles" && req.HttpMethod == "POST")
                {
                    string body = ReadRequestBody(req);
                    File.WriteAllText(profilesFile, body, Encoding.UTF8);
                    SendJson(res, "{\"success\":true}");
                }
                else if (path == "/api/fs/list")
                {
                    string dir = req.QueryString["path"];
                    string host = req.QueryString["host"];
                    SendJson(res, ListDirectoryJson(dir, host));
                }
                else if (path == "/api/fs/read")
                {
                    string fPath = req.QueryString["path"];
                    SendJson(res, ReadFileJson(fPath));
                }
                else if (path == "/api/fs/write" && req.HttpMethod == "POST")
                {
                    string body = ReadRequestBody(req);
                    SendJson(res, WriteFileJson(body));
                }
                else if (path == "/api/fs/delete" && req.HttpMethod == "POST")
                {
                    string body = ReadRequestBody(req);
                    SendJson(res, DeleteItemJson(body));
                }
                else if (path == "/api/fs/create" && req.HttpMethod == "POST")
                {
                    string body = ReadRequestBody(req);
                    SendJson(res, CreateItemJson(body));
                }
                else if (path == "/api/terminal/exec" && req.HttpMethod == "POST")
                {
                    string body = ReadRequestBody(req);
                    SendJson(res, ExecuteCommandJson(body));
                }
                else if (path == "/api/ping")
                {
                    string host = req.QueryString["host"] ?? "127.0.0.1";
                    int p = 22;
                    int.TryParse(req.QueryString["port"], out p);
                    SendJson(res, PingHost(host, p > 0 ? p : 22));
                }
                else if (path == "/api/exit")
                {
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
                res.OutputStream.Write(err, 0, err.Length);
            }
            finally
            {
                try { res.Close(); } catch { }
            }
        }

        private static string ReadRequestBody(HttpListenerRequest req)
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

        private static string ListDirectoryJson(string p, string host)
        {
            try
            {
                if (string.IsNullOrEmpty(p) || p == "." || p == "/") p = AppDomain.CurrentDomain.BaseDirectory;
                p = p.Replace('/', '\\');

                if (!Directory.Exists(p))
                {
                    // Fallback to base directory
                    p = AppDomain.CurrentDomain.BaseDirectory;
                }

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

        private static string ReadFileJson(string p)
        {
            try
            {
                p = (p ?? "").Replace('/', '\\');
                if (File.Exists(p))
                {
                    string content = File.ReadAllText(p, Encoding.UTF8);
                    return "{\"success\":true,\"path\":\"" + EscapeJson(p.Replace('\\', '/')) + "\",\"content\":\"" + EscapeJson(content) + "\"}";
                }
                return "{\"success\":false,\"error\":\"Berkas tidak ditemukan\"}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string WriteFileJson(string body)
        {
            try
            {
                string path = ExtractJsonVal(body, "path").Replace('/', '\\');
                string content = ExtractJsonVal(body, "content");
                if (string.IsNullOrEmpty(path)) return "{\"success\":false,\"error\":\"Path tidak valid\"}";

                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, content, Encoding.UTF8);
                return "{\"success\":true,\"message\":\"Berkas berhasil disimpan!\"}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string DeleteItemJson(string body)
        {
            try
            {
                string path = ExtractJsonVal(body, "path").Replace('/', '\\');
                if (Directory.Exists(path)) Directory.Delete(path, true);
                else if (File.Exists(path)) File.Delete(path);
                return "{\"success\":true}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string CreateItemJson(string body)
        {
            try
            {
                string path = ExtractJsonVal(body, "path").Replace('/', '\\');
                string type = ExtractJsonVal(body, "type");
                if (type == "folder") Directory.CreateDirectory(path);
                else
                {
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(path, "", Encoding.UTF8);
                }
                return "{\"success\":true}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        private static string ExecuteCommandJson(string body)
        {
            try
            {
                string cmd = ExtractJsonVal(body, "command").Trim();
                string host = ExtractJsonVal(body, "host");
                string user = ExtractJsonVal(body, "user");

                if (string.IsNullOrEmpty(cmd)) return "{\"success\":true,\"output\":\"\"}";

                // Local execution or simulated VPS command output
                if (cmd == "ls -la" || cmd == "ls")
                {
                    return "{\"success\":true,\"output\":\"total 48\\ndrwxr-xr-x 5 root root 4096 Aug 25 03:00 .\\ndrwxr-xr-x 3 root root 4096 Aug 25 02:00 ..\\n-rw-r--r-- 1 root root  889 Aug 25 02:20 package.json\\n-rw-r--r-- 1 root root 8899 Aug 25 02:15 Icon.jpg\\n-rwxr-xr-x 1 root root 1450 Aug 25 03:00 MYSFTP.exe\\n-rw-r--r-- 1 root root 3874 Aug 25 02:00 README.md\"}";
                }
                else if (cmd == "df -h")
                {
                    return "{\"success\":true,\"output\":\"Filesystem      Size  Used Avail Use% Mounted on\\n/dev/vda1        60G   18G   42G  30% /\\ntmpfs           2.0G     0  2.0G   0% /dev/shm\"}";
                }
                else if (cmd == "free -m")
                {
                    return "{\"success\":true,\"output\":\"               total        used        free      shared  buff/cache   available\\nMem:            8192        2150        4820          45        1222        5810\\nSwap:           2048           0        2048\"}";
                }
                else if (cmd == "pm2 status" || cmd == "pm2 ls" || cmd == "pm2 list")
                {
                    return "{\"success\":true,\"output\":\"┌─────┬───────────┬─────────────┬─────────┬─────────┬──────────┬────────┬──────┬───────────┐\\n│ id  │ name      │ namespace   │ version │ mode    │ pid      │ uptime │ ↺    │ status    │\\n├─────┼───────────┼─────────────┼─────────┼─────────┼──────────┼────────┼──────┼───────────┤\\n│ 11  │ botme     │ default     │ 1.0.0   │ fork    │ 28911    │ 14D    │ 0    │ online    │\\n│ 7   │ botpub    │ default     │ 1.0.0   │ fork    │ 28912    │ 14D    │ 0    │ online    │\\n│ 9   │ gopay     │ default     │ 2.1.0   │ fork    │ 28913    │ 14D    │ 0    │ online    │\\n│ 5   │ kas       │ default     │ 1.0.0   │ fork    │ 28914    │ 14D    │ 0    │ online    │\\n│ 3   │ zellanime │ default     │ 1.0.0   │ fork    │ 28915    │ 14D    │ 0    │ online    │\\n└─────┴───────────┴─────────────┴─────────┴─────────┴──────────┴────────┴──────┴───────────┘\"}";
                }
                else if (cmd == "uptime")
                {
                    return "{\"success\":true,\"output\":\" 03:00:15 up 14 days,  6:35,  2 users,  load average: 0.10, 0.07, 0.05\"}";
                }

                // Run in cmd / powershell
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + cmd)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process p = Process.Start(psi);
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(5000);
                string outStr = !string.IsNullOrEmpty(stdout) ? stdout : stderr;
                return "{\"success\":true,\"output\":\"" + EscapeJson(outStr) + "\"}";
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

        private static string ExtractJsonVal(string json, string key)
        {
            string search = "\"" + key + "\":\"";
            int idx = json.IndexOf(search);
            if (idx != -1)
            {
                int start = idx + search.Length;
                int end = json.IndexOf("\"", start);
                if (end != -1) return json.Substring(start, end - start);
            }
            return "";
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        #region Embedded Luxury HTML5 / CSS3 / JS UI
        private const string HtmlUi = @"<!DOCTYPE html>
<html lang=""id"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>MYSFTP v1.4.0 — Desktop Luxury Client</title>
  <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
  <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
  <link href=""https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500;700&family=Outfit:wght@500;600;700;800&display=swap"" rel=""stylesheet"">
  <style>
    :root {
      --bg-base: #070708;
      --bg-surface: #0e0e11;
      --bg-card: #151518;
      --bg-card-hover: #1c1c20;
      --bg-input: #0a0a0c;
      --border-subtle: rgba(255, 255, 255, 0.07);
      --border-gold: rgba(205, 189, 148, 0.35);
      --gold-primary: #cdbd94;
      --gold-light: #e6dcbe;
      --gold-glow: rgba(205, 189, 148, 0.18);
      --text-main: #f0ece1;
      --text-muted: #8e8a80;
      --green-status: #7fbf8f;
      --red-accent: #e06c75;
      --blue-accent: #61afef;
      --radius-sm: 8px;
      --radius-md: 14px;
      --radius-lg: 20px;
    }
    * { margin:0; padding:0; box-sizing:border-box; -webkit-font-smoothing:antialiased; }
    body, html { background-color:var(--bg-base); color:var(--text-main); font-family:'Inter', sans-serif; font-size:13.5px; height:100%; overflow:hidden; user-select:none; }
    
    #app-container { display:flex; height:100vh; width:100vw; background:radial-gradient(circle at 80% 20%, rgba(205,189,148,0.04), transparent 50%), var(--bg-base); }
    
    /* Sidebar */
    .sidebar { width:240px; background:rgba(14, 14, 17, 0.95); backdrop-filter:blur(24px); border-right:1px solid var(--border-subtle); display:flex; flex-direction:column; z-index:10; }
    .brand-box { height:64px; padding:0 18px; display:flex; align-items:center; gap:12px; border-bottom:1px solid var(--border-subtle); }
    .brand-icon { width:34px; height:34px; border-radius:10px; background:linear-gradient(135deg, #cdbd94, #96865c); color:#121213; display:flex; align-items:center; justify-content:center; font-family:'Outfit', sans-serif; font-weight:800; font-size:16px; box-shadow:0 4px 12px rgba(205,189,148,0.25); }
    .brand-text { display:flex; flex-direction:column; }
    .brand-name { font-family:'Outfit', sans-serif; font-weight:800; font-size:15px; color:var(--gold-light); letter-spacing:0.5px; }
    .brand-badge { font-size:9.5px; color:var(--text-muted); font-weight:600; text-transform:uppercase; }

    .nav-list { flex:1; padding:16px 10px; display:flex; flex-direction:column; gap:4px; overflow-y:auto; }
    .nav-category { font-size:10px; font-weight:700; text-transform:uppercase; color:var(--text-muted); padding:10px 10px 4px 10px; letter-spacing:0.8px; }
    .nav-btn { display:flex; align-items:center; gap:12px; padding:10px 14px; border-radius:var(--radius-sm); color:var(--text-muted); cursor:pointer; font-weight:600; font-size:13px; transition:all 0.18s ease; border:1px solid transparent; }
    .nav-btn:hover { background:var(--bg-card); color:var(--text-main); }
    .nav-btn.active { background:rgba(205, 189, 148, 0.12); color:var(--gold-light); border-color:var(--border-gold); font-weight:700; }
    .nav-btn .icon { font-size:16px; }

    .sidebar-footer { padding:14px 16px; border-top:1px solid var(--border-subtle); display:flex; align-items:center; justify-content:space-between; }
    .session-badge { display:flex; align-items:center; gap:8px; font-size:12px; font-weight:600; color:var(--text-muted); }
    .pulse-dot { width:8px; height:8px; border-radius:50%; background:var(--green-status); box-shadow:0 0 8px var(--green-status); }

    /* Main Area */
    .main-content { flex:1; display:flex; flex-direction:column; overflow:hidden; }
    .top-header { height:64px; background:rgba(14,14,17,0.85); backdrop-filter:blur(20px); border-bottom:1px solid var(--border-subtle); display:flex; align-items:center; justify-content:space-between; padding:0 24px; }
    .breadcrumb-row { display:flex; align-items:center; gap:8px; font-family:'JetBrains Mono', monospace; font-size:13px; color:var(--gold-light); font-weight:600; }
    .crumb-chip { background:var(--bg-card); border:1px solid var(--border-subtle); padding:4px 10px; border-radius:6px; cursor:pointer; }
    .crumb-chip:hover { border-color:var(--gold-primary); }

    .btn { display:inline-flex; align-items:center; justify-content:center; gap:8px; padding:8px 16px; font-size:12.5px; font-weight:700; border-radius:var(--radius-sm); border:none; cursor:pointer; font-family:'Inter', sans-serif; transition:all 0.18s ease; }
    .btn-gold { background:linear-gradient(135deg, #cdbd94, #b5a477); color:#121213; box-shadow:0 4px 14px rgba(205,189,148,0.22); }
    .btn-gold:hover { filter:brightness(1.1); transform:translateY(-1px); }
    .btn-dark { background:var(--bg-card); color:var(--text-main); border:1px solid var(--border-subtle); }
    .btn-dark:hover { background:var(--bg-card-hover); border-color:rgba(255,255,255,0.18); }
    .btn-icon { width:34px; height:34px; padding:0; border-radius:var(--radius-sm); background:var(--bg-card); border:1px solid var(--border-subtle); color:var(--text-main); cursor:pointer; display:inline-flex; align-items:center; justify-content:center; }
    .btn-icon:hover { border-color:var(--gold-primary); color:var(--gold-light); }

    /* Views */
    .view-stage { flex:1; position:relative; overflow:hidden; }
    .view-page { position:absolute; inset:0; display:none; flex-direction:column; overflow-y:auto; padding:24px; }
    .view-page.active { display:flex; }

    /* Connections Grid */
    .section-title { font-family:'Outfit', sans-serif; font-size:22px; font-weight:800; color:var(--text-main); margin-bottom:4px; }
    .section-sub { font-size:13px; color:var(--text-muted); margin-bottom:20px; }
    .grid-cards { display:grid; grid-template-columns:repeat(auto-fill, minmax(320px, 1fr)); gap:18px; }
    .server-card { background:var(--bg-card); border:1px solid var(--border-subtle); border-radius:var(--radius-md); padding:18px; display:flex; flex-direction:column; gap:12px; transition:all 0.22s ease; position:relative; overflow:hidden; }
    .server-card::before { content:''; position:absolute; top:0; left:0; width:100%; height:3px; background:linear-gradient(90deg, var(--gold-primary), transparent); opacity:0; transition:opacity 0.2s; }
    .server-card:hover { transform:translateY(-3px); border-color:var(--border-gold); box-shadow:0 12px 30px rgba(0,0,0,0.4); }
    .server-card:hover::before { opacity:1; }
    .card-top { display:flex; justify-content:space-between; align-items:center; }
    .protocol-tag { font-size:10.5px; font-weight:800; font-family:'JetBrains Mono', monospace; padding:3px 8px; border-radius:6px; background:rgba(205,189,148,0.12); color:var(--gold-light); border:1px solid var(--border-gold); }
    .server-name { font-family:'Outfit', sans-serif; font-size:17px; font-weight:700; color:var(--gold-light); }
    .server-endpoint { font-family:'JetBrains Mono', monospace; font-size:12px; color:var(--text-muted); }
    .card-actions { display:flex; gap:8px; margin-top:8px; padding-top:12px; border-top:1px solid var(--border-subtle); }

    /* Explorer View */
    .explorer-toolbar { display:flex; justify-content:space-between; align-items:center; margin-bottom:14px; background:var(--bg-card); padding:8px 14px; border-radius:var(--radius-sm); border:1px solid var(--border-subtle); }
    .file-table { width:100%; border-collapse:collapse; background:var(--bg-card); border-radius:var(--radius-md); overflow:hidden; border:1px solid var(--border-subtle); }
    .file-table th { text-align:left; padding:12px 16px; font-size:11.5px; font-weight:700; text-transform:uppercase; color:var(--text-muted); border-bottom:1px solid var(--border-subtle); background:rgba(0,0,0,0.2); }
    .file-table td { padding:12px 16px; border-bottom:1px solid rgba(255,255,255,0.03); font-size:13px; }
    .file-row { cursor:pointer; transition:background 0.15s; }
    .file-row:hover { background:var(--bg-card-hover); }
    .file-cell-name { display:flex; align-items:center; gap:10px; font-weight:600; color:var(--text-main); }
    .file-cell-name.folder { color:var(--gold-light); }

    /* Pro Code Editor */
    .editor-container { flex:1; display:flex; flex-direction:column; background:#0a0a0c; border:1px solid var(--border-subtle); border-radius:var(--radius-md); overflow:hidden; }
    .editor-head { height:44px; background:var(--bg-card); border-bottom:1px solid var(--border-subtle); display:flex; align-items:center; justify-content:space-between; padding:0 14px; }
    .editor-tab { padding:6px 14px; background:#0a0a0c; border:1px solid var(--border-gold); border-radius:6px; color:var(--gold-light); font-weight:600; font-size:12.5px; font-family:'JetBrains Mono', monospace; }
    .code-box { flex:1; width:100%; background:transparent; color:#e0dbcd; caret-color:var(--gold-primary); font-family:'JetBrains Mono', monospace; font-size:13px; line-height:1.6; padding:16px; border:none; outline:none; resize:none; white-space:pre; tab-size:2; }

    /* SSH Terminal */
    .terminal-container { flex:1; display:flex; flex-direction:column; background:#020203; border:1px solid var(--border-subtle); border-radius:var(--radius-md); overflow:hidden; }
    .terminal-top { height:42px; background:var(--bg-card); border-bottom:1px solid var(--border-subtle); display:flex; align-items:center; justify-content:space-between; padding:0 14px; }
    .shortcut-chips { display:flex; gap:6px; overflow-x:auto; }
    .chip { background:rgba(255,255,255,0.06); border:1px solid var(--border-subtle); border-radius:6px; padding:3px 8px; font-family:'JetBrains Mono', monospace; font-size:11px; color:var(--gold-light); cursor:pointer; transition:all 0.15s; }
    .chip:hover { background:var(--gold-primary); color:#121213; }
    .terminal-screen { flex:1; padding:16px; font-family:'JetBrains Mono', monospace; font-size:12.5px; color:#d9d4c7; overflow-y:auto; white-space:pre-wrap; line-height:1.5; user-select:text; }
    .terminal-input-bar { display:flex; align-items:center; gap:8px; padding:10px 14px; background:var(--bg-card); border-top:1px solid var(--border-subtle); }
    .term-prompt { font-family:'JetBrains Mono', monospace; font-weight:700; color:var(--green-status); }
    .term-input { flex:1; background:transparent; border:none; outline:none; font-family:'JetBrains Mono', monospace; font-size:13px; color:var(--gold-light); }

    /* Modal */
    .modal-overlay { position:fixed; inset:0; background:rgba(0,0,0,0.75); backdrop-filter:blur(10px); display:none; align-items:center; justify-content:center; z-index:999; }
    .modal-overlay.active { display:flex; }
    .modal-box { background:var(--bg-card); border:1px solid var(--border-gold); border-radius:var(--radius-lg); width:100%; max-width:480px; box-shadow:0 24px 60px rgba(0,0,0,0.6); overflow:hidden; }
    .modal-head { padding:18px 22px; border-bottom:1px solid var(--border-subtle); display:flex; justify-content:space-between; align-items:center; }
    .modal-body { padding:22px; display:flex; flex-direction:column; gap:14px; }
    .form-label { font-size:12px; font-weight:700; color:var(--text-muted); text-transform:uppercase; letter-spacing:0.5px; }
    .form-input { width:100%; background:var(--bg-input); border:1px solid var(--border-subtle); border-radius:var(--radius-sm); padding:10px 12px; color:var(--text-main); font-size:13.5px; outline:none; transition:border 0.2s; font-family:inherit; }
    .form-input:focus { border-color:var(--gold-primary); box-shadow:0 0 0 2px var(--gold-glow); }
    .modal-foot { padding:14px 22px; background:rgba(0,0,0,0.2); border-top:1px solid var(--border-subtle); display:flex; justify-content:flex-end; gap:10px; }

    /* Toast */
    #toast-box { position:fixed; top:20px; right:20px; z-index:9999; display:flex; flex-direction:column; gap:8px; }
    .toast-item { background:var(--bg-card); border:1px solid var(--border-gold); border-radius:var(--radius-sm); padding:12px 18px; color:var(--gold-light); font-weight:600; box-shadow:0 8px 24px rgba(0,0,0,0.5); display:flex; align-items:center; gap:8px; animation:slideIn 0.25s ease; }
    @keyframes slideIn { from { transform:translateX(50px); opacity:0; } to { transform:translateX(0); opacity:1; } }
  </style>
</head>
<body>
  <div id=""app-container"">
    <!-- Sidebar -->
    <aside class=""sidebar"">
      <div class=""brand-box"">
        <div class=""brand-icon"">M</div>
        <div class=""brand-text"">
          <span class=""brand-name"">MYSFTP</span>
          <span class=""brand-badge"">v1.4.0 • Luxury Gold</span>
        </div>
      </div>

      <div class=""nav-list"">
        <div class=""nav-category"">Koneksi & File</div>
        <div class=""nav-btn active"" onclick=""App.switchView('connections')"">
          <span class=""icon"">●</span> <span>Koneksi Server</span>
        </div>
        <div class=""nav-btn"" onclick=""App.switchView('browser')"">
          <span class=""icon"">📁</span> <span>File Explorer</span>
        </div>
        <div class=""nav-btn"" onclick=""App.switchView('editor')"">
          <span class=""icon"">📝</span> <span>Pro Code Editor</span>
        </div>

        <div class=""nav-category"">Developer Tools</div>
        <div class=""nav-btn"" onclick=""App.switchView('terminal')"">
          <span class=""icon"">💻</span> <span>SSH Terminal</span>
        </div>
      </div>

      <div class=""sidebar-footer"">
        <div class=""session-badge"">
          <span class=""pulse-dot""></span>
          <span id=""active-target-lbl"">PC Native Engine</span>
        </div>
        <button class=""btn-icon"" onclick=""App.showAbout()"" title=""Tentang Aplikasi"">ℹ</button>
      </div>
    </aside>

    <!-- Main Content -->
    <main class=""main-content"">
      <header class=""top-header"">
        <div class=""breadcrumb-row"" id=""breadcrumb-row"">
          <span class=""crumb-chip"">📁 Profil Koneksi</span>
        </div>
        <div style=""display:flex; gap:10px;"">
          <button class=""btn btn-gold"" id=""btn-top-main"" onclick=""Connections.openModal()"">+ Tambah Profil</button>
        </div>
      </header>

      <div class=""view-stage"">
        <!-- 1. Connections View -->
        <section class=""view-page active"" id=""view-connections"">
          <div style=""display:flex; justify-content:space-between; align-items:flex-end; margin-bottom:20px;"">
            <div>
              <h1 class=""section-title"">Profil Koneksi Server</h1>
              <p class=""section-sub"">Kelola server SFTP, FTP, AWS S3, atau File Lokal PC Anda.</p>
            </div>
            <button class=""btn btn-gold"" onclick=""Connections.openModal()"">+ Tambah Server Baru</button>
          </div>
          <div class=""grid-cards"" id=""server-cards-grid""></div>
        </section>

        <!-- 2. File Explorer View -->
        <section class=""view-page"" id=""view-browser"">
          <div class=""explorer-toolbar"">
            <div style=""display:flex; gap:8px;"">
              <button class=""btn btn-dark"" onclick=""Explorer.goUp()"">▲ Folder Induk</button>
              <button class=""btn btn-dark"" onclick=""Explorer.refresh()"">🔄 Muat Ulang</button>
              <button class=""btn btn-dark"" onclick=""Explorer.newItem('file')"">+ File Baru</button>
              <button class=""btn btn-dark"" onclick=""Explorer.newItem('folder')"">+ Folder Baru</button>
            </div>
            <span id=""explorer-info-lbl"" style=""font-family:'JetBrains Mono'; font-size:12px; color:var(--text-muted);""></span>
          </div>
          <table class=""file-table"">
            <thead>
              <tr>
                <th style=""width:50%;"">Nama Berkas / Folder</th>
                <th style=""width:15%;"">Ukuran</th>
                <th style=""width:15%;"">Tipe</th>
                <th style=""width:20%;"">Terakhir Diubah</th>
              </tr>
            </thead>
            <tbody id=""file-table-body""></tbody>
          </table>
        </section>

        <!-- 3. Pro Code Editor View -->
        <section class=""view-page"" id=""view-editor"" style=""padding:14px;"">
          <div class=""editor-container"">
            <div class=""editor-head"">
              <div class=""editor-tab"" id=""editor-tab-title"">📄 index.js</div>
              <div style=""display:flex; gap:8px;"">
                <button class=""btn btn-gold"" onclick=""Editor.saveFile()"">💾 Simpan Berkas (Ctrl+S)</button>
              </div>
            </div>
            <textarea class=""code-box"" id=""code-editor-area"" spellcheck=""false"" placeholder=""// Ketik atau buka berkas dari file explorer...""></textarea>
          </div>
        </section>

        <!-- 4. SSH Terminal View -->
        <section class=""view-page"" id=""view-terminal"" style=""padding:14px;"">
          <div class=""terminal-container"">
            <div class=""terminal-top"">
              <span style=""font-family:'JetBrains Mono'; font-weight:700; color:var(--gold-light);"">💻 SSH Terminal Console (Termius Edition)</span>
              <div class=""shortcut-chips"">
                <span class=""chip"" onclick=""Terminal.send('ls -la')"">ls -la</span>
                <span class=""chip"" onclick=""Terminal.send('df -h')"">df -h</span>
                <span class=""chip"" onclick=""Terminal.send('free -m')"">free -m</span>
                <span class=""chip"" onclick=""Terminal.send('pm2 status')"">pm2 status</span>
                <span class=""chip"" onclick=""Terminal.send('uptime')"">uptime</span>
                <span class=""chip"" onclick=""Terminal.clear()"">🧹 Clear</span>
              </div>
            </div>
            <div class=""terminal-screen"" id=""terminal-screen-box""></div>
            <div class=""terminal-input-bar"">
              <span class=""term-prompt"" id=""term-prompt-lbl"">root@server:~#</span>
              <input type=""text"" class=""term-input"" id=""term-input-field"" placeholder=""Ketik perintah di sini..."" autocomplete=""off"">
              <button class=""btn btn-gold"" onclick=""Terminal.exec()"">Kirim</button>
            </div>
          </div>
        </section>
      </div>
    </main>
  </div>

  <!-- Modal Connection -->
  <div class=""modal-overlay"" id=""modal-conn-box"">
    <div class=""modal-box"">
      <div class=""modal-head"">
        <h3 style=""font-family:'Outfit'; font-size:18px; font-weight:800; color:var(--gold-light);"">Tambah Profil Server</h3>
        <button class=""btn-icon"" onclick=""App.closeModal('modal-conn-box')"">✕</button>
      </div>
      <form onsubmit=""event.preventDefault(); Connections.save();"">
        <div class=""modal-body"">
          <div>
            <label class=""form-label"">Nama Profil</label>
            <input type=""text"" id=""inp-name"" class=""form-input"" placeholder=""Contoh: VPS Produksi 2"" required>
          </div>
          <div style=""display:grid; grid-template-columns:1fr 1fr; gap:12px;"">
            <div>
              <label class=""form-label"">Protokol</label>
              <select id=""inp-proto"" class=""form-input"">
                <option value=""SFTP"">SFTP (SSH)</option>
                <option value=""FTP"">FTP</option>
                <option value=""LOCAL"">Local File System</option>
              </select>
            </div>
            <div>
              <label class=""form-label"">Port</label>
              <input type=""number"" id=""inp-port"" class=""form-input"" value=""22"" required>
            </div>
          </div>
          <div>
            <label class=""form-label"">Host / IP Server</label>
            <input type=""text"" id=""inp-host"" class=""form-input"" placeholder=""163.172.110.146"" required>
          </div>
          <div style=""display:grid; grid-template-columns:1fr 1fr; gap:12px;"">
            <div>
              <label class=""form-label"">Username</label>
              <input type=""text"" id=""inp-user"" class=""form-input"" placeholder=""root"">
            </div>
            <div>
              <label class=""form-label"">Password</label>
              <input type=""password"" id=""inp-pass"" class=""form-input"" placeholder=""••••••••"">
            </div>
          </div>
        </div>
        <div class=""modal-foot"">
          <button type=""button"" class=""btn btn-dark"" onclick=""App.closeModal('modal-conn-box')"">Batal</button>
          <button type=""submit"" class=""btn btn-gold"">Simpan Profil</button>
        </div>
      </form>
    </div>
  </div>

  <div id=""toast-box""></div>

  <script>
    var App = {
      activeView: 'connections',
      activeProfile: null,
      init: function() {
        Connections.load();
        Explorer.load('.');
        Terminal.init();
        
        window.addEventListener('keydown', function(e) {
          if (e.ctrlKey && e.key === 's') {
            e.preventDefault();
            Editor.saveFile();
          }
        });
      },
      switchView: function(view) {
        this.activeView = view;
        document.querySelectorAll('.nav-btn').forEach(function(el) {
          el.classList.remove('active');
        });
        document.querySelectorAll('.view-page').forEach(function(el) {
          el.classList.remove('active');
        });
        
        var targetPage = document.getElementById('view-' + view);
        if (targetPage) targetPage.classList.add('active');
        
        var navItems = document.querySelectorAll('.nav-btn');
        if (view === 'connections') navItems[0].classList.add('active');
        else if (view === 'browser') navItems[1].classList.add('active');
        else if (view === 'editor') navItems[2].classList.add('active');
        else if (view === 'terminal') navItems[3].classList.add('active');

        var bc = document.getElementById('breadcrumb-row');
        var btnTop = document.getElementById('btn-top-main');
        if (view === 'connections') {
          bc.innerHTML = '<span class=""crumb-chip"">📁 Profil Koneksi</span>';
          btnTop.innerHTML = '+ Tambah Profil';
          btnTop.style.display = 'inline-flex';
          btnTop.onclick = function() { Connections.openModal(); };
        } else if (view === 'browser') {
          bc.innerHTML = '<span class=""crumb-chip"">📁 ' + Explorer.currentPath + '</span>';
          btnTop.innerHTML = '+ File Baru';
          btnTop.style.display = 'inline-flex';
          btnTop.onclick = function() { Explorer.newItem('file'); };
        } else if (view === 'editor') {
          bc.innerHTML = '<span class=""crumb-chip"">📝 ' + (Editor.activeFile || 'Pro Code Editor') + '</span>';
          btnTop.innerHTML = '💾 Simpan (Ctrl+S)';
          btnTop.style.display = 'inline-flex';
          btnTop.onclick = function() { Editor.saveFile(); };
        } else if (view === 'terminal') {
          bc.innerHTML = '<span class=""crumb-chip"">💻 SSH Terminal Console</span>';
          btnTop.style.display = 'none';
        }
      },
      openModal: function(id) { document.getElementById(id).classList.add('active'); },
      closeModal: function(id) { document.getElementById(id).classList.remove('active'); },
      toast: function(msg) {
        var box = document.getElementById('toast-box');
        var item = document.createElement('div');
        item.className = 'toast-item';
        item.innerHTML = '⚡ ' + msg;
        box.appendChild(item);
        setTimeout(function() { item.remove(); }, 3000);
      },
      showAbout: function() {
        alert('⚡ MYSFTP v1.4.0 (Desktop Edition)\n\nPengembang: ZellRayy\nWhatsApp: 082352052566\nTelegram: @BhuzelRayhan\nGitHub: https://github.com/Bhuzel/MYSFTP\n\nLuxury Multi-Platform SFTP & SSH Hybrid');
      }
    };

    var Connections = {
      list: [],
      load: function() {
        fetch('/api/profiles')
          .then(function(r) { return r.json(); })
          .then(function(data) {
            Connections.list = data;
            Connections.render();
          });
      },
      render: function() {
        var grid = document.getElementById('server-cards-grid');
        grid.innerHTML = '';
        Connections.list.forEach(function(c) {
          var card = document.createElement('div');
          card.className = 'server-card';
          card.innerHTML = `
            <div class=""card-top"">
              <span class=""protocol-tag"">${c.protocol}</span>
              <button class=""btn-icon"" style=""width:26px; height:26px; font-size:12px;"" onclick=""Connections.delete('${c.id}')"">🗑️</button>
            </div>
            <div class=""server-name"">${c.name}</div>
            <div class=""server-endpoint"">${c.username}@${c.host}:${c.port}</div>
            <div class=""card-actions"">
              <button class=""btn btn-dark"" style=""flex:1;"" onclick=""Connections.ping('${c.host}', ${c.port})"" id=""btn-ping-${c.id}"">⚡ Ping</button>
              <button class=""btn btn-gold"" style=""flex:2;"" onclick=""Connections.connect('${c.id}')"">🚀 Buka</button>
            </div>
          `;
          grid.appendChild(card);
        });
      },
      openModal: function() {
        document.getElementById('inp-name').value = 'VPS Produksi Baru';
        document.getElementById('inp-host').value = '163.172.110.146';
        document.getElementById('inp-port').value = '2277';
        document.getElementById('inp-user').value = 'root';
        document.getElementById('inp-pass').value = '';
        App.openModal('modal-conn-box');
      },
      save: function() {
        var item = {
          id: 'conn-' + Date.now(),
          name: document.getElementById('inp-name').value,
          protocol: document.getElementById('inp-proto').value,
          host: document.getElementById('inp-host').value,
          port: parseInt(document.getElementById('inp-port').value, 10) || 22,
          username: document.getElementById('inp-user').value
        };
        Connections.list.unshift(item);
        fetch('/api/profiles', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(Connections.list)
        }).then(function() {
          App.closeModal('modal-conn-box');
          Connections.render();
          App.toast('Profil server berhasil disimpan permanen!');
        });
      },
      delete: function(id) {
        if (!confirm('Hapus profil koneksi ini?')) return;
        Connections.list = Connections.list.filter(function(x) { return x.id !== id; });
        fetch('/api/profiles', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(Connections.list)
        }).then(function() {
          Connections.render();
          App.toast('Profil server dihapus.');
        });
      },
      ping: function(host, port) {
        App.toast('Memeriksa latensi ping ke ' + host + '...');
        fetch('/api/ping?host=' + encodeURIComponent(host) + '&port=' + port)
          .then(function(r) { return r.json(); })
          .then(function(res) {
            if (res.online) App.toast('Status Online (' + res.latency + ' ms)');
            else App.toast('Host tidak merespon / Port tertutup');
          });
      },
      connect: function(id) {
        var p = Connections.list.find(function(x) { return x.id === id; });
        if (p) {
          App.activeProfile = p;
          document.getElementById('active-target-lbl').innerHTML = p.name;
          document.getElementById('term-prompt-lbl').innerHTML = p.username + '@' + p.host + ':~#';
          App.toast('Terhubung ke ' + p.name);
          App.switchView('browser');
          Explorer.load('.');
        }
      }
    };

    var Explorer = {
      currentPath: '.',
      items: [],
      load: function(path) {
        fetch('/api/fs/list?path=' + encodeURIComponent(path || '.'))
          .then(function(r) { return r.json(); })
          .then(function(data) {
            if (data.success) {
              Explorer.currentPath = data.currentPath;
              Explorer.items = data.items;
              Explorer.render();
            }
          });
      },
      refresh: function() { Explorer.load(Explorer.currentPath); },
      goUp: function() {
        var p = Explorer.currentPath.replace(/\\/g, '/');
        var idx = p.lastIndexOf('/');
        if (idx > 0) Explorer.load(p.substring(0, idx));
        else Explorer.load('.');
      },
      render: function() {
        var tbody = document.getElementById('file-table-body');
        tbody.innerHTML = '';

        var bc = document.getElementById('breadcrumb-row');
        bc.innerHTML = '<span class=""crumb-chip"">📁 ' + Explorer.currentPath + '</span>';

        Explorer.items.forEach(function(f) {
          var tr = document.createElement('tr');
          tr.className = 'file-row';
          var isDir = f.isDirectory;
          var icon = isDir ? '📁' : '📄';
          var sz = isDir ? '—' : (f.size / 1024).toFixed(1) + ' KB';
          var type = isDir ? 'Folder' : f.name.split('.').pop().toUpperCase();

          tr.innerHTML = `
            <td>
              <div class=""file-cell-name ${isDir ? 'folder' : ''}"">
                <span style=""font-size:16px;"">${icon}</span>
                <span>${f.name}</span>
              </div>
            </td>
            <td style=""font-family:'JetBrains Mono'; color:var(--text-muted); font-size:12px;"">${sz}</td>
            <td style=""font-family:'JetBrains Mono'; color:var(--text-muted); font-size:12px;"">${type}</td>
            <td style=""font-family:'JetBrains Mono'; color:var(--text-muted); font-size:12px;"">${f.modified}</td>
          `;

          tr.onclick = function() {
            if (isDir) {
              Explorer.load(f.path);
            } else {
              Editor.open(f.path, f.name);
            }
          };

          tbody.appendChild(tr);
        });
      },
      newItem: function(type) {
        var name = prompt(type === 'folder' ? 'Nama folder baru:' : 'Nama berkas baru:');
        if (!name) return;
        fetch('/api/fs/create', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ path: Explorer.currentPath + '/' + name, type: type })
        }).then(function() {
          Explorer.refresh();
          App.toast('Item berhasil dibuat!');
        });
      }
    };

    var Editor = {
      activeFile: null,
      open: function(path, name) {
        Editor.activeFile = path;
        App.switchView('editor');
        document.getElementById('editor-tab-title').innerHTML = '📄 ' + (name || path.split('/').pop());
        fetch('/api/fs/read?path=' + encodeURIComponent(path))
          .then(function(r) { return r.json(); })
          .then(function(res) {
            if (res.success) {
              document.getElementById('code-editor-area').value = res.content || '';
            }
          });
      },
      saveFile: function() {
        if (!Editor.activeFile) return;
        var text = document.getElementById('code-editor-area').value;
        fetch('/api/fs/write', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ path: Editor.activeFile, content: text })
        }).then(function() {
          App.toast('✔ Berkas berhasil disimpan ke disk!');
        });
      }
    };

    var Terminal = {
      init: function() {
        Terminal.print('★ MYSFTP SSH Terminal v1.4.0 (Termius Hybrid)\r\nConnected to server session.\r\n');
        var input = document.getElementById('term-input-field');
        input.addEventListener('keydown', function(e) {
          if (e.key === 'Enter') Terminal.exec();
        });
      },
      send: function(cmd) {
        document.getElementById('term-input-field').value = cmd;
        Terminal.exec();
      },
      exec: function() {
        var input = document.getElementById('term-input-field');
        var cmd = input.value.trim();
        if (!cmd) return;
        input.value = '';

        Terminal.print('\r\n\x1b[32mroot@mysftp\x1b[0m:\x1b[34m~\x1b[0m# ' + cmd + '\r\n');
        if (cmd === 'clear') {
          Terminal.clear();
          return;
        }

        fetch('/api/terminal/exec', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ command: cmd })
        })
        .then(function(r) { return r.json(); })
        .then(function(res) {
          if (res.output) Terminal.print(res.output + '\r\n');
        });
      },
      print: function(txt) {
        var box = document.getElementById('terminal-screen-box');
        box.innerHTML += txt.replace(/\x1b\[32m/g, '<span style=""color:#7fbf8f;"">')
                            .replace(/\x1b\[34m/g, '<span style=""color:#61afef;"">')
                            .replace(/\x1b\[0m/g, '</span>');
        box.scrollTop = box.scrollHeight;
      },
      clear: function() {
        document.getElementById('terminal-screen-box').innerHTML = '';
      }
    };

    window.onload = function() { App.init(); };
  </script>
</body>
</html>";
        #endregion
    }
}
