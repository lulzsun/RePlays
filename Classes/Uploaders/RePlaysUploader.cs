using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RePlays.Services;
using RePlays.Utils;
using static RePlays.Utils.Functions;

namespace RePlays.Uploaders {
    public class RePlaysUploader : BaseUploader {
        public class StreamableResult {
            public string shortcode { get; set; }
            public int status { get; set; }
        }

        public override async Task<string> Upload(string id, string title, string file) {
            using (var httpClient = new HttpClient()) {
                httpClient.Timeout = Timeout.InfiniteTimeSpan; // sometimes, uploading can take long
                var rePlaysSettings = SettingsService.Settings.uploadSettings.rePlaysSettings;
                var credentials = $"{rePlaysSettings.email}:{DecryptString(rePlaysSettings.password)}";
                var authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authorization);

                using (var formDataContent = new MultipartFormDataContent()) {
                    var titleContent = new StringContent(title);
                    var fileContent = new ProgressableStreamContent(new StreamContent(File.OpenRead(file)), 4096,
                        (sent, total) => {
                            WebMessage.DisplayToast(id, title, "Upload", "none", (long)((float)sent/total*100), 100);
                        }
                    );
                    formDataContent.Add(fileContent, "uploaded", "video.mp4");
                    formDataContent.Add(titleContent, "title");

                    var response = await httpClient.PostAsync("https://replays.app/v1/method/upload.php", formDataContent);
                    var content = response.Content.ReadAsStringAsync().Result;
                    Logger.WriteLine(response.StatusCode.ToString() + " " + content);
                    var result = JsonSerializer.Deserialize<StreamableResult>(content);
                    if (result.shortcode != null) {
                        Process browserProcess = new Process();
                        browserProcess.StartInfo.UseShellExecute = true;
                        browserProcess.StartInfo.FileName = "https://replays.app/Video/" + result.shortcode;
                        browserProcess.Start();
                        return "https://replays.app/Video/" + result.shortcode;
                    } else {
                        throw new NullReferenceException($"shortcode is null, but response is {(int)response.StatusCode}: {response.StatusCode}");
                    }
                }
            }
        }
    }
}