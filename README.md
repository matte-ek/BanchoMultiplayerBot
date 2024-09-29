# Bancho Multiplayer Bot

Multiplayer bot for osu! written in C#, powered by [BanchoSharp](https://github.com/hburn7/BanchoSharp)

![image](https://bot.matte.sh/api/banner)

## v2.0 branch

### Why?

Been wanting to re-write the bot (or parts of the bot) for a while, both due to the messy code and "execution flow", but also due to general stability issues. Some of the initial goals of V2 where:
* Handle events and bancho commands better
  * Previously, bancho tournament commands were executed "manually" and had verification code so the command executed successfully all over the place
  * The "!mp settings" event basically ran the whole show for the auto rotate stuff, therefore I had hacked together code to skip rotating whenever I had to do "!mp settings" outside of the "after map finish" event.
  * All of this combined caused some random errors to occour, where players would get skipped twice and whatnot, and trying to debug and resolve issues in that spaghetti wasn't really easy.
* More or less stateless
  * Earlier versions would attempt to save runtime stuff to a file called `lobby_states.json` in a really stupid way, not only would a lot of data not get stored in the first place, but this is also counting on the fact that the bot will *always* exit gracefully.
  * Obviosuly the bot will still have to connect to Bancho, join channels and stuff so it's not fully stateless, but stopping the bot and restarting shouldn't have any effect on the lobbies at all.
* Better lobby configuration handling
  * Previous lobby configurations should never get lost, even if the initial channel is gone and a re-creating the channel failed.
* Handle user commands automatically
  * Previously the commands were all parsed manually inside of a `OnMessageReceived` event, which can become annoying very fast with arguments/permissions etc.
* New external (as in not tied to the bot application) front-end with osu! OAuth support
 
### Progress

As of `2024-08-11` most of the user facing features are pretty much 1:1 to v1, with the exception of announcements. However the API and frontend is not completed yet, and there will also be a lot of stability and testing to be done. While the earlier version may have buggy code, it's up-time is very good. 

### Implementation Details

To make things easier and cleaner on the bot side, I've decided to move out Bancho and bancho connection related stuff to it's own project `BanchoMultiplayerBot.Bancho`, of which will completely abstract:
* Connecting and maintaining the connection to Bancho
* Handling rate limting and other message releated stuff
* Handling execution of Bancho commands

The new event system treats behavious similarly to a controller in ASP, where a new instance of the behavior will be created for each event, and the appropriate event will be executed inside of the behavior. A behavior will therefore not have to handle subscribing and unsubscribing to events, nor will it have to deal with saving/loading data. A fully functional behavior can therefore be implemented as such:
```cs
public class TestBehavior(BehaviorEventContext context) : IBehavior
{
    [BanchoEvent(BanchoEventType.MatchStarted)]
    public void OnMatchStarted()
    {
        context.SendMessage("Good luck!");
    }
}
```

Dealing with data inside of a behavior is dealt via `BehaviorDataProvider<T>` and `BehaviorConfigProvider<T>`. The data provider should be used for dynamic data related to the current lobby itself, such as the current queue for example. While the config provider should be used for static configuration such as the minimum/maximum map length etc. There is still some minor things I probably want to change with these, so they aren't really final at all. 

Timers and votes are also a somewhat common thing to deal with, and for that the `VoteProvider` and `TimerProvider` can be used, see below example for a timer:
```cs
public class TestBehavior(BehaviorEventContext context) : IBehavior
{
    [BanchoEvent(BanchoEventType.MatchStarted)]
    public void OnMatchStarted()
    {
        _context.SendMessage("Waiting for 30 seconds...");
        _context.TimerProvider.FindOrCreateTimer("TestTimer").Start(TimeSpan.FromSeconds(30));
    }

    [BotEvent(BotEventType.TimerElapsed, "TestTimer")]
    public void OnTestTimerElapsed()
    {
        _context.SendMessage("30 seconds has passed!");
    }
}
```

You can also for example send arbitrary events to all behaviors in the lobby with `BehaviorEventProcessor.OnBehaviorEvent("MyEvent", optionalParameter)`.

Commands can now also be created more easily, by implementing the `IPlayerCommand` interface. Commands can also be processed in an event by the attribute `[BotEvent(BotEventType.CommandExecuted, "MyCommand")]`.

## Libraries used

* [BanchoSharp](https://github.com/hburn7/BanchoSharp) by [hburn7 (Stage)](https://github.com/hburn7) - Bancho client library used to interact with bancho
* [osu-sharp](https://github.com/minisbett/osu-sharp) by [minisbett](https://github.com/minisbett) - osu!api library used to interact with the osu!api
* [rosu-pp](https://github.com/MaxOhn/rosu-pp) by [MaxOhn](https://github.com/MaxOhn) - Performance point calculating library used to calculate performance points

(And also the "bigger" ones such as the ASP.NET stack, SeriLog, EntityFrameworkCore, Newtonsoft.Json, Moq, prometheus-net)

## Additional Thanks
[hburn7 (Stage)](https://github.com/hburn7) for BanchoSharp and other help

[iWeeti](https://github.com/iWeeti) for a [very nice statistics website](https://github.com/iWeeti/weetisoft-osu) built for the bot

[bitknox](https://github.com/bitknox) for giving me cool ideas for the project and additional help
