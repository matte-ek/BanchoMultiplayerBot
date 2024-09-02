using BanchoMultiplayerBot.Bancho;
using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoSharp.Interfaces;
using Moq;

namespace BanchoMultiplayerBot.Tests.Bancho;

// As you probably can tell, I've never done any unit testing before. :)

[TestClass]
public class MessageHandlerTest
{
    private readonly Mock<IBanchoClient> _banchoClientMock;
    private readonly Mock<IBanchoConnection> _banchoConnectionMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    
    private readonly DateTime _startTime = DateTime.UtcNow;
    
    public MessageHandlerTest()
    {
        _timeProviderMock = new Mock<ITimeProvider>();
        _timeProviderMock.Setup(x => x.UtcNow).Returns(_startTime);
        
        _banchoClientMock = new Mock<IBanchoClient>();

        _banchoClientMock.Setup(x => x.SendPrivateMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        _banchoConnectionMock = new Mock<IBanchoConnection>();
        _banchoConnectionMock.Setup(x => x.BanchoClient).Returns(_banchoClientMock.Object);
    }
    
    [TestMethod]
    public async Task TestSendMessage()
    {
        var messageHandler = new MessageHandler(_banchoConnectionMock.Object, new BanchoClientConfiguration());
        
        Assert.IsFalse(messageHandler.IsRunning);
        
        messageHandler.Start();
        
        messageHandler.SendMessage("TestChannel", "TestMessage");
        messageHandler.SendMessage("TestChannel1", "TestMessage1");
        messageHandler.SendMessage("TestChannel2", "TestMessage2");
        
        // ugh
        await Task.Delay(100);
        
        Assert.IsTrue(messageHandler.IsRunning);
        
        _banchoClientMock.Verify(foo => foo.SendPrivateMessageAsync("TestChannel", "TestMessage"));
        _banchoClientMock.Verify(foo => foo.SendPrivateMessageAsync("TestChannel1", "TestMessage1"));
        _banchoClientMock.Verify(foo => foo.SendPrivateMessageAsync("TestChannel2", "TestMessage2"));
        
        messageHandler.Stop();

        Assert.IsFalse(messageHandler.IsRunning);
    }
    
    [TestMethod]
    public async Task TestSendMessageRateLimit()
    {
        var messageHandler = new MessageHandler(_banchoConnectionMock.Object, new BanchoClientConfiguration(), _timeProviderMock.Object);
        
        messageHandler.Start();

        // Burst send 20 messages
        for (int i = 0; i < 20; i++)
        {
            messageHandler.SendMessage("TestChannel", "TestMessage");
        }
        
        // ugh
        await Task.Delay(100);
        
        // Make sure only 10 messages were sent
        _banchoClientMock.Verify(foo => foo.SendPrivateMessageAsync("TestChannel", "TestMessage"), Times.Exactly(10));
        
        // Wait 5.9 seconds
        _timeProviderMock.Setup(x => x.UtcNow).Returns(_startTime.AddSeconds(5.99));
        await Task.Delay(100);

        // Since the message age is 6 seconds, we still shouldn't have sent anything.
        _banchoClientMock.Verify(foo => foo.SendPrivateMessageAsync("TestChannel", "TestMessage"), Times.Exactly(10));
        
        // Wait the full 6 seconds
        _timeProviderMock.Setup(x => x.UtcNow).Returns(_startTime.AddSeconds(6.01));
        await Task.Delay(100);
        
        // Now the next 10 messages should be sent
        _banchoClientMock.Verify(foo => foo.SendPrivateMessageAsync("TestChannel", "TestMessage"), Times.Exactly(20));
        
        messageHandler.Stop();
    }
    
    [TestMethod]
    public async Task TestSendMessageCookie()
    {
        var messageHandler = new MessageHandler(_banchoConnectionMock.Object, new BanchoClientConfiguration(), _timeProviderMock.Object);
        
        messageHandler.Start();
        
        var firstMessageCookie = messageHandler.SendMessageTracked("TestChannel", "TrackedMessage");
        
        // Burst send 10 messages to delay the second cookie
        for (int i = 0; i < 10; i++)
        {
            messageHandler.SendMessage("TestChannel", "TestMessage");
        }
        
        var secondMessageCookie = messageHandler.SendMessageTracked("TestChannel", "TrackedMessage2");
        
        await Task.Delay(100);
        
        Assert.IsTrue(firstMessageCookie.IsSent);
        Assert.IsFalse(secondMessageCookie.IsSent);

        _timeProviderMock.Setup(x => x.UtcNow).Returns(_startTime.AddSeconds(6.01));

        await Task.Delay(100);

        Assert.IsTrue(firstMessageCookie.IsSent);
        Assert.IsTrue(secondMessageCookie.IsSent);
        
        messageHandler.Stop();
    }
}