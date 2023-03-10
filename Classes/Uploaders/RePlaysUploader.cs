using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RePlays.Services;
using RePlays.Utils;
using SharpCompress.Common;
using static RePlays.Utils.Functions;

namespace RePlays.Uploaders {
    public class RePlaysUploader : BaseUploader {
        public class RePlaysResult {
            public string shortcode { get; set; }
            public int status { get; set; }
        }

        public class RePlaysStatus
        {
            public string title { get; set; }
            public string content { get; set; }
        }

        public override async Task<string> Upload(string id, string title, string file, string game) {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = Timeout.InfiniteTimeSpan; // sometimes, uploading can take long
                    var rePlaysSettings = SettingsService.Settings.uploadSettings.rePlaysSettings;
                    var credentials = $"{rePlaysSettings.email}:{DecryptString(rePlaysSettings.password)}";
                    var authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authorization);

                    HttpResponseMessage uploadLimitResponse = await httpClient.GetAsync("https://replays.app/GetUploadLimit");
                    int uploadFileLimit = int.Parse(await uploadLimitResponse.Content.ReadAsStringAsync());

                    int localFileSize = (int)(new FileInfo(file).Length / (1024.0 * 1024.0));
                    if (localFileSize > uploadFileLimit)
                    {
                        WebMessage.DisplayModal("Max file size is " + uploadFileLimit + " MB. Your file is " + localFileSize + " MB", "Warning", "warning");
                        return null;
                    }

                    using (var formDataContent = new MultipartFormDataContent())
                    {
                        var titleContent = new StringContent(title);
                        var gameContent = new StringContent(game);
                        var fileContent = new ProgressableStreamContent(new StreamContent(File.OpenRead(file)), 4096,
                            (sent, total) =>
                            {
                                WebMessage.DisplayToast(id, title, "Upload", "none", (long)((float)sent / total * 100), 100);
                            }
                        );
                        formDataContent.Add(fileContent, "uploaded", "video.mp4");
                        formDataContent.Add(titleContent, "title");
                        formDataContent.Add(gameContent, "game");
                        var response = await httpClient.PostAsync("https://upload.replays.app/Upload", formDataContent);
                        var content = response.Content.ReadAsStringAsync().Result;
                        Logger.WriteLine(response.StatusCode.ToString() + " " + content);
                        var result = JsonSerializer.Deserialize<RePlaysResult>(content);
                        if (result.shortcode != null)
                        {
                            Process browserProcess = new Process();
                            browserProcess.StartInfo.UseShellExecute = true;
                            browserProcess.StartInfo.FileName = "https://replays.app/Video/" + result.shortcode;
                            browserProcess.Start();
                            return "https://replays.app/Video/" + result.shortcode;
                        }

                        WebMessage.DisplayModal(result.status == 401 ? "Invalid login credentials. Please double-check and try again. " : "An unexpected error occured.", result.status == 401 ? "Wrong Credentials" : "Warning", "warning");
                        WebMessage.DestroyToast(id);
                        return null;
                    }
                }
            }
            catch
            {
                try
                {
                    string clientVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    using (var httpClient = new HttpClient())
                    {
                        HttpResponseMessage uploadLimitResponse = await httpClient.GetAsync("https://replays.app/Status/" + clientVersion);
                        string statusContent = await uploadLimitResponse.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<RePlaysStatus>(statusContent);
                        if (result.title != "OK")
                        {
                            WebMessage.DisplayModal(result.content, result.title, "warning");
                        }
                        else
                        {
                            WebMessage.DisplayModal("An unexpected error occured.", "Warning", "warning");
                        }
                    }
                    WebMessage.DestroyToast(id);
                    return null;
                }
                catch
                {
                    WebMessage.DisplayModal("An unexpected error occured.", "Warning", "warning");
                    WebMessage.DestroyToast(id);
                    return null;
                }
            }
        }
    }
}