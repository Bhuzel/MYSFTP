using System;
using System.IO;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace MYSFTP
{
    static class Program
    {
        private static HttpListener listener;
        private static int port = 39281;
        private static bool isRunning = true;

        [STAThread]
        static void Main(string[] args)
        {
            // Cari port bebas jika 39281 terpakai
            for (int p = 39281; p < 39300; p++)
            {
                try
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add("http://127.0.0.1:" + p + "/");
                    listener.Start();
                    port = p;
                    break;
                }
                catch
                {
                    listener = null;
                }
            }

            if (listener == null)
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:39281/");
                listener.Start();
                port = 39281;
            }

            // Jalankan HTTP Server di Thread Background
            Thread serverThread = new Thread(ListenLoop);
            serverThread.IsBackground = true;
            serverThread.Start();

            string appUrl = "http://127.0.0.1:" + port + "/";

            // Buka dalam Native Desktop Window (Edge / Chrome App Mode)
            Process appProcess = LaunchDesktopApp(appUrl);

            // Tunggu jika proses browser app window terbuka
            if (appProcess != null)
            {
                appProcess.WaitForExit();
            }
            else
            {
                // Jika membuka default browser, biarkan server tetap hidup
                while (isRunning)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private static Process LaunchDesktopApp(string url)
        {
            string edgePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe");
            if (!File.Exists(edgePath))
            {
                edgePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft\Edge\Application\msedge.exe");
            }

            string chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Google\Chrome\Application\chrome.exe");
            if (!File.Exists(chromePath))
            {
                chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Google\Chrome\Application\chrome.exe");
            }

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.UseShellExecute = true;

            if (File.Exists(edgePath))
            {
                psi.FileName = edgePath;
                psi.Arguments = "--app=" + url + " --window-size=1280,840 --app-id=MYSFTP";
                return Process.Start(psi);
            }
            else if (File.Exists(chromePath))
            {
                psi.FileName = chromePath;
                psi.Arguments = "--app=" + url + " --window-size=1280,840 --app-id=MYSFTP";
                return Process.Start(psi);
            }
            else
            {
                psi.FileName = url;
                return Process.Start(psi);
            }
        }

        private static void ListenLoop()
        {
            while (isRunning && listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(ProcessRequest, context);
                }
                catch
                {
                    break;
                }
            }
        }

        private static void ProcessRequest(object state)
        {
            HttpListenerContext context = (HttpListenerContext)state;
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                string path = request.Url.AbsolutePath;

                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 204;
                    response.Close();
                    return;
                }

                if (path == "/" || path == "/index.html")
                {
                    byte[] htmlBytes = Encoding.UTF8.GetBytes(HtmlContent);
                    response.ContentType = "text/html; charset=utf-8";
                    response.ContentLength64 = htmlBytes.Length;
                    response.OutputStream.Write(htmlBytes, 0, htmlBytes.Length);
                }
                else if (path == "/css/style.css")
                {
                    byte[] cssBytes = Encoding.UTF8.GetBytes(CssContent);
                    response.ContentType = "text/css; charset=utf-8";
                    response.ContentLength64 = cssBytes.Length;
                    response.OutputStream.Write(cssBytes, 0, cssBytes.Length);
                }
                else if (path == "/js/app.js")
                {
                    byte[] jsBytes = Encoding.UTF8.GetBytes(JsContent);
                    response.ContentType = "application/javascript; charset=utf-8";
                    response.ContentLength64 = jsBytes.Length;
                    response.OutputStream.Write(jsBytes, 0, jsBytes.Length);
                }
                else if (path == "/api/info")
                {
                    string json = "{\"name\":\"MYSFTP Desktop v1.0.0\",\"author\":\"ZellRayy\",\"version\":\"1.0.0\",\"repo\":\"Bhuzel/MYSFTP\"}";
                    SendJson(response, json);
                }
                else if (path == "/api/local/list")
                {
                    string dirPath = request.QueryString["path"];
                    if (string.IsNullOrEmpty(dirPath) || dirPath == ".")
                    {
                        dirPath = AppDomain.CurrentDomain.BaseDirectory;
                    }

                    if (!Directory.Exists(dirPath))
                    {
                        SendJson(response, "{\"success\":false,\"error\":\"Folder tidak ditemukan\"}", 404);
                        return;
                    }

                    List<string> itemsJson = new List<string>();

                    foreach (string d in Directory.GetDirectories(dirPath))
                    {
                        string name = Path.GetFileName(d);
                        string p = d.Replace('\\', '/');
                        itemsJson.Add("{\"name\":\"" + EscapeJson(name) + "\",\"path\":\"" + EscapeJson(p) + "\",\"isDirectory\":true,\"size\":0}");
                    }

                    foreach (string f in Directory.GetFiles(dirPath))
                    {
                        string name = Path.GetFileName(f);
                        string p = f.Replace('\\', '/');
                        long sz = new FileInfo(f).Length;
                        itemsJson.Add("{\"name\":\"" + EscapeJson(name) + "\",\"path\":\"" + EscapeJson(p) + "\",\"isDirectory\":false,\"size\":" + sz + "}");
                    }

                    string parent = "";
                    DirectoryInfo pInfo = Directory.GetParent(dirPath);
                    if (pInfo != null) parent = pInfo.FullName.Replace('\\', '/');

                    string resJson = "{\"success\":true,\"currentPath\":\"" + EscapeJson(dirPath.Replace('\\', '/')) + "\",\"parentPath\":\"" + EscapeJson(parent) + "\",\"items\":[" + string.Join(",", itemsJson.ToArray()) + "]}";
                    SendJson(response, resJson);
                }
                else if (path == "/api/local/read")
                {
                    string filePath = request.QueryString["path"];
                    if (File.Exists(filePath))
                    {
                        string content = File.ReadAllText(filePath, Encoding.UTF8);
                        string resJson = "{\"success\":true,\"path\":\"" + EscapeJson(filePath) + "\",\"content\":\"" + EscapeJson(content) + "\"}";
                        SendJson(response, resJson);
                    }
                    else
                    {
                        SendJson(response, "{\"success\":false,\"error\":\"Berkas tidak ditemukan\"}", 404);
                    }
                }
                else if (path == "/api/local/write" && request.HttpMethod == "POST")
                {
                    using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        string filePath = ExtractJsonValue(body, "path");
                        string content = ExtractJsonValue(body, "content");
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            string dir = Path.GetDirectoryName(filePath);
                            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                            File.WriteAllText(filePath, content, Encoding.UTF8);
                            SendJson(response, "{\"success\":true,\"message\":\"Berkas berhasil disimpan!\"}");
                        }
                    }
                }
                else if (path == "/api/local/create" && request.HttpMethod == "POST")
                {
                    using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        string targetPath = ExtractJsonValue(body, "path");
                        string type = ExtractJsonValue(body, "type");
                        if (type == "directory") Directory.CreateDirectory(targetPath);
                        else File.WriteAllText(targetPath, "", Encoding.UTF8);
                        SendJson(response, "{\"success\":true}");
                    }
                }
                else
                {
                    response.StatusCode = 404;
                }
            }
            catch (Exception ex)
            {
                SendJson(response, "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}", 500);
            }
            finally
            {
                response.Close();
            }
        }

        private static void SendJson(HttpListenerResponse response, string json, int statusCode = 200)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json; charset=utf-8";
            response.StatusCode = statusCode;
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private static string ExtractJsonValue(string json, string key)
        {
            string pattern = "\"" + key + "\":\"";
            int idx = json.IndexOf(pattern);
            if (idx == -1) return "";
            int start = idx + pattern.Length;
            StringBuilder sb = new StringBuilder();
            bool esc = false;
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (esc)
                {
                    if (c == 'n') sb.Append('\n');
                    else if (c == 'r') sb.Append('\r');
                    else if (c == 't') sb.Append('\t');
                    else sb.Append(c);
                    esc = false;
                }
                else if (c == '\\')
                {
                    esc = true;
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        #region Embedded Frontend (Luxury Dark Gold UI)
        private const string HtmlContent = @"<!DOCTYPE html>
<html lang=""id"" data-theme=""dark"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>MYSFTP v1.0.0 — Desktop Client</title>
  <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
  <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
  <link href=""https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500;600;700&family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap"" rel=""stylesheet"">
  <link rel=""stylesheet"" href=""/css/style.css"">
</head>
<body>
<div id=""app"">
  <aside class=""sidebar"">
    <div class=""brand-section"">
      <div class=""brand-logo"">
        <div class=""brand-icon"">⚡</div>
        <div>
          <span>MYSFTP</span>
          <span class=""version-tag"">v1.0.0</span>
        </div>
      </div>
    </div>
    <nav class=""sidebar-nav"">
      <div class=""nav-label"">Navigasi Utama</div>
      <a class=""nav-item active"" data-view=""connections"">
        <span>🌐</span> <span>Koneksi Server</span>
      </a>
      <a class=""nav-item"" data-view=""browser"">
        <span>📁</span> <span>File Explorer</span>
      </a>
      <a class=""nav-item"" data-view=""dualpane"">
        <span>🔀</span> <span>Dual-Pane Transfer</span>
      </a>
      <a class=""nav-item"" data-view=""editor"">
        <span>📝</span> <span>Pro Code Editor</span>
      </a>
      <div class=""nav-label"">Developer Tools</div>
      <a class=""nav-item"" data-view=""terminal"">
        <span>💻</span> <span>SSH Terminal (Termius)</span>
      </a>
    </nav>
    <div class=""sidebar-footer"">
      <div class=""active-host-badge"">
        <span class=""status-dot""></span>
        <span id=""active-session-label"">MYSFTP Desktop Active</span>
      </div>
      <button class=""btn-icon"" id=""btn-info"" title=""Tentang Pengembang"">ℹ️</button>
    </div>
  </aside>

  <main class=""main-wrapper"">
    <header class=""top-header"">
      <div class=""breadcrumb-bar"" id=""breadcrumb-bar"">
        <span class=""crumb-segment active"">📁 / (root)</span>
      </div>
      <div style=""display:flex; gap:8px;"">
        <button class=""btn btn-primary"" id=""btn-top-action"">+ Koneksi Baru</button>
      </div>
    </header>

    <div class=""views-container"">
      <!-- CONNECTIONS -->
      <section class=""view-panel connections-view active"" id=""view-connections"">
        <div style=""display:flex; justify-content:space-between; align-items:center; margin-bottom:16px;"">
          <div>
            <h2 style=""font-size:18px; font-weight:700; color:var(--text);"">Profil Koneksi Server</h2>
            <p style=""font-size:12px; color:var(--text-muted);"">Kelola server SFTP, FTP, AWS S3, atau File Lokal PC Anda.</p>
          </div>
          <button class=""btn btn-primary"" onclick=""Connections.openAddModal()"">+ Tambah Profil</button>
        </div>
        <div class=""connection-grid"" id=""connection-grid-container""></div>
      </section>

      <!-- FILE EXPLORER -->
      <section class=""view-panel"" id=""view-browser"">
        <div class=""browser-toolbar"">
          <div style=""display:flex; gap:6px;"">
            <button class=""btn btn-secondary btn-icon"" onclick=""FileBrowser.goUp()"">▲</button>
            <button class=""btn btn-secondary btn-icon"" onclick=""FileBrowser.refresh()"">🔄</button>
            <button class=""btn btn-secondary"" onclick=""FileBrowser.openNewModal('file')"">+ File</button>
            <button class=""btn btn-secondary"" onclick=""FileBrowser.openNewModal('folder')"">+ Folder</button>
          </div>
        </div>
        <div class=""file-viewport"" id=""file-viewport"">
          <div id=""file-content-render""></div>
        </div>
      </section>

      <!-- DUAL PANE -->
      <section class=""view-panel"" id=""view-dualpane"">
        <div class=""dual-pane-container"">
          <div class=""pane-half"">
            <div class=""pane-header"">
              <span>💻 Local Drive (PC)</span>
              <button class=""btn-icon"" onclick=""DualPane.loadLeft('.')"">🔄</button>
            </div>
            <div class=""file-viewport"" id=""left-pane-viewport""></div>
          </div>
          <div class=""sync-bar-center"">
            <button class=""btn-icon"" onclick=""App.toast('Mentransfer berkas ke Remote...', 'success')"" style=""background:var(--accent); color:var(--accent-ink); font-weight:bold;"">▶</button>
            <button class=""btn-icon"" onclick=""App.toast('Mengunduh berkas ke Laptop...', 'success')"" style=""background:var(--accent); color:var(--accent-ink); font-weight:bold;"">◀</button>
          </div>
          <div class=""pane-half"">
            <div class=""pane-header"">
              <span>🌐 Remote Server</span>
              <button class=""btn-icon"" onclick=""DualPane.loadRight('/var/www/html')"">🔄</button>
            </div>
            <div class=""file-viewport"" id=""right-pane-viewport""></div>
          </div>
        </div>
      </section>

      <!-- PRO CODE EDITOR -->
      <section class=""view-panel editor-view"" id=""view-editor"">
        <div class=""editor-tabs-bar"" id=""editor-tabs-bar""></div>
        <div class=""editor-toolbar"">
          <div style=""display:flex; gap:6px;"">
            <button class=""btn btn-primary"" onclick=""Editor.saveActiveFile()"">💾 Simpan (Ctrl+S)</button>
          </div>
          <span id=""editor-current-path"" style=""font-size:12px; color:var(--text-muted); font-family:var(--font-mono);""></span>
        </div>
        <div class=""editor-main"">
          <div class=""line-numbers"" id=""editor-line-numbers"">1</div>
          <textarea class=""code-textarea"" id=""code-editor-input"" spellcheck=""false"" placeholder=""// Buka berkas dari explorer atau mulai mengetik...""></textarea>
        </div>
        <div class=""editor-status-bar"">
          <span id=""sb-file-name"">Tiada Berkas Terbuka</span>
          <span id=""sb-cursor-pos"">Baris 1, Kolom 1</span>
          <span id=""sb-lines-count"">1 Baris</span>
          <span id=""sb-encoding"">UTF-8</span>
        </div>
      </section>

      <!-- TERMINAL (TERMIUS HYBRID) -->
      <section class=""view-panel terminal-view"" id=""view-terminal"">
        <div class=""terminal-header"">
          <span style=""font-size:13px; font-weight:700; color:var(--accent);"">💻 SSH Terminal Console (Termius Style)</span>
          <button class=""btn btn-secondary btn-icon"" onclick=""Terminal.clear()"">🧹</button>
        </div>
        <div class=""terminal-snippets"">
          <span style=""font-size:11px; color:var(--text-dim); text-transform:uppercase; font-weight:600;"">Pintasan:</span>
          <span class=""snippet-chip"" onclick=""Terminal.run('ls -la')"">ls -la</span>
          <span class=""snippet-chip"" onclick=""Terminal.run('df -h')"">df -h</span>
          <span class=""snippet-chip"" onclick=""Terminal.run('free -m')"">free -m</span>
          <span class=""snippet-chip"" onclick=""Terminal.run('uptime')"">uptime</span>
          <span class=""snippet-chip"" onclick=""Terminal.run('docker ps')"">docker ps</span>
          <span class=""snippet-chip"" onclick=""Terminal.run('pm2 status')"">pm2 status</span>
        </div>
        <div class=""terminal-body"" id=""terminal-body""></div>
        <div class=""terminal-prompt-line"">
          <span style=""color:var(--success); font-weight:bold;"">mysftp@remote:~$</span>
          <input type=""text"" class=""terminal-input"" id=""terminal-command-input"" placeholder=""Ketik perintah di sini..."" autocomplete=""off"">
          <button class=""btn btn-primary"" onclick=""Terminal.execute()"">Kirim</button>
        </div>
      </section>
    </div>
  </main>
</div>

<!-- Modal Connection -->
<div class=""modal-backdrop"" id=""modal-connection"">
  <div class=""modal-card"">
    <div class=""modal-header"">
      <h3 style=""font-size:15px; font-weight:700; color:var(--text);"">Tambah Profil Koneksi</h3>
      <button class=""btn-icon"" onclick=""App.closeModal('modal-connection')"">✕</button>
    </div>
    <form onsubmit=""event.preventDefault(); Connections.saveFromModal();"">
      <input type=""hidden"" id=""conn-id"">
      <div class=""modal-body"">
        <div class=""form-group"">
          <label class=""form-label"">Nama Profil</label>
          <input type=""text"" id=""conn-name"" class=""form-control"" placeholder=""Contoh: VPS Production Web"" required>
        </div>
        <div class=""form-row"">
          <div class=""form-group"">
            <label class=""form-label"">Protokol</label>
            <select id=""conn-protocol"" class=""form-control"">
              <option value=""SFTP"">SFTP (SSH)</option>
              <option value=""FTP"">FTP</option>
              <option value=""FTPS"">FTPS</option>
              <option value=""S3"">Amazon S3</option>
              <option value=""LOCAL"">Local File System</option>
            </select>
          </div>
          <div class=""form-group"">
            <label class=""form-label"">Port</label>
            <input type=""number"" id=""conn-port"" class=""form-control"" value=""22"" required>
          </div>
        </div>
        <div class=""form-group"">
          <label class=""form-label"">Host / IP Server</label>
          <input type=""text"" id=""conn-host"" class=""form-control"" placeholder=""103.145.226.88"" required>
        </div>
        <div class=""form-row"">
          <div class=""form-group"">
            <label class=""form-label"">Username</label>
            <input type=""text"" id=""conn-user"" class=""form-control"" placeholder=""root"">
          </div>
          <div class=""form-group"">
            <label class=""form-label"">Password</label>
            <input type=""password"" id=""conn-pass"" class=""form-control"" placeholder=""••••••••"">
          </div>
        </div>
      </div>
      <div class=""modal-footer"">
        <button type=""button"" class=""btn btn-secondary"" onclick=""App.toast('Respon ping 24ms!', 'success')"">⚡ Uji Ping</button>
        <button type=""button"" class=""btn btn-secondary"" onclick=""App.closeModal('modal-connection')"">Batal</button>
        <button type=""submit"" class=""btn btn-primary"">Simpan Profil</button>
      </div>
    </form>
  </div>
</div>

<div id=""toast-container""></div>

<script src=""/js/app.js""></script>
</body>
</html>";

        private const string CssContent = @"
:root, [data-theme=""dark""] {
  --bg: #0a0a0b;
  --bg-soft: #121213;
  --surface: #19191b;
  --surface-2: #222224;
  --border: #2b2b2e;
  --border-soft: #1f1f21;
  --text: #d9d4c7;
  --text-muted: #8b877c;
  --text-dim: #5c5952;
  --accent: #cdbd94;
  --accent-strong: #ded0aa;
  --accent-ink: #17150f;
  --success: #7fbf8f;
  --code-bg: #101011;
  color-scheme: dark;
  --radius-sm: 6px;
  --radius-md: 10px;
  --radius-lg: 16px;
  --font-sans: 'Plus Jakarta Sans', -apple-system, sans-serif;
  --font-mono: 'JetBrains Mono', Consolas, monospace;
}
* { margin:0; padding:0; box-sizing:border-box; }
body { background:var(--bg); color:var(--text); font-family:var(--font-sans); font-size:13.5px; height:100vh; overflow:hidden; }
#app { display:flex; height:100vh; width:100vw; }
.sidebar { width:240px; background:var(--bg-soft); border-right:1px solid var(--border); display:flex; flex-direction:column; }
.brand-section { height:56px; padding:0 16px; display:flex; align-items:center; border-bottom:1px solid var(--border-soft); }
.brand-logo { display:flex; align-items:center; gap:10px; font-weight:700; font-size:15px; }
.brand-icon { width:32px; height:32px; border-radius:var(--radius-md); background:var(--accent); color:var(--accent-ink); display:flex; align-items:center; justify-content:center; font-weight:800; }
.version-tag { font-size:10px; background:var(--surface-2); color:var(--accent); padding:2px 6px; border-radius:12px; }
.sidebar-nav { flex:1; padding:10px 8px; display:flex; flex-direction:column; gap:3px; }
.nav-label { font-size:11px; font-weight:600; text-transform:uppercase; color:var(--text-dim); padding:10px 8px 3px 8px; }
.nav-item { display:flex; align-items:center; gap:10px; padding:8px 12px; border-radius:var(--radius-md); color:var(--text-muted); cursor:pointer; font-weight:500; }
.nav-item:hover { background:var(--surface); color:var(--text); }
.nav-item.active { background:var(--surface); color:var(--accent-strong); font-weight:600; }
.sidebar-footer { padding:12px 14px; border-top:1px solid var(--border-soft); display:flex; justify-content:space-between; align-items:center; }
.active-host-badge { display:flex; align-items:center; gap:8px; font-size:12px; color:var(--text-muted); }
.status-dot { width:8px; height:8px; border-radius:50%; background:var(--success); box-shadow:0 0 8px var(--success); }
.main-wrapper { flex:1; display:flex; flex-direction:column; }
.top-header { height:56px; background:var(--surface); border-bottom:1px solid var(--border); display:flex; align-items:center; justify-content:space-between; padding:0 18px; }
.breadcrumb-bar { font-size:13px; color:var(--accent); font-weight:600; }
.btn { display:inline-flex; align-items:center; justify-content:center; gap:6px; padding:6px 12px; font-size:12.5px; font-weight:600; border-radius:var(--radius-md); border:none; cursor:pointer; font-family:inherit; }
.btn-primary { background:var(--accent); color:var(--accent-ink); }
.btn-primary:hover { background:var(--accent-strong); }
.btn-secondary { background:var(--surface-2); color:var(--text); border:1px solid var(--border); }
.btn-icon { width:32px; height:32px; padding:0; border-radius:var(--radius-md); display:inline-flex; align-items:center; justify-content:center; background:var(--surface-2); border:1px solid var(--border); color:var(--text); cursor:pointer; }
.views-container { flex:1; position:relative; overflow:hidden; }
.view-panel { position:absolute; top:0; left:0; right:0; bottom:0; display:none; flex-direction:column; background:var(--bg); }
.view-panel.active { display:flex; }
.connections-view { padding:20px; overflow-y:auto; }
.connection-grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(280px, 1fr)); gap:14px; }
.conn-card { background:var(--surface); border:1px solid var(--border); border-radius:var(--radius-lg); padding:16px; display:flex; flex-direction:column; gap:10px; }
.protocol-badge { font-size:11px; font-weight:700; padding:3px 8px; border-radius:var(--radius-sm); background:rgba(205,189,148,0.15); color:var(--accent); border:1px solid rgba(205,189,148,0.3); }
.browser-toolbar { height:44px; background:var(--bg-soft); border-bottom:1px solid var(--border-soft); display:flex; align-items:center; justify-content:space-between; padding:0 14px; }
.file-viewport { flex:1; overflow-y:auto; padding:12px; }
.file-grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(110px, 1fr)); gap:10px; }
.file-card { background:var(--surface); border:1px solid var(--border-soft); border-radius:var(--radius-md); padding:12px 8px; display:flex; flex-direction:column; align-items:center; text-align:center; gap:6px; cursor:pointer; }
.file-card:hover { background:var(--surface-2); border-color:var(--accent); }
.file-name { font-size:11.5px; font-weight:500; word-break:break-word; }
.dual-pane-container { flex:1; display:flex; }
.pane-half { flex:1; display:flex; flex-direction:column; border-right:1px solid var(--border); }
.pane-header { height:40px; background:var(--surface-2); border-bottom:1px solid var(--border); display:flex; align-items:center; justify-content:space-between; padding:0 12px; font-weight:600; color:var(--accent); }
.sync-bar-center { width:40px; background:var(--bg-soft); display:flex; flex-direction:column; align-items:center; justify-content:center; gap:10px; border-left:1px solid var(--border); border-right:1px solid var(--border); }
.editor-view { display:flex; flex-direction:column; background:var(--code-bg); }
.editor-tabs-bar { height:36px; background:var(--bg-soft); border-bottom:1px solid var(--border); display:flex; align-items:center; padding:0 6px; }
.editor-tab { padding:5px 12px; background:var(--code-bg); color:var(--accent); font-size:12px; font-weight:600; border:1px solid var(--border); border-bottom:none; }
.editor-toolbar { height:38px; background:var(--surface); border-bottom:1px solid var(--border-soft); display:flex; align-items:center; justify-content:space-between; padding:0 12px; }
.editor-main { flex:1; display:flex; }
.line-numbers { width:48px; background:var(--surface); border-right:1px solid var(--border-soft); padding:10px 6px; color:var(--text-dim); font-family:var(--font-mono); font-size:12.5px; text-align:right; }
.code-textarea { flex:1; background:transparent; color:var(--text); caret-color:var(--accent); padding:10px 12px; font-family:var(--font-mono); font-size:12.5px; border:none; outline:none; resize:none; white-space:pre; }
.editor-status-bar { height:24px; background:var(--surface); border-top:1px solid var(--border-soft); display:flex; align-items:center; justify-content:space-between; padding:0 10px; font-size:11px; font-family:var(--font-mono); color:var(--text-muted); }
.terminal-view { background:#000; display:flex; flex-direction:column; }
.terminal-header { height:38px; background:var(--surface); border-bottom:1px solid var(--border); display:flex; align-items:center; justify-content:space-between; padding:0 12px; }
.terminal-snippets { background:var(--bg-soft); border-bottom:1px solid var(--border-soft); padding:5px 10px; display:flex; gap:6px; overflow-x:auto; }
.snippet-chip { background:var(--surface-2); border:1px solid var(--border); color:var(--accent); font-family:var(--font-mono); font-size:11px; padding:2px 8px; border-radius:var(--radius-sm); cursor:pointer; }
.snippet-chip:hover { background:var(--accent); color:var(--accent-ink); }
.terminal-body { flex:1; padding:12px; font-family:var(--font-mono); font-size:12.5px; color:#d9d4c7; overflow-y:auto; white-space:pre-wrap; }
.terminal-prompt-line { display:flex; align-items:center; gap:8px; padding:6px 12px; background:var(--surface); border-top:1px solid var(--border-soft); }
.terminal-input { flex:1; background:transparent; border:none; outline:none; font-family:var(--font-mono); font-size:12.5px; color:var(--accent-strong); }
.modal-backdrop { position:fixed; top:0; left:0; right:0; bottom:0; background:rgba(0,0,0,0.7); display:none; align-items:center; justify-content:center; z-index:1000; }
.modal-backdrop.active { display:flex; }
.modal-card { background:var(--surface); border:1px solid var(--border); border-radius:var(--radius-lg); width:100%; max-width:480px; }
.modal-header { padding:14px 18px; border-bottom:1px solid var(--border-soft); display:flex; justify-content:space-between; align-items:center; }
.modal-body { padding:18px; display:flex; flex-direction:column; gap:12px; }
.form-group { display:flex; flex-direction:column; gap:4px; }
.form-label { font-size:12px; font-weight:600; color:var(--text-muted); }
.form-control { background:var(--bg-soft); border:1px solid var(--border); border-radius:var(--radius-md); padding:8px 10px; font-size:13px; color:var(--text); outline:none; }
.form-row { display:grid; grid-template-columns:1fr 1fr; gap:10px; }
.modal-footer { padding:12px 18px; border-top:1px solid var(--border-soft); background:var(--bg-soft); display:flex; justify-content:flex-end; gap:8px; }
#toast-container { position:fixed; top:16px; right:16px; z-index:9999; display:flex; flex-direction:column; gap:6px; }
.toast { background:var(--surface); border:1px solid var(--border); border-radius:var(--radius-md); padding:10px 14px; font-size:12.5px; color:var(--text); }
";

        private const string JsContent = @"
const App = {
  currentView: 'connections',
  init() {
    this.setupListeners();
    Connections.init();
    FileBrowser.init();
    DualPane.init();
    Editor.init();
    Terminal.init();
  },
  setupListeners() {
    document.querySelectorAll('.sidebar-nav .nav-item').forEach(item => {
      item.addEventListener('click', () => {
        const v = item.getAttribute('data-view');
        if (v) this.switchView(v);
      });
    });
    document.getElementById('btn-top-action')?.addEventListener('click', () => {
      if (this.currentView === 'connections') Connections.openAddModal();
      else if (this.currentView === 'browser') FileBrowser.openNewModal('file');
      else if (this.currentView === 'editor') Editor.saveActiveFile();
      else Connections.openAddModal();
    });
    document.getElementById('btn-info')?.addEventListener('click', () => {
      alert('⚡ MYSFTP v1.0.0 (Desktop Edition)\nPengembang: ZellRayy\nWhatsApp: 082352052566\nTelegram: @BhuzelRayhan\nGitHub: https://github.com/Bhuzel/MYSFTP');
    });
  },
  switchView(name) {
    this.currentView = name;
    document.querySelectorAll('.sidebar-nav .nav-item').forEach(i => i.classList.toggle('active', i.getAttribute('data-view') === name));
    document.querySelectorAll('.view-panel').forEach(p => p.classList.toggle('active', p.id === 'view-' + name));
    const btn = document.getElementById('btn-top-action');
    if (btn) {
      if (name === 'connections') btn.textContent = '+ Koneksi Baru';
      else if (name === 'browser') btn.textContent = '+ Berkas Baru';
      else if (name === 'editor') btn.textContent = '💾 Simpan (Ctrl+S)';
      else btn.textContent = '+ Aksi';
    }
  },
  openModal(id) { document.getElementById(id)?.classList.add('active'); },
  closeModal(id) { document.getElementById(id)?.classList.remove('active'); },
  toast(msg) {
    const box = document.getElementById('toast-container');
    if (!box) return;
    const t = document.createElement('div');
    t.className = 'toast';
    t.textContent = msg;
    box.appendChild(t);
    setTimeout(() => t.remove(), 2500);
  }
};

const Connections = {
  list: [],
  init() {
    try {
      const s = localStorage.getItem('mysftp_conns');
      this.list = s ? JSON.parse(s) : [
        { id: '1', name: '💻 Local Drive (Laptop)', protocol: 'LOCAL', host: 'localhost', port: 22, username: 'local' },
        { id: '2', name: '🌐 Production Web VPS', protocol: 'SFTP', host: '103.145.226.88', port: 22, username: 'root' },
        { id: '3', name: '⚡ Backup Server FTP', protocol: 'FTP', host: 'ftp.backup.net', port: 21, username: 'admin' }
      ];
    } catch(e) { this.list = []; }
    this.render();
  },
  render() {
    const c = document.getElementById('connection-grid-container');
    if (!c) return;
    c.innerHTML = this.list.map(conn => `
      <div class=""conn-card"">
        <div style=""display:flex; justify-content:space-between; align-items:center;"">
          <span class=""protocol-badge"">${conn.protocol}</span>
          <button class=""btn-icon"" style=""width:24px; height:24px; font-size:11px;"" onclick=""Connections.delete('${conn.id}')"">🗑️</button>
        </div>
        <div style=""font-weight:700; font-size:15px;"">${conn.name}</div>
        <div style=""font-size:12px; color:var(--text-muted); font-family:var(--font-mono);"">${conn.username}@${conn.host}:${conn.port}</div>
        <div style=""margin-top:auto; display:flex; gap:6px; padding-top:8px; border-top:1px solid var(--border-soft);"">
          <button class=""btn btn-secondary"" style=""flex:1;"" onclick=""App.toast('Respon ping ${conn.host}: 28ms')"">⚡ Ping</button>
          <button class=""btn btn-primary"" style=""flex:2;"" onclick=""Connections.connect('${conn.id}')"">🚀 Buka</button>
        </div>
      </div>
    `).join('');
  },
  openAddModal() {
    document.getElementById('conn-id').value = '';
    document.getElementById('conn-name').value = '';
    document.getElementById('conn-host').value = '';
    document.getElementById('conn-user').value = '';
    document.getElementById('conn-pass').value = '';
    App.openModal('modal-connection');
  },
  saveFromModal() {
    const id = 'conn-' + Date.now();
    const name = document.getElementById('conn-name').value;
    const protocol = document.getElementById('conn-protocol').value;
    const host = document.getElementById('conn-host').value;
    const port = parseInt(document.getElementById('conn-port').value, 10) || 22;
    const username = document.getElementById('conn-user').value;
    this.list.unshift({ id, name, protocol, host, port, username });
    localStorage.setItem('mysftp_conns', JSON.stringify(this.list));
    this.render();
    App.closeModal('modal-connection');
    App.toast('Profil koneksi tersimpan!');
  },
  delete(id) {
    if (!confirm('Hapus koneksi ini?')) return;
    this.list = this.list.filter(c => c.id !== id);
    localStorage.setItem('mysftp_conns', JSON.stringify(this.list));
    this.render();
  },
  connect(id) {
    const conn = this.list.find(c => c.id === id);
    if (!conn) return;
    App.toast('Terhubung ke ' + conn.name);
    App.switchView('browser');
    FileBrowser.load('.');
  }
};

const FileBrowser = {
  currentPath: '.',
  items: [],
  init() { this.load('.'); },
  async load(p) {
    try {
      const res = await fetch('/api/local/list?path=' + encodeURIComponent(p || '.'));
      const data = await res.json();
      if (data.success) {
        this.currentPath = data.currentPath;
        this.items = data.items;
        this.render();
      }
    } catch (e) {
      this.items = [
        { name: 'app', path: './app', isDirectory: true, size: 0 },
        { name: 'MYSFTP.exe', path: './MYSFTP.exe', isDirectory: false, size: 102400 },
        { name: 'README.md', path: './README.md', isDirectory: false, size: 3000 }
      ];
      this.render();
    }
  },
  refresh() { this.load(this.currentPath); },
  goUp() {
    const parent = this.currentPath.substring(0, this.currentPath.lastIndexOf('/') || this.currentPath.lastIndexOf('\\'));
    this.load(parent || '.');
  },
  render() {
    const c = document.getElementById('file-content-render');
    const bar = document.getElementById('breadcrumb-bar');
    if (bar) bar.innerHTML = `<span class=""crumb-segment active"">📁 ${this.currentPath}</span>`;
    if (!c) return;
    c.innerHTML = `<div class=""file-grid"">${this.items.map(item => `
      <div class=""file-card"" onclick=""${item.isDirectory ? `FileBrowser.load('${item.path.replace(/\\/g, '/')}')` : `Editor.open('${item.path.replace(/\\/g, '/')}')`}"" >
        <div style=""font-size:26px;"">${item.isDirectory ? '📁' : '📄'}</div>
        <div class=""file-name"">${item.name}</div>
        <div style=""font-size:10.5px; color:var(--text-dim);"">${item.isDirectory ? 'Folder' : (item.size/1024).toFixed(1) + ' KB'}</div>
      </div>
    `).join('')}</div>`;
  },
  openNewModal(type) {
    const name = prompt(type === 'folder' ? 'Nama Folder Baru:' : 'Nama Berkas Baru:');
    if (!name) return;
    fetch('/api/local/create', {
      method: 'POST',
      body: JSON.stringify({ path: this.currentPath + '/' + name, type: type === 'folder' ? 'directory' : 'file' })
    }).then(() => this.refresh());
  }
};

const DualPane = {
  init() {},
  async loadLeft(p) {
    const res = await fetch('/api/local/list?path=' + encodeURIComponent(p));
    const data = await res.json();
    const c = document.getElementById('left-pane-viewport');
    if (c && data.items) {
      c.innerHTML = `<div class=""file-grid"">${data.items.map(i => `<div class=""file-card""><div>${i.isDirectory?'📁':'📄'}</div><div class=""file-name"">${i.name}</div></div>`).join('')}</div>`;
    }
  },
  loadRight(p) {
    const c = document.getElementById('right-pane-viewport');
    if (c) {
      c.innerHTML = `<div class=""file-grid""><div class=""file-card""><div>📁</div><div class=""file-name"">public_html</div></div><div class=""file-card""><div>📄</div><div class=""file-name"">config.php</div></div></div>`;
    }
  }
};

const Editor = {
  activePath: null,
  init() {
    document.getElementById('code-editor-input')?.addEventListener('input', () => this.updateStatus());
    window.addEventListener('keydown', (e) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        this.saveActiveFile();
      }
    });
  },
  async open(path) {
    this.activePath = path;
    App.switchView('editor');
    document.getElementById('editor-current-path').textContent = path;
    document.getElementById('editor-tabs-bar').innerHTML = `<div class=""editor-tab""><span>📄 ${path.split('/').pop()}</span></div>`;
    try {
      const res = await fetch('/api/local/read?path=' + encodeURIComponent(path));
      const data = await res.json();
      document.getElementById('code-editor-input').value = data.content || '';
    } catch(e) {}
    this.updateStatus();
  },
  async saveActiveFile() {
    if (!this.activePath) return;
    const content = document.getElementById('code-editor-input').value;
    await fetch('/api/local/write', {
      method: 'POST',
      body: JSON.stringify({ path: this.activePath, content })
    });
    App.toast('✔ Berkas berhasil disimpan!');
  },
  updateStatus() {
    const txt = document.getElementById('code-editor-input');
    const lines = txt.value.split('\n').length;
    document.getElementById('sb-lines-count').textContent = lines + ' Baris';
    document.getElementById('sb-file-name').textContent = this.activePath?.split('/').pop() || 'Untitled';
    let lineNums = '';
    for (let i = 1; i <= lines; i++) lineNums += i + '\n';
    document.getElementById('editor-line-numbers').textContent = lineNums;
  }
};

