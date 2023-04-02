using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using RePlays.Services;
using RePlays.Utils;

namespace RePlays.Uploaders
{
    public class CustomUploader : BaseUploader
    {
        public override async Task<string> Upload(string id, string title, string file, string game)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = Timeout.InfiniteTimeSpan; // sometimes, uploading can take long
                var customSettings = SettingsService.Settings.uploadSettings.customUploaderSettings;

                // add the url params to the url
                var url = customSettings.urlparams.Aggregate(customSettings.url,
                    (current, param) =>
                    {
                        if (param.Key == "") return current;
                        return current + ((current.Contains("?") ? "&" : "?") + param.Key + "=" + param.Value);
                    });


                // add the custom http headers
                foreach (var header in customSettings.headers)
                {
                    if(header.Key == "") continue;
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                switch (customSettings.responseType)
                {
                    case "JSON":
                        httpClient.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        break;
                    case "XML":
                        httpClient.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/xml"));
                        break;
                    case "TEXT":
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                using (var formDataContent = new MultipartFormDataContent())
                {
                    var titleContent = new StringContent(title);
                    var fileContent = new ProgressableStreamContent(new StreamContent(File.OpenRead(file)), 4096,
                        (sent, total) =>
                        {
                            WebMessage.DisplayToast(id, title, "Upload", "none", (long)((float)sent / total * 100),
                                100);
                        }
                    );
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
                    formDataContent.Add(fileContent, "file", "video.mp4");
                    formDataContent.Add(titleContent, "title");

                    // send request using the correct method
                    HttpResponseMessage response;
                    switch (customSettings.method)
                    {
                        case "POST":
                            response = await httpClient.PostAsync(url, formDataContent);
                            break;
                        case "PUT":
                            response = await httpClient.PutAsync(url, formDataContent);
                            break;
                        case "PATCH":
                            response = await httpClient.PatchAsync(url, formDataContent);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // parse the response according the the response type and the responsepath setting using JSONpath or XMLPath
                    var content = response.Content.ReadAsStringAsync().Result;
                    Logger.WriteLine(response.StatusCode.ToString() + " " + content);

                    string result;
                    switch (customSettings.responseType)
                    {
                        case "JSON":
                            result = JsonPath.Get(content, customSettings.responsePath);
                            break;
                        case "XML":
                            result = XmlPath.Get(content, customSettings.responsePath);
                            break;
                        case "TEXT":
                            result = content;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    return result;
                }
            }
        }

        // define JsonPath and XmlPath classes
        public class JsonPath
        {
            public static string Get(string json, string path)
            {
                var jObject = JObject.Parse(json);
                var file = jObject.SelectToken(path);
                return file != null ? file.ToString() : throw new Exception("JsonPath not found");
            }
        }
       

        public class XmlPath
        {
            public static string Get(string xml, string path)
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xml);
                var parts = path.Split('/');
                var current = parts.Aggregate(doc.DocumentElement,
                    (current1, part) => (System.Xml.XmlElement)current1.SelectSingleNode(part));

                return current != null ? current.InnerText : string.Empty;
            }
        }
    }
}