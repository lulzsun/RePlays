using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Replays.Messages;
using Squirrel;
using static Replays.Helpers.Functions;

namespace WinFormsApp
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
            InitializeWebView2();
            PurgeTempVideos();
            CheckForUpdates();
        }
        private async Task CheckForUpdates()
        {
            using (var manager = UpdateManager.GitHubUpdateManager("https://github.com/lulzsun/RePlays"))
            {
                await manager.Result.UpdateApp();
            }
        }

        private async void InitializeWebView2()
        {
            CoreWebView2EnvironmentOptions environmentOptions = new CoreWebView2EnvironmentOptions()
            {
                AdditionalBrowserArguments = "--unlimited-storage"
            };
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, environmentOptions);
            await webView21.EnsureCoreWebView2Async(environment);
        }

        private async void CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            // if null, webview2 runtime is missing, prompt error?
            await webView21.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.setIgnoreCertificateErrors", "{\"ignore\": true}");
            webView21.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView21.CoreWebView2.Settings.IsWebMessageEnabled = true;
            //webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        }

        private void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            WebMessage.RecieveMessage(webView21, e.WebMessageAsJson);
        }
    }
}
