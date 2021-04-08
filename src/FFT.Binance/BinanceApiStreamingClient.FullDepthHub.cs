// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Text.Json;
  using System.Threading.Tasks;
  using FFT.Binance.Serialization;

  public sealed partial class BinanceApiStreamingClient
  {
    private sealed class FullDepthHub : BroadcastHubBase
    {
      private Book _book = null!;

      public override async ValueTask Initialize(BinanceApiStreamingClient client, string streamId)
      {
        var symbol = streamId.Substring(0, streamId.IndexOf('@'));
        var orderBook = await client.ApiClient!.GetOrderBook(symbol, 1000);
        _book = Book.From(orderBook);
      }

      public override void Handle(object message)
      {
        var diff = JsonSerializer.Deserialize<MessageEnvelope<BookUpdate>>(((ReadOnlyMemory<byte>)message).Span, SerializationOptions.Instance).Data!;
        if (diff.UpdateIdTo > _book.LastUpdateId)
        {
          _book = _book.ApplyDiff(ref diff);
          foreach (var subscriber in Subscribers)
            subscriber.Write(_book);
        }
      }

      protected override object Convert(ReadOnlyMemory<byte> message)
        => throw new NotImplementedException();
    }
  }
}
