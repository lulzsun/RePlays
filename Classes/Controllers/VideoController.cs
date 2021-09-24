using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using static RePlays.Helpers.Functions;

namespace RePlays.Controllers {
    [ApiController]
    public class VideoController : Controller {
        public static FileStream videoStream;

        [HttpGet("/Plays/{game}/{fileName}")]
        public IActionResult GetVideo(string game, string fileName) {
            if (videoStream != null) videoStream.Dispose();
            videoStream = new(Path.Join(GetPlaysFolder(), game + "\\", fileName), FileMode.Open, FileAccess.Read);
            FileStreamResult file = File(videoStream, "video/mp4", true);
            return file;
        }

        [HttpGet("/Plays/{game}/.thumbs/{fileName}")]
        public IActionResult GetThumb(string game, string fileName) {
            FileStream stream = new(Path.Join(GetPlaysFolder(), game + "\\.thumbs\\", fileName), FileMode.Open, FileAccess.Read);
            FileStreamResult file = File(stream, "image/png", true);
            return file;
        }

        public static void DisposeOpenStreams() {
            videoStream.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
