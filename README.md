# Bancho Multiplayer Bot

Multiplayer bot for osu! written in C#, powered by [BanchoSharp](https://github.com/hburn7/BanchoSharp)

Preview (Web interface):
![image](https://user-images.githubusercontent.com/49276951/203641535-64726e7b-637e-4a4c-a06a-b0eeb0a58d9f.png)

## Notice
Currently work in progress and in early testing/development stages. Depends on v1.2.0 of BanchoSharp, building yourself required.

## Setup
### **WARNING!** I cannot guarantee any security, as I haven't written any Blazor applications before, and the "authentication" system right now was quickly implemented. Hosting on a local/trusted network advised. Use at your own risk. Traffic is unencrypted unless a reverse proxy is used (and properly configured), see https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/server?view=aspnetcore-7.0#linux-with-nginx for more information. 

The setup process is currently kind of complicated, and the application itself is very designed/implemented for my needs as of right now.

Build `BanchoMultiplayerBot.Host.Web` and create and fill out `config.json` and `appsettings.json` as follows:

`config.json`
```
{
  "Username": "",
  "Password": "",
  "ApiKey": ""
}
```
*These fields may be left out empty as they can be entered later on in the web interface*

`appsettings.json`
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Authentication": {
    "AdminUsername": "<fill out a username>",
    "AdminPassword": "<fill out a password>"
  },
  "AllowedHosts": "*"
}
```

Authentication is "temporary" and is subject to change in the future, as storing credentials there is not ideal. By default, the application will only listen at `http://localhost:5000/osu-bot`.

## Roadmap
- [x] Manage lobbies via configurations
- [x] Limit maps via regulations, such as maximum star range or map length
- [x] Start timer and/or auto-start
- [x] Auto host rotate
- [x] Votes for skip/start/abort
- [x] Detect and kick AFK players
