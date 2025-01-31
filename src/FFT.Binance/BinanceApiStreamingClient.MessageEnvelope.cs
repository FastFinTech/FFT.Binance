﻿// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  public sealed partial class BinanceApiStreamingClient
  {
    // Keep member names unchanged so you don't mess up serialization, or use
    // member serialization name attributes.
    private struct MessageEnvelope<T>
    {
      public string Stream { get; set; }

      public T Data { get; set; }
    }
  }
}