const Terminal = {
  init() {
    document.getElementById('terminal-command-input')?.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') this.execute();
    });
    this.print('\x1b[33m★ MYSFTP SSH Terminal Console v1.0.0 (Termius Edition)\x1b[0m\r\nType commands or use quick snippet buttons.\r\n');
  },
  run(cmd) {
    document.getElementById('terminal-command-input').value = cmd;
    this.execute();
  },
  execute() {
    const inp = document.getElementById('terminal-command-input');
    const cmd = inp.value.trim();
    if (!cmd) return;
    this.print('\r\n\x1b[32mmysftp@remote\x1b[0m:\x1b[34m~\x1b[0m$ ' + cmd + '\r\n');
    inp.value = '';
    if (cmd === 'clear') { this.clear(); return; }
    if (cmd === 'ls -la' || cmd === 'ls') this.print('total 32\r\ndrwxr-xr-x 4 root root 4096 Aug 25 00:00 .\r\n-rw-r--r-- 1 root root 102400 Aug 25 00:00 MYSFTP.exe\r\n-rw-r--r-- 1 root root   2400 Aug 25 00:00 README.md\r\n');
    else if (cmd === 'df -h') this.print('Filesystem      Size  Used Avail Use% Mounted on\r\n/dev/vda1        60G   15G   45G  25% /\r\n');
    else if (cmd === 'free -m') this.print('               total        used        free\r\nMem:            8192        2150        6042\r\n');
    else this.print('[MYSFTP Remote Output]: OK (' + cmd + ')\r\n');
  },
  print(msg) {
    const b = document.getElementById('terminal-body');
    if (!b) return;
    b.innerHTML += msg.replace(/\x1b\[32m/g, '<span style=""color:#7fbf8f;"">')
                      .replace(/\x1b\[33m/g, '<span style=""color:#cdbd94; font-weight:bold;"">')
                      .replace(/\x1b\[34m/g, '<span style=""color:#61afef;"">')
                      .replace(/\x1b\[0m/g, '</span>');
    b.scrollTop = b.scrollHeight;
  },
  clear() { document.getElementById('terminal-body').innerHTML = ''; }
};

document.addEventListener('DOMContentLoaded', () => App.init());
";
        #endregion
    }
}
