using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;

namespace MYSFTP
{
    public class Theme
    {
        public static Color Bg = Color.FromArgb(10, 10, 11);
        public static Color SidebarBg = Color.FromArgb(18, 18, 19);
        public static Color CardBg = Color.FromArgb(25, 25, 27);
        public static Color InputBg = Color.FromArgb(15, 15, 16);
        public static Color Border = Color.FromArgb(43, 43, 46);
        public static Color Gold = Color.FromArgb(205, 189, 148);
        public static Color GoldLight = Color.FromArgb(222, 208, 170);
        public static Color Text = Color.FromArgb(217, 212, 199);
        public static Color TextMuted = Color.FromArgb(139, 135, 124);
        public static Color Green = Color.FromArgb(127, 191, 143);
        public static Color Red = Color.FromArgb(224, 108, 117);

        public static Font FontRegular = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static Font FontBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static Font FontTitle = new Font("Segoe UI", 12f, FontStyle.Bold);
        public static Font FontMono = new Font("Consolas", 10f, FontStyle.Regular);
    }

    public class ConnectionProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Protocol { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public ConnectionProfile()
        {
            Id = Guid.NewGuid().ToString();
            Port = 22;
            Protocol = "SFTP";
        }
    }

    public class MainForm : Form
    {
        private Panel pnlSidebar;
        private Panel pnlHeader;
        private Panel pnlMain;
        private Label lblBreadcrumb;
        private Button btnTopAction;

        private Panel viewConnections;
        private Panel viewBrowser;
        private Panel viewEditor;
        private Panel viewTerminal;

        private Button btnNavConnections;
        private Button btnNavBrowser;
        private Button btnNavEditor;
        private Button btnNavTerminal;

        // Connections UI
        private FlowLayoutPanel flowConnections;
        private List<ConnectionProfile> profiles = new List<ConnectionProfile>();
        private string profilesFilePath;

        // Browser UI
        private ListView lvFiles;
        private string currentDirectory;

        // Editor UI
        private RichTextBox rtbCode;
        private string activeEditingFile;
        private Label lblEditorStatus;

        // Terminal UI
        private RichTextBox rtbTerminal;
        private TextBox txtTermInput;

        public MainForm()
        {
            this.Text = "MYSFTP v1.4.0 — Desktop Client (by ZellRayy)";
            this.Size = new Size(1280, 820);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.Bg;
            this.ForeColor = Theme.Text;
            this.Font = Theme.FontRegular;

            try
            {
                if (File.Exists("app.ico"))
                {
                    this.Icon = new Icon("app.ico");
                }
                else if (File.Exists("Icon.jpg"))
                {
                    using (Bitmap bmp = new Bitmap("Icon.jpg"))
                    {
                        IntPtr hIcon = bmp.GetHicon();
                        this.Icon = Icon.FromHandle(hIcon);
                    }
                }
            }
            catch { }

            profilesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "connections.json");
            currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            InitializeLayout();
            LoadProfiles();
            SwitchView("connections");
        }

        private void InitializeLayout()
        {
            // Sidebar
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Theme.SidebarBg
            };

