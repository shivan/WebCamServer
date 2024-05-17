using System.Reflection;

string videoDeviceName = "";
int resolutionWidth = 0;
int resolutionHeight = 0;
int webPort = 8080;

var version = Assembly.GetExecutingAssembly().GetName().Version;
Console.WriteLine($"WebCamServer {version}\n");

if ((args.Length == 1) && ((args[0] == "/?") || (args[0] == "--help")))
{
    Console.WriteLine("Usage:");
    Console.WriteLine("WebCamServer --device=<device name> --width=<resWidth> --heigth=<resHeigth>");
    return;
}

// Kommandozeilenargumente parsen
foreach (var arg in args)
{
    if (arg.StartsWith("--device="))
    {
        videoDeviceName = arg.Split('=')[1];
    }
    else if (arg.StartsWith("--width="))
    {
        resolutionWidth = int.Parse(arg.Split('=')[1]);
    }
    else if (arg.StartsWith("--height="))
    {
        resolutionHeight = int.Parse(arg.Split('=')[1]);
    }
    else if (arg.StartsWith("--portt="))
    {
        webPort = int.Parse(arg.Split('=')[1]);
    }
}

var server = new HttpCameraServer(webPort, videoDeviceName, resolutionWidth, resolutionHeight);
server.Start();