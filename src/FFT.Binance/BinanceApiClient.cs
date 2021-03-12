// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Buffers;
  using System.Collections.Generic;
  using System.Collections.Immutable;
  using System.Globalization;
  using System.Linq;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Net.WebSockets;
  using System.Text;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using FFT.Binance.Serialization;
  using FFT.Disposables;
  using FFT.TimeStamps;
  using Microsoft.AspNetCore.WebUtilities;
  using Nito.AsyncEx;

  /// <summary>
  /// Provides access to Binance market data. This is a single-use object. It
  /// disposes itself when the connection drops.
  /// </summary>
  public sealed partial class BinanceApiClient : AsyncDisposeBase, IAsyncDisposable
  {
    private static readonly IReadOnlyList<int> _orderBookDepthLimits = new List<int>
    {
      5, 10, 20, 50, 100, 500, 1000, 5000,
    }.AsReadOnly();

    /// <summary>
    /// Used for making rest api requests.
    /// </summary>
    private readonly HttpClient _client;

    /// <summary>
    /// Used to track completion of the Work method. Disposal triggers
    /// completion of the method via cancelling a cancellation token, then waits
    /// for the task to be completed.
    /// </summary>
    private readonly Task _workTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinanceApiClient"/>
    /// class and triggers immediate connection to the streaming websocket.
    /// </summary>
    /// <param name="options">Configures the client, particularly for
    /// authentication. Can be left <c>null</c> if you are only accessing
    /// functions that do not require authentication.</param>
    public BinanceApiClient(BinanceApiClientOptions? options)
    {
      // This allows connection-reuse for multiple rest api calls. It requires
      // targeting net5 as discussed here:
      // https://github.com/dotnet/runtime/issues/24613
      _client = new(new SocketsHttpHandler());
      _client.BaseAddress = new("https://api.binance.com");

      if (options is not null)
      {
        Options = options;
        _client.DefaultRequestHeaders.Add("X-MBX-APIKEY", Options.ApiKey);
      }

      _workTask = Task.Run(Work);
    }

    /// <inheritdoc/>
    protected override ValueTask CustomDisposeAsync()
    {
      _client.Dispose();
      return new(_workTask);
    }
  }

  // Utility stuff
  public partial class BinanceApiClient
  {
    /// <summary>
    /// Configures this <see cref="BinanceApiClient"/> instance. May be
    /// <c>null</c> if this instance is not intended for use with functions that
    /// require authentication.
    /// </summary>
    public BinanceApiClientOptions? Options { get; }

    /// <summary>
    /// Current used weight for the local IP for all request rate limiters
    /// defined.
    /// </summary>
    public int UsedWeight { get; private set; } = 0;

    /// <summary>
    /// The number of seconds required to wait, in the case of a 429, to prevent
    /// a ban, or, in the case of a 418, until the ban is over.
    /// </summary>
    public int RetryAfterSeconds { get; private set; } = 0;

    /// <summary>
    /// This method completes successfully if a connection ping test succeeded.
    /// </summary>
    public async Task TestConnectivity()
    {
      using var request = new HttpRequestMessage(HttpMethod.Get, "api/v3/ping");
      using var response = await _client.SendAsync(request);
      await RequestFailedException.ThrowIfNecessary(response);
    }

    /// <summary>
    /// Gets the current time on the Binance api server.
    /// </summary>
    public async Task<ServerTimeResponse> GetServerTime()
    {
      using var request = new HttpRequestMessage(HttpMethod.Get, "api/v3/time");
      using var response = await _client.SendAsync(request);
      return await ParseResponse<ServerTimeResponse>(response);
    }

    private async Task<T> ParseResponse<T>(HttpResponseMessage response)
    {
      if (response.Headers.TryGetValues("X-MBX-USED-WEIGHT", out var usedWeightResponse))
        UsedWeight = int.Parse(usedWeightResponse.First(), NumberStyles.Any, CultureInfo.InvariantCulture);

      if (response.Headers.TryGetValues("Retry-After", out var retryAfterResponse))
        RetryAfterSeconds = int.Parse(retryAfterResponse.First(), NumberStyles.Any, CultureInfo.InvariantCulture);
      else
        RetryAfterSeconds = 0;

      await RequestFailedException.ThrowIfNecessary(response);

      // TODO: Parse and store the
      // X-MBX-ORDER-COUNT-(intervalNum)(intervalLetter) response headers.

      return (await response.Content.ReadFromJsonAsync<T>(SerializationOptions.Instance, DisposedToken))!;
    }
  }

  // Market data (rest api)
  public partial class BinanceApiClient
  {
    /// <summary>
    /// Gets the current order book for the given <paramref name="symbol"/> to a
    /// maximum depth of <paramref name="limit"/> values.
    /// </summary>
    public async Task<OrderBookResponse> GetOrderBook(string symbol, int limit = 100)
    {
      if (string.IsNullOrWhiteSpace(symbol))
        throw new ArgumentException(nameof(symbol));

      if (!_orderBookDepthLimits.Contains(limit))
        throw new ArgumentException(nameof(limit));

      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/depth?symbol={symbol.ToUpperInvariant()}&limit={limit}");
      using var response = await _client.SendAsync(request);
      return await ParseResponse<OrderBookResponse>(response);
    }

    /// <summary>
    /// Get the top order book for the given <paramref name="symbol"/>.
    /// </summary>
    public async Task<TopOrderBook> GetTopOrderBook(string symbol)
    {
      if (string.IsNullOrWhiteSpace(symbol))
        throw new ArgumentException(nameof(symbol));

      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/ticker/bookTicker?symbol={symbol}");
      using var response = await _client.SendAsync(request);
      return await ParseResponse<TopOrderBook>(response);
    }

    /// <summary>
    /// Get the top order book for all instruments.
    /// </summary>
    public async Task<ImmutableList<TopOrderBook>> GetTopOrderBooks()
    {
      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/ticker/bookTicker");
      using var response = await _client.SendAsync(request);
      return await ParseResponse<ImmutableList<TopOrderBook>>(response);
    }

    /// <summary>
    /// Gets the most recent trades for the given <paramref name="symbol"/> up
    /// to maximum <paramref name="limit"/> number of values. Values are
    /// returned in ascending chronological order.
    /// </summary>
    public async Task<ImmutableList<HistoricalTrade>> GetTrades(string symbol, int limit = 500)
    {
      if (string.IsNullOrWhiteSpace(symbol))
        throw new ArgumentException(nameof(symbol));

      if (limit < 1 || limit > 1000)
        throw new ArgumentException(nameof(limit));

      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/trades?symbol={symbol}&limit={limit}");
      using var response = await _client.SendAsync(request);
      return await ParseResponse<ImmutableList<HistoricalTrade>>(response);
    }

    /// <summary>
    /// Get the last trade price for the given <paramref name="symbol"/>.
    /// </summary>
    public async Task<LastPrice> GetLastPrice(string symbol)
    {
      if (string.IsNullOrWhiteSpace(symbol))
        throw new ArgumentException(nameof(symbol));

      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/ticker/price?symbol={symbol}");
      using var response = await _client.SendAsync(request);
      return await ParseResponse<LastPrice>(response);
    }

    /// <summary>
    /// Get the last traded price for all instruments on the exchange.
    /// </summary>
    public async Task<ImmutableList<LastPrice>> GetLastPrices()
    {
      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/ticker/price");
      using var response = await _client.SendAsync(request);
      return await ParseResponse<ImmutableList<LastPrice>>(response);
    }

    /// <summary>
    /// Get compressed, aggregate trades. Trades that fill at the time, from the
    /// same taker order, with the same price will have the quantity aggregated.
    /// </summary>
    public async Task<ImmutableList<AggregateTrade>> GetAggregateTrades(string symbol, TimeStamp from, TimeStamp until)
    {
      symbol.EnsureNotNullOrWhiteSpace(nameof(symbol));
      from.EnsureIs(nameof(from), "must be an exact millisecond.", f => f == f.ToMillisecondFloor());
      until.EnsureIs(nameof(until), "must be an exact millisecond.", f => f == f.ToMillisecondFloor());
      until.Subtract(from).EnsureIs("time difference", "must be less than one hour.", t => t.TotalHours < 1);
      var query = new Dictionary<string, string>
      {
        { "symbol", symbol },
        { "startTime", from.ToUnixMillieconds().ToString() },
        { "endTime", until.ToUnixMillieconds().ToString() },
      };
      var url = QueryHelpers.AddQueryString("api/v3/aggTrades", query);
      using var request = new HttpRequestMessage(HttpMethod.Get, url);
      using var response = await _client.SendAsync(request);
      return await ParseResponse<ImmutableList<AggregateTrade>>(response);
    }
  }

  // Streams
  public partial class BinanceApiClient
  {
    /// <summary>
    /// Signals the addition or removal of subscriptions.
    /// </summary>
    private readonly AsyncAutoResetEvent _signalEvent = new(false);

    /// <summary>
    /// Used to marshal new subscription requests into a thread-safe context.
    /// Null when disposal has begun.
    /// </summary>
    private ImmutableList<Subscription>? _newSubscriptions = ImmutableList<Subscription>.Empty;

    /// <summary>
    /// Used to marshal subscription cancellations into a thread-safe context.
    /// Null when disposal has begun.
    /// </summary>
    private ImmutableList<Subscription>? _cancelSubscriptions = ImmutableList<Subscription>.Empty;

    /// <summary>
    /// Gets a subscription to the given <paramref name="streamInfo"/>.
    /// </summary>
    public async ValueTask<ISubscription> Subscribe(StreamInfo streamInfo)
    {
      var subscription = new Subscription(this, streamInfo);
      while (true)
      {
        var original = Interlocked.CompareExchange(ref _newSubscriptions, null, null);
        if (original is null)
        {
          // We have been disposed. Disposing the subscription before we return
          // it will "complete" the message channel within it, signalling to the
          // user code that the subscription will not yield data and a new
          // subscription should be requested (from a new
          // BinanceMarketDataClient)
          await subscription.DisposeAsync();
          return subscription;
        }
        else
        {
          var @new = original.Add(subscription);
          var result = Interlocked.CompareExchange(ref _newSubscriptions, @new, original);
          if (ReferenceEquals(original, result))
          {
            // Signal the presence of a new subscription to the Work method.
            _signalEvent.Set();
            return subscription;
          }
        }

        // Threadrace. We lost. Start again.
      }
    }

    /// <summary>
    /// Called by the Subscription object when it is disposed.
    /// </summary>
    private void Remove(Subscription subscription)
    {
      while (true)
      {
        var original = Interlocked.CompareExchange(ref _cancelSubscriptions, null, null);
        if (original is null)
        {
          // We have been disposed. There's nothing to do so just return.
          // Execution reaches here This happens when, during disposal, at the
          // end of the "Work" method, we dispose all the subscription objects.
          return;
        }

        // If execution reaches here, the subscription was disposed by user code
        // that no longer wants to receive subscription data.
        var @new = original.Add(subscription);
        if (ReferenceEquals(@new, original)) return;
        var result = Interlocked.CompareExchange(ref _cancelSubscriptions, @new, original);
        if (ReferenceEquals(original, result))
        {
          // Signal the presence of a subscription cancellation to the Work method.
          _signalEvent.Set();
          return;
        }

        // Threadrace. We lost. Start again.
      }
    }

    private async Task Work()
    {
      // Setup storage for parsing incoming messages
      const int BUFFER_SIZE = 1024 * 1024;
      var buffer = new ArrayBufferWriter<byte>(BUFFER_SIZE);

      // Some helper variables
      var requestId = 0;
      var streams = new Dictionary<string, Stream>();

      // The actual websocket
      using var ws = new ClientWebSocket();

      try
      {
        await ws.ConnectAsync(new Uri("wss://stream.binance.com:9443/stream"), DisposedToken);
        var readTask = ReadMessage(ws, buffer, DisposedToken);

        while (true)
        {
          // Insert all new subscriptions.
          var newSubscriptions = Interlocked.Exchange(ref _newSubscriptions, ImmutableList<Subscription>.Empty)!;
          foreach (var subscription in newSubscriptions)
          {
            if (!streams.TryGetValue(subscription.StreamInfo.Name, out var stream))
            {
              stream = subscription.StreamInfo.Type switch
              {
                StreamType.FullDepth => new FullDepthStream(subscription.StreamInfo),
                StreamType.BookTicker => new BookTickerStream(subscription.StreamInfo),
                StreamType.BookTickerAllMarkets => new BookTickerStream(subscription.StreamInfo),
                _ => throw new NotImplementedException(),
              };
              await stream.Initiate(this);
              streams[subscription.StreamInfo.Name] = stream;
              var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
              {
                id = ++requestId,
                method = "SUBSCRIBE",
                @params = new[] { subscription.StreamInfo.Name },
              }));
              await ws.SendAsync(messageBytes, WebSocketMessageType.Text, true, DisposedToken);
            }

            await stream.Add(subscription);
          }

          // Remove all canceled subscriptions
          var canceledSubscriptions = Interlocked.Exchange(ref _cancelSubscriptions, ImmutableList<Subscription>.Empty)!;
          foreach (var subscription in canceledSubscriptions)
          {
            // NB: There's no need to dispose the "subscription" object. It's
            // already disposed -- that's how execution reaches here.
            if (streams.TryGetValue(subscription.StreamInfo.Name, out var stream))
            {
              await stream.Remove(subscription);
              if (stream.SubscriptionCount == 0)
              {
                streams.Remove(subscription.StreamInfo.Name);
                var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                {
                  id = ++requestId,
                  method = "UNSUBSCRIBE",
                  @params = new[] { subscription.StreamInfo.Name },
                }));
                await ws.SendAsync(messageBytes, WebSocketMessageType.Text, true, DisposedToken);
              }
            }
          }

          // Now go ahead and receive messages until we are either a) disposed,
          // or b) receive a signal that subscriptions have been added or
          // canceled.

          var signalTask = _signalEvent.WaitAsync(DisposedToken);
          var completedTask = await Task.WhenAny(signalTask, readTask);
          while (completedTask == readTask)
          {
            await readTask;

            // streamName will turn out null when we receive a control message
            // that is not part of an actual subscription.
#if DEBUG
            var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
#endif
            var streamName = JsonSerializer.Deserialize<StreamNameDTO>(buffer.WrittenSpan, SerializationOptions.Instance).Stream;
            if (!string.IsNullOrWhiteSpace(streamName))
            {
              // We can still have messages in the receive buffer that belong
              // to streams we unsubscribed from.
              if (streams.TryGetValue(streamName, out var stream))
                await stream.Handle(buffer.WrittenMemory);
            }

            readTask = ReadMessage(ws, buffer, DisposedToken);
            completedTask = await Task.WhenAny(signalTask, readTask);
          }

          await signalTask;
        }
      }
      catch (Exception x)
      {
        // Since disposal actually waits for this method to complete, we need to
        // kickoff disposal in a background task.
        _ = DisposeAsync(x);
      }
      finally
      {
        // Cleanup our operations.

        // Important to null this so that new subscription requests are rejected
        // by returning disposed and completed subscription objects.
        var newSubscriptions = Interlocked.Exchange(ref _newSubscriptions, null)!;

        // For each new subscription waiting in the queue, we dispose them to
        // signal they won't be getting any more data.
        foreach (var subscription in newSubscriptions)
          await subscription.DisposeAsync();

        // Important to null this before disposing the subscriptions below
        var removedSubscriptions = Interlocked.Exchange(ref _cancelSubscriptions, null)!;

        // Disposing all the current subscription objects will cause them to
        // send "completed" signal to the user code via the channel
        // writer/reader. It will also cause the subscriptions to make calls
        // into the "RemoveSubscription" method above, but that won't do
        // anything "bad" because we have already nulled the
        // "removedSubscriptions" list above.
        foreach (var kv in streams)
          await kv.Value.DisposeAsync();
      }

      static async Task ReadMessage(ClientWebSocket ws, ArrayBufferWriter<byte> buffer, CancellationToken cancellationToken)
      {
        buffer.Clear();

        var result = await ws.ReceiveAsync(buffer.GetMemory(BUFFER_SIZE), cancellationToken);
        buffer.Advance(result.Count);

        while (!result.EndOfMessage)
        {
          result = await ws.ReceiveAsync(buffer.GetMemory(BUFFER_SIZE), cancellationToken);
          buffer.Advance(result.Count);
        }
      }
    }
  }
}
