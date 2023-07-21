﻿#if WINDOWS
using Microsoft.Web.WebView2.Core;
using RePlays.Recorders;
using RePlays.Services;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static RePlays.Utils.Functions;
using System.Threading;

namespace RePlays {
    public partial class frmMain : Form {
        public static Microsoft.Web.WebView2.WinForms.WebView2 webView2;
        public ContextMenuStrip recentLinksMenu;
        public static frmMain Instance;

        public frmMain() {
            Instance = this;
            SettingsService.LoadSettings();
            SettingsService.SaveSettings();
            ScreenSize.UpdateMaximumScreenResolution();
            StorageService.ManageStorage();
            HotkeyService.Start();
            InitializeComponent();
            PurgeTempVideos();
            Updater.CheckForUpdates();
            notifyIcon1.Icon = this.Icon;

            // INIT RECORDER API
            if (!File.Exists(Path.Join(Application.StartupPath, "obs.dll"))) {
#if DEBUG
                WebMessage.DisplayModal("Missing obs.dll, make sure to check to wiki to learn how to build libobs", "Warning", "warning");
#else
                WebMessage.DisplayModal("Missing obs.dll, recording may not be functional", "Warning", "warning");
#endif
                if (File.Exists(Path.Join(GetPlaysLtcFolder(), "PlaysTVComm.exe"))) {
                    RecordingService.Start(typeof(PlaysLTCRecorder));
                }
            }
            else {
                RecordingService.Start(typeof(LibObsRecorder));
            }
            GetAudioDevices();

            Thread socketThread = new(StartSocketServer);
            socketThread.Start();
        }

        private void frmMain_Load(object sender, System.EventArgs e) {
            recentLinksMenu = new();
            recentLinksMenu.Items.Add("Left click to copy to clipboard. Right click to open URL.").Enabled = false;

            if (SettingsService.Settings.generalSettings.startMinimized) {
                this.Size = new Size(1080, 600);
                CenterToScreen();
                this.WindowState = FormWindowState.Minimized;
                this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
                this.Opacity = 0;
                this.ShowInTaskbar = false;
                label1.Visible = false;
                firstTime = false;
            }
            else {
                InitializeWebView2();
            }
        }

        private void RefreshLoader() {
            pictureBox1.Size = new Size(128, 128);
            pictureBox1.Location = new Point((this.Size.Width / 2) - (pictureBox1.Size.Width / 2),
                                            (this.Size.Height / 2) - (pictureBox1.Size.Height / 2));
            pictureBox1.Refresh();
        }

        bool firstLaunch = true;
        private async void InitializeWebView2() {
            RefreshLoader();

            try {
                if (firstLaunch) {
                    Logger.WriteLine("WebView2 Version: " + CoreWebView2Environment.GetAvailableBrowserVersionString());
                    firstLaunch = false;
                }
            }
            catch (WebView2RuntimeNotFoundException exception) {
                Logger.WriteLine(exception.Message);
                Logger.WriteLine("Prompting user to install WebView2");
                DialogResult result =
                    MessageBox.Show(
                        "Microsoft Edge WebView2 Runtime is required to run this application.",
                        "Missing Microsoft Edge WebView2 Runtime", MessageBoxButtons.OK);
            }

            if (webView2 == null || webView2.IsDisposed) {
                webView2 = new Microsoft.Web.WebView2.WinForms.WebView2();
                webView2.Dock = DockStyle.Fill;
                webView2.CoreWebView2InitializationCompleted += CoreWebView2InitializationCompleted;
                webView2.WebMessageReceived += WebMessageReceivedAsync;
                CoreWebView2EnvironmentOptions environmentOptions = new CoreWebView2EnvironmentOptions() {
                    AdditionalBrowserArguments = "--unlimited-storage --disable-web-security --allow-file-access-from-files --allow-file-access",
                    Language = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName
                };
                CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, GetCfgFolder(), environmentOptions);
                await webView2.EnsureCoreWebView2Async(environment);
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
            await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.setIgnoreCertificateErrors", "{\"ignore\": true}");
            webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView2.CoreWebView2.Settings.IsWebMessageEnabled = true;
            webView2.CoreWebView2.PermissionRequested += CoreWebView2PermissionRequested;
#if (DEBUG)
            webView2.CoreWebView2.SetVirtualHostNameToFolderMapping("videos.replays.app", GetPlaysFolder(), CoreWebView2HostResourceAccessKind.Allow);
            webView2.CoreWebView2.SetVirtualHostNameToFolderMapping("replays.app", Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "/ClientApp/public/", CoreWebView2HostResourceAccessKind.Allow);
#elif (RELEASE)
            webView2.CoreWebView2.SetVirtualHostNameToFolderMapping("replays.app", Application.StartupPath + "/ClientApp/build/", CoreWebView2HostResourceAccessKind.Allow);
            webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
#endif
            webView2.CoreWebView2.Navigate("https://replays.app/preload.html");
        }

