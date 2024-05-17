using System;
using System.Net.Sockets;
using System.Net;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Drawing;
using System.IO;
using System.Threading;

public class HttpCameraServer
{
    private int Port = 8080;
    private FilterInfoCollection videoDevices;
    private VideoCaptureDevice videoSource;
    private Bitmap? currentFrame;
    private object frameLock = new object();
    private bool isStreaming;
    private TcpListener? listener;
    private Thread? listenerThread;

    public HttpCameraServer(int webPort, string deviceName, int resX = 0, int resY = 0)
    {
        videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        this.Port = webPort;

        FilterInfo selectedDevice;

        if (videoDevices.Count == 0)
        {
            throw new ApplicationException("No camera devices found.");
        }
        else
        {
            Console.WriteLine($"{videoDevices.Count} camera devices found:");

            selectedDevice = videoDevices[0];

            foreach (FilterInfo device in videoDevices)
            {
                Console.WriteLine($"* {device.Name}");
            }
            Console.WriteLine();

            // select device by name
            if (deviceName != "")
            {
                foreach (FilterInfo device in videoDevices)
                {
                    if (device.Name.ToLower().Contains(deviceName.ToLower()))
                    {
                        selectedDevice = device;
                        break;
                    }
                }
            }
        }

        Console.WriteLine($"Using device: {selectedDevice.Name}\n");

        videoSource = new VideoCaptureDevice(selectedDevice.MonikerString);
        videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);

        Console.WriteLine("Available frame sizes:");
        foreach (var capability in videoSource.VideoCapabilities)
        {
            Console.WriteLine($"* {capability.FrameSize.Width}x{capability.FrameSize.Height}@{capability.AverageFrameRate}");
        }

        // search for selected
        if ((resX != 0) || (resY != 0))
        {
            foreach (var capability in videoSource.VideoCapabilities)
            {
                if (capability.FrameSize.Width == resX && capability.FrameSize.Height == resY)
                {
                    videoSource.VideoResolution = capability;
                }
                else if (capability.FrameSize.Width == resX && resY == 0)
                {
                    videoSource.VideoResolution = capability;
                }
                else if (resX == 0 && capability.FrameSize.Height == resY)
                {
                    videoSource.VideoResolution = capability;
                }
            }
        }

        Console.WriteLine();

        if (videoSource.VideoResolution != null)
        {
            Console.WriteLine($"Using {videoSource.VideoResolution.FrameSize.Width}x{videoSource.VideoResolution.FrameSize.Height}@{videoSource.VideoResolution.AverageFrameRate}");
        }
    }

    public void Start()
    {
        listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Console.WriteLine($"Server started on port {Port}.");

        listenerThread = new Thread(ListenForClients);
        listenerThread.Start();
    }

    private void ListenForClients()
    {
        while (true)
        {
            var client = listener.AcceptTcpClient();
            var thread = new Thread(new ParameterizedThreadStart(HandleClient));
            thread.Start(client);
        }
    }

    private void HandleClient(object clientObj)
    {
        var client = (TcpClient)clientObj;
        var stream = client.GetStream();
        var reader = new StreamReader(stream);
        var writer = new StreamWriter(stream);

        try
        {
            Console.WriteLine("Client connected.");
            string requestLine = reader.ReadLine();
            string[] tokens = requestLine.Split(' ');
            string url = tokens[1];

            if (url == "/current.jpg")
            {
                ServeCurrentImage(writer, stream);
            }
            else if (url == "/stream.jpg")
            {
                ServeStream(writer, stream, client);
            }
            else
            {
                writer.WriteLine("HTTP/1.1 404 Not Found");
                writer.WriteLine("Content-Type: text/plain");
                writer.WriteLine();
                writer.WriteLine("Not Found");
                writer.Flush();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client disconnected: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("Client disconnected.");
            stream.Close();
            client.Close();
        }
    }

    private void ServeCurrentImage(StreamWriter writer, NetworkStream stream)
    {
        Bitmap frame;
        lock (frameLock)
        {
            frame = (Bitmap)currentFrame?.Clone();
        }

        if (frame != null)
        {
            MemoryStream ms = new MemoryStream();
            frame.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            writer.WriteLine("HTTP/1.1 200 OK");
            writer.WriteLine("Content-Type: image/jpeg");
            writer.WriteLine($"Content-Length: {ms.Length}");
            writer.WriteLine();
            writer.Flush();

            ms.WriteTo(stream);
            writer.WriteLine();
            writer.Flush();

            ms.Close();
        }
        else
        {
            writer.WriteLine("HTTP/1.1 503 Service Unavailable");
            writer.WriteLine("Content-Type: text/plain");
            writer.WriteLine();
            writer.WriteLine("No image available");
            writer.Flush();
        }
    }

    private void ServeStream(StreamWriter writer, NetworkStream stream, TcpClient client)
    {
        StartStreaming();

        writer.WriteLine("HTTP/1.1 200 OK");
        writer.WriteLine("Content-Type: multipart/x-mixed-replace; boundary=--boundary");
        writer.WriteLine();
        writer.Flush();

        while (client.Connected)
        {
            Bitmap frame;
            lock (frameLock)
            {
                frame = (Bitmap)currentFrame?.Clone();
            }

            if (frame != null)
            {
                MemoryStream ms = new MemoryStream();
                frame.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                writer.WriteLine("--boundary");
                writer.WriteLine("Content-Type: image/jpeg");
                writer.WriteLine($"Content-Length: {ms.Length}");
                writer.WriteLine();
                writer.Flush();

                ms.WriteTo(stream);
                writer.WriteLine();
                writer.Flush();

                ms.Close();
            }
            Thread.Sleep(100);
        }

        StopStreaming();
    }

    private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        lock (frameLock)
        {
            currentFrame?.Dispose();
            currentFrame = (Bitmap)eventArgs.Frame.Clone();
        }
    }

    private void StartStreaming()
    {
        if (!isStreaming)
        {
            videoSource.Start();
            isStreaming = true;
            Console.WriteLine("Video streaming started.");
        }
    }

    private void StopStreaming()
    {
        if (isStreaming)
        {
            videoSource.SignalToStop();
            videoSource.WaitForStop();
            isStreaming = false;
            Console.WriteLine("Video streaming stopped.");
        }
    }
}
