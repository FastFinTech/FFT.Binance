// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Buffers;
  using System.Collections.Immutable;
  using System.Diagnostics;
  using System.Globalization;
  using System.Linq;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Net.WebSockets;
  using System.Runtime.CompilerServices;
  using System.Text;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using FFT.Binance.Serialization;
  using FFT.Disposables;
  using FFT.Subscriptions;
  using FFT.TimeStamps;
  using Microsoft.AspNetCore.WebUtilities;
  using Nito.AsyncEx;

  public sealed partial class BinanceApiStreamingClient : AsyncDisposeBase, IAsyncDisposable
  {
    private const int BUFFER_SIZE = 1024 * 1024;

    private readonly ClientWebSocket _ws;

    private readonly Task _initializationTask;

    private readonly SubscriptionManager<string> _subscriptionManager;

    private readonly ArrayBufferWriter<byte> _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinanceApiStreamingClient"/> class.
    /// A connection attempt is immediately initiated in a background task.
    /// </summary>
    public BinanceApiStreamingClient()
    {
      _buffer = new(BUFFER_SIZE);

      _ws = new();

      _initializationTask = Task.Run(async () =>
      {
        try
        {
          await _ws.ConnectAsync(new Uri("wss://stream.binance.com:9443/stream"), DisposedToken);
        }
        catch (Exception x)
        {
          throw new Exception("There was an error initializing the web socket connection. See inner exception for more information.", x);
        }
      });

      var requestId = 0;
      _subscriptionManager = new(new SubscriptionManagerOptions<string>
      {
        StartStream = async (_, streamId, cancellationToken) =>
        {
          await _initializationTask.WaitAsync(cancellationToken);
          var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
          {
            id = ++requestId,
            method = "SUBSCRIBE",
            @params = new[] { streamId },
          }));
          using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, DisposedToken);
          await _ws.SendAsync(messageBytes, WebSocketMessageType.Text, true, linked.Token);
          var streamType = StreamInfo.ParseStreamType(streamId);
          var broadcastHub = streamType switch
          {
            StreamType.BookTicker => (BroadcastHubBase)new BookTickerHub(),
            StreamType.FullDepth => (BroadcastHubBase)new FullDepthHub(),
            StreamType.AggregatedTrade => (BroadcastHubBase)new AggregateTradeHub(),
            _ => throw streamType.UnknownValueException(),
          };
          await broadcastHub.Initialize(this, streamId);
          return broadcastHub;
        },
        EndStream = async (_, streamId) =>
        {
          var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
          {
            id = ++requestId,
            method = "UNSUBSCRIBE",
            @params = new[] { streamId },
          }));
          await _ws.SendAsync(messageBytes, WebSocketMessageType.Text, true, DisposedToken);
        },
        GetNextMessage = async (_, cancellationToken) =>
        {
          await _initializationTask.WaitAsync(cancellationToken);
          using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, DisposedToken);
          while (true)
          {
            _buffer.Clear();
receive:
            var result = await _ws.ReceiveAsync(_buffer.GetMemory(BUFFER_SIZE), linked.Token);
            _buffer.Advance(result.Count);
            if (!result.EndOfMessage) goto receive;

#if DEBUG
            var json = Encoding.UTF8.GetString(_buffer.WrittenSpan);
            Trace.WriteLine(json);
#endif
            try
            {
              string streamId = null;
              try
              {
                streamId = JsonSerializer.Deserialize<StreamNameDTO>(_buffer.WrittenSpan, SerializationOptions.Instance).Stream;
              }
              catch (Exception x)
              {
                int i = 0;
              }

              if (!string.IsNullOrWhiteSpace(streamId))
              {
                return (streamId, _buffer.WrittenMemory);
              }
            }
            catch (Exception x)
            {
              int i = 0;
            }
          }
        },
      });
      _subscriptionManager.DisposedTask.ContinueWith(
        t =>
        {
          var exception = new Exception("Subscription manager encountered an error. See inner exception for details.", _subscriptionManager.DisposalReason);
          _ = DisposeAsync(exception);
        },
        DisposedToken).Ignore();
    }

    public BinanceApiClient? ApiClient { get; set; }

    /// <summary>
    /// Gets a subscription to the given <paramref name="streamInfo"/>.
    /// </summary>
    public async ValueTask<ISubscription> Subscribe(StreamInfo streamInfo)
    {
      await _initializationTask;
      return _subscriptionManager.Subscribe(streamInfo.Name);
    }

    /// <inheritdoc/>
    protected override async ValueTask CustomDisposeAsync()
    {
      await _subscriptionManager.DisposeAsync();
      _ws.Dispose();
    }
  }
}
