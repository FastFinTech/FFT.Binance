// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using System;
  using FFT.Market.Instruments;
  using FFT.Market.Providers;

  internal sealed class LiveProviderStore : ProviderStore<IInstrument, LiveProvider>
  {
    private readonly Func<BinanceApiClient> _getClient;
    private readonly Func<BinanceApiStreamingClient> _getStreamingClient;

    internal LiveProviderStore(Func<BinanceApiClient> getClient, Func<BinanceApiStreamingClient> getStreamingClient)
    {
      _getClient = getClient;
      _getStreamingClient = getStreamingClient;
    }

    protected override LiveProvider Create(IInstrument instrument)
      => new LiveProvider(instrument, _getClient, _getStreamingClient);
  }
}
