using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace PlaysLTCWrapper {
    public class LTCProcess {
        TcpListener server;
        NetworkStream ns;
        Process ltcProcess;
        public void Connect(string playsLtcFolder) {
            Process currentProcess = Process.GetCurrentProcess();
            string pid = currentProcess.Id.ToString();
            int port = 9500;
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            ltcProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = playsLtcFolder,
                    Arguments = port + " " + pid + "",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            ltcProcess.OutputDataReceived += new DataReceivedEventHandler((s, e) => {
                WriteToLog("LTCPROCESS", e.Data);
            });

            ltcProcess.Start();
            TcpClient client = server.AcceptTcpClient();
            ns = client.GetStream();

            while (client.Connected || !ltcProcess.HasExited) {
                int streamByte = ns.ReadByte();
                StringBuilder stringBuilder = new StringBuilder();

                while (streamByte != 12)
                {
                    stringBuilder.Append((char)streamByte);
                    streamByte = ns.ReadByte();
                }

                string msg = stringBuilder.ToString().Replace("\n", "").Replace("\r", "").Trim();
                WriteToLog("RECEIVED", msg);

                JsonElement jsonElement = GetDataType(msg);
                string type = jsonElement.GetProperty("type").GetString();
                var data = jsonElement.GetProperty("data");

                switch (type)
                {
                    case "LTC:handshake":
                        ConnectionHandshakeArgs connectionHandshakeArgs = new ConnectionHandshakeArgs
                        {
                            Version = data.GetProperty("version").ToString(),
                            IntegrityCheck = data.GetProperty("integrityCheck").ToString(),
                        };
                        OnConnectionHandshake(connectionHandshakeArgs);
                        WriteToLog("INFO", string.Format("Connection Handshake: {0}, {1}", connectionHandshakeArgs.Version, connectionHandshakeArgs.IntegrityCheck));
                        break;
                    case "LTC:processCreated":
                        ProcessCreatedArgs processCreatedArgs = new ProcessCreatedArgs
                        {
                            Pid = data.GetProperty("pid").GetInt32(),
                            ExeFile = data.GetProperty("exeFile").GetString(),
                            CmdLine = data.GetProperty("cmdLine").GetString()
                        };
                        OnProcessCreated(processCreatedArgs);
                        WriteToLog("INFO", string.Format("Process Created: {0}, {1}, {2}", processCreatedArgs.Pid, processCreatedArgs.ExeFile, processCreatedArgs.CmdLine));
                        break;
                    case "LTC:processTerminated":
                        ProcessTerminatedArgs processTerminatedArgs = new ProcessTerminatedArgs
                        {
                            Pid = data.GetProperty("pid").GetInt32(),
                        };
                        OnProcessTerminated(processTerminatedArgs);
                        WriteToLog("INFO", string.Format("Process Terminated: {0}", processTerminatedArgs.Pid));
                        break;
                    case "LTC:graphicsLibLoaded":
                        GraphicsLibLoadedArgs graphicsLibLoadedArgs = new GraphicsLibLoadedArgs
                        {
                            Pid = data.GetProperty("pid").GetInt32(),
                            ModuleName = data.GetProperty("moduleName").GetString()
                        };
                        OnGraphicsLibLoaded(graphicsLibLoadedArgs);
                        WriteToLog("INFO", string.Format("Graphics Lib Loaded: {0}, {1}", graphicsLibLoadedArgs.Pid, graphicsLibLoadedArgs.ModuleName));
                        break;
                    case "LTC:moduleLoaded":
                        ModuleLoadedArgs moduleLoadedArgs = new ModuleLoadedArgs
                        {
                            Pid = data.GetProperty("pid").GetInt32(),
                            ModuleName = data.GetProperty("moduleName").GetString()
                        };
                        OnModuleLoaded(moduleLoadedArgs);
                        WriteToLog("INFO", string.Format("Plays-ltc Recording Module Loaded: {0}, {1}", moduleLoadedArgs.Pid, moduleLoadedArgs.ModuleName));
                        break;
                    case "LTC:gameLoaded":
                        GameLoadedArgs gameLoadedArgs = new GameLoadedArgs
                        {
                            Pid = data.GetProperty("pid").GetInt32(),
                            Width = data.GetProperty("size").GetProperty("width").GetInt32(),
                            Height = data.GetProperty("size").GetProperty("height").GetInt32(),
                        };
                        OnGameLoaded(gameLoadedArgs);
                        WriteToLog("INFO", string.Format("Game finished loading: {0}, {1}x{2}", gameLoadedArgs.Pid, gameLoadedArgs.Width, gameLoadedArgs.Height));
                        break;
                    case "LTC:gameBehaviorDetected":
                        GameBehaviorDetectedArgs gameBehaviorDetectedArgs = new GameBehaviorDetectedArgs {
                            Pid = data.GetProperty("pid").GetInt32()
                        };
                        OnGameBehaviorDetected(gameBehaviorDetectedArgs);
                        WriteToLog("INFO", string.Format("Game behavior detected for pid: {0}", gameBehaviorDetectedArgs.Pid));
                        break;
                    case "LTC:videoCaptureReady":
                        VideoCaptureReadyArgs videoCaptureReadyArgs = new VideoCaptureReadyArgs
                        {
                            Pid = data.GetProperty("pid").GetInt32()
                        };
                        OnVideoCaptureReady(videoCaptureReadyArgs);
                        WriteToLog("INFO", string.Format("Video capture ready, can start recording: {0}", videoCaptureReadyArgs.Pid));
                        break;
                    case "LTC:recordingError":
                        int errorCode = data.GetProperty("code").GetInt32();
                        string errorDetails = "";
                        switch (errorCode)
                        {
                            case 11:
                                errorDetails = "- Issue with video directory";
                                break;
                            case 12:
                                errorDetails = "- Issue with temp directory";
                                break;
                            case 16:
                                errorDetails = "- Issue with disk space";
                                break;
                            default:
                                break;
                        }
                        WriteToLog("ERROR", string.Format("Recording Error code: {0} {1}", errorCode, errorDetails));
                        break;
                    case "LTC:gameScreenSizeChanged":
                        WriteToLog("INFO", string.Format("Game screen size changed, {0}x{1}", data.GetProperty("width").GetInt32(), data.GetProperty("height").GetInt32()));
                        break;
                    case "LTC:saveStarted":
                        WriteToLog("INFO", string.Format("Started saving recording to file, {0}", data.GetProperty("filename").GetString()));
                        break;
                    case "LTC:saveFinished":
                        WriteToLog("INFO", string.Format("Finished saving recording to file, {0}, {1}x{2}, {3}, {4}",
                                            data.GetProperty("fileName"),
                                            data.GetProperty("width"),
                                            data.GetProperty("height"),
                                            data.GetProperty("duration"),
                                            data.GetProperty("recMode")));
                        break;
                    default:
                        WriteToLog("WARNING", string.Format("WAS SENT AN EVENT THAT DOES NOT MATCH CASE: {0}", msg));
                        break;
                }
            }

            client.Close();
            ltcProcess.Close();
            server.Stop();
        }

        public void GetEncoderSupportLevel() {
            Emit("LTC:getEncoderSupportLevel");
        }

        public void SetSavePaths(string saveFolder, string tempFolder) {
            Emit("LTC:setSavePaths",
            "{" +
                "'saveFolder': '" + saveFolder + "', " +
                "'tempFolder': '" + tempFolder + "'" +
            "}");
        }

        public void ScanForGraphLib(int pid) {
            string data =
            "{" +
                "'pid': " + pid +
            "}";
            Emit("LTC:scanForGraphLib", data);
        }

        public void StartAutoHookedGame(int pid) {
            string data =
            "{" +
                "'pid': " + pid +
            "}";
            Emit("LTC:startAutoHookedGame", data);
        }

        public void SetGameName(string name) {
            string data =
            "{" +
                "'gameName': '" + Regex.Replace(name, "[/:*?\"<>|]", "") + "'" +
            "}";
            Emit("LTC:setGameName", data);
        }

        public void LoadGameModule(int pid) {
            string data =
            "{" +
                "'pid': " + pid +
            "}";
            Emit("LTC:loadGameModule", data);
        }

        public void SetCaptureMode(int mode) {
            string data =
            "{" +
                "'captureMode': " + mode +
            "}";
            Emit("LTC:setCaptureMode", data);
        }

        public void SetGameDVRCaptureEngine(int engine) {
            string data =
            "{" +
                "'engine': " + engine + ", " +
                "'previewMode': false" +
            "}";
            Emit("LTC:setGameDVRCaptureEngine", data);
        }

        public void SetKeyBinds(string keyBinds = "[]") {
            string data =
            "{" +
                "'keyBinds': " + keyBinds +
            "}";
            Emit("LTC:setKeyBinds", data);
        }

        public void StartRecording() {
            Emit("LTC:startRecording");
        }

        public void StopRecording() {
            Emit("LTC:stopRecording");
        }

        public void SetGameDVRQuality(int bitRate, int frameRate, int videoResolution) {
            Emit("LTC:setGameDVRQuality",
            "{" +
                "'bitRate': " + bitRate + ", " +
                "'frameRate': " + frameRate + ", " +
                "'videoResolution': " + videoResolution +
            "}");
        }

        public void Emit(string type, string data = "{}") {
            data = data.Replace("'", "\"");
            string json = "{ \"type\": \"" + type + "\", \"data\": " + data + " }\f";

            if(ns != null && server != null) {
                byte[] jsonBytes = Encoding.Default.GetBytes(json);
                ns.Write(jsonBytes, 0, jsonBytes.Length);     //sending the message
                WriteToLog("SENT", json);
            }
        }

        public JsonElement GetDataType(string jsonString) {
            JsonElement jsonElement = JsonDocument.Parse(jsonString).RootElement;

            return jsonElement;
        }

        #region Log
        public class LogArgs : EventArgs {
            public string Title { get; internal set; }
            public string Message { get; internal set; }
            public string File { get; internal set; }
            public int Line { get; internal set; }
        }
        public void WriteToLog(string _Title, string _Message, 
                [CallerFilePath] string _File = null,
                [CallerLineNumber] int _Line = 0) {
            LogArgs logArgs = new LogArgs {
                Title = _Title,
                Message = _Message,
                File = _File,
                Line = _Line,
            };
            OnLog(logArgs);
        }
        public event EventHandler<LogArgs> Log;
        protected virtual void OnLog(LogArgs e) {
            Log?.Invoke(this, e);
        }
        #endregion

        #region ConnectionHandshake
        public class ConnectionHandshakeArgs : EventArgs {
            public string Version { get; internal set; }
            public string IntegrityCheck { get; internal set; }
        }
        public event EventHandler<ConnectionHandshakeArgs> ConnectionHandshake;
        protected virtual void OnConnectionHandshake(ConnectionHandshakeArgs e) {
            ConnectionHandshake?.Invoke(this, e);
        }
        #endregion

        #region ProcessCreated
        public class ProcessCreatedArgs : EventArgs { 
            public int Pid { get; internal set; }
            public string ExeFile { get; internal set; }
            public string CmdLine { get; internal set; }
        }
        public event EventHandler<ProcessCreatedArgs> ProcessCreated;
        protected virtual void OnProcessCreated(ProcessCreatedArgs e) {
            ProcessCreated?.Invoke(this, e);
        }
        #endregion

        #region ProcessTerminated
        public class ProcessTerminatedArgs : EventArgs {
            public int Pid { get; internal set; }
        }
        public event EventHandler<ProcessTerminatedArgs> ProcessTerminated;
        protected virtual void OnProcessTerminated(ProcessTerminatedArgs e) {
            ProcessTerminated?.Invoke(this, e);
        }
        #endregion

        #region GraphicsLibLoaded
        public class GraphicsLibLoadedArgs : EventArgs {
            public int Pid { get; internal set; }
            public string ModuleName { get; internal set; }
        }
        public event EventHandler<GraphicsLibLoadedArgs> GraphicsLibLoaded;
        protected virtual void OnGraphicsLibLoaded(GraphicsLibLoadedArgs e) {
            GraphicsLibLoaded?.Invoke(this, e);
        }
        #endregion

        #region GameBehaviorDetected
        public class GameBehaviorDetectedArgs : EventArgs {
            public int Pid { get; internal set; }
        }
        public event EventHandler<GameBehaviorDetectedArgs> GameBehaviorDetected;
        protected virtual void OnGameBehaviorDetected(GameBehaviorDetectedArgs e) {
            GameBehaviorDetected?.Invoke(this, e);
        }
        #endregion

        #region ModuleLoaded
        public class ModuleLoadedArgs : EventArgs {
            public int Pid { get; internal set; }
            public string ModuleName { get; internal set; }
        }
        public event EventHandler<ModuleLoadedArgs> ModuleLoaded;
        protected virtual void OnModuleLoaded(ModuleLoadedArgs e) {
            ModuleLoaded?.Invoke(this, e);
        }
        #endregion

        #region GameLoaded
        public class GameLoadedArgs : EventArgs {
            public int Pid { get; internal set; }
            public int Width { get; internal set; }
            public int Height { get; internal set; }
        }
        public event EventHandler<GameLoadedArgs> GameLoaded;
        protected virtual void OnGameLoaded(GameLoadedArgs e) {
            GameLoaded?.Invoke(this, e);
        }
        #endregion

        #region VideoCaptureReady
        public class VideoCaptureReadyArgs : EventArgs {
            public int Pid { get; internal set; }
            public int Width { get; internal set; }
            public int Height { get; internal set; }
        }
        public event EventHandler<VideoCaptureReadyArgs> VideoCaptureReady;
        protected virtual void OnVideoCaptureReady(VideoCaptureReadyArgs e) {
            VideoCaptureReady?.Invoke(this, e);
        }
        #endregion
    }
}
