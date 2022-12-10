# Bancho Multiplayer Bot

Multiplayer bot for osu! written in C#, powered by [BanchoSharp](https://github.com/hburn7/BanchoSharp)

Preview (Web interface):
![image](https://user-images.githubusercontent.com/49276951/203641535-64726e7b-637e-4a4c-a06a-b0eeb0a58d9f.png)

## Why?
There are already a lot of good osu multiplayer bots, (such as [osu!ahr](https://github.com/Meowhal/osu-ahr) and [osu bot framework v3](https://github.com/jramseygreen/osu_bot_framework-v3)), however they had some missing features that I wanted to have, and I was limited to add functionally in a fork as I wasn't (and isn't) that familiar enough with Python or JS. To add to that, creating my own bot sounded like a fun project, which is how this bot was born.

To clearly what I meant with extra features:
* Completely abstract the whole "separate lobby config" stuff, configure the lobbies once within the UI and forget about it. It'll save them and either rejoin/create them for you, with their respective configs.
* Ability to fully recover and pick up where it left of during a restart, network issues, bancho restart etc. 
* Ability for me to add more features in the future, for example like what I did with the map pp counter.

This is a bit more complicated to get up and running than other bots as of now, if you just want a normal osu! auto host rotate bot up and running, I would highly recommend the mentioned alternatives above for now. The reason to this is mostly due to the WebUI, might add functionally for something like Discord in the future.

## Notice
Currently work in progress and in early testing/development stages. Depends on [v1.2.0 of BanchoSharp](https://github.com/hburn7/BanchoSharp/tree/v1.2.0), building yourself required.

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
- [x] Web interface for management 
- [x] Fully recover and pick up where it left off during bot restarts
- [x] Detect and restart the bot during network connectivity issues
- [x] Detect and restart the bot during osu!Bancho restarts (Currently untested)
- [x] Announcements at specific intervals in all lobbies
- [x] Add better logging with Serilog
- [x] Support more options, such as osu! mode, mods (sort of completed.)
- [x] Behaviour customization directly within the web interface
- [ ] Add "for fun" player ranking behaviour