using System.Net.Sockets;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EventBus.RabbitMQ;

public class RabbitMQPersistentConnection : IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private IConnection _connection;
    private readonly int retryCount;
    private object lock_object = new object();
    private bool _disposed;

    public RabbitMQPersistentConnection(IConnectionFactory connectionFactory,int retrycount = 5)
    {
        this._connectionFactory = connectionFactory;
        this.retryCount = retrycount;
    }

    public bool IsConnected => _connection != null && _connection.IsOpen;

    public IModel CreateModel()
    {
        return _connection.CreateModel();
    }
    
    public void Dispose()
    {
        _connection.Dispose();
    }

    public bool TryConnect()
    {
        lock (lock_object)
        {
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time) => { }
                    );
            policy.Execute(() =>
            {
                _connection = _connectionFactory.CreateConnection();
            });

            if (IsConnected)
            {
                _connection.ConnectionShutdown += Connection_ConnectionShutDown;
                _connection.CallbackException += Connection_CallbackException;
                _connection.ConnectionBlocked += Connection_ConnectionBlocked;

                return true;
            }

            return false;
        }
    }

    private void Connection_ConnectionShutDown(object sender, ShutdownEventArgs e)
    {
        if (_disposed) return;
        TryConnect();
    }
    
    private void Connection_CallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;
        TryConnect();
    }
    
    private void Connection_ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;
        TryConnect();
    }
}