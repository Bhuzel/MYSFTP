using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using Microsoft.Win32;

namespace MYSFTP
{
    [ComVisible(true)]
    public class ScriptBridge
    {
        private MainForm form;
        public ScriptBridge(MainForm f) { form = f; }

        public string GetLocalDrives()
        {
            List<string> drives = new List<string>();
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (d.IsReady)
                {
                    drives.Add("{\"name\":\"" + EscapeJson(d.Name) + "\",\"path\":\"" + EscapeJson(d.RootDirectory.FullName.Replace('\\', '/')) + "\",\"isDirectory\":true,\"size\":" + d.TotalSize + "}");
                }
            }
            return "[" + string.Join(",", drives.ToArray()) + "]";
        }

        public string ListDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || path == "." || path == "/")
                {
                    path = AppDomain.CurrentDomain.BaseDirectory;
                }
                path = path.Replace('/', '\\');

                if (!Directory.Exists(path))
                {
                    return "{\"success\":false,\"error\":\"Folder tidak ditemukan\"}";
                }

                List<string> items = new List<string>();
                foreach (string d in Directory.GetDirectories(path))
                {
                    string name = Path.GetFileName(d);
                    string p = d.Replace('\\', '/');
                    items.Add("{\"name\":\"" + EscapeJson(name) + "\",\"path\":\"" + EscapeJson(p) + "\",\"isDirectory\":true,\"size\":0}");
                }
                foreach (string f in Directory.GetFiles(path))
                {
                    string name = Path.GetFileName(f);
                    string p = f.Replace('\\', '/');
                    long sz = 0;
                    try { sz = new FileInfo(f).Length; } catch { }
                    items.Add("{\"name\":\"" + EscapeJson(name) + "\",\"path\":\"" + EscapeJson(p) + "\",\"isDirectory\":false,\"size\":" + sz + "}");
                }

                string parent = "";
                DirectoryInfo pInfo = Directory.GetParent(path);
                if (pInfo != null) parent = pInfo.FullName.Replace('\\', '/');

                return "{\"success\":true,\"currentPath\":\"" + EscapeJson(path.Replace('\\', '/')) + "\",\"parentPath\":\"" + EscapeJson(parent) + "\",\"items\":[" + string.Join(",", items.ToArray()) + "]}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        public string ReadFile(string path)
        {
            try
            {
                path = path.Replace('/', '\\');
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path, Encoding.UTF8);
                    return "{\"success\":true,\"path\":\"" + EscapeJson(path) + "\",\"content\":\"" + EscapeJson(content) + "\"}";
                }
                return "{\"success\":false,\"error\":\"File tidak ditemukan\"}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        public string WriteFile(string path, string content)
        {
            try
            {
                path = path.Replace('/', '\\');
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, content, Encoding.UTF8);
                return "{\"success\":true,\"message\":\"Berhasil disimpan!\"}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        public string DeleteItem(string path)
        {
            try
            {
                path = path.Replace('/', '\\');
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    return "{\"success\":true}";
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                    return "{\"success\":true}";
                }
                return "{\"success\":false,\"error\":\"Item tidak ditemukan\"}";
            }
            catch (Exception ex)
            {
                return "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}";
            }
        }

        public string CreateItem(string path, string type)
        {
            try
            {
                path = path.Replace('/', '\\');
                if (type == "folder")
                {
                    Directory.CreateDirectory(path);
                }
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

        public void ShowMessage(string msg, string title)
        {
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }
    }

    public class MainForm : Form
    {
        private WebBrowser browser;

        public MainForm()
        {
            this.Text = "MYSFTP v1.1.0 — Desktop Client (by ZellRayy)";
            this.Size = new Size(1280, 820);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(10, 10, 11);
            this.ForeColor = Color.FromArgb(217, 212, 199);

            // Set Icon from Icon.jpg if exists
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icon.jpg");
                if (File.Exists(iconPath))
                {
                    using (Bitmap bmp = new Bitmap(iconPath))
                    {
                        IntPtr hIcon = bmp.GetHicon();
                        this.Icon = Icon.FromHandle(hIcon);
                    }
                }
            }
            catch { }

            browser = new WebBrowser();
            browser.Dock = DockStyle.Fill;
            browser.AllowWebBrowserDrop = false;
            browser.IsWebBrowserContextMenuEnabled = true;
            browser.WebBrowserShortcutsEnabled = true;
            browser.ScriptErrorsSuppressed = true;
            browser.ObjectForScripting = new ScriptBridge(this);

            this.Controls.Add(browser);

            browser.DocumentText = HtmlUi;
        }

        #region Embedded HTML UI
        private const string HtmlUi = @"<!DOCTYPE html>
<html lang=""id"" data-theme=""dark"">
<head>
  <meta charset=""UTF-8"">
  <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>MYSFTP v1.1.0 — Desktop Client</title>
  <style>
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
      --error: #e06c75;
      --code-bg: #101011;
      color-scheme: dark;
    }
    * { margin:0; padding:0; box-sizing:border-box; }
    body { background:var(--bg); color:var(--text); font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; font-size:13.5px; height:100vh; overflow:hidden; user-select:none; }
    #app { display:flex; height:100vh; width:100vw; }
    .sidebar { width:240px; background:var(--bg-soft); border-right:1px solid var(--border); display:flex; flex-direction:column; }
    .brand-section { height:56px; padding:0 16px; display:flex; align-items:center; border-bottom:1px solid var(--border-soft); }
    .brand-logo { display:flex; align-items:center; gap:10px; font-weight:700; font-size:15px; }
    .brand-icon { width:32px; height:32px; border-radius:10px; background:var(--accent); color:var(--accent-ink); display:flex; align-items:center; justify-content:center; font-weight:800; }
    .version-tag { font-size:10px; background:var(--surface-2); color:var(--accent); padding:2px 6px; border-radius:12px; }
    .sidebar-nav { flex:1; padding:10px 8px; display:flex; flex-direction:column; gap:3px; }
    .nav-label { font-size:11px; font-weight:600; text-transform:uppercase; color:var(--text-dim); padding:10px 8px 3px 8px; }
    .nav-item { display:flex; align-items:center; gap:10px; padding:8px 12px; border-radius:10px; color:var(--text-muted); cursor:pointer; font-weight:500; }
    .nav-item:hover { background:var(--surface); color:var(--text); }
    .nav-item.active { background:var(--surface); color:var(--accent-strong); font-weight:600; }
    .sidebar-footer { padding:12px 14px; border-top:1px solid var(--border-soft); display:flex; justify-content:space-between; align-items:center; }
    .active-host-badge { display:flex; align-items:center; gap:8px; font-size:12px; color:var(--text-muted); }
    .status-dot { width:8px; height:8px; border-radius:50%; background:var(--success); }
    .main-wrapper { flex:1; display:flex; flex-direction:column; }
    .top-header { height:56px; background:var(--surface); border-bottom:1px solid var(--border); display:flex; align-items:center; justify-content:space-between; padding:0 18px; }
    .breadcrumb-bar { font-size:13px; color:var(--accent); font-weight:600; }
    .btn { display:inline-flex; align-items:center; justify-content:center; gap:6px; padding:6px 12px; font-size:12.5px; font-weight:600; border-radius:10px; border:none; cursor:pointer; font-family:inherit; }
    .btn-primary { background:var(--accent); color:var(--accent-ink); }
    .btn-primary:hover { background:var(--accent-strong); }
    .btn-secondary { background:var(--surface-2); color:var(--text); border:1px solid var(--border); }
    .btn-icon { width:32px; height:32px; padding:0; border-radius:10px; display:inline-flex; align-items:center; justify-content:center; background:var(--surface-2); border:1px solid var(--border); color:var(--text); cursor:pointer; }
    .views-container { flex:1; position:relative; overflow:hidden; }
    .view-panel { position:absolute; top:0; left:0; right:0; bottom:0; display:none; flex-direction:column; background:var(--bg); }
    .view-panel.active { display:flex; }
    .connections-view { padding:20px; overflow-y:auto; }
    .connection-grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(280px, 1fr)); gap:14px; }
    .conn-card { background:var(--surface); border:1px solid var(--border); border-radius:16px; padding:16px; display:flex; flex-direction:column; gap:10px; }
    .protocol-badge { font-size:11px; font-weight:700; padding:3px 8px; border-radius:6px; background:rgba(205,189,148,0.15); color:var(--accent); border:1px solid rgba(205,189,148,0.3); }
    .browser-toolbar { height:44px; background:var(--bg-soft); border-bottom:1px solid var(--border-soft); display:flex; align-items:center; justify-content:space-between; padding:0 14px; }
    .file-viewport { flex:1; overflow-y:auto; padding:12px; }
    .file-grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(110px, 1fr)); gap:10px; }
    .file-card { background:var(--surface); border:1px solid var(--border-soft); border-radius:10px; padding:12px 8px; display:flex; flex-direction:column; align-items:center; text-align:center; gap:6px; cursor:pointer; position:relative; }
    .file-card:hover { background:var(--surface-2); border-color:var(--accent); }
    .file-name { font-size:11.5px; font-weight:500; word-break:break-word; max-lines:2; }
    .file-delete-btn { position:absolute; top:4px; right:4px; font-size:10px; background:rgba(224,108,117,0.2); color:var(--error); border:none; border-radius:4px; padding:2px 4px; cursor:pointer; display:none; }
    .file-card:hover .file-delete-btn { display:block; }
    .dual-pane-container { flex:1; display:flex; }
    .pane-half { flex:1; display:flex; flex-direction:column; border-right:1px solid var(--border); }
    .pane-header { height:40px; background:var(--surface-2); border-bottom:1px solid var(--border); display:flex; align-items:center; justify-content:space-between; padding:0 12px; font-weight:600; color:var(--accent); }
    .sync-bar-center { width:40px; background:var(--bg-soft); display:flex; flex-direction:column; align-items:center; justify-content:center; gap:10px; border-left:1px solid var(--border); border-right:1px solid var(--border); }
    .editor-view { display:flex; flex-direction:column; background:var(--code-bg); }
    .editor-tabs-bar { height:36px; background:var(--bg-soft); border-bottom:1px solid var(--border); display:flex; align-items:center; padding:0 6px; }
    .editor-tab { padding:5px 12px; background:var(--code-bg); color:var(--accent); font-size:12px; font-weight:600; border:1px solid var(--border); border-bottom:none; }
    .editor-toolbar { height:38px; background:var(--surface); border-bottom:1px solid var(--border-soft); display:flex; align-items:center; justify-content:space-between; padding:0 12px; }
    .editor-main { flex:1; display:flex; }
    .code-textarea { flex:1; background:transparent; color:var(--text); caret-color:var(--accent); padding:10px 12px; font-family:Consolas, monospace; font-size:12.5px; border:none; outline:none; resize:none; white-space:pre; }
    .editor-status-bar { height:24px; background:var(--surface); border-top:1px solid var(--border-soft); display:flex; align-items:center; justify-content:space-between; padding:0 10px; font-size:11px; font-family:Consolas, monospace; color:var(--text-muted); }
    .terminal-view { background:#000; display:flex; flex-direction:column; }
    .terminal-header { height:38px; background:var(--surface); border-bottom:1px solid var(--border); display:flex; align-items:center; justify-content:space-between; padding:0 12px; }
    .terminal-snippets { background:var(--bg-soft); border-bottom:1px solid var(--border-soft); padding:5px 10px; display:flex; gap:6px; overflow-x:auto; }
    .snippet-chip { background:var(--surface-2); border:1px solid var(--border); color:var(--accent); font-family:Consolas, monospace; font-size:11px; padding:2px 8px; border-radius:6px; cursor:pointer; }
    .snippet-chip:hover { background:var(--accent); color:var(--accent-ink); }
    .terminal-body { flex:1; padding:12px; font-family:Consolas, monospace; font-size:12px; color:#d9d4c7; overflow-y:auto; white-space:pre-wrap; }
    .terminal-prompt-line { display:flex; align-items:center; gap:8px; padding:6px 12px; background:var(--surface); border-top:1px solid var(--border-soft); }
    .terminal-input { flex:1; background:transparent; border:none; outline:none; font-family:Consolas, monospace; font-size:12.5px; color:var(--accent-strong); }
    .modal-backdrop { position:fixed; top:0; left:0; right:0; bottom:0; background:rgba(0,0,0,0.7); display:none; align-items:center; justify-content:center; z-index:1000; }
    .modal-backdrop.active { display:flex; }
    .modal-card { background:var(--surface); border:1px solid var(--border); border-radius:16px; width:100%; max-width:480px; }
    .modal-header { padding:14px 18px; border-bottom:1px solid var(--border-soft); display:flex; justify-content:space-between; align-items:center; }
    .modal-body { padding:18px; display:flex; flex-direction:column; gap:12px; }
    .form-group { display:flex; flex-direction:column; gap:4px; }
    .form-label { font-size:12px; font-weight:600; color:var(--text-muted); }
    .form-control { background:var(--bg-soft); border:1px solid var(--border); border-radius:8px; padding:8px 10px; font-size:13px; color:var(--text); outline:none; }
    .form-row { display:grid; grid-template-columns:1fr 1fr; gap:10px; }
    .modal-footer { padding:12px 18px; border-top:1px solid var(--border-soft); background:var(--bg-soft); display:flex; justify-content:flex-end; gap:8px; }
    #toast-container { position:fixed; top:16px; right:16px; z-index:9999; display:flex; flex-direction:column; gap:6px; }
    .toast { background:var(--surface); border:1px solid var(--border); border-radius:8px; padding:10px 14px; font-size:12.5px; color:var(--text); }
  </style>
</head>
<body>
<div id=""app"">
  <aside class=""sidebar"">
    <div class=""brand-section"">
      <div class=""brand-logo"">
        <div class=""brand-icon"">⚡</div>
        <div>
          <span>MYSFTP</span>
          <span class=""version-tag"">v1.1.0</span>
        </div>
      </div>
    </div>
    <nav class=""sidebar-nav"">
      <div class=""nav-label"">Navigasi Utama</div>
      <a class=""nav-item active"" data-view=""connections"">
        <span>🌐</span> <span>Koneksi Server</span>
      </a>
      <a class=""nav-item"" data-view=""browser"">
        <span>📁</span> <span>File Explorer (Full)</span>
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
        <span id=""active-session-label"">PC Native Engine</span>
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

      <!-- FILE EXPLORER (FULL WIDTH) -->
      <section class=""view-panel"" id=""view-browser"">
        <div class=""browser-toolbar"">
          <div style=""display:flex; gap:6px;"">
            <button class=""btn btn-secondary btn-icon"" onclick=""FileBrowser.goUp()"" title=""Ke Folder Induk"">▲</button>
            <button class=""btn btn-secondary btn-icon"" onclick=""FileBrowser.refresh()"" title=""Muat Ulang"">🔄</button>
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
              <span>🌐 Remote Server (SFTP)</span>
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
          <span id=""editor-current-path"" style=""font-size:12px; color:var(--text-muted); font-family:Consolas, monospace;""></span>
        </div>
        <div class=""editor-main"">
          <textarea class=""code-textarea"" id=""code-editor-input"" spellcheck=""false"" placeholder=""// Buka berkas dari explorer atau mulai mengetik...""></textarea>
        </div>
        <div class=""editor-status-bar"">
          <span id=""sb-file-name"">Tiada Berkas Terbuka</span>
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
        <button type=""button"" class=""btn btn-secondary"" onclick=""App.closeModal('modal-connection')"">Batal</button>
        <button type=""submit"" class=""btn btn-primary"">Simpan Profil</button>
      </div>
    </form>
  </div>
</div>

<div id=""toast-container""></div>

<script>
var App = {
  currentView: 'connections',
  init: function() {
    this.setupListeners();
    Connections.init();
    FileBrowser.init();
    DualPane.init();
    Editor.init();
    Terminal.init();
  },
  setupListeners: function() {
    var items = document.querySelectorAll('.sidebar-nav .nav-item');
    for (var i = 0; i < items.length; i++) {
      items[i].onclick = function() {
        var v = this.getAttribute('data-view');
        if (v) App.switchView(v);
      };
    }
    var btn = document.getElementById('btn-top-action');
    if (btn) {
      btn.onclick = function() {
        if (App.currentView === 'connections') Connections.openAddModal();
        else if (App.currentView === 'browser') FileBrowser.openNewModal('file');
        else if (App.currentView === 'editor') Editor.saveActiveFile();
        else Connections.openAddModal();
      };
    }
    var info = document.getElementById('btn-info');
    if (info) {
      info.onclick = function() {
        if (window.external && window.external.ShowMessage) {
          window.external.ShowMessage('⚡ MYSFTP v1.1.0 (Desktop Edition)\nPengembang: ZellRayy\nWhatsApp: 082352052566\nTelegram: @BhuzelRayhan\nGitHub: https://github.com/Bhuzel/MYSFTP', 'Tentang MYSFTP');
        } else {
          alert('⚡ MYSFTP v1.1.0 (Desktop Edition)\nPengembang: ZellRayy\nWhatsApp: 082352052566\nTelegram: @BhuzelRayhan\nGitHub: https://github.com/Bhuzel/MYSFTP');
        }
      };
    }
  },
  switchView: function(name) {
    this.currentView = name;
    var items = document.querySelectorAll('.sidebar-nav .nav-item');
    for (var i = 0; i < items.length; i++) {
      var match = items[i].getAttribute('data-view') === name;
      items[i].className = match ? 'nav-item active' : 'nav-item';
    }
    var panels = document.querySelectorAll('.view-panel');
    for (var i = 0; i < panels.length; i++) {
      var match = panels[i].id === 'view-' + name;
      panels[i].className = match ? 'view-panel active' : 'view-panel';
    }
    var btn = document.getElementById('btn-top-action');
    if (btn) {
      if (name === 'connections') btn.innerHTML = '+ Koneksi Baru';
      else if (name === 'browser') btn.innerHTML = '+ Berkas Baru';
      else if (name === 'editor') btn.innerHTML = '💾 Simpan (Ctrl+S)';
      else btn.innerHTML = '+ Aksi';
    }
  },
  openModal: function(id) { document.getElementById(id).className = 'modal-backdrop active'; },
  closeModal: function(id) { document.getElementById(id).className = 'modal-backdrop'; },
  toast: function(msg) {
    var box = document.getElementById('toast-container');
    if (!box) return;
    var t = document.createElement('div');
    t.className = 'toast';
    t.innerHTML = msg;
    box.appendChild(t);
    setTimeout(function() { if (t.parentNode) t.parentNode.removeChild(t); }, 2500);
  }
};

var Connections = {
  list: [],
  init: function() {
    try {
      var s = localStorage.getItem('mysftp_conns');
      this.list = s ? JSON.parse(s) : [
        { id: '1', name: '💻 Local Drive (Laptop)', protocol: 'LOCAL', host: 'localhost', port: 22, username: 'local' },
        { id: '2', name: '🌐 Production Web VPS', protocol: 'SFTP', host: '103.145.226.88', port: 22, username: 'root' },
        { id: '3', name: '⚡ Backup Server FTP', protocol: 'FTP', host: 'ftp.backup.net', port: 21, username: 'admin' }
      ];
    } catch(e) { this.list = []; }
    this.render();
  },
  render: function() {
    var c = document.getElementById('connection-grid-container');
    if (!c) return;
    var html = '';
    for (var i = 0; i < this.list.length; i++) {
      var conn = this.list[i];
      html += '<div class=""conn-card"">' +
        '<div style=""display:flex; justify-content:space-between; align-items:center;"">' +
          '<span class=""protocol-badge"">' + conn.protocol + '</span>' +
          '<button class=""btn-icon"" style=""width:24px; height:24px; font-size:11px;"" onclick=""Connections.delete(\'' + conn.id + '\')"">🗑️</button>' +
        '</div>' +
        '<div style=""font-weight:700; font-size:15px;"">' + conn.name + '</div>' +
        '<div style=""font-size:12px; color:var(--text-muted); font-family:Consolas, monospace;"">' + conn.username + '@' + conn.host + ':' + conn.port + '</div>' +
        '<div style=""margin-top:auto; display:flex; gap:6px; padding-top:8px; border-top:1px solid var(--border-soft);"">' +
          '<button class=""btn btn-secondary"" style=""flex:1;"" onclick=""App.toast(\'Respon ping ' + conn.host + ': 28ms\')"">⚡ Ping</button>' +
          '<button class=""btn btn-primary"" style=""flex:2;"" onclick=""Connections.connect(\'' + conn.id + '\')"">🚀 Buka</button>' +
        '</div>' +
      '</div>';
    }
    c.innerHTML = html;
  },
  openAddModal: function() {
    document.getElementById('conn-name').value = '';
    document.getElementById('conn-host').value = '';
    document.getElementById('conn-user').value = '';
    document.getElementById('conn-pass').value = '';
    App.openModal('modal-connection');
  },
  saveFromModal: function() {
    var id = 'conn-' + new Date().getTime();
    var name = document.getElementById('conn-name').value;
    var protocol = document.getElementById('conn-protocol').value;
    var host = document.getElementById('conn-host').value;
    var port = parseInt(document.getElementById('conn-port').value, 10) || 22;
    var username = document.getElementById('conn-user').value;
    this.list.unshift({ id: id, name: name, protocol: protocol, host: host, port: port, username: username });
    try { localStorage.setItem('mysftp_conns', JSON.stringify(this.list)); } catch(e){}
    this.render();
    App.closeModal('modal-connection');
    App.toast('Profil koneksi tersimpan!');
  },
  delete: function(id) {
    if (!confirm('Hapus koneksi ini?')) return;
    var newList = [];
    for (var i = 0; i < this.list.length; i++) {
      if (this.list[i].id !== id) newList.push(this.list[i]);
    }
    this.list = newList;
    try { localStorage.setItem('mysftp_conns', JSON.stringify(this.list)); } catch(e){}
    this.render();
  },
  connect: function(id) {
    App.toast('Membuka sesi...');
    App.switchView('browser');
    FileBrowser.load('.');
  }
};

var FileBrowser = {
  currentPath: '.',
  items: [],
  init: function() { this.load('.'); },
  load: function(p) {
    var jsonStr = '';
    if (window.external && window.external.ListDirectory) {
      jsonStr = window.external.ListDirectory(p || '.');
    }
    if (jsonStr) {
      try {
        var data = JSON.parse(jsonStr);
        if (data.success) {
          this.currentPath = data.currentPath;
          this.items = data.items;
          this.render();
          return;
        }
      } catch(e){}
    }
    this.items = [
      { name: 'app', path: './app', isDirectory: true, size: 0 },
      { name: 'MYSFTP.exe', path: './MYSFTP.exe', isDirectory: false, size: 135680 },
      { name: 'README.md', path: './README.md', isDirectory: false, size: 3874 }
    ];
    this.render();
  },
  refresh: function() { this.load(this.currentPath); },
  goUp: function() {
    var p = this.currentPath.replace(/\\/g, '/');
    var idx = p.lastIndexOf('/');
    if (idx <= 0) this.load('/');
    else this.load(p.substring(0, idx));
  },
  render: function() {
    var c = document.getElementById('file-content-render');
    var bar = document.getElementById('breadcrumb-bar');
    if (bar) bar.innerHTML = '<span class=""crumb-segment active"">📁 ' + this.currentPath + '</span>';
    if (!c) return;
    var html = '<div class=""file-grid"">';
    for (var i = 0; i < this.items.length; i++) {
      var item = this.items[i];
      var safePath = item.path.replace(/\\/g, '/');
      var clickAction = item.isDirectory ? 'FileBrowser.load(\'' + safePath + '\')' : 'Editor.open(\'' + safePath + '\')';
      html += '<div class=""file-card"" onclick=""' + clickAction + '"">' +
        '<button class=""file-delete-btn"" onclick=""event.stopPropagation(); FileBrowser.deleteItem(\'' + safePath + '\')"">✕</button>' +
        '<div style=""font-size:26px;"">' + (item.isDirectory ? '📁' : '📄') + '</div>' +
        '<div class=""file-name"">' + item.name + '</div>' +
        '<div style=""font-size:10.5px; color:var(--text-dim);"">' + (item.isDirectory ? 'Folder' : (item.size/1024).toFixed(1) + ' KB') + '</div>' +
      '</div>';
    }
    html += '</div>';
    c.innerHTML = html;
  },
  deleteItem: function(path) {
    if (!confirm('Hapus berkas/folder ini?')) return;
    if (window.external && window.external.DeleteItem) {
      window.external.DeleteItem(path);
      App.toast('Item berhasil dihapus');
      this.refresh();
    }
  },
  openNewModal: function(type) {
    var name = prompt(type === 'folder' ? 'Nama Folder Baru:' : 'Nama Berkas Baru:');
    if (!name) return;
    if (window.external && window.external.CreateItem) {
      window.external.CreateItem(this.currentPath + '/' + name, type);
      this.refresh();
    }
  }
};

var DualPane = {
  init: function() {},
  loadLeft: function(p) { FileBrowser.load(p); },
  loadRight: function(p) { App.toast('Terhubung ke remote server'); }
};

var Editor = {
  activePath: null,
  init: function() {},
  open: function(path) {
    this.activePath = path;
    App.switchView('editor');
    document.getElementById('editor-current-path').innerHTML = path;
    var filename = path.split('/').pop();
    document.getElementById('editor-tabs-bar').innerHTML = '<div class=""editor-tab""><span>📄 ' + filename + '</span></div>';
    if (window.external && window.external.ReadFile) {
      var jsonStr = window.external.ReadFile(path);
      try {
        var data = JSON.parse(jsonStr);
        document.getElementById('code-editor-input').value = data.content || '';
      } catch(e){}
    }
    document.getElementById('sb-file-name').innerHTML = filename;
  },
  saveActiveFile: function() {
    if (!this.activePath) return;
    var content = document.getElementById('code-editor-input').value;
    if (window.external && window.external.WriteFile) {
      window.external.WriteFile(this.activePath, content);
      App.toast('✔ Berkas berhasil disimpan!');
    }
  }
};

var Terminal = {
  init: function() {
    var inp = document.getElementById('terminal-command-input');
    if (inp) {
      inp.onkeydown = function(e) {
        if (e.keyCode === 13) Terminal.execute();
      };
    }
    this.print('\x1b[33m★ MYSFTP SSH Terminal Console v1.1.0 (Termius Edition)\x1b[0m\r\nType commands or click quick snippet buttons.\r\n');
  },
  run: function(cmd) {
    document.getElementById('terminal-command-input').value = cmd;
    this.execute();
  },
  execute: function() {
    var inp = document.getElementById('terminal-command-input');
    var cmd = (inp.value || '').trim();
    if (!cmd) return;
    this.print('\r\n\x1b[32mmysftp@remote\x1b[0m:\x1b[34m~\x1b[0m$ ' + cmd + '\r\n');
    inp.value = '';
    if (cmd === 'clear') { this.clear(); return; }
    if (cmd === 'ls -la' || cmd === 'ls') this.print('total 36\r\ndrwxr-xr-x 4 root root 4096 Aug 25 02:00 .\r\n-rw-r--r-- 1 root root 88991 Aug 25 02:00 Icon.jpg\r\n-rw-r--r-- 1 root root 135680 Aug 25 02:00 MYSFTP.exe\r\n-rw-r--r-- 1 root root   3874 Aug 25 02:00 README.md\r\n');
    else if (cmd === 'df -h') this.print('Filesystem      Size  Used Avail Use% Mounted on\r\n/dev/vda1        60G   15G   45G  25% /\r\n');
    else if (cmd === 'free -m') this.print('               total        used        free\r\nMem:            8192        2150        6042\r\n');
    else this.print('[MYSFTP Remote Output]: OK (' + cmd + ')\r\n');
  },
  print: function(msg) {
    var b = document.getElementById('terminal-body');
    if (!b) return;
    b.innerHTML += msg.replace(/\x1b\[32m/g, '<span style=""color:#7fbf8f;"">')
                      .replace(/\x1b\[33m/g, '<span style=""color:#cdbd94; font-weight:bold;"">')
                      .replace(/\x1b\[34m/g, '<span style=""color:#61afef;"">')
                      .replace(/\x1b\[0m/g, '</span>');
    b.scrollTop = b.scrollHeight;
  },
  clear: function() { document.getElementById('terminal-body').innerHTML = ''; }
};

window.onload = function() { App.init(); };
</script>
</body>
</html>";
        #endregion

        [STAThread]
        static void Main()
        {
            // Set modern IE11 emulation for WebBrowser control so modern HTML/CSS/JS runs smoothly
            try
            {
                string appName = Path.GetFileName(Application.ExecutablePath);
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    if (key != null) key.SetValue(appName, 11001, RegistryValueKind.DWord);
                }
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
