// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Buffers;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.Collections.Immutable;
  using System.Diagnostics;
  using System.Globalization;
  using System.Linq;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Net.WebSockets;
  using System.Runtime.CompilerServices;
  using System.Security.Cryptography;
  using System.Text;
  using System.Text.Json;
  using System.Text.Json.Serialization;
  using System.Threading;
  using System.Threading.Channels;
  using System.Threading.Tasks;
  using FFT.Binance.MarketDataStreams;
  using FFT.Disposables;
  using FFT.TimeStamps;

  /// <summary>
  /// Create a long-lived singleton instance of this class and use it throughout
  /// your application. Once instance per connection.
  /// </summary>
  public sealed partial class BinanceApiClient : DisposeBase, IDisposable
  {
    private static readonly IReadOnlyList<string> _endPoints = new List<string>
    {
      "https://api.binance.com",  // The main api endpoint
      "https://api1.binance.com", // Failover endpoints in case of system degradation
      "https://api2.binance.com",
      "https://api3.binance.com",
    }.AsReadOnly();

    private static readonly IReadOnlyList<int> _orderBookDepthLimits = new List<int>
    {
      5, 10, 20, 50, 100, 500, 1000, 5000,
    }.AsReadOnly();

    private readonly HttpClient _client;
    private readonly ClientWebSocket _webSocket;
    private readonly byte[] _secretKeyBytes;
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinanceApiClient"/> class.
    /// </summary>
    /// <param name="options">Contains the configurable options for this
    /// connection.</param>
    public BinanceApiClient(BinanceApiClientOptions options)
    {
      Options = options;

      _secretKeyBytes = Encoding.UTF8.GetBytes(options.SecretKey);
      _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
      _serializerOptions.Converters.Add(new TimeStampConverter());
      _serializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
      _serializerOptions.PropertyNameCaseInsensitive = false;

      _client = new HttpClient(new SocketsHttpHandler());
      _client.BaseAddress = new Uri(_endPoints[0] + '/');
      _client.DefaultRequestHeaders.Add("X-MBX-APIKEY", Options.ApiKey);

      _webSocket = new ClientWebSocket();
    }

    /// <summary>
    /// Contains the configurable options for this connection.
    /// </summary>
    public BinanceApiClientOptions Options { get; }

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

    /// <inheritdoc/>
    protected override void CustomDispose()
    {
      _client.Dispose();
      _webSocket.Dispose();
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

      return (await response.Content.ReadFromJsonAsync<T>(_serializerOptions, DisposedToken))!;
    }

    /// <summary>
    /// Creates a signature value to add to the end of a query string.
    /// </summary>
    /// <param name="queryString">The entire query string NOT including the
    /// leading '?'.</param>
    private string CreateSignature(string queryString)
    {
      var queryStringBytes = Encoding.UTF8.GetBytes(queryString);
      using var hmac = new HMACSHA256(_secretKeyBytes);
      var signatureBytes = hmac.ComputeHash(queryStringBytes);
      // TODO: This should perhaps be base64 representation instead, I'm not
      // sure yet.
      return Encoding.UTF8.GetString(signatureBytes);
    }

    private async Task WorkWebSocket()
    {
      try
      {
        while (true)
        {
          await _webSocket.ConnectAsync(new Uri($"wss://stream.binance.com:9443/stream"), DisposedToken);
        }
      }
      catch (OperationCanceledException)
      {
        return;
      }
      catch (ObjectDisposedException)
      {
        return;
      }
      catch (Exception x)
      {
        Debug.Fail(x.ToString());
      }
    }
  }

  public partial class BinanceApiClient
  {
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
  }

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

      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/depth?symbol={symbol}&limit={limit}");
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
  }

  public partial class BinanceApiClient
  {
    public async IAsyncEnumerable<Book> GetDepthStream(string symbol, bool rapid, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      var bufferWriter = new ArrayBufferWriter<byte>();
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
      using var ws = new ClientWebSocket();
      await ws.ConnectAsync(new Uri($"wss://stream.binance.com:9443/ws"), cts.Token);

      var subscribeMessage = rapid
        ? @"{""method"": ""SUBSCRIBE"",""params"": [""btcusdt@depth@100ms""],""id"": 1}"
        : @"{""method"": ""SUBSCRIBE"",""params"": [""btcusdt@depth""],""id"": 1}";

      await ws.SendAsync(Encoding.UTF8.GetBytes(subscribeMessage), WebSocketMessageType.Text, true, cts.Token);

      var initialDepth = await GetOrderBook(symbol, 1000); // TODO: Add cancellation
      var orderBook = Book.From(initialDepth);
      var diff = await ReadDiff(ws, bufferWriter, cts.Token);
      while (diff.UpdateIdTo <= initialDepth.LastUpdateId)
        diff = await ReadDiff(ws, bufferWriter, cts.Token);

      while (true)
      {
        orderBook = orderBook.ApplyDiff(diff);
        yield return orderBook;
        diff = await ReadDiff(ws, bufferWriter, cts.Token);
      }
    }

    private async Task<BookUpdate> ReadDiff(ClientWebSocket ws, ArrayBufferWriter<byte> writer, CancellationToken cancellationToken)
    {
      // At 1000 levels deep, the messages can be fairly long. I didn't spend
      // long testing, but the first single update I saw was 6kb. I think it
      // doesn't hurt to have a re-usable 1Mb buffer inside the writer.
      const int BUFFER_SIZE = 1024 * 1024;
      while (true)
      {
        // TODO: Really just need to reset the pointer in the writer. Clearing
        // it (by resetting all the values in the buffer) is overkill.
        writer.Clear();

        var result = await ws.ReceiveAsync(writer.GetMemory(BUFFER_SIZE), cancellationToken);
        writer.Advance(result.Count);

        while (!result.EndOfMessage)
        {
          result = await ws.ReceiveAsync(writer.GetMemory(BUFFER_SIZE), cancellationToken);
          writer.Advance(result.Count);
        }

        // Other kinds of control messages also come through. When they do, the
        // diff deserializes with missing property values, so we check the
        // EvenType property to make sure we have a valid diff. Otherwise just
        // read the next message, hence the while loop.
        var diff = JsonSerializer.Deserialize<BookUpdate>(writer.WrittenSpan, _serializerOptions)!;
        if (diff.EventType != "depthUpdate")
          return diff;
      }
    }
  }

  public partial class BinanceApiClient
  {
    private sealed class WebsocketConnection : DisposeBase
    {
      private readonly BinanceApiClient _client;
      private readonly ClientWebSocket _ws;
      private readonly Channel<string> _newSubscriptions;
      private readonly Func<object, Task> _messageHandler;

      public WebsocketConnection(BinanceApiClient client, Func<object, Task> messageHandler)
      {
        _client = client;
        _ws = new ClientWebSocket();
        _newSubscriptions = Channel.CreateBounded<string>(1024);
        _messageHandler = messageHandler;
        Task.Run(Work);
      }

      protected override void CustomDispose()
      {
        _ws.Dispose();
      }

      private async Task Work()
      {
        try
        {
          await _ws.ConnectAsync(new Uri($"wss://stream.binance.com:9443/stream"), DisposedToken);
          Task.Run(DoSubscriptions);
          Task.Run(ReadMessages);
        }
        catch (Exception x)
        {
          Dispose(x);
        }
      }

      async Task DoSubscriptions()
      {
        try
        {
          var id = 0;
          while (true)
          {
            var subscription = await _newSubscriptions.Reader.ReadAsync(DisposedToken);
            var message = JsonSerializer.Serialize(new
            {
              id = ++id,
              method = "SUBSCRIBE",
              @params = new[] { subscription.Name },
            }).ToUtf8();
            await _ws.SendAsync(message, WebSocketMessageType.Text, true, DisposedToken);
          }
        }
        catch (Exception x)
        {
          Dispose(x);
        }
      }

      async Task ReadMessages()
      {
        try
        {
          const int BUFFER_SIZE = 1024 * 1024;
          var buffer = new ArrayBufferWriter<byte>(BUFFER_SIZE);
          while (true)
          {
            // At 1000 levels deep, the order book update messages can be fairly long. I didn't spend
            // long testing, but the first single update I saw was 6kb. I think it
            // doesn't hurt to have a re-usable 1Mb buffer inside the writer.
            while (true)
            {
              // TODO: Really just need to reset the pointer in the writer. Clearing
              // it (by resetting all the values in the buffer) is overkill.
              buffer.Clear();

              var result = await _ws.ReceiveAsync(buffer.GetMemory(BUFFER_SIZE), DisposedToken);
              buffer.Advance(result.Count);

              while (!result.EndOfMessage)
              {
                result = await _ws.ReceiveAsync(buffer.GetMemory(BUFFER_SIZE), DisposedToken);
                buffer.Advance(result.Count);
              }

              await _messageHandler(StreamObjectDeserializer.Deserialize(buffer.WrittenSpan));

              // Other kinds of control messages also come through. When they do, the
              // diff deserializes with missing property values, so we check the
              // EvenType property to make sure we have a valid diff. Otherwise just
              // read the next message, hence the while loop.
              var diff = JsonSerializer.Deserialize<BookUpdate>(buffer.WrittenSpan, _client._serializerOptions)!;
              if (diff.EventType != "depthUpdate")
                return diff;
            }
          }
        }
        catch (Exception x)
        {
          Dispose(x);
        }
      }
    }

    private interface ISubscription
    {
      string Name { get; }
    }

    private static class StreamObjectDeserializer
    {
      public static object Deserialize(ReadOnlySpan<byte> messageBytes)
      {

      }
    }

    private struct StreamEvent
    {
      public string StreamName { get; set; }
    }
  }
}
