using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace TCPServer
{
    public class Handlers
    {
        public static Options Options;

        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }

            Logger.Log(LogInfoType.Info, "No network adapters with an IPv4 address in the system!");

            throw new EntryPointNotFoundException();
        }

        public static void StartDefaultPage()
        {
            var explorerProcess = new ProcessStartInfo(Options.BrowserPath)
            {
                UseShellExecute = false,
                WindowStyle = Options.BrowserWindowOverlayOnTop ? ProcessWindowStyle.Maximized : ProcessWindowStyle.Minimized,
                Arguments = Options.DefaultWebPageLink,
            };
            Process.Start(explorerProcess);
        }

        public static void LoadConfig()
        {
            using (StreamReader fileReadingStream = new StreamReader("config.json"))
            {
                try
                {
                    string json = fileReadingStream.ReadToEnd();
                    MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Options));
                    Options = (Options)serializer.ReadObject(stream);
                }
                catch (Exception exp)
                {
                    Console.WriteLine($"Error while reading config.json file. \nDetails: {exp.Message}");
                    Console.ReadKey();
                }
            }
        }

        public static void SendResposneToClient(NetworkStream stream, string message)
        {
            byte[] response = Encoding.ASCII.GetBytes(message);
            stream.Write(response, 0, response.Length);            
        }

        public static void KillProcesses(string processName)
        {
            foreach (Process process in Process.GetProcessesByName(processName))
            {
                if(!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                }                
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public static string GetInternetExplorerUrl(Process process)
        {
            StringBuilder title = new StringBuilder(256);
            GetWindowText(process.MainWindowHandle, title, title.Capacity);
            return title.ToString();
        }
        
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        public static StatusOfWindow IsProcessMinimized(Process process)
        {
            IntPtr handle = process.MainWindowHandle;
            if (IsIconic(handle)) return StatusOfWindow.Minimized;
            else return StatusOfWindow.Maximized;
        }
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        public static void MaximizeWindow(Process process)
        {
            IntPtr handle = process.MainWindowHandle;
            ShowWindowAsync(handle, SW_MAXIMIZE);
        }

        public static void MinimizeWindow(Process process)
        {
            IntPtr handle = process.MainWindowHandle;
            ShowWindowAsync(handle, SW_MINIMIZE);
        }

        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
    }

    [DataContract]
    public class Options
    {
        [DataMember]
        public int Port { get; set; }
        [DataMember]
        public string BrowserProcessName { get; set; }
        [DataMember]
        public bool BrowserWindowOverlayOnTop { get; set; }
        [DataMember]
        public string BrowserPath { get; set; }
        [DataMember]
        public string DefaultWebPageLink { get; set; }
        [DataMember]
        public string LogfilePath { get; set; }
        [DataMember]
        public int LogfileLimitMB { get; set; }
        
    }
    public enum StatusOfWindow
    {
        Minimized,
        Maximized
    }
}
