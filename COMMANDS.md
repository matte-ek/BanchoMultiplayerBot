## Bancho Multiplayer Bot

### Overview
An osu! multiplayer bot that will maintain a queue and pass the host around every match automatically. You are automatically added to the queue upon joining the match. 

### Player Commands

| Command              | Description |
|----------------------| ----------- |
| !queue / !q          | Displays the current auto host rotate queue. |
| !queuepos / !qp      | Displays your current position in the queue. |
| !skip / !s           | Allows you to skip your turn as host, or initiate a vote skip for the current host as a non-host player. |
| !start [\<seconds\>] | Allows the host to start a match start timer, or for the non-host players to vote to start the match. |
| !stop                | Allows the host to stop any ongoing match start timer. |
| !regulations / !r    | Displays the current map regulations, such as star rating and/or map length. |
| !abort               | Vote to abort the currently on-going match |
| !playtime / !pt      | Shows your current and total playtime. |
| !playstats / !ps     | Shows your total matches played and #1's |
| !mapstats / !ms      | Shows play stats of the currently picked map. |
| !maprecord / !mr     | Shows best lobby score of the currently picked map. |
| !lastplayed / !lp    | Shows the last time the currently picked map was played. |
| !leavecount / !lc    | Shows the amount of people that left the previous map. |
| !rs                  | Shows information about your most recent score. |
| !timeleft / !tl      | Shows the estimated time left of the current map. You may also add "ping" (as in `!timeleft ping`) to get notified when the match finishes. |
| !autoskip (on/off)   | Feeling like just tagging along? Auto-skip will automatically skip your turn. |
| !mirror              | Sends a download link to the map from a mirror. |
| !mplink              | Sends a link to the osu! match history. |
| !help                | Sends a link to the command/help page. |

### Admin Commands

| Command                       | Description                                                                                        |
|-------------------------------|----------------------------------------------------------------------------------------------------|
| !forceskip                    | Will skip the current host, without any vote.                                                      |
| !sethost \<name\>             | Sets a new host for the round, will also place the previous host as #2.                                                                |
| !setqueuepos \<name\> \<pos\> | Sets a new queue position for the player. Queue position 0 is host.                         |
| !ban \<name\> \<time-days\>   | Ban a user from being host in all lobbies.                                                                        |
| !pban \<name\> \<time-days\>  | Ban a user from playing in all lobbies.
| !pardon \<name\>              | Unbans a player.                                                                      |
| !banmapset \<id\>             | Ban a mapset by id                                                                                 |
| !addref		                | Adds you as a match referee to use bancho tournament commands.                                     |

### Debug Commands
*Can be used by anyone*

| Command               | Description |
| -----------           | ----------- |
| !version              | Shows the currently running bot version. |
