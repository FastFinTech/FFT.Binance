// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.Serialization
{
  using System;
  using System.Buffers;
  using System.Globalization;
  using System.Text.Json;
  using System.Text.Json.Serialization;
  using LadderMemoryOwner = System.Buffers.IMemoryOwner<(decimal Price, decimal Qty)>;

  internal class LadderConverter : JsonConverter<LadderMemoryOwner>
  {
    public override LadderMemoryOwner? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      var memoryOwner = MemoryPool<(decimal Price, decimal Qty)>.Shared.Rent(1024);
      var span = memoryOwner.Memory.Span;
      try
      {
        var index = 0;
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
        reader.Read();

        while (reader.TokenType == JsonTokenType.StartArray)
        {
          reader.Read();

          // Since a ladder max depth is 1000, and we have rented 1024 spaces,
          // this occurence should be VERY rare. It could happen when an update
          // contains a lot of deletions as well as updates, with the total
          // going past 1024.
          if (index >= span.Length)
          {
            var newMemoryOwner = MemoryPool<(decimal Price, decimal Qty)>.Shared.Rent(span.Length * 2);
            var newSpan = newMemoryOwner.Memory.Span;
            span.CopyTo(newSpan);
            span = newSpan;
            memoryOwner.Dispose();
            memoryOwner = newMemoryOwner;
          }

          if (reader.TokenType != JsonTokenType.String) throw new JsonException();
          span[index].Price = decimal.Parse(reader.GetString()!, NumberStyles.Any, CultureInfo.InvariantCulture);
          reader.Read();

          if (reader.TokenType != JsonTokenType.String) throw new JsonException();
          span[index].Qty = decimal.Parse(reader.GetString()!, NumberStyles.Any, CultureInfo.InvariantCulture);
          reader.Read();

          if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException();
          reader.Read();

          index++;
        }

        if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException();
      }
      catch (Exception)
      {
        memoryOwner.Dispose();
        throw;
      }

      return memoryOwner;
    }

    public override void Write(Utf8JsonWriter writer, LadderMemoryOwner value, JsonSerializerOptions options)
      => throw new NotImplementedException();
  }
}
