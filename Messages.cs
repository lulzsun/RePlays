namespace Replays.Messages
{
    public class WebMessage
    {
        public string message { get; set; }
        public string data { get; set; }
    }

    public class RetrieveVideos
    {
        public string game { get; set; }
        public string sortBy { get; set; }
    }

    public class ShowInFolder
    {
        private string _filePath;
        public string filePath
        {
            get
            {
                return _filePath.Replace("/", "\\");
            }
            set
            {
                _filePath = value;
            }
        }
    }

    public class Delete
    {
        private string[] _filePaths;
        public string[] filePaths
        {
            get
            {
                return _filePaths;
            }
            set
            {
                _filePaths = value;
                for (int i = 0; i < _filePaths.Length; i++)
                {
                    _filePaths[i] = _filePaths[i].Replace("/", "\\");
                }
            }
        }
    }
}