using BanchoMultiplayerBot.Config;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BanchoMultiplayerBot.Behaviour
{
    public class ConfigBehaviour : IBotBehaviour
    {
        private Lobby _lobby = null!;

        public void Setup(Lobby lobby)
        {
            _lobby = lobby;

            _lobby.OnAdminMessage += OnAdminMessage;
        }

        private void OnAdminMessage(BanchoSharp.Interfaces.IPrivateIrcMessage message)
        {
            if (!message.Content.StartsWith("!config "))
            {
                return;
            }

            var configMessageSplit = message.Content.Split(" ");

            if (configMessageSplit.Length <= 2)
            {
                return;
            }

            try
            {
                var lobbyConfigProperties = typeof(LobbyConfiguration).GetProperties();
                var property = lobbyConfigProperties.FirstOrDefault(x => (x.Name.ToLower() == configMessageSplit[1].ToLower()));

                if (property != null)
                {
                    // this feels stupid
                    var value = message.Content[("!config ".Length + configMessageSplit[1].Length + 1)..];
                    var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (type.IsEnum)
                    {
                        property?.SetValue(_lobby.Configuration, Enum.Parse(type, value));
                    }
                    else if (type.IsArray)
                    {
                        var values = value.Split(",");

                        property?.SetValue(_lobby.Configuration, values);
                    }
                    else
                    {
                        property?.SetValue(_lobby.Configuration, Convert.ChangeType(value, type), null);
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
