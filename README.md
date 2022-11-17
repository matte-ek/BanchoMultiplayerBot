# Bancho Multiplayer Bot

Multiplayer bot for osu! written in C#, powered by [BanchoSharp](https://github.com/hburn7/BanchoSharp)

## Notice
Currently work in progress and in early testing/development stages. Depends on v1.2.0 of BanchoSharp, building yourself required.

## Setup
Build `BanchoMultiplayerBot.Host.Console` and create and fill out a `config.json` as follows:
```
{
  "Username": "",
  "Password": "",
  "ApiKey": ""
}
```
Specific lobby configuration is currently required to be set up in `Program.cs` direcly in `BanchoMultiplayerBot.Host.Console`.

## Roadmap
- [x] Manage lobbies via configurations
- [x] Limit maps via regulations, such as maximum star range or map length
- [x] Start timer and/or auto-start
- [x] Auto host rotate
- [x] Votes for skip/start/abort
- [ ] Detect and kick AFK players
