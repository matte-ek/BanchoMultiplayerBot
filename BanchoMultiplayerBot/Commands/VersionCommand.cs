﻿using System.Diagnostics;
using System.Reflection;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class VersionCommand : IPlayerCommand
{
    public string Command => "version";

    public List<string>? Aliases => [ "uptime" ];

    public bool AllowGlobal => true;

    public bool Administrator => false;

    public int MinimumArguments => 0;

    public string? Usage => null;

    public Task ExecuteAsync(CommandEventContext message)
    {
        var version = Assembly.GetExecutingAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (version == null)
        {
            return Task.CompletedTask;
        }

        var commitHashBegin = version.IndexOf('+') + 1;
        var commitHash = version[commitHashBegin..];
        var commitHashSubset = version[commitHashBegin..(commitHashBegin + 7)];
        var upTime = Process.GetCurrentProcess().StartTime - DateTime.UtcNow;
        
        message.Reply($"BanchoMultiplayerBot@[https://github.com/matte-ek/BanchoMultiplayerBot/commit/{commitHash} {commitHashSubset}], current uptime: {upTime:HH:mm:ss}");
            
        return Task.CompletedTask;
    }
}