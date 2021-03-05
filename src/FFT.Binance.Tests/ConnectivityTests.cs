// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.Tests
{
  using System;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using static System.Math;
  using static System.MidpointRounding;
  using static FFT.Binance.Tests.Services;

  [TestClass]
  public class ConnectivityTests
  {
    [TestMethod]
    public async Task Connectivity()
    {
      await Client.TestConnectivity();
      var time = await Client.GetServerTime();
      var orderBooks = await Client.GetTopOrderBooks();
      var btcOrderBooks = orderBooks.Where(b => b.Symbol.StartsWith("BTC")).ToList();
      var btcUSDOrderBook = orderBooks.Single(b => b.Symbol == "BTCUSDT");
      var btcBidDollars = Round(btcUSDOrderBook.BidQty * btcUSDOrderBook.BidPrice, 2, AwayFromZero);
      var btcAskDollars = Round(btcUSDOrderBook.AskQty * btcUSDOrderBook.AskPrice, 2, AwayFromZero);
    }

    [TestMethod]
    public async Task DepthStream()
    {
      var count = 0;
      using var cts = new CancellationTokenSource(600000);
      await foreach (var orderBook in Client.GetDepthStream("BTCUSDT", false, cts.Token))
      {
        if (++count == 200000)
          return;
      }
    }
  }
}
