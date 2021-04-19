// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.Tests
{
  using System;
  using FFT.Binance.BinanceTickProviders;

  internal static class Services
  {
    static Services()
    {
      BinanceTickProviderStore = new(new("D:/BinanceTickData"), new BinanceApiClientOptions
      {
        ApiKey = Environment.GetEnvironmentVariable("Binance_ApiKey")!,
        SecretKey = Environment.GetEnvironmentVariable("Binance_ApiSecret")!,
        MaxSimultaneousRequests = 2,
        RequestTimeout = TimeSpan.FromMinutes(10),
      });
    }

    public static BinanceTickProviderStore BinanceTickProviderStore { get; }
  }
}
