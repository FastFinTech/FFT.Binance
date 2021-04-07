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

  /// <summary>
  /// Provides access to Binance market data. This is a single-use object. It
  /// disposes itself when the connection drops.
  /// </summary>
  public sealed partial class BinanceApiClient : DisposeBase, IDisposable
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
    }

    /// <inheritdoc/>
    protected override void CustomDispose()
    {
      _client.Dispose();
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
      until.Subtract(from).EnsureIs("time difference", "must be less than one hour.", t => t.TotalHours <= 1);
      var query = new Dictionary<string, string>
      {
        { "symbol", symbol },
        { "startTime", (from.ToUnixMillieconds() + 1).ToString() },
        { "endTime", until.ToUnixMillieconds().ToString() },
      };
      var url = QueryHelpers.AddQueryString("api/v3/aggTrades", query);
      using var request = new HttpRequestMessage(HttpMethod.Get, url);
      using var response = await _client.SendAsync(request);
      return await ParseResponse<ImmutableList<AggregateTrade>>(response);
    }

    public async IAsyncEnumerable<ImmutableList<AggregateTrade>> GetAggregateTradesForMoreThanOneHour(string symbol, TimeStamp from, TimeStamp until)
    {
      var time = from;
      while (time < until)
      {
        var thisUntil = TimeStamp.Min(until, time.AddHours(1));
        yield return await GetAggregateTrades(symbol, time, thisUntil);
        time = time.AddHours(1);
      }
    }
  }
}
