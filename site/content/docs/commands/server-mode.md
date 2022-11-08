# server-mode command

### Usage

`dotnet aws server-mode`  - Launches the tool in a server mode for integrations with IDE, for example Visual Studio.

### Synopsis

```
dotnet aws server-mode [-d|--diagnostics] [-s|--silent] [-?|-h|--help] [--port <PORT>] [--parent-pid <PARENT-PID>] [--unsecure-mode]
```

### Description
Starts the tool in the server mode to provide integration with IDEs, for example Visual Studio. This tool is not intended for end user usage unless you are writing a custom integration into an IDE.

### Examples
```
dotnet aws server-mode --port 1234 --parent-pid 12345
```
