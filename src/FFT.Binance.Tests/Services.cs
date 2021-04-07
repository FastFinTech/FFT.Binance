// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.Tests
{
  using System;

  internal static class Services
  {
    static Services()
    {
      BinanceApiClientOptions = new BinanceApiClientOptions
      {
        ApiKey = Environment.GetEnvironmentVariable("Binance_ApiKey")!,
        SecretKey = Environment.GetEnvironmentVariable("Binance_ApiSecret")!,
      };

      Client = new BinanceApiClient(BinanceApiClientOptions);
      StreamingClient = new BinanceApiStreamingClient { ApiClient = Client };
    }

    public static BinanceApiClientOptions BinanceApiClientOptions { get; }

    public static BinanceApiClient Client { get; }

    public static BinanceApiStreamingClient StreamingClient { get; }
  }
}
