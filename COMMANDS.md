## Bancho Multiplayer Bot

### Overview
An osu! multiplayer bot that will maintain a queue and pass the host around every match automatically. You are automatically added to the queue upon joining the match. 

### Player Commands

| Command               | Description |
| -----------           | ----------- |
| !queue / !q           | Displays the current auto host rotate queue. |
| !queuepos / !qp       | Displays your current position in the queue. |
| !skip                 | Allows you to skip your turn as host, or initiate a vote skip for the current host as a non-host player. |
| !start [\<seconds\>]  | Allows the host to start a match start timer, or for the non-host players to vote to start the match. |
| !stop                 | Allows the host to stop any ongoing match start timer. |
| !regulations / !r     | Displays the current map regulations, such as star rating and/or map length. |
| !abort                | Vote to abort the currently on-going match |
| !playtime             | Shows your current and total playtime. |
| !playstats            | Shows your total matches played and #1's |
| !mirror               | Sends a download link to the map from a mirror. |
| !help                 | Sends a link to the command/help page. |


*Notice:* Some commands, such as `!regulations`, `!queue` and `!queueposition` have shortened versions, like `!r`, `!q` and `!qp` respectively.

### Admin Commands

| Command                   | Description |
| -----------               | ----------- |
| !forceskip                | Will skip the current host, without any vote. |
| !sethost \<name\>         | Sets a new host for the round. |
| !ban \<name\>             | Ban a user, will run `!mp ban` and also save the username and automatically ban in future lobbies. |
| !banmapset \<id\>         | Ban a mapset by id |
| !config \<name\> \<value\> | Update the lobby properties directly in-game
| !addref		            | Adds you as a match referee to use bancho tournament commands. |
| !togglemapcheck           | Toggle map regulation checker for the lobby. |

### Debug Commands
*Can be used by anyone*

| Command               | Description |
| -----------           | ----------- |
| !uptime               | Shows the bot up-time |
| !issuetime            | Shows the last time since a bot networking issue. |
| !version              | Shows the currently running bot version. |
