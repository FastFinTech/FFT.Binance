// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  public sealed partial class BinanceApiStreamingClient
  {
    /// <summary>
    /// Used to extract the stream name from incoming messages. Keep the member
    /// names unchanged (or use serialization property name attributes) so you
    /// don't screw up deserialization.
    /// </summary>
    private struct StreamNameDTO
    {
      public string? Stream { get; set; }
    }
  }
}
