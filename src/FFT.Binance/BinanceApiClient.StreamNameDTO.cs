// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  public sealed partial class BinanceApiClient
  {
    /// <summary>
    /// Used to extract the stream name from incoming messages.
    /// </summary>
    private struct StreamNameDTO
    {
      public string? Stream { get; set; }
    }
  }
}
