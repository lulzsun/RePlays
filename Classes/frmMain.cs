using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using RePlays.Recorders;
using RePlays.Services;
using Squirrel;
using RePlays.Utils;
using static RePlays.Utils.Functions;
using System.Collections.Generic;

namespace RePlays {
    public partial class frmMain : Form {
        public static Microsoft.Web.WebView2.WinForms.WebView2 webView2;
        public ContextMenuStrip recentLinksMenu;
        public static frmMain Instance;
        public static string currentVersion = "?";
        public static string latestVersion = "Offline";

        public frmMain() {
            Instance = this;
            SettingsService.LoadSettings();
            SettingsService.SaveSettings();
            StorageService.ManageStorage();
            HotkeyService.Start();
            InitializeComponent();
            PurgeTempVideos();
            CheckForUpdates();
            notifyIcon1.Icon = this.Icon;
        }

        private void frmMain_Load(object sender, System.EventArgs e) {
            recentLinksMenu = new();
            recentLinksMenu.Items.Add("Left click to copy to clipboard. Right click to open URL.").Enabled = false;

            if (SettingsService.Settings.generalSettings.startMinimized) {
                this.Size = new Size(1080, 600);
                this.FormBorderStyle = FormBorderStyle.Sizable;
                CenterToScreen();
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                firstTime = false;
            }
            else {
                InitializeWebView2();
            }

            RecordingService.Start(typeof(PlaysLTC));
        }

        private async void CheckForUpdates() {
            try {
                var manager = await UpdateManager.GitHubUpdateManager("https://github.com/lulzsun/RePlays");
                currentVersion = manager.CurrentlyInstalledVersion().ToString();
                var updateInfo = await manager.CheckForUpdate();
                latestVersion = updateInfo.FutureReleaseEntry.Version.ToString();

                if (SettingsService.Settings.generalSettings.update == "none") return;

                if (updateInfo.ReleasesToApply.Count > 0) {
                    if (SettingsService.Settings.generalSettings.update == "automatic") {
                        Logger.WriteLine($"New version found! Preparing to automatically update to version {updateInfo.FutureReleaseEntry.Version} from {updateInfo.CurrentlyInstalledVersion.Version}");
                        await manager.UpdateApp();
                        Logger.WriteLine($"Update to version {updateInfo.FutureReleaseEntry.Version} successful!");
                    }
                    else { // manual
                        WebMessage.DisplayToast("ManualUpdate", "New version available!", "Update", "info");
                    }
                    Logger.WriteLine($"Found no updates higher than current version {updateInfo.CurrentlyInstalledVersion.Version}");
                }
            }
            catch (System.Exception exception) {
                Logger.WriteLine("Error: Issue fetching update releases: " + exception.ToString());
            }
        }

        private void RefreshLoader() {
            pictureBox1.Size = new Size(pictureBox1.Image.Width, pictureBox1.Image.Height);
            pictureBox1.Location = new Point((pictureBox1.Parent.ClientSize.Width / 2) - (pictureBox1.Image.Width / 2),
                                            (pictureBox1.Parent.ClientSize.Height / 2) - (pictureBox1.Image.Height / 2));
            pictureBox1.Refresh();
        }

        private async void InitializeWebView2() {
            RefreshLoader();

            if (webView2 == null || webView2.IsDisposed) {
                webView2 = new Microsoft.Web.WebView2.WinForms.WebView2();
                webView2.Dock = DockStyle.Fill;
                webView2.CoreWebView2InitializationCompleted += CoreWebView2InitializationCompleted;
                webView2.WebMessageReceived += WebMessageReceivedAsync;
                CoreWebView2EnvironmentOptions environmentOptions = new CoreWebView2EnvironmentOptions() {
                    AdditionalBrowserArguments = "--unlimited-storage --disable-web-security --allow-file-access-from-files --allow-file-access"
                };
                CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, environmentOptions);
                await webView2.EnsureCoreWebView2Async(environment);
#if (DEBUG)
                webView2.CoreWebView2.Navigate("http://localhost:3000/#/");
#elif (RELEASE)
                webView2.CoreWebView2.Navigate("file://D:/Documents/GitHub/RePlays/ClientApp/build/index.html");
#endif

            }
        }

        private void DisposeWebView2() {
            if (webView2 != null && !webView2.IsDisposed) {
                webView2.CoreWebView2.PermissionRequested -= CoreWebView2PermissionRequested;
                webView2.CoreWebView2InitializationCompleted -= CoreWebView2InitializationCompleted;
                webView2.WebMessageReceived -= WebMessageReceivedAsync;
                webView2.Dispose();
                webView2 = null;
            }
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }

