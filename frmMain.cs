using System.Diagnostics;
using System.Windows.Forms;
using System.Text.Json;

namespace WinFormsApp
{
    public partial class frmMain : Form
    {
        public class WebMessage
        {
            public string message { get; set; }
            public string data { get; set; }
        }
        public frmMain()
        {
            InitializeComponent();
        }

        private async void CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            await webView21.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.setIgnoreCertificateErrors", "{\"ignore\": true}");
            webView21.CoreWebView2.Settings.IsStatusBarEnabled = false;
        }

        private void WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            WebMessage webMessage = JsonSerializer.Deserialize<WebMessage>(e.WebMessageAsJson);
            Debug.WriteLine($"{webMessage.message} ::: {webMessage.data}");

            switch (webMessage.message)
            {
                default:
                    break;
            }
        }
    }
}
