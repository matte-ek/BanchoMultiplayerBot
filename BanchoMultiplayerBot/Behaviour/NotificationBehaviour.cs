using BanchoMultiplayerBot.Utilities;

namespace BanchoMultiplayerBot.Behaviour
{
    public class NotificationBehaviour : IBotBehaviour
    {
        private Lobby _lobby = null!;

        private string? WebhookUrl => _lobby.Bot.Configuration.WebhookMentionSeperateWebhook == true ? _lobby.Bot.Configuration.WebhookSeperateUrl : _lobby.Bot.Configuration.WebhookUrl;

        public void Setup(Lobby lobby)
        {
            _lobby = lobby;

            _lobby.Bot.Client.OnPrivateMessageReceived += OnPrivateMessageReceived;
            _lobby.OnUserMessage += OnUserMessage;
        }

        private async void OnPrivateMessageReceived(BanchoSharp.Interfaces.IPrivateIrcMessage msg)
        {
            if (_lobby.Bot.Configuration.EnableWebhookNotifications != true || WebhookUrl == null)
            {
                return;
            }

            if (_lobby.Bot.Configuration.WebhookNotifyUserMessage != true)
            {
                return;
            }

            if (!msg.IsDirect || msg.IsBanchoBotMessage)
            {
                return;
            }

            if (msg.Content.StartsWith("!") || msg.Content.Contains("is listening to"))
            {
                return;
            }

            await WebhookUtils.SendWebhookMessage(WebhookUrl, $"User Direct Message ({SanitizeUserMessage(msg.Sender)})", $"{SanitizeUserMessage(msg.Content)}");
        }

        private async void OnUserMessage(Data.PlayerMessage msg)
        {
            if (_lobby.Bot.Configuration.EnableWebhookNotifications != true || WebhookUrl == null)
            {
                return;
            }

            if (_lobby.Bot.Configuration.WebhookNotifyBotMention != true)
            {
                return;
            }

            if (msg.Sender == msg.Recipient)
            {
                return;
            }

            if (msg.Content.EndsWith(" bot") || 
                msg.Content.StartsWith("bot ") || 
                msg.Content.Contains(" bot ") ||
                msg.Content.StartsWith($"{_lobby.Bot.Configuration.Username} ") ||
                msg.Content.EndsWith($" {_lobby.Bot.Configuration.Username}") ||
                msg.Content.Contains($" {_lobby.Bot.Configuration.Username} "))
            {
                await WebhookUtils.SendWebhookMessage(WebhookUrl, $"Name Mention ({_lobby.Configuration.Name})", $"{SanitizeUserMessage(msg.Sender)}: {SanitizeUserMessage(msg.Content)}");
            }
        }

        // Passing raw user input into a Discord webhook doesn't sound ideal to me, so to be 
        // on the safe side, just remove any non alphanumerical characters, we don't need anything
        // else anyway.
        private static string SanitizeUserMessage(string input)
        {
            if (input.Length > 48)
            {
                input = input[..48];
            }

            return new string(input.Where(c => (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))).ToArray());
        }
    }
}
