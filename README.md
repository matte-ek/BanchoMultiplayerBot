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
### **WARNING!** Traffic is unencrypted unless a reverse proxy with HTTPS is used (and properly configured), see [this](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/server?view=aspnetcore-7.0#linux-with-nginx) for more information. This pretty much only matters if you expose the WebUI on the web, if you're hosting locally (i.e. on your computer on your home network) then it's not that big of a deal. Hosting on a local/trusted network advised. Use at your own risk. 

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

At this point you should open up the Authentication page (within Settings) and fill out your osu! IRC credentials which you can get from [here](https://osu.ppy.sh/p/irc). You also need an osu!api v1 key which you can get from [here](https://osu.ppy.sh/p/api/). (You can use localhost for the app URL).

After this is done you may go back to the Dashboard page and hit the "Connect" button, and after waiting a few seconds it should hopefully connect and the Bancho Connection label should say OK. If you encounter issues and you are sure that the credentails are correct, try hitting the Save Configuration button, then manually restart the bot and try again. If you still have issues check out the console window for any error messages which might point you in the right direction. If you still have issues feel free to open up an issue here.

At this point you may start creating lobbies, with the "Add new lobby" page to the left. Use the Auto Host Rotate preset to create an AHR lobby. The lobby should then be created and you should be able to click on it on the menu to the left, where you can configure it to your liking and see the current players and chat etc.

### General bot information
* The bot will automatically save the current lobbies (with their channel and queue) when closed by hitting the "Exit" button in the Dashboard. This means that you can for example update the bot, and then restart it without affecting the user experience and minimize downtime. If you for some reason want to restart it without this, remove the lobby_states.json file in the bot directory.
* Lobby behaviours are modules that each have a specific task within managing the lobby, such as AntiAfk, AutoHostRotate etc. If you want to for example disable AntiAfk you can remove that module from the lobby (see below!). The auto host rotate preset in the create lobby section will enable all relevant behaviours required by default.
* Change in behaviours will NOT update until the lobby/bot is restarted. The easiest way to deal with this is just hitting the Restart button in the dashboard.

### Map performance-points information
The bot will function just fine without it, but if you want the bot to provide pp information for each map, you will need to build [osu-tools](https://github.com/ppy/osu-tools), and then provide it's binaries in a directory called `osu-pp-tools` in the bot's parent directory.

1. Download the source code of osu-tools with the currently deployed version, which you can find [here](https://github.com/ppy/osu-infrastructure/wiki/Star-Rating-and-Performance-Points). (Click the version number which corresponds to osu-tools, and then download the source code from there)
2. Open a terminal and move into the `PerformanceCalculator` directory
3. Run `dotnet build -c Release`
4. You should now have a directory within the `bin/Release`, with all the required binary files. In my case it's called `net-6.0`. Rename this folder to `osu-pp-tools`.
5. Move `osu-pp-tools` to the bot's parent directory.
6. After restarting the bot, it should not longer warn about it not finding `osu-pp-tools` and it should now print out pp values automatically.

## Build Note
The current newest release of BanchoSharp will not work with the bot currently, instead use [my fork](https://github.com/matte-ek/BanchoSharp/tree/v1.2.0-add-join-time) for now.
