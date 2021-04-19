// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using System;
  using FFT.FileManagement;
  using FFT.Market.Providers;
  using FFT.Market.Providers.Ticks;

  internal sealed class HourProviderStore : ProviderStore<TickProviderInfo, HourProvider>
  {
    private readonly IFileManager _fileManager;
    private readonly Func<BinanceApiClient> _getClient;

    internal HourProviderStore(IFileManager fileManager, Func<BinanceApiClient> getClient)
    {
      _fileManager = fileManager;
      _getClient = getClient;
    }

    protected override HourProvider Create(TickProviderInfo info)
      => new HourProvider(info, _fileManager, _getClient);
  }
}
