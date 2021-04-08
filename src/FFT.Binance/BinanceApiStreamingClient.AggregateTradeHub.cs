// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Text;
  using System.Text.Json;
  using FFT.Binance.Serialization;

  public sealed partial class BinanceApiStreamingClient
  {
    private sealed class AggregateTradeHub : BroadcastHubBase
    {
      protected override object Convert(ReadOnlyMemory<byte> message)
      {
        var json = Encoding.UTF8.GetString(message.Span);
        try
        {
          return JsonSerializer.Deserialize<MessageEnvelope<AggregateTrade>>(message.Span, SerializationOptions.Instance).Data!;
        }
        catch (Exception x)
        {
          throw;
        }
      }
    }
  }
}
