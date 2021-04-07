// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using System.Collections.Immutable;
  using System.Threading;
  using System.Threading.Tasks;
  using FFT.TimeStamps;
  using Nito.AsyncEx;

  internal static class ClientProvider
  {
    private static BinanceApiClient? _client;

    private static BinanceApiStreamingClient? _streamingClient;

    public static SemaphoreSlim Semaphore { get; } = new(2, 2);

    public static BinanceApiClient GetClient()
    {
      var client = Interlocked.CompareExchange(ref _client, null, null);
      if (client is not null) return client;
      client = new BinanceApiClient(null);
      var swapResult = Interlocked.CompareExchange(ref _client, client, null);
      if (swapResult is null)
      {
        client.DisposedTask.ContinueWith(
          t =>
          {
            Interlocked.CompareExchange(ref _client, null!, client);
          },
          TaskScheduler.Default).Ignore();
        return client;
      }
      else
      {
        // Thread race. We lost.
        client.Dispose();
        return swapResult;
      }
    }

    public static BinanceApiStreamingClient GetStreamingClient()
    {
      var result = Interlocked.CompareExchange(ref _streamingClient, null, null);
      if (result is not null) return result;
      result = new BinanceApiStreamingClient { ApiClient = GetClient() };
      var exchangeResult = Interlocked.CompareExchange(ref _streamingClient, result, null);
      if (exchangeResult is not null)
      {
        // Thread race, we lost.
        _ = result.DisposeAsync();
        return exchangeResult;
      }
      else
      {
        result.DisposedTask.ContinueWith(
          t =>
          {
            Interlocked.Exchange(ref _streamingClient, null!);
          },
          TaskScheduler.Default).Ignore();
        return result;
      }
    }
  }
}
