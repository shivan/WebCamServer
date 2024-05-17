# WebCamServer

This is a simple web cam server to stream a local camera via http (jpeg)

## Requirements

.NET 6.0

## Usage

Simply copy the binary files to a directory and execute the main application.

```
WebCamServer.exe --help
```

shows the help information.

```
Usage:
WebCamServer --device=<device name> --width=<resWidth> --heigth=<resHeigth>
```

Device can be a part of the device name (case insensitive).

Width and/or heigth can be selected, too. Otherwise default will be used.

If you don't pass any parameters, it will show all available cameras and use the first one. Also it will show the available resolutions on the selected camera.

## Run as service

You can use NSSM (non-sucking service manager - https://nssm.cc/) to run it as a service.

nssm install 

## URLs

| URL                            | Description          |
| ------------------------------ | -------------------- |
| http://server:8080/current.jpg | current live picture |
| http://server:8080/stream.jpg  | livestream           |

## How to add the camera to home-assistant

1. add new generic camera
2. use http://server:8080/current.jpg for current url
3. use http://server:8080/stream.jpg for stream url
4. use http as transport protocol
5. leave everything else as default