        private async void CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e) {
            if (webView2.CoreWebView2 == null) {
                DialogResult result =
                    MessageBox.Show(
                        "Microsoft Edge WebView2 Runtime is required to display interface. Would you like to download and run the installer?",
                        "Missing Microsoft Edge WebView2 Runtime", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes) {
                    Process.Start("https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section");
                }
            }
            await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.setIgnoreCertificateErrors", "{\"ignore\": true}");
            webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView2.CoreWebView2.Settings.IsWebMessageEnabled = true;
            webView2.CoreWebView2.PermissionRequested += CoreWebView2PermissionRequested;
            webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            //webView2.CoreWebView2.SetVirtualHostNameToFolderMapping("replays.local", GetPlaysFolder(), CoreWebView2HostResourceAccessKind.Allow);
        }

        private void CoreWebView2PermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e) {
            e.State = CoreWebView2PermissionState.Allow;
        }

        public static void PostWebMessageAsJson(string message) {
            if (webView2 == null || webView2.IsDisposed == true) return;
            if (webView2.InvokeRequired) {
                // Call this same method but make sure it is on UI thread
                webView2.BeginInvoke((MethodInvoker)delegate {
                    PostWebMessageAsJson(message);
                });
            }
            else
                if(webView2 != null && webView2.CoreWebView2 != null)
                    webView2.CoreWebView2.PostWebMessageAsJson(message);
        }

        bool firstTime = true;
        private async void WebMessageReceivedAsync(object sender, CoreWebView2WebMessageReceivedEventArgs e) {
            var webMessage = await WebMessage.RecieveMessage(e.WebMessageAsJson);
            if (!this.Controls.Contains(webView2) && webMessage.message == "Initialize") {
                this.Controls.Add(webView2);
                webView2.BringToFront();
                if (firstTime) {
                    this.Size = new Size(1080, 600);
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    CenterToScreen();
                    firstTime = false;
                }
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                DisposeWebView2();
            }
        }

        FormWindowState _PreviousWindowState;
        private void frmMain_Resize(object sender, System.EventArgs e) {
            RefreshLoader();

            if (this.WindowState != FormWindowState.Minimized)
                _PreviousWindowState = WindowState;

            if (this.WindowState != FormWindowState.Minimized) {
                if(!firstTime) InitializeWebView2();
                this.ShowInTaskbar = true;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, System.EventArgs e) {
            this.Activate();
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = _PreviousWindowState;
        }

        private void exitToolStripMenuItem_Click(object sender, System.EventArgs e) {
            notifyIcon1.Visible = false;
            this.Close();
            Application.Exit();
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, System.EventArgs e) {
            CheckForUpdates();
        }

        bool moving;
        Point offset;
        Point original;
        private void frmMain_MouseDown(object sender, MouseEventArgs e) {
            moving = true;
            this.Capture = true;
            offset = MousePosition;
            original = this.Location;
        }

        private void frmMain_MouseMove(object sender, MouseEventArgs e) {
            if (!moving)
                return;

            int x = original.X + MousePosition.X - offset.X;
            int y = original.Y + MousePosition.Y - offset.Y;

            this.Location = new Point(x, y);
        }

        private void frmMain_MouseUp(object sender, MouseEventArgs e) {
            moving = false;
            this.Capture = false;
        }

        private void recentLinks_ToolStripMenuItem_DropDownOpening(object sender, System.EventArgs e) {
            PopulateRecentLinks(recentLinksToolStripMenuItem.DropDown.Items);
        }

        public void PopulateRecentLinks(ToolStripItemCollection itemCollection=null) {
            if (itemCollection == null) {
                PopulateRecentLinks(recentLinksMenu.Items);
                recentLinksMenu.Show(Cursor.Position);
                return;
            }

            for (int i = 1; i < itemCollection.Count; i++) {
                itemCollection.RemoveAt(i);
            }
            foreach (var url in SettingsService.Settings.uploadSettings.recentLinks) {
                itemCollection.Add(url).MouseDown += (s, e) => {
                    if (e.Button == MouseButtons.Right) {
                        Process.Start(new ProcessStartInfo(url.Split("] ")[1]) { UseShellExecute = true });
                    }
                    else if (e.Button == MouseButtons.Left) {
                        Clipboard.SetText(url.Split("] ")[1]);
                    }
                };
            }
        }

        public void DisplayNotification(string title, string context) {
            notifyIcon1.BalloonTipText = context;
            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon1.BalloonTipTitle = title;
            notifyIcon1.ShowBalloonTip(500);
        }

        public void HideRecentLinks() {
            recentLinksMenu.Hide();
        }

        private List<Keys> PressedKeys = new();
        private void button1_KeyDown(object sender, KeyEventArgs e) {
            Keys k = e.KeyCode;
            if (k.ToString().Contains(e.Modifiers.ToString())) k = e.Modifiers;

            if (!PressedKeys.Contains(k)) {
                PressedKeys.Add(k);
            }
            PressedKeys.Sort();
            PressedKeys.Reverse();
            Logger.WriteLine(string.Join(" | ", PressedKeys.ToArray()));
        }

        private void button1_KeyUp(object sender, KeyEventArgs e) {
            if(HotkeyService.EditId != null && SettingsService.Settings.keybindings.ContainsKey(HotkeyService.EditId)) {
                SettingsService.Settings.keybindings[HotkeyService.EditId] = string.Join(" | ", PressedKeys.ToArray()).Split(" | ");
                SettingsService.SaveSettings();
                WebMessage.SendMessage(GetUserSettings());
                HotkeyService.Start();
            }
            if (webView2 != null) webView2.Focus();
            else pictureBox1.Focus();
            PressedKeys.Clear();
        }

        public void EditKeybind(string id) {
            HotkeyService.EditId = id;
            HotkeyService.Stop();
            //button1 is a hacky way of logging keypresses
            this.button1.Focus();
        }
    }
}