// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.Tests
{
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

  using System;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using FFT.Binance.BinanceTickProviders;
  using FFT.FileManagement;
  using FFT.Market;
  using FFT.Market.Instruments;
  using FFT.Market.Providers.Ticks;
  using FFT.Market.TickStreams;
  using FFT.TimeStamps;
  using FFT.TimeZoneList;
  using Microsoft.VisualStudio.TestTools.UnitTesting;

  [TestClass]
  public sealed class TickProviderTests
  {
    private static readonly IInstrument _bitcoin = new Instrument
    {
      Name = "BTCUSDT",
      BaseAsset = KnownAssets.Crypto_Bitcoin,
      QuoteAsset = KnownAssets.Crypto_Tether,
      Exchange = KnownExchanges.Binance,
      MinPriceIncrement = 0.01,
      MinQtyIncrement = 0.00000001,
      SettlementTime = new SettlementTime { TimeZone = TimeZones.UTC, TimeOfDay = TimeSpan.Zero },
    };

    [TestMethod]
    public async Task HourProvider()
    {
      var fileManager = FileManager.Create("data");
      var store = Services.BinanceTickProviderStore.GetHourProviderStore();
      var from = TimeStamp.Now.AddHours(-10).ToHourFloor();
      var until = from.AddHours(1);
      var hourProvider = store.GetCreate(new TickProviderInfo
      {
        From = from,
        Until = until,
        Instrument = _bitcoin,
      });
      using var usageToken = hourProvider.GetUserCountToken();
      await hourProvider.WaitForReadyAsync(default);
      var reader = hourProvider.CreateReader();
      var count = 0;
      while (reader.ReadNext() is not null)
        count++;
      Assert.IsTrue(count > 1000);
    }

    [TestMethod]
    public async Task LiveProvider()
    {
      var cts = new CancellationTokenSource(300000);
      var liveProvider = Services.BinanceTickProviderStore.GetLiveProviderStore().GetCreate(_bitcoin);
      using var usageToken = liveProvider.GetUserCountToken();
      await liveProvider.WaitForReadyAsync(cts.Token);
      var reader = liveProvider.CreateReader();
      var initialCount = reader.ReadRemaining().Count();
      await Task.Delay(10000);
      var remainingCount = reader.ReadRemaining().Count();
      Assert.IsTrue(initialCount > 100);
      Assert.IsTrue(remainingCount > 1);
    }

    [TestMethod]
    public async Task TickProvider()
    {
      using var timeout = new CancellationTokenSource(3000000);
      using var provider = Services.BinanceTickProviderStore.GetCreate(new TickProviderInfo
      {
        From = TimeStamp.Now.AddDays(-1),
        Until = null,
        Instrument = _bitcoin,
      });
      using var usageToken = provider.GetUserCountToken();
      await provider.WaitForReadyAsync(timeout.Token);
      var reader = provider.CreateReader();
      var count = 0;
      while (reader.ReadNext() is not null)
        count++;
      Assert.IsTrue(count > 1000);
    }
  }
}