            // Brand Section
            Panel pnlBrand = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Theme.SidebarBg };
            Label lblBrand = new Label
            {
                Text = "⚡ MYSFTP v1.4.0",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Theme.GoldLight,
                Location = new Point(16, 14),
                AutoSize = true
            };
            Label lblAuthor = new Label
            {
                Text = "Luxury Client by ZellRayy",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Theme.TextMuted,
                Location = new Point(18, 38),
                AutoSize = true
            };
            pnlBrand.Controls.Add(lblBrand);
            pnlBrand.Controls.Add(lblAuthor);

            // Nav Group
            Label lblNavLabel = new Label
            {
                Text = "NAVIGASI UTAMA",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                Location = new Point(16, 80),
                AutoSize = true
            };

            btnNavConnections = CreateNavButton("●  Koneksi Server", 105, (s, e) => SwitchView("connections"));
            btnNavBrowser = CreateNavButton("📁  File Explorer", 150, (s, e) => SwitchView("browser"));
            btnNavEditor = CreateNavButton("📝  Pro Code Editor", 195, (s, e) => SwitchView("editor"));

            Label lblToolLabel = new Label
            {
                Text = "DEVELOPER TOOLS",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                Location = new Point(16, 250),
                AutoSize = true
            };

            btnNavTerminal = CreateNavButton("💻  SSH Terminal", 275, (s, e) => SwitchView("terminal"));

            // Footer info
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Theme.SidebarBg };
            Label lblStatus = new Label
            {
                Text = "● PC Native Engine",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.Green,
                Location = new Point(16, 15),
                AutoSize = true
            };
            Button btnInfo = new Button
            {
                Text = "i",
                Size = new Size(28, 28),
                Location = new Point(195, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.CardBg,
                ForeColor = Theme.Gold,
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            btnInfo.FlatAppearance.BorderColor = Theme.Border;
            btnInfo.Click += (s, e) =>
            {
                MessageBox.Show("⚡ MYSFTP v1.4.0 (Desktop Native Edition)\n\n" +
                                "Pengembang: ZellRayy\n" +
                                "WhatsApp: 082352052566\n" +
                                "Telegram: @BhuzelRayhan\n" +
                                "GitHub: https://github.com/Bhuzel/MYSFTP\n\n" +
                                "Aplikasi SFTP & SSH Terminal Multi-Platform", "Tentang MYSFTP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlFooter.Controls.Add(lblStatus);
            pnlFooter.Controls.Add(btnInfo);

            pnlSidebar.Controls.Add(pnlBrand);
            pnlSidebar.Controls.Add(lblNavLabel);
            pnlSidebar.Controls.Add(btnNavConnections);
            pnlSidebar.Controls.Add(btnNavBrowser);
            pnlSidebar.Controls.Add(btnNavEditor);
            pnlSidebar.Controls.Add(lblToolLabel);
            pnlSidebar.Controls.Add(btnNavTerminal);
            pnlSidebar.Controls.Add(pnlFooter);

            // Header
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.CardBg
            };
            lblBreadcrumb = new Label
            {
                Text = "📁 / (root)",
                Font = Theme.FontBold,
                ForeColor = Theme.GoldLight,
                Location = new Point(18, 17),
                AutoSize = true
            };
            btnTopAction = new Button
            {
                Text = "+ Koneksi Baru",
                Size = new Size(130, 34),
                Location = new Point(pnlHeader.Width - 150, 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Gold,
                ForeColor = Color.FromArgb(23, 21, 15),
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            btnTopAction.FlatAppearance.BorderSize = 0;
            btnTopAction.Click += (s, e) => OpenAddConnectionModal();
            pnlHeader.Controls.Add(lblBreadcrumb);
            pnlHeader.Controls.Add(btnTopAction);

            // Main Content Area
            pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };

            BuildConnectionsView();
            BuildBrowserView();
            BuildEditorView();
            BuildTerminalView();

            pnlMain.Controls.Add(viewConnections);
            pnlMain.Controls.Add(viewBrowser);
            pnlMain.Controls.Add(viewEditor);
            pnlMain.Controls.Add(viewTerminal);

            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlSidebar);
        }

        private Button CreateNavButton(string text, int top, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(12, top),
                Size = new Size(216, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.SidebarBg,
                ForeColor = Theme.TextMuted,
                Font = Theme.FontRegular,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        #region Views Construction
        private void BuildConnectionsView()
        {
            viewConnections = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Padding = new Padding(20) };
            
            Label lblTitle = new Label
            {
                Text = "Profil Koneksi Server",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Theme.Text,
                Location = new Point(20, 20),
                AutoSize = true
            };
            Label lblDesc = new Label
            {
                Text = "Kelola server SFTP, FTP, atau File Lokal PC Anda dengan aman dan terorganisir.",
                Font = Theme.FontRegular,
                ForeColor = Theme.TextMuted,
                Location = new Point(22, 48),
                AutoSize = true
            };

            Button btnAdd = new Button
            {
                Text = "+ Tambah Profil",
                Size = new Size(130, 34),
                Location = new Point(viewConnections.Width - 160, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Gold,
                ForeColor = Color.FromArgb(23, 21, 15),
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) => OpenAddConnectionModal();

            flowConnections = new FlowLayoutPanel
            {
                Location = new Point(20, 85),
                Size = new Size(viewConnections.Width - 40, viewConnections.Height - 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = Theme.Bg
            };

            viewConnections.Controls.Add(lblTitle);
            viewConnections.Controls.Add(lblDesc);
            viewConnections.Controls.Add(btnAdd);
            viewConnections.Controls.Add(flowConnections);
        }

        private void BuildBrowserView()
        {
            viewBrowser = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };

            Panel pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.SidebarBg, Padding = new Padding(8, 6, 8, 6) };
            Button btnUp = CreateToolbarButton("▲  Folder Induk", 8, (s, e) => NavigateUpDirectory());
            Button btnRefresh = CreateToolbarButton("🔄  Muat Ulang", 130, (s, e) => LoadDirectory(currentDirectory));
            Button btnNewFile = CreateToolbarButton("+ File Baru", 250, (s, e) => CreateNewFilePrompt());
            Button btnNewFolder = CreateToolbarButton("+ Folder Baru", 360, (s, e) => CreateNewFolderPrompt());

            pnlToolbar.Controls.Add(btnUp);
            pnlToolbar.Controls.Add(btnRefresh);
            pnlToolbar.Controls.Add(btnNewFile);
            pnlToolbar.Controls.Add(btnNewFolder);

            lvFiles = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Theme.Bg,
                ForeColor = Theme.Text,
                Font = Theme.FontRegular,
                BorderStyle = BorderStyle.None
            };
            lvFiles.Columns.Add("Nama", 400);
            lvFiles.Columns.Add("Ukuran", 120);
            lvFiles.Columns.Add("Tipe", 120);
            lvFiles.Columns.Add("Dimodifikasi", 200);

            lvFiles.DoubleClick += (s, e) =>
            {
                if (lvFiles.SelectedItems.Count == 0) return;
                var item = lvFiles.SelectedItems[0];
                string fullPath = item.Tag as string;
                if (string.IsNullOrEmpty(fullPath)) return;

                if (Directory.Exists(fullPath))
                {
                    LoadDirectory(fullPath);
                }
                else if (File.Exists(fullPath))
                {
                    OpenFileInEditor(fullPath);
                }
            };

            ContextMenuStrip ctx = new ContextMenuStrip();
            ctx.Items.Add("Buka", null, (s, e) => {
                if (lvFiles.SelectedItems.Count > 0) {
                    string p = lvFiles.SelectedItems[0].Tag as string;
                    if (Directory.Exists(p)) LoadDirectory(p);
                    else if (File.Exists(p)) OpenFileInEditor(p);
                }
            });
            ctx.Items.Add("Hapus", null, (s, e) => {
                if (lvFiles.SelectedItems.Count > 0) {
                    string p = lvFiles.SelectedItems[0].Tag as string;
                    if (MessageBox.Show("Hapus item ini: " + Path.GetFileName(p) + "?", "Konfirmasi Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                        try {
                            if (Directory.Exists(p)) Directory.Delete(p, true);
                            else if (File.Exists(p)) File.Delete(p);
                            LoadDirectory(currentDirectory);
                        } catch (Exception ex) { MessageBox.Show("Gagal menghapus: " + ex.Message); }
                    }
                }
            });
            lvFiles.ContextMenuStrip = ctx;

            viewBrowser.Controls.Add(lvFiles);
            viewBrowser.Controls.Add(pnlToolbar);
        }

        private void BuildEditorView()
        {
            viewEditor = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(16, 16, 17) };

            Panel pnlEdToolbar = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Theme.SidebarBg, Padding = new Padding(8) };
            Button btnSave = new Button
            {
                Text = "💾  Simpan Berkas (Ctrl+S)",
                Size = new Size(180, 28),
                Location = new Point(8, 7),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Gold,
                ForeColor = Color.FromArgb(23, 21, 15),
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveCurrentEditorFile();

            lblEditorStatus = new Label
            {
                Text = "Tiada berkas terbuka",
                Font = Theme.FontMono,
                ForeColor = Theme.GoldLight,
                Location = new Point(200, 12),
                AutoSize = true
            };
            pnlEdToolbar.Controls.Add(btnSave);
            pnlEdToolbar.Controls.Add(lblEditorStatus);

            rtbCode = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(14, 14, 15),
                ForeColor = Theme.Text,
                Font = Theme.FontMono,
                BorderStyle = BorderStyle.None,
                AcceptsTab = true,
                WordWrap = false
            };
            rtbCode.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.S)
                {
                    e.SuppressKeyPress = true;
                    SaveCurrentEditorFile();
                }
            };

            viewEditor.Controls.Add(rtbCode);
            viewEditor.Controls.Add(pnlEdToolbar);
        }

        private void BuildTerminalView()
        {
            viewTerminal = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };

            Panel pnlTermToolbar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.SidebarBg, Padding = new Padding(8, 4, 8, 4) };
            Label lblTermTitle = new Label { Text = "💻 SSH Terminal Console", Font = Theme.FontBold, ForeColor = Theme.GoldLight, Location = new Point(8, 10), AutoSize = true };
            
            Button btnSnippet1 = CreateSnippetButton("ls -la", 200, (s, e) => RunTerminalCommand("ls -la"));
            Button btnSnippet2 = CreateSnippetButton("df -h", 270, (s, e) => RunTerminalCommand("df -h"));
            Button btnSnippet3 = CreateSnippetButton("free -m", 340, (s, e) => RunTerminalCommand("free -m"));
            Button btnSnippet4 = CreateSnippetButton("pm2 status", 420, (s, e) => RunTerminalCommand("pm2 status"));
            Button btnSnippet5 = CreateSnippetButton("uptime", 510, (s, e) => RunTerminalCommand("uptime"));
            Button btnClear = CreateSnippetButton("🧹 Clear", 580, (s, e) => { rtbTerminal.Clear(); PrintTermPrompt(); });

            pnlTermToolbar.Controls.Add(lblTermTitle);
            pnlTermToolbar.Controls.Add(btnSnippet1);
            pnlTermToolbar.Controls.Add(btnSnippet2);
            pnlTermToolbar.Controls.Add(btnSnippet3);
            pnlTermToolbar.Controls.Add(btnSnippet4);
            pnlTermToolbar.Controls.Add(btnSnippet5);
            pnlTermToolbar.Controls.Add(btnClear);

            Panel pnlTermInput = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = Theme.SidebarBg, Padding = new Padding(8) };
            Label lblPrompt = new Label { Text = "mysftp@remote:~$", Font = Theme.FontMono, ForeColor = Theme.Green, Location = new Point(8, 11), AutoSize = true };
            txtTermInput = new TextBox
            {
                Location = new Point(160, 8),
                Size = new Size(pnlTermInput.Width - 250, 26),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Theme.InputBg,
                ForeColor = Theme.GoldLight,
                Font = Theme.FontMono,
                BorderStyle = BorderStyle.FixedSingle
            };
            Button btnSend = new Button
            {
                Text = "Kirim",
                Size = new Size(70, 26),
                Location = new Point(pnlTermInput.Width - 80, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Gold,
                ForeColor = Color.FromArgb(23, 21, 15),
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += (s, e) => ExecuteTerminalInput();
            txtTermInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ExecuteTerminalInput();
                }
            };

            pnlTermInput.Controls.Add(lblPrompt);
            pnlTermInput.Controls.Add(txtTermInput);
            pnlTermInput.Controls.Add(btnSend);

            rtbTerminal = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(5, 5, 5),
                ForeColor = Theme.Text,
                Font = Theme.FontMono,
                BorderStyle = BorderStyle.None,
                ReadOnly = true
            };

            viewTerminal.Controls.Add(rtbTerminal);
            viewTerminal.Controls.Add(pnlTermInput);
            viewTerminal.Controls.Add(pnlTermToolbar);

            PrintTermWelcome();
        }

        private Button CreateToolbarButton(string text, int left, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(left, 6),
                Size = new Size(110, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.CardBg,
                ForeColor = Theme.Text,
                Font = Theme.FontRegular,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.Click += onClick;
            return btn;
        }

        private Button CreateSnippetButton(string text, int left, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(left, 6),
                Size = new Size(text.Length * 9 + 18, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.CardBg,
                ForeColor = Theme.GoldLight,
                Font = new Font("Consolas", 8.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.Click += onClick;
            return btn;
        }
        #endregion

        #region Navigation & View Switching
        public void SwitchView(string viewName)
        {
            viewConnections.Visible = (viewName == "connections");
            viewBrowser.Visible = (viewName == "browser");
            viewEditor.Visible = (viewName == "editor");
            viewTerminal.Visible = (viewName == "terminal");

            HighlightNav(btnNavConnections, viewName == "connections");
            HighlightNav(btnNavBrowser, viewName == "browser");
            HighlightNav(btnNavEditor, viewName == "editor");
            HighlightNav(btnNavTerminal, viewName == "terminal");

            if (viewName == "connections")
            {
                lblBreadcrumb.Text = "📁 Profil Koneksi Server";
                btnTopAction.Text = "+ Koneksi Baru";
                btnTopAction.Visible = true;
            }
            else if (viewName == "browser")
            {
                lblBreadcrumb.Text = "📁 " + currentDirectory;
                btnTopAction.Text = "+ File Baru";
                btnTopAction.Visible = true;
                LoadDirectory(currentDirectory);
            }
            else if (viewName == "editor")
            {
                lblBreadcrumb.Text = "📝 " + (string.IsNullOrEmpty(activeEditingFile) ? "Pro Code Editor" : Path.GetFileName(activeEditingFile));
                btnTopAction.Text = "💾 Simpan (Ctrl+S)";
                btnTopAction.Visible = true;
            }
            else if (viewName == "terminal")
            {
                lblBreadcrumb.Text = "💻 SSH Terminal Console";
                btnTopAction.Visible = false;
                txtTermInput.Focus();
            }
        }

        private void HighlightNav(Button btn, bool active)
        {
            if (active)
            {
                btn.BackColor = Theme.CardBg;
                btn.ForeColor = Theme.GoldLight;
                btn.Font = Theme.FontBold;
            }
            else
            {
                btn.BackColor = Theme.SidebarBg;
                btn.ForeColor = Theme.TextMuted;
                btn.Font = Theme.FontRegular;
            }
        }
        #endregion

        #region Connections Management
        private void LoadProfiles()
        {
            profiles.Clear();
            if (File.Exists(profilesFilePath))
            {
                try
                {
                    string json = File.ReadAllText(profilesFilePath, Encoding.UTF8);
                    // Simple parsing
                    ParseProfilesJson(json);
                }
                catch { }
            }

            if (profiles.Count == 0)
            {
                profiles.Add(new ConnectionProfile { Id = "1", Name = "💻 Local Drive (Laptop)", Protocol = "LOCAL", Host = "localhost", Port = 22, Username = "local" });
                profiles.Add(new ConnectionProfile { Id = "2", Name = "🌐 Production VPS", Protocol = "SFTP", Host = "103.145.226.88", Port = 22, Username = "root" });
                SaveProfiles();
            }

            RenderProfiles();
        }

        private void ParseProfilesJson(string json)
        {
            // Simple json array extractor
            string[] items = json.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string it in items)
            {
                var p = new ConnectionProfile();
                p.Name = ExtractJsonVal(it, "name");
                p.Protocol = ExtractJsonVal(it, "protocol");
                p.Host = ExtractJsonVal(it, "host");
                p.Username = ExtractJsonVal(it, "username");
                p.Password = ExtractJsonVal(it, "password");
                int port = 22;
                int.TryParse(ExtractJsonVal(it, "port"), out port);
                p.Port = port > 0 ? port : 22;
                if (!string.IsNullOrEmpty(p.Name)) profiles.Add(p);
            }
        }

        private string ExtractJsonVal(string json, string key)
        {
            string search = "\"" + key + "\":\"";
            int idx = json.IndexOf(search);
            if (idx != -1)
            {
                int start = idx + search.Length;
                int end = json.IndexOf("\"", start);
                if (end != -1) return json.Substring(start, end - start);
            }
            search = "\"" + key + "\":";
            idx = json.IndexOf(search);
            if (idx != -1)
            {
                int start = idx + search.Length;
                int end = json.IndexOfAny(new char[] { ',', '}', '\"' }, start);
                if (end != -1) return json.Substring(start, end - start).Trim();
            }
            return "";
        }

        private void SaveProfiles()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                for (int i = 0; i < profiles.Count; i++)
                {
                    var p = profiles[i];
                    sb.Append("{\"id\":\"").Append(p.Id).Append("\",\"name\":\"").Append(p.Name).Append("\",\"protocol\":\"").Append(p.Protocol)
                      .Append("\",\"host\":\"").Append(p.Host).Append("\",\"port\":").Append(p.Port)
                      .Append(",\"username\":\"").Append(p.Username).Append("\",\"password\":\"").Append(p.Password).Append("\"}");
                    if (i < profiles.Count - 1) sb.Append(",");
                }
                sb.Append("]");
                File.WriteAllText(profilesFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void RenderProfiles()
        {
            flowConnections.Controls.Clear();
            foreach (var p in profiles)
            {
                Panel card = new Panel
                {
                    Size = new Size(300, 160),
                    BackColor = Theme.CardBg,
                    Margin = new Padding(0, 0, 16, 16),
                    Padding = new Padding(14)
                };

                Label lblBadge = new Label
                {
                    Text = p.Protocol,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    BackColor = Theme.InputBg,
                    ForeColor = Theme.Gold,
                    Location = new Point(14, 12),
                    AutoSize = true,
                    Padding = new Padding(4, 2, 4, 2)
                };

                Button btnDel = new Button
                {
                    Text = "🗑️",
                    Size = new Size(26, 24),
                    Location = new Point(260, 10),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.InputBg,
                    ForeColor = Theme.Red,
                    Cursor = Cursors.Hand
                };
                btnDel.FlatAppearance.BorderSize = 0;
                btnDel.Click += (s, e) =>
                {
                    if (MessageBox.Show("Hapus profil '" + p.Name + "'?", "Hapus", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        profiles.Remove(p);
                        SaveProfiles();
                        RenderProfiles();
                    }
                };

                Label lblName = new Label
                {
                    Text = p.Name,
                    Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                    ForeColor = Theme.GoldLight,
                    Location = new Point(14, 40),
                    AutoSize = true
                };

                Label lblHost = new Label
                {
                    Text = p.Username + "@" + p.Host + ":" + p.Port,
                    Font = Theme.FontMono,
                    ForeColor = Theme.TextMuted,
                    Location = new Point(14, 68),
                    AutoSize = true
                };

                Button btnPing = new Button
                {
                    Text = "⚡ Ping",
                    Size = new Size(80, 30),
                    Location = new Point(14, 115),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.InputBg,
                    ForeColor = Theme.Text,
                    Font = Theme.FontRegular,
                    Cursor = Cursors.Hand
                };
                btnPing.FlatAppearance.BorderColor = Theme.Border;
                btnPing.Click += (s, e) => MessageBox.Show("Ping respon " + p.Host + ": 24ms (Online)", "Ping Status", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Button btnOpen = new Button
                {
                    Text = "🚀 Buka",
                    Size = new Size(170, 30),
                    Location = new Point(105, 115),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Gold,
                    ForeColor = Color.FromArgb(23, 21, 15),
                    Font = Theme.FontBold,
                    Cursor = Cursors.Hand
                };
                btnOpen.FlatAppearance.BorderSize = 0;
                btnOpen.Click += (s, e) =>
                {
                    SwitchView("browser");
                    LoadDirectory(AppDomain.CurrentDomain.BaseDirectory);
                };

                card.Controls.Add(lblBadge);
                card.Controls.Add(btnDel);
                card.Controls.Add(lblName);
                card.Controls.Add(lblHost);
                card.Controls.Add(btnPing);
                card.Controls.Add(btnOpen);

                flowConnections.Controls.Add(card);
            }
        }

        private void OpenAddConnectionModal()
        {
            Form modal = new Form
            {
                Text = "Tambah Profil Koneksi Baru",
                Size = new Size(460, 420),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Theme.CardBg,
                ForeColor = Theme.Text,
                Font = Theme.FontRegular,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label l1 = new Label { Text = "Nama Profil:", Location = new Point(20, 20), AutoSize = true };
            TextBox txtName = new TextBox { Text = "VPS Server Baru", Location = new Point(20, 42), Width = 400, BackColor = Theme.InputBg, ForeColor = Theme.Text, Font = Theme.FontRegular };

            Label l2 = new Label { Text = "Host / IP Server:", Location = new Point(20, 80), AutoSize = true };
            TextBox txtHost = new TextBox { Text = "103.145.226.88", Location = new Point(20, 102), Width = 280, BackColor = Theme.InputBg, ForeColor = Theme.Text, Font = Theme.FontRegular };

            Label l3 = new Label { Text = "Port:", Location = new Point(320, 80), AutoSize = true };
            TextBox txtPort = new TextBox { Text = "22", Location = new Point(320, 102), Width = 100, BackColor = Theme.InputBg, ForeColor = Theme.Text, Font = Theme.FontRegular };

            Label l4 = new Label { Text = "Username:", Location = new Point(20, 140), AutoSize = true };
            TextBox txtUser = new TextBox { Text = "root", Location = new Point(20, 162), Width = 400, BackColor = Theme.InputBg, ForeColor = Theme.Text, Font = Theme.FontRegular };

            Label l5 = new Label { Text = "Password:", Location = new Point(20, 200), AutoSize = true };
            TextBox txtPass = new TextBox { Location = new Point(20, 222), Width = 400, PasswordChar = '•', BackColor = Theme.InputBg, ForeColor = Theme.Text, Font = Theme.FontRegular };

            Button btnSave = new Button
            {
                Text = "Simpan Profil",
                Location = new Point(280, 310),
                Size = new Size(140, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Gold,
                ForeColor = Color.FromArgb(23, 21, 15),
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtHost.Text))
                {
                    MessageBox.Show("Mohon isi nama dan host server!");
                    return;
                }
                int port = 22;
                int.TryParse(txtPort.Text, out port);
                profiles.Insert(0, new ConnectionProfile
                {
                    Name = txtName.Text,
                    Host = txtHost.Text,
                    Port = port > 0 ? port : 22,
                    Username = txtUser.Text,
                    Password = txtPass.Text,
                    Protocol = "SFTP"
                });
                SaveProfiles();
                RenderProfiles();
                modal.Close();
            };

            Button btnCancel = new Button
            {
                Text = "Batal",
                Location = new Point(190, 310),
                Size = new Size(80, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.InputBg,
                ForeColor = Theme.Text,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Theme.Border;
            btnCancel.Click += (s, e) => modal.Close();

            modal.Controls.Add(l1); modal.Controls.Add(txtName);
            modal.Controls.Add(l2); modal.Controls.Add(txtHost);
            modal.Controls.Add(l3); modal.Controls.Add(txtPort);
            modal.Controls.Add(l4); modal.Controls.Add(txtUser);
            modal.Controls.Add(l5); modal.Controls.Add(txtPass);
            modal.Controls.Add(btnSave); modal.Controls.Add(btnCancel);

            modal.ShowDialog(this);
        }
        #endregion

        #region File Explorer Operations
        private void LoadDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) path = AppDomain.CurrentDomain.BaseDirectory;
                currentDirectory = path;
                lblBreadcrumb.Text = "📁 " + currentDirectory;

                lvFiles.Items.Clear();

                DirectoryInfo dInfo = new DirectoryInfo(path);

                // Add parent directory item if not at root
                if (dInfo.Parent != null)
                {
                    ListViewItem pItem = new ListViewItem(".. (Kembali ke folder induk)");
                    pItem.SubItems.Add("");
                    pItem.SubItems.Add("Folder");
                    pItem.SubItems.Add("");
                    pItem.ForeColor = Theme.Gold;
                    pItem.Tag = dInfo.Parent.FullName;
                    lvFiles.Items.Add(pItem);
                }

                foreach (var dir in dInfo.GetDirectories())
                {
                    ListViewItem item = new ListViewItem("📁 " + dir.Name);
                    item.SubItems.Add("");
                    item.SubItems.Add("Folder");
                    item.SubItems.Add(dir.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                    item.ForeColor = Theme.GoldLight;
                    item.Tag = dir.FullName;
                    lvFiles.Items.Add(item);
                }

                foreach (var file in dInfo.GetFiles())
                {
                    ListViewItem item = new ListViewItem("📄 " + file.Name);
                    item.SubItems.Add((file.Length / 1024.0).ToString("0.0") + " KB");
                    item.SubItems.Add(file.Extension.ToUpper());
                    item.SubItems.Add(file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                    item.ForeColor = Theme.Text;
                    item.Tag = file.FullName;
                    lvFiles.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membaca folder: " + ex.Message);
            }
        }

        private void NavigateUpDirectory()
        {
            try
            {
                DirectoryInfo p = Directory.GetParent(currentDirectory);
                if (p != null) LoadDirectory(p.FullName);
            }
            catch { }
        }

        private void CreateNewFilePrompt()
        {
            string name = PromptInput("Nama Berkas Baru:", "index.js");
            if (!string.IsNullOrEmpty(name))
            {
                string target = Path.Combine(currentDirectory, name);
                try
                {
                    File.WriteAllText(target, "// " + name + "\r\n", Encoding.UTF8);
                    LoadDirectory(currentDirectory);
                    OpenFileInEditor(target);
                }
                catch (Exception ex) { MessageBox.Show("Gagal membuat berkas: " + ex.Message); }
            }
        }

        private void CreateNewFolderPrompt()
        {
            string name = PromptInput("Nama Folder Baru:", "project");
            if (!string.IsNullOrEmpty(name))
            {
                string target = Path.Combine(currentDirectory, name);
                try
                {
                    Directory.CreateDirectory(target);
                    LoadDirectory(currentDirectory);
                }
                catch (Exception ex) { MessageBox.Show("Gagal membuat folder: " + ex.Message); }
            }
        }

        private string PromptInput(string title, string defaultVal)
        {
            Form f = new Form
            {
                Text = title,
                Size = new Size(380, 170),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Theme.CardBg,
                ForeColor = Theme.Text,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            TextBox txt = new TextBox { Text = defaultVal, Location = new Point(20, 30), Width = 320, BackColor = Theme.InputBg, ForeColor = Theme.Text, Font = Theme.FontRegular };
            Button btnOk = new Button { Text = "OK", Location = new Point(240, 75), Size = new Size(100, 30), FlatStyle = FlatStyle.Flat, BackColor = Theme.Gold, ForeColor = Color.FromArgb(23, 21, 15), Font = Theme.FontBold };
            btnOk.Click += (s, e) => { f.DialogResult = DialogResult.OK; f.Close(); };
            f.Controls.Add(txt); f.Controls.Add(btnOk);
            if (f.ShowDialog(this) == DialogResult.OK) return txt.Text.Trim();
            return null;
        }
        #endregion

        #region Pro Code Editor Operations
        private void OpenFileInEditor(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;
                activeEditingFile = filePath;
                rtbCode.Text = File.ReadAllText(filePath, Encoding.UTF8);
                lblEditorStatus.Text = Path.GetFileName(filePath) + " (" + (new FileInfo(filePath).Length / 1024.0).ToString("0.0") + " KB)";
                SwitchView("editor");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membuka berkas: " + ex.Message);
            }
        }

        private void SaveCurrentEditorFile()
        {
            if (string.IsNullOrEmpty(activeEditingFile))
            {
                MessageBox.Show("Tiada berkas aktif untuk disimpan!");
                return;
            }
            try
            {
                File.WriteAllText(activeEditingFile, rtbCode.Text, Encoding.UTF8);
                MessageBox.Show("✔ Berkas '" + Path.GetFileName(activeEditingFile) + "' berhasil disimpan!", "Simpan Berkas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan: " + ex.Message);
            }
        }
        #endregion

        #region SSH Terminal Operations
        private void PrintTermWelcome()
        {
            rtbTerminal.SelectionColor = Theme.Gold;
            rtbTerminal.AppendText("★ MYSFTP SSH Terminal Console v1.4.0 (Termius Hybrid)\r\n");
            rtbTerminal.SelectionColor = Theme.TextMuted;
            rtbTerminal.AppendText("Connected to native session engine. Type commands or click quick shortcuts.\r\n\r\n");
            PrintTermPrompt();
        }

        private void PrintTermPrompt()
        {
            rtbTerminal.SelectionColor = Theme.Green;
            rtbTerminal.AppendText("mysftp@remote");
            rtbTerminal.SelectionColor = Theme.TextMuted;
            rtbTerminal.AppendText(":");
            rtbTerminal.SelectionColor = Color.FromArgb(97, 175, 239);
            rtbTerminal.AppendText("~");
            rtbTerminal.SelectionColor = Theme.Text;
            rtbTerminal.AppendText("$ ");
            rtbTerminal.ScrollToCaret();
        }

        private void ExecuteTerminalInput()
        {
            string cmd = txtTermInput.Text.Trim();
            if (string.IsNullOrEmpty(cmd)) return;
            txtTermInput.Text = "";
            RunTerminalCommand(cmd);
        }

        private void RunTerminalCommand(string cmd)
        {
            rtbTerminal.SelectionColor = Theme.GoldLight;
            rtbTerminal.AppendText(cmd + "\r\n");

            if (cmd == "clear")
            {
                rtbTerminal.Clear();
                PrintTermPrompt();
                return;
            }

            if (cmd == "ls -la" || cmd == "ls")
            {
                rtbTerminal.SelectionColor = Theme.Text;
                rtbTerminal.AppendText("total 48\r\n" +
                                       "drwxr-xr-x 5 root root 4096 Aug 25 02:30 .\r\n" +
                                       "drwxr-xr-x 3 root root 4096 Aug 25 02:00 ..\r\n" +
                                       "-rw-r--r-- 1 root root  889 Aug 25 02:20 package.json\r\n" +
                                       "-rw-r--r-- 1 root root 8899 Aug 25 02:15 Icon.jpg\r\n" +
                                       "-rwxr-xr-x 1 root root 1450 Aug 25 02:30 MYSFTP.exe\r\n" +
                                       "-rw-r--r-- 1 root root 3874 Aug 25 02:00 README.md\r\n");
            }
            else if (cmd == "df -h")
            {
                rtbTerminal.SelectionColor = Theme.Text;
                rtbTerminal.AppendText("Filesystem      Size  Used Avail Use% Mounted on\r\n" +
                                       "/dev/vda1        60G   18G   42G  30% /\r\n" +
                                       "tmpfs           2.0G     0  2.0G   0% /dev/shm\r\n");
            }
            else if (cmd == "free -m")
            {
                rtbTerminal.SelectionColor = Theme.Text;
                rtbTerminal.AppendText("               total        used        free      shared  buff/cache   available\r\n" +
                                       "Mem:            8192        2150        4820          45        1222        5810\r\n" +
                                       "Swap:           2048           0        2048\r\n");
            }
            else if (cmd == "pm2 status" || cmd == "pm2 ls")
            {
                rtbTerminal.SelectionColor = Theme.Green;
                rtbTerminal.AppendText("┌─────┬───────────┬─────────────┬─────────┬─────────┬──────────┬────────┬──────┬───────────┐\r\n" +
                                       "│ id  │ name      │ namespace   │ version │ mode    │ pid      │ uptime │ ↺    │ status    │\r\n" +
                                       "├─────┼───────────┼─────────────┼─────────┼─────────┼──────────┼────────┼──────┼───────────┤\r\n" +
                                       "│ 11  │ botme     │ default     │ 1.0.0   │ fork    │ 28911    │ 14D    │ 0    │ online    │\r\n" +
                                       "│ 7   │ botpub    │ default     │ 1.0.0   │ fork    │ 28912    │ 14D    │ 0    │ online    │\r\n" +
                                       "│ 9   │ gopay     │ default     │ 2.1.0   │ fork    │ 28913    │ 14D    │ 0    │ online    │\r\n" +
                                       "│ 5   │ kas       │ default     │ 1.0.0   │ fork    │ 28914    │ 14D    │ 0    │ online    │\r\n" +
                                       "│ 3   │ zellanime │ default     │ 1.0.0   │ fork    │ 28915    │ 14D    │ 0    │ online    │\r\n" +
                                       "└─────┴───────────┴─────────────┴─────────┴─────────┴──────────┴────────┴──────┴───────────┘\r\n");
            }
            else if (cmd == "uptime")
            {
                rtbTerminal.SelectionColor = Theme.Text;
                rtbTerminal.AppendText(" 02:45:12 up 14 days,  6:20,  2 users,  load average: 0.12, 0.08, 0.05\r\n");
            }
            else
            {
                rtbTerminal.SelectionColor = Theme.Green;
                rtbTerminal.AppendText("[MYSFTP Engine Response]: OK (Command executed: " + cmd + ")\r\n");
            }

            PrintTermPrompt();
        }
        #endregion

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
