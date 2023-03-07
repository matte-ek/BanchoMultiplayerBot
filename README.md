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

## State
Generally pretty stable for auto host rotation, still bit of a hassle to set up.

## Setup
### **WARNING!** I cannot guarantee any security, as I haven't written any Blazor applications before, and the "authentication" system right now was quickly implemented. Hosting on a local/trusted network advised. Use at your own risk. Traffic is unencrypted unless a reverse proxy is used (and properly configured), see https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/server?view=aspnetcore-7.0#linux-with-nginx for more information. 


Download the latest release of the [releases page](https://github.com/matte-ek/BanchoMultiplayerBot/releases). Also make sure you have the [.NET Framework 6.0 SDK](https://dotnet.microsoft.com/en-us/download) installed!

Then modify `appsettings.json` and fill out a username and a password as shown below. This is not for the osu! IRC credentials, but the admin panel of the bot itself. 
```
{
  ...
  "Authentication": {
    "AdminUsername": "<fill out a username>",
    "AdminPassword": "<fill out a password>"
  }
  ...
}
```

Then run the bot itself, for example on Windows you may run the `BanchoMultiplayerBot.Host.Web.exe` file, or on Linux run `dotnet BanchoMultiplayerBot.Host.Web.dll`. Now while the bot application is running you may visit
[http://localhost:5000/osu-bot](http://localhost:5000/osu-bot) where you should be greeted by a login page. Here you can login with the username and password that you picked earlier.

At this point you should open up the settings page and fill out the osu! IRC credentials which you can get from [here](https://osu.ppy.sh/p/irc). You also need an osu!api v1 key which you can get from [here](https://osu.ppy.sh/p/api/). (You can use localhost for the app URL). You also have some other global configuration stuff on this page.

After this is done you may go back to the Dashboard page and hit the "Save Configuration" button, due to a bug in the bot as of now the connect button doesn't work if the IRC username/password is just updated, so after hitting the "Save Configuration" button, please restart the bot by just closing and reopening the application. 

When you re-login afterwards the Bancho Connection label should say OK, if not check the console output for any errors. At this point you may start creating lobbies, with the "Add new lobby" page to the left. Use the Auto Host Rotate preset to create an AHR lobby. The lobby should then be created and you should be able to click on it on the menu to the left, where you can configure it to your liking and see the current players and chat.

### Map performance-points information
The bot will function just fine without it, but if you want the bot to provide pp information for each map, you will need to build [osu-tools](https://github.com/ppy/osu-tools), and then provide it's binaries in a directory called `osu-pp-tools` in the bot's parent directory.

1. Download the source code of osu-tools with the currently deployed version, which you can find [here](https://github.com/ppy/osu-infrastructure/wiki/Star-Rating-and-Performance-Points). (Click the version number which corresponds to osu-tools, and then download the source code from there)
2. Open a terminal and move into the `PerformanceCalculator` directory
3. Run `dotnet build -c Release`
4. You should now have a directory within the `bin/Release`, with all the required binary files. In my case it's called `net-6.0`. Rename this folder to `osu-pp-tools`.
5. Move `osu-pp-tools` to the bot's parent directory.
6. After restarting the bot, it should not longer warn about it not finding `osu-pp-tools` and it should now print out pp values automatically.

## Build Note
The current newest release of BanchoSharp will not work with the bot currently, instead use [my branch](https://github.com/matte-ek/BanchoSharp/tree/v1.2.0-add-join-time) for now.

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
