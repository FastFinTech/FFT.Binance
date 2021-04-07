// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using FFT.FileManagement;
  using FFT.Market.Providers;
  using FFT.Market.Providers.Ticks;

  internal sealed class HourProviderStore : ProviderStore<TickProviderInfo, HourProvider>
  {
    private readonly IFileManager _fileManager;

    public HourProviderStore(IFileManager fileManager)
    {
      _fileManager = fileManager;
    }

    protected override HourProvider Create(TickProviderInfo info)
      => new HourProvider(info, _fileManager);
  }
}
