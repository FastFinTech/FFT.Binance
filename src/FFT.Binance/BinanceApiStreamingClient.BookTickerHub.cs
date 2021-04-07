// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Text.Json;
  using FFT.Binance.Serialization;

  public sealed partial class BinanceApiStreamingClient
  {
    private sealed class BookTickerHub : BroadcastHubBase
    {
      protected override object Convert(ReadOnlyMemory<byte> message)
        => JsonSerializer.Deserialize<MessageEnvelope<BookTicker>>(message.Span, SerializationOptions.Instance).Data!;
    }
  }
}
