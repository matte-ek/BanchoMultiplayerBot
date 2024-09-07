using BanchoMultiplayerBot.Bancho;
using BanchoMultiplayerBot.Bancho.Commands;
using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoSharp.Messaging.ChatMessages;
using Moq;

namespace BanchoMultiplayerBot.Tests.Bancho;

[TestClass]
public class CommandHandlerTest
{
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly Mock<IMessageHandler> _messageHandlerMock;
    
    private readonly DateTime _startTime = DateTime.UtcNow;

    public CommandHandlerTest()
    {
        _timeProviderMock = new Mock<ITimeProvider>();
        _timeProviderMock.Setup(x => x.UtcNow).Returns(_startTime);
        
        _messageHandlerMock = new Mock<IMessageHandler>();
    }
    
    [TestMethod]
    public async Task TestExecuteCommandExact()
    {
        var commandHandler = new CommandHandler(_messageHandlerMock.Object, new BanchoClientConfiguration(), _timeProviderMock.Object);
        var messageCookie = new TrackedMessageCookie();
        
        _messageHandlerMock.Setup(x => x.SendMessageTracked(It.IsAny<string>(), It.IsAny<string>())).Returns(messageCookie);

        var sendTask = commandHandler.ExecuteAsync<MatchStartCommand>("#mp_12345678");

        // Fake the command being sent from our side
        messageCookie.SentTime = _startTime.AddMilliseconds(10);
        messageCookie.IsSent = true;

        await Task.Delay(100);
        
        // Make sure the command was sent
        _messageHandlerMock.Verify(x => x.SendMessageTracked("#mp_12345678", "!mp start"));
        
        // Add some fake time
        _timeProviderMock.Setup(x => x.UtcNow).Returns(_startTime.AddSeconds(1));

        // Fake the response to the command
        _messageHandlerMock.Raise(x => x.OnMessageReceived += null, PrivateIrcMessage.CreateFromParameters("BanchoBot", "#mp_12345678", "Started the match"));
        
        // This should complete successfully
        var response = await sendTask.WaitAsync(TimeSpan.FromSeconds(1));

        // Make sure the command was seemingly executed successfully
        Assert.IsTrue(response);
    }
    
    [TestMethod]
    public async Task TestExecuteCommandStartsWith()
    {
        var commandHandler = new CommandHandler(_messageHandlerMock.Object, new BanchoClientConfiguration(), _timeProviderMock.Object);
        var messageCookie = new TrackedMessageCookie();
        
        _messageHandlerMock.Setup(x => x.SendMessageTracked(It.IsAny<string>(), It.IsAny<string>())).Returns(messageCookie);

        var sendTask = commandHandler.ExecuteAsync<MatchSetBeatmapCommand>("#mp_12345678", ["123"]);

        // Fake the command being sent from our side
        messageCookie.SentTime = _startTime.AddMilliseconds(10);
        messageCookie.IsSent = true;

        await Task.Delay(100);
        
        // Make sure the command was sent
        _messageHandlerMock.Verify(x => x.SendMessageTracked("#mp_12345678", "!mp map 123"));
        
        // Add some fake time
        _timeProviderMock.Setup(x => x.UtcNow).Returns(_startTime.AddSeconds(1));

        // Fake the response to the command
        _messageHandlerMock.Raise(x => x.OnMessageReceived += null, PrivateIrcMessage.CreateFromParameters("BanchoBot", "#mp_12345678", "Changed beatmap to 123"));
        
        // This should complete successfully
        var response = await sendTask.WaitAsync(TimeSpan.FromSeconds(1));

        // Make sure the command was seemingly executed successfully
        Assert.IsTrue(response);
    }
    
