using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using RePlays.Messages;
using RePlays.Services;
using Squirrel;
using static RePlays.Helpers.Functions;

namespace WinFormsApp {
    public partial class frmMain : Form {
        public frmMain() {
            SettingsService.LoadSettings();
            SettingsService.SaveSettings();
            InitializeComponent();
            InitializeWebView2();
            PurgeTempVideos();
            notifyIcon1.Icon = SystemIcons.Application;
            //CheckForUpdates();
        }
        private async Task CheckForUpdates() {
            using (var manager = UpdateManager.GitHubUpdateManager("https://github.com/lulzsun/RePlays")) {
                await manager.Result.UpdateApp();
            }
        }

        private async void InitializeWebView2() {
            CoreWebView2EnvironmentOptions environmentOptions = new CoreWebView2EnvironmentOptions() {
                AdditionalBrowserArguments = "--unlimited-storage"
            };
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, environmentOptions);
            await webView21.EnsureCoreWebView2Async(environment);
        }

        private async void CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e) {
            if (webView21.CoreWebView2 == null) {
                DialogResult result =
                    MessageBox.Show(
                        "Microsoft Edge WebView2 Runtime is required to display interface. Would you like to download and run the installer?",
                        "Missing Microsoft Edge WebView2 Runtime", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes) {
                    Process.Start("https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section");
                }
            }
            await webView21.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.setIgnoreCertificateErrors", "{\"ignore\": true}");
            webView21.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView21.CoreWebView2.Settings.IsWebMessageEnabled = true;
            //webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        }

        private void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e) {
            WebMessage.RecieveMessage(webView21, e.WebMessageAsJson);
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.WindowsShutDown) return;

            if (!new StackTrace().GetFrames().Any(x => x.GetMethod().Name == "Close")) {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }
        }

        FormWindowState _PreviousWindowState;
        private void frmMain_Resize(object sender, System.EventArgs e) {
            if (this.WindowState != FormWindowState.Minimized)
                _PreviousWindowState = WindowState;

            if (this.WindowState != FormWindowState.Minimized) {
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
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, System.EventArgs e) {
            CheckForUpdates();
        }
    }
}