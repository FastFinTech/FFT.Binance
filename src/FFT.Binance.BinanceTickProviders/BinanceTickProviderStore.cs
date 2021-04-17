// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using System.IO;
  using FFT.FileManagement;
  using FFT.Market.Providers;
  using FFT.Market.Providers.Ticks;

  /// <summary>
  /// Implements the <see cref="ITickProviderFactory"/> interface for Binance data.
  /// </summary>
  public sealed class BinanceTickProviderStore : ProviderStore<TickProviderInfo, ITickProvider>, ITickProviderFactory
  {
    private readonly IFileManager _fileManager;
    private readonly HourProviderStore _hourProviderStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinanceTickProviderStore"/> class.
    /// </summary>
    /// <param name="dataLocation">The root directory to find and save all historical binance tick data.</param>
    public BinanceTickProviderStore(DirectoryInfo dataLocation)
    {
      _fileManager = FileManager.Create(dataLocation.FullName);
      _hourProviderStore = new HourProviderStore(_fileManager);
    }

    /// <inheritdoc/>
    public ITickProvider GetTickProvider(TickProviderInfo info)
      => GetCreate(info);

    /// <inheritdoc/>
    protected override ITickProvider Create(TickProviderInfo info)
      => new BinanceTickProvider(info, _hourProviderStore);
  }
}
