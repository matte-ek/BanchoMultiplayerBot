﻿using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;
using BanchoMultiplayerBot.Host.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanchoMultiplayerBot.Host.WebApi.Controllers;

[ApiController]
[Route("api/[controller]/lobby")]
[Authorize]
public class MessageController(MessageService messageService) : ControllerBase
{
    [HttpGet("{lobbyId:int}")]
    public IEnumerable<MessageModel> GetLobbyMessages(int lobbyId, int offset = 0, int limit = 100)
    {
        return messageService.GetLobbyMessages(lobbyId, offset, limit);
    }
    
    [HttpPost("send")]
    public void SendMessage([FromBody] SendMessageModel message)
    {
        messageService.SendLobbyMessage(message.LobbyId, message.Content);
    }
}