using TCPServer;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    static void Main(string[] args)
    {
        Handlers.LoadConfig();
        ServerProgram();
    }

    public static void ServerProgram()
    {
        Handlers.KillProcesses(Handlers.Options.BrowserProcessName);

        IPAddress ipAddress = Handlers.GetLocalIPAddress();
        var tcpListener = new TcpListener(ipAddress, Handlers.Options.Port);

        try
        {
            tcpListener.Start();
            Logger.Log(LogInfoType.Info, $"Server started at {ipAddress}");
            bool connected = false;
            TcpClient tcpClient = null;

            while (true)
            {
                while (!connected)
                {
                    if (tcpListener.Pending())
                    {
                        // If there is already a client connection waiting, close it
                        var pendingClient = tcpListener.AcceptTcpClient();
                        Handlers.SendResposneToClient(pendingClient.GetStream(), "\nSorry, another client is using the server");
                        pendingClient.Close();
                        continue;
                    }

                    // Accept the next client connection
                    tcpClient = tcpListener.AcceptTcpClient();
                    connected = true;
                }

                NetworkStream stream = tcpClient.GetStream();

                while (connected)
                {
                    //Parse message from client
                    byte[] buffer = new byte[1024];
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string data = Encoding.ASCII.GetString(buffer, 0, bytes);

                    Logger.Log(LogInfoType.Info, $"Incoming message: {tcpClient.Client.RemoteEndPoint}. Data received from client: {data}");

                    switch (data)
                    {
                        case string a when a.StartsWith("Start"):
                            {
                                Handlers.KillProcesses(Handlers.Options.BrowserProcessName);

                                var explorerProcess = new ProcessStartInfo(Handlers.Options.BrowserPath)
                                {
                                    UseShellExecute = false,
                                    WindowStyle = Handlers.Options.BrowserWindowOverlayOnTop ? ProcessWindowStyle.Maximized : ProcessWindowStyle.Minimized,
                                    Arguments = data.Replace("Start ", ""),
                                };

                                Process.Start(explorerProcess);

                                Logger.Log(LogInfoType.Info, $"Opened {explorerProcess.Arguments} with default web browser.");
                                Handlers.SendResposneToClient(stream, $"\nOpened {explorerProcess.Arguments} with default web browser.\n");
                                break;
                            }
                        case "Stop":
                            {
                                Handlers.KillProcesses(Handlers.Options.BrowserProcessName);

                                Logger.Log(LogInfoType.Info, "Stopping all browser processes...");
                                Handlers.SendResposneToClient(stream, "\nClosed default web browser.\n");
                                break;
                            }
                        case "Alive ping":
                            {
                                var activeBrowserProcesses = Process.GetProcessesByName(Handlers.Options.BrowserProcessName).Where(pp => pp.MainWindowHandle != null && pp.MainWindowHandle != IntPtr.Zero);

                                if (!activeBrowserProcesses.Any())
                                    Handlers.SendResposneToClient(stream, "\nNo active browser processes found.\n");
                                else
                                    foreach (var process in activeBrowserProcesses)
                                        Handlers.SendResposneToClient(stream, $"\nBrowser window: {Handlers.GetInternetExplorerUrl(process)}, Status: {Handlers.IsProcessMinimized(process)}\n");
                                break;
                            }
                        case "Show":
                            {
                                var activeBrowserProcesses = Process.GetProcessesByName(Handlers.Options.BrowserProcessName).Where(pp => pp.MainWindowHandle != null && pp.MainWindowHandle != IntPtr.Zero);
                                if (!activeBrowserProcesses.Any())
                                    Handlers.SendResposneToClient(stream, "\nNo active browser processes found.\n");
                                else
                                {
                                    foreach (var process in activeBrowserProcesses)
                                    {
                                        Handlers.MaximizeWindow(process);
                                    }
                                    Handlers.SendResposneToClient(stream, "\nThe browser window is displayed successfully. \n");
                                }
                                break;
                            }
                        case "Hide":
                            {
                                var activeBrowserProcesses = Process.GetProcessesByName(Handlers.Options.BrowserProcessName).Where(pp => pp.MainWindowHandle != null && pp.MainWindowHandle != IntPtr.Zero);
                                if (!activeBrowserProcesses.Any())
                                    Handlers.SendResposneToClient(stream, "\nNo active browser processes found.\n");
                                else
                                {
                                    foreach (var process in activeBrowserProcesses)
                                    {
                                        Handlers.MinimizeWindow(process);
                                    }
                                    Handlers.SendResposneToClient(stream, "\nThe browser window is hidden successfully. \n");
                                }
                                break;
                            }
                        default:
                            {
                                Handlers.KillProcesses(Handlers.Options.BrowserProcessName);
                                Handlers.SendResposneToClient(stream, "\nClosing connection.\n");
                                Logger.Log(LogInfoType.Info, $"Client {tcpClient.Client.RemoteEndPoint} closed connection.");
                                tcpClient.Close();
                                connected = false;
                                break;
                            }
                    }
                }
            }
        }
        catch (Exception exp)
        {
            Logger.Log(LogInfoType.Error, $"Error while processing: \n{exp.Message}");
            throw;
        }
        finally
        {
            Logger.Log(LogInfoType.Info, $"Server stoped listeting on {ipAddress}");
            tcpListener.Stop();
        }
    }
}

