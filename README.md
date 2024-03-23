# Bancho Multiplayer Bot

Multiplayer bot for osu! written in C#, powered by [BanchoSharp](https://github.com/hburn7/BanchoSharp)

Preview (Web interface):
![image](https://github.com/matte-ek/BanchoMultiplayerBot/assets/49276951/1cfbbcb3-15a5-4ca2-ab6c-e138ea26b29e)

## Why?
There are already a lot of good osu multiplayer bots, (such as [osu!ahr](https://github.com/Meowhal/osu-ahr) and [osu bot framework v3](https://github.com/jramseygreen/osu_bot_framework-v3)), however they had some missing features that I wanted to have, and I was limited to add functionally in a fork as I wasn't (and isn't) that familiar enough with Python or JS. To add to that, creating my own bot sounded like a fun project, which is how this bot was born.

To clearly what I meant with extra features:
* Completely abstract the whole "separate lobby config" stuff, configure the lobbies once within the UI and forget about it. It'll save them and either rejoin/create them for you, with their respective configs.
* Ability to fully recover and pick up where it left of during a normal restart, network issues, bancho restart etc. 
* Ability for me to add more features in the future, for example map pp information.

This is a bit more complicated to get up and running than other bots as of now, if you just want a normal osu! auto host rotate bot up and running, I would highly recommend the mentioned alternatives above for now. The reason to this is mostly due to the WebUI, might add functionally for something like Discord in the future. If you don't really care about accessing the bot remotely, it shouldn't be too complicated though.

## Setup
Information on how to setup and use the bot are avaliable on the [wiki](https://github.com/matte-ek/BanchoMultiplayerBot/wiki/Setup#installation).

## Additional Thanks
[hburn7 (Stage)](https://github.com/hburn7) for BanchoSharp and other help

[iWeeti](https://github.com/iWeeti) for a [very nice statistics website](https://github.com/iWeeti/weetisoft-osu) built for the bot

[MaxOhn](https://github.com/MaxOhn) for rosu-pp that the bot uses to calculate performance points

[bitknox](https://github.com/bitknox) for giving me cool ideas and additional help
