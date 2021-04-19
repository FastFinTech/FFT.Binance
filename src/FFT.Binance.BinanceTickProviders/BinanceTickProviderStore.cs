// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using System.IO;
  using System.Threading;
  using System.Threading.Tasks;
  using FFT.FileManagement;
  using FFT.Market.Providers;
  using FFT.Market.Providers.Ticks;
  using Nito.AsyncEx;

  /// <summary>
  /// Implements the <see cref="ITickProviderFactory"/> interface for Binance data.
  /// </summary>
  public sealed class BinanceTickProviderStore : ProviderStore<TickProviderInfo, ITickProvider>, ITickProviderFactory
  {
    private readonly IFileManager _fileManager;
    private readonly HourProviderStore _hourProviderStore;
    private readonly LiveProviderStore _liveProviderStore;
    private readonly BinanceApiClientOptions _clientOptions;

    private BinanceApiClient? _client;
    private BinanceApiStreamingClient? _streamingClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinanceTickProviderStore"/> class.
    /// </summary>
    /// <param name="dataLocation">The root directory to find and save all historical binance tick data.</param>
    /// <param name="clientOptions">The options to use when accessing the rest api.</param>
    public BinanceTickProviderStore(DirectoryInfo dataLocation, BinanceApiClientOptions? clientOptions = null)
    {
      _fileManager = FileManager.Create(dataLocation.FullName);
      _hourProviderStore = new HourProviderStore(_fileManager, GetApiClient);
      _liveProviderStore = new LiveProviderStore(GetApiClient, GetStreamingClient);
      _clientOptions = clientOptions ?? new();
    }

    /// <inheritdoc/>
    public ITickProvider GetTickProvider(TickProviderInfo info)
      => GetCreate(info);

    // Made internal for test visibility

    internal BinanceApiClient GetApiClient()
    {
      var client = Interlocked.CompareExchange(ref _client, null, null);
      if (client is not null) return client;
      client = new BinanceApiClient(_clientOptions);
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

    internal BinanceApiStreamingClient GetStreamingClient()
    {
      var result = Interlocked.CompareExchange(ref _streamingClient, null, null);
      if (result is not null) return result;
      result = new BinanceApiStreamingClient { ApiClient = GetApiClient() };
      var exchangeResult = Interlocked.CompareExchange(ref _streamingClient, result, null);
      if (exchangeResult is null)
      {
        result.DisposedTask.ContinueWith(
          t =>
          {
            Interlocked.Exchange(ref _streamingClient, null!);
          },
          TaskScheduler.Default).Ignore();
        return result;
      }
      else
      {
        // Thread race, we lost.
        _ = result.DisposeAsync();
        return exchangeResult;
      }
    }

    // Supplied entirely for test visibility

    internal HourProviderStore GetHourProviderStore() => _hourProviderStore;

    internal LiveProviderStore GetLiveProviderStore() => _liveProviderStore;

    /// <inheritdoc/>
    protected override ITickProvider Create(TickProviderInfo info)
      => new BinanceTickProvider(info, _hourProviderStore, _liveProviderStore);
  }
}
