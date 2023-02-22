using System;
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
    public class StreamableUploader : BaseUploader {
        public class StreamableResult {
            public string shortcode { get; set; }
            public int status { get; set; }
        }

        public override async Task<string> Upload(string id, string title, string file, string game) {
            using (var httpClient = new HttpClient()) {
                httpClient.Timeout = Timeout.InfiniteTimeSpan; // sometimes, uploading can take long
                var streamableSettings = SettingsService.Settings.uploadSettings.streamableSettings;
                var credentials = $"{streamableSettings.email}:{DecryptString(streamableSettings.password)}";
                
                var authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authorization);

                using (var formDataContent = new MultipartFormDataContent()) {
                    var titleContent = new StringContent(title);
                    var fileContent = new ProgressableStreamContent(new StreamContent(File.OpenRead(file)), 4096,
                        (sent, total) => {
                            WebMessage.DisplayToast(id, title, "Upload", "none", (long)((float)sent/total*100), 100);
                        }
                    );
                    formDataContent.Add(fileContent, "file", "video.mp4");
                    formDataContent.Add(titleContent, "title");

                    var response = await httpClient.PostAsync("https://api.streamable.com/upload", formDataContent);
                    var content = response.Content.ReadAsStringAsync().Result;
                    Logger.WriteLine(response.StatusCode.ToString() + " " + content);
                    var result = JsonSerializer.Deserialize<StreamableResult>(content);
                    if (result.shortcode != null) {
                        return "https://streamable.com/" + result.shortcode;
                    } else {
                        throw new NullReferenceException($"shortcode is null, but response is {(int)response.StatusCode}: {response.StatusCode}");
                    }
                }
            }
        }
    }
}