        private void CoreWebView2PermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e) {
            e.State = CoreWebView2PermissionState.Allow;
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
                LoadBackupBookmarks();
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
                this.Opacity = 0;
                this.ShowInTaskbar = false;
                label1.Visible = false;
                DisposeWebView2();
            }
        }

        FormWindowState _PreviousWindowState;
        private void frmMain_Resize(object sender, System.EventArgs e) {
            RefreshLoader();

            if (this.WindowState != FormWindowState.Minimized) {
                if (!firstTime) InitializeWebView2();
                this.ShowInTaskbar = true;
                _PreviousWindowState = WindowState;
            }
        }

        private static void StartSocketServer() {
            int port = 3333;
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new(ipAddress, port);
            Socket listener = new(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (true) {
                    Socket handler = listener.Accept();
                    string message = ReceiveMessage(handler);

                    if (message == "BringToForeground") {
                        Logger.WriteLine($"Got socket message: {message}");
                        frmMain.Instance.Invoke((MethodInvoker)(() => frmMain.Instance.InitializeWebView2()));
                        frmMain.Instance.Invoke((MethodInvoker)(() => frmMain.Instance.ActivateFormAndRestoreWindowState()));
                    }

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception ex) {
                Logger.WriteLine($"Socket server exception: {ex.Message}");
            }
        }

        static string ReceiveMessage(Socket socket) {
            byte[] buffer = new byte[1024];
            int bytesRead = socket.Receive(buffer);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        private void notifyIcon1_DoubleClick(object sender, System.EventArgs e) {
            ActivateFormAndRestoreWindowState();
        }

        private void ActivateFormAndRestoreWindowState() {
            this.Activate();
            if (this.WindowState == FormWindowState.Minimized) {
                this.Opacity = 100;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = _PreviousWindowState;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, System.EventArgs e) {
            notifyIcon1.Visible = false;
            this.Close();
            Application.Exit();
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, System.EventArgs e) {
            Updater.CheckForUpdates();
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

        public void PopulateRecentLinks(ToolStripItemCollection itemCollection = null) {
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

        bool _hasHotkeyTimeout = false;
        private void button1_KeyUp(object sender, KeyEventArgs e) {
            if (HotkeyService.EditId != null && SettingsService.Settings.keybindings.ContainsKey(HotkeyService.EditId) && !_hasHotkeyTimeout) {
                SettingsService.Settings.keybindings[HotkeyService.EditId] = string.Join(" | ", PressedKeys.ToArray()).Split(" | ");
                SettingsService.SaveSettings();
                WebMessage.SendMessage(GetUserSettings());
                HotkeyService.Start();
                var cleanupTask = new Task(() => ClearKeys()); cleanupTask.Start();
            }
            if (webView2 != null) webView2.Focus();
            else pictureBox1.Focus();
            _hasHotkeyTimeout = PressedKeys.Count >= 2;
        }

        public async void ClearKeys() {
            await Task.Delay(1000);
            _hasHotkeyTimeout = false;
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
#endif