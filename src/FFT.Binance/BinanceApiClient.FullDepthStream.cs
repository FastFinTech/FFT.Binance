// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Text.Json;
  using System.Threading.Tasks;
  using FFT.Binance.Serialization;

  public sealed partial class BinanceApiClient
  {
    private sealed class FullDepthStream : Stream
    {
      private Book _book = null!;

      public FullDepthStream(StreamInfo streamInfo)
            : base(streamInfo)
      {
      }

      public override async ValueTask Initiate(BinanceApiClient connection)
      {
        var symbol = StreamInfo.Name.Substring(0, StreamInfo.Name.IndexOf('@'));
        var orderBookResponse = await connection.GetOrderBook(symbol, 1000);
        _book = Book.From(orderBookResponse);
      }

      public override ValueTask Handle(ReadOnlyMemory<byte> data)
      {
        var diff = JsonSerializer.Deserialize<MessageEnvelope<BookUpdate>>(data.Span, SerializationOptions.Instance).Data!;
        if (diff.UpdateIdTo > _book.LastUpdateId)
        {
          _book = _book.ApplyDiff(ref diff);
          foreach (var subscriber in _subscriptions)
            subscriber.Handle(_book);
        }

        return default;
      }
    }
  }
}
