// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Collections.Generic;
  using System.Collections.Immutable;
  using System.Linq;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using FFT.Binance.Serialization;
  using FFT.Disposables;
  using FFT.TimeStamps;
  using Microsoft.AspNetCore.WebUtilities;
  using Nito.AsyncEx;
  using static System.Globalization.CultureInfo;
  using static System.Globalization.NumberStyles;

  /// <summary>
  /// Provides access to non-streaming Binance market data via the rest api.
  /// </summary>
  public sealed partial class BinanceApiClient : DisposeBase, IDisposable
  {
    private static readonly IReadOnlyList<int> _orderBookDepthLimits = new List<int>
    {
      5, 10, 20, 50, 100, 500, 1000, 5000,
    };

    /// <summary>
    /// Used for making rest api requests.
    /// </summary>
    private readonly HttpClient _client;

    /// <summary>
    /// Used to limit the number of requests that can be simultaneously made.
    /// Currently this one semaphore is used for all request types. This could
    /// be updated in future to have a different semaphore for historical data
    /// downloads than for order commands.
    /// </summary>
    private readonly AsyncSemaphore _simultaneousRequests;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinanceApiClient"/> class
    /// and triggers immediate connection to the streaming websocket.
    /// </summary>
    /// <param name="options">Configures the client, particularly for
    /// authentication. Can be left <c>null</c> if you are only accessing
    /// functions that do not require authentication and are happy with all
    /// default values..</param>
    public BinanceApiClient(BinanceApiClientOptions? options = null)
    {
      options ??= new();
      // This allows connection-reuse for multiple rest api calls. It requires
      // targeting net5 as discussed here:
      // https://github.com/dotnet/runtime/issues/24613
      _client = new(new SocketsHttpHandler());
      _client.BaseAddress = new("https://api.binance.com");
      _client.Timeout = options.RequestTimeout;

      _simultaneousRequests = new AsyncSemaphore(options.MaxSimultaneousRequests);
      if (!string.IsNullOrWhiteSpace(options.ApiKey))
        _client.DefaultRequestHeaders.Add("X-MBX-APIKEY", options.ApiKey);

      Options = options;
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
    /// Configuration for this <see cref="BinanceApiClient"/> instance.
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

    private async Task<T> ParseResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
      // TODO: Implement usage of this value.
      if (response.Headers.TryGetValues("X-MBX-USED-WEIGHT", out var usedWeightResponse))
        UsedWeight = int.Parse(usedWeightResponse.First(), Any, InvariantCulture);

      // TODO: Implement usage of this value.
      if (response.Headers.TryGetValues("Retry-After", out var retryAfterResponse))
        RetryAfterSeconds = int.Parse(retryAfterResponse.First(), Any, InvariantCulture);
      else
        RetryAfterSeconds = 0;

      // TODO: Parse and store the
      // X-MBX-ORDER-COUNT-(intervalNum)(intervalLetter) response headers.

      await RequestFailedException.ThrowIfNecessary(response);

      return (await response.Content.ReadFromJsonAsync<T>(SerializationOptions.Instance, cancellationToken))!;
    }
  }

  // Market data (rest api)
  public partial class BinanceApiClient
  {
    /// <summary>
    /// This method completes successfully if a connection ping test succeeded.
    /// </summary>
    public async Task TestConnectivity(CancellationToken cancellationToken = default)
    {
      using var linked = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
      using var wait = await _simultaneousRequests.LockAsync(linked.Token);
      using var request = new HttpRequestMessage(HttpMethod.Get, "api/v3/ping");
      using var response = await _client.SendAsync(request, linked.Token);
      await RequestFailedException.ThrowIfNecessary(response);
    }

    /// <summary>
    /// Gets the current time on the Binance api server.
    /// </summary>
    public async Task<ServerTimeResponse> GetServerTime(CancellationToken cancellationToken = default)
    {
      using var linked = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
      using var wait = await _simultaneousRequests.LockAsync(linked.Token);
      using var request = new HttpRequestMessage(HttpMethod.Get, "api/v3/time");
      using var response = await _client.SendAsync(request, linked.Token);
      return await ParseResponse<ServerTimeResponse>(response, linked.Token);
    }

    /// <summary>
    /// Gets general information from the exchange.
    /// </summary>
    public async Task<ExchangeInfoResponse> GetExchangeInformation(CancellationToken cancellationToken = default)
    {
      using var linked = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
      using var wait = await _simultaneousRequests.LockAsync(linked.Token);
      using var request = new HttpRequestMessage(HttpMethod.Get, "api/v3/exchangeInfo");
      using var response = await _client.SendAsync(request, linked.Token);
      return await ParseResponse<ExchangeInfoResponse>(response, linked.Token);
    }

    /// <summary>
    /// Gets the current order book for the given <paramref name="symbol"/> to a
    /// maximum depth of <paramref name="limit"/> values.
    /// </summary>
    public async Task<OrderBookResponse> GetOrderBook(string symbol, int limit = 100, CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrWhiteSpace(symbol))
        throw new ArgumentException(nameof(symbol));

      if (!_orderBookDepthLimits.Contains(limit))
        throw new ArgumentException(nameof(limit));

      using var linked = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
      using var wait = await _simultaneousRequests.LockAsync(linked.Token);
      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/depth?symbol={symbol.ToUpperInvariant()}&limit={limit}");
      using var response = await _client.SendAsync(request, linked.Token);
      return await ParseResponse<OrderBookResponse>(response, linked.Token);
    }

    /// <summary>
    /// Get the top order book for the given <paramref name="symbol"/>.
    /// </summary>
    public async Task<TopOrderBook> GetTopOrderBook(string symbol, CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrWhiteSpace(symbol))
        throw new ArgumentException(nameof(symbol));

      using var linked = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
      using var wait = await _simultaneousRequests.LockAsync(linked.Token);
      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/ticker/bookTicker?symbol={symbol}");
      using var response = await _client.SendAsync(request, linked.Token);
      return await ParseResponse<TopOrderBook>(response, linked.Token);
    }

    /// <summary>
    /// Get the top order book for all instruments.
    /// </summary>
    public async Task<ImmutableList<TopOrderBook>> GetTopOrderBooks(CancellationToken cancellationToken = default)
    {
      using var linked = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
      using var wait = await _simultaneousRequests.LockAsync(linked.Token);
      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/ticker/bookTicker");
      using var response = await _client.SendAsync(request, linked.Token);
      return await ParseResponse<ImmutableList<TopOrderBook>>(response, linked.Token);
    }

    /// <summary>
    /// Gets the most recent trades for the given <paramref name="symbol"/> up
    /// to maximum <paramref name="limit"/> number of values. Values are
    /// returned in ascending chronological order.
    /// </summary>
    public async Task<ImmutableList<HistoricalTrade>> GetTrades(string symbol, int limit = 500, CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrWhiteSpace(symbol))
        throw new ArgumentException(nameof(symbol));

      if (limit < 1 || limit > 1000)
        throw new ArgumentException(nameof(limit));

      using var linked = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
      using var wait = await _simultaneousRequests.LockAsync(linked.Token);
      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/trades?symbol={symbol}&limit={limit}");
      using var response = await _client.SendAsync(request, linked.Token);
      return await ParseResponse<ImmutableList<HistoricalTrade>>(response, linked.Token);
    }

    /// <summary>
    /// Get the last trade price for the given <paramref name="symbol"/>.
    /// </summary>
    public async Task<LastPrice> GetLastPrice(string symbol, CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrWhiteSpace(symbol))
        throw new ArgumentException(nameof(symbol));

      using var linked = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
      using var wait = await _simultaneousRequests.LockAsync(linked.Token);
      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/ticker/price?symbol={symbol}");
      using var response = await _client.SendAsync(request, linked.Token);
      return await ParseResponse<LastPrice>(response, linked.Token);
    }

    /// <summary>
    /// Get the last traded price for all instruments on the exchange.
    /// </summary>
    public async Task<ImmutableList<LastPrice>> GetLastPrices(CancellationToken cancellationToken = default)
    {
      using var linked = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
      using var wait = await _simultaneousRequests.LockAsync(linked.Token);
      using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/ticker/price");
      using var response = await _client.SendAsync(request, linked.Token);
      return await ParseResponse<ImmutableList<LastPrice>>(response, linked.Token);
    }

    /// <summary>
    /// Get compressed, aggregate trades. Trades that fill at the time, from the
    /// same taker order, with the same price will have the quantity aggregated.
    /// </summary>
    public async Task<ImmutableList<AggregateTrade>> GetAggregateTrades(string symbol, TimeStamp from, TimeStamp until, CancellationToken cancellationToken = default)
    {
      symbol.EnsureNotNullOrWhiteSpace(nameof(symbol));
      from.EnsureIs(nameof(from), "must be an exact millisecond.", f => f == f.ToMillisecondFloor());
      until.EnsureIs(nameof(until), "must be an exact millisecond.", f => f == f.ToMillisecondFloor());
      until.Subtract(from).EnsureIs("time difference", "must be less than or equal to one hour.", t => t.TotalHours <= 1);
      var query = new Dictionary<string, string>
      {
        { "symbol", symbol },
        { "startTime", (from.ToUnixMillieconds() + 1).ToString() },
        { "endTime", until.ToUnixMillieconds().ToString() },
      };
      var url = QueryHelpers.AddQueryString("api/v3/aggTrades", query);
      using var linked = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
      using var wait = await _simultaneousRequests.LockAsync(linked.Token);
      using var request = new HttpRequestMessage(HttpMethod.Get, url);
      using var response = await _client.SendAsync(request, linked.Token);
      return await ParseResponse<ImmutableList<AggregateTrade>>(response, linked.Token);
    }
  }
}
