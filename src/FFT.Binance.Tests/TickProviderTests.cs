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
      BaseAsset = Asset.BitCoin,
      QuoteAsset = new Asset(AssetType.Currency, "USD"),
      Exchange = new Exchange { ShortName = "Binance", LongName = "Binance" },
      TickSize = 0.00000001,
      SettlementTime = new SettlementTime { TimeZone = TimeZones.America_New_York, TimeOfDay = TimeSpan.FromHours(16) },
    };

    [TestMethod]
    public async Task HourProvider()
    {
      var fileManager = FileManager.Create("data");
      var store = new HourProviderStore(fileManager);
      var from = TimeStamp.Now.AddHours(-10).ToHourFloor();
      var until = from.AddHours(1);
      using var hourProvider = store.GetCreate(new TickProviderInfo
      {
        From = from,
        Until = until,
        Instrument = _bitcoin,
      });
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
      var liveProvider = LiveProviderStore.Instance.GetCreate(_bitcoin);
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
      var fileManager = FileManager.Create("data");
      var store = new BinanceTickProviderStore(fileManager);
      using var provider = store.GetCreate(new TickProviderInfo
      {
        From = TimeStamp.Now.AddDays(-1),
        Until = null,
        Instrument = _bitcoin,
      });
      await provider.WaitForReadyAsync(timeout.Token);
      var reader = provider.CreateReader();
      var count = 0;
      while (reader.ReadNext() is not null)
        count++;
      Assert.IsTrue(count > 1000);
    }
  }

  internal sealed record Instrument : IInstrument
  {
    public string Name { get; init; }
    public Asset BaseAsset { get; init; }
    public Asset QuoteAsset { get; init; }
    public Exchange Exchange { get; init; }
    public SettlementTime SettlementTime { get; init; }
    public double TickSize { get; init; }

    public bool IsTradingDay(DateStamp date) => true;

    public DateStamp ThisOrNextTradingDay(DateStamp date) => date;

    public DateStamp ThisOrPreviousTradingDay(DateStamp date) => date;
  }
}
