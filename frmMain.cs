using System.Windows.Forms;
using Replays.Messages;

namespace WinFormsApp
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private async void CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            // if null, webview2 runtime is missing, prompt error?
            await webView21.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.setIgnoreCertificateErrors", "{\"ignore\": true}");
            webView21.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView21.CoreWebView2.Settings.IsWebMessageEnabled = true;
            //webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        }

        private void WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            WebMessage.RecieveMessage(webView21, e.WebMessageAsJson);
        }
    }
}
