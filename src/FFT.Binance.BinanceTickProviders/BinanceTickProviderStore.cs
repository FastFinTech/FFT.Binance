// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using FFT.FileManagement;
  using FFT.Market.Providers;
  using FFT.Market.Providers.Ticks;

  public sealed class BinanceTickProviderStore : ProviderStore<TickProviderInfo, ITickProvider>
  {
    private readonly HourProviderStore _dayProviderStore;

    public BinanceTickProviderStore(IFileManager fileManager)
    {
      _dayProviderStore = new HourProviderStore(fileManager);
    }

    protected override ITickProvider Create(TickProviderInfo info)
      => new BinanceTickProvider(info, _dayProviderStore);
  }
}
