// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using FFT.Market.Instruments;
  using FFT.Market.Providers;

  internal sealed class LiveProviderStore : ProviderStore<IInstrument, LiveProvider>
  {
    internal static readonly LiveProviderStore Instance = new();

    private LiveProviderStore()
    {
    }

    protected override LiveProvider Create(IInstrument instrument)
      => new LiveProvider(instrument);
  }
}