    [TestMethod]
    public async Task TestExecuteCommandSpamFilter()
    {
        var commandHandler = new CommandHandler(_messageHandlerMock.Object, new BanchoClientConfiguration(), _timeProviderMock.Object);
        var messageCookie = new TrackedMessageCookie();
        
        List<string> sentMessages = [];
        
        _messageHandlerMock.Setup(x => x.SendMessageTracked(It.IsAny<string>(), It.IsAny<string>())).Callback((string channel, string msg) =>
        {
            sentMessages.Add(msg);    
        }).Returns(messageCookie);
        
        List<Task> sendTasks = [];

        for (int i = 0; i < 4; i++)
        {
            sendTasks.Add(commandHandler.ExecuteAsync<MatchAbortCommand>("#mp_12345678"));
        }
        
        // Fake the command being sent from our side
        messageCookie.SentTime = _startTime.AddMilliseconds(10);
        messageCookie.IsSent = true;

        await Task.Delay(100);
        
        // Make sure the commands were sent and were all unique
        Assert.IsTrue(sentMessages.Count == 4);
        Assert.IsTrue(sentMessages.Distinct().Count() == sentMessages.Count);
        
        // Add some fake time
        _timeProviderMock.Setup(x => x.UtcNow).Returns(_startTime.AddSeconds(1));

        // Fake the response to the command
        for (int i = 0; i < 4; i++)
        {
            _messageHandlerMock.Raise(x => x.OnMessageReceived += null, PrivateIrcMessage.CreateFromParameters("BanchoBot", "#mp_12345678", "Aborted the match"));
        }
        
        // This should complete successfully
        await Task.WhenAny(Task.WhenAll(sendTasks), Task.Delay(TimeSpan.FromSeconds(1)));

        // Make sure the command was seemingly executed successfully
        foreach (var task in sendTasks.Cast<Task<bool>>())
        {
            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.Result);
        }
    }
    
    [TestMethod]
    public async Task TestCommandDelayedSend()
    {
        var commandHandler = new CommandHandler(_messageHandlerMock.Object, new BanchoClientConfiguration(), _timeProviderMock.Object);
        var messageCookie = new TrackedMessageCookie();
        
        _messageHandlerMock.Setup(x => x.SendMessageTracked(It.IsAny<string>(), It.IsAny<string>())).Returns(messageCookie);

        // Attempt to execute the command
        var sendTask = commandHandler.ExecuteAsync<MatchStartCommand>("#mp_12345678");
        
        await Task.Delay(100);
        
        // Make sure the command execution is still pending
        Assert.IsFalse(sendTask.IsCompleted);
        
        // Fake the command being sent from our side
        messageCookie.SentTime = _startTime.AddMilliseconds(10);
        messageCookie.IsSent = true;
        
        await Task.Delay(100);
        
        // Make sure the command was sent
        _messageHandlerMock.Verify(x => x.SendMessageTracked("#mp_12345678", "!mp start"));
        
        // Add some fake time
        _timeProviderMock.Setup(x => x.UtcNow).Returns(_startTime.AddSeconds(1));

        // Fake the response to the command
        _messageHandlerMock.Raise(x => x.OnMessageReceived += null, PrivateIrcMessage.CreateFromParameters("BanchoBot", "#mp_12345678", "Started the match"));
        
        // This should complete successfully
        var response = await sendTask.WaitAsync(TimeSpan.FromSeconds(1));

        // Make sure the command was seemingly executed successfully
        Assert.IsTrue(response);
    }
    
    [TestMethod]
    public async Task TestExecuteCommandIgnoredResponse()
    {
        var commandHandler = new CommandHandler(_messageHandlerMock.Object, new BanchoClientConfiguration(), _timeProviderMock.Object);
        var messageCookie = new TrackedMessageCookie();
        
        _messageHandlerMock.Setup(x => x.SendMessageTracked(It.IsAny<string>(), It.IsAny<string>())).Returns(messageCookie);

        var sendTask = commandHandler.ExecuteAsync<MatchStartCommand>("#mp_12345678");

        // Fake the command being sent from our side
        messageCookie.SentTime = _startTime.AddMilliseconds(10);
        messageCookie.IsSent = true;

        await Task.Delay(100);
        
        // Add some fake time, since it was more than 5 seconds ago we sent the first command,
        // the code should attempt to send it again.
        _timeProviderMock.Setup(x => x.UtcNow).Returns(_startTime.AddSeconds(6));
        
        await Task.Delay(100);
        
        // Fake the response to the command
        _messageHandlerMock.Raise(x => x.OnMessageReceived += null, PrivateIrcMessage.CreateFromParameters("BanchoBot", "#mp_12345678", "Started the match"));
        
        // This should complete successfully
        var response = await sendTask.WaitAsync(TimeSpan.FromSeconds(1));
        
        // Make sure the command was twice
        _messageHandlerMock.Verify(x => x.SendMessageTracked("#mp_12345678", "!mp start"), Times.Exactly(2));

        // Make sure the command was seemingly executed successfully
        Assert.IsTrue(response);
    }
}