// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Globalization;
  using System.Text.Json;
  using System.Text.Json.Serialization;

  /// <summary>
  /// A component of an order book.
  /// </summary>
  [JsonConverter(typeof(Converter))]
  public sealed class PriceQty
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="PriceQty"/> class.
    /// </summary>
    [JsonConstructor]
    public PriceQty(
      decimal price,
      decimal qty)
    {
      Price = price;
      Qty = qty;
    }

    /// <summary>
    /// The price of the order book entry.
    /// </summary>
    public decimal Price { get; }

    /// <summary>
    /// The qty of the order book entry.
    /// </summary>
    public decimal Qty { get; }

    private class Converter : JsonConverter<PriceQty>
    {
      public override PriceQty? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
        reader.Read();
        if (reader.TokenType != JsonTokenType.String) throw new JsonException();
        var price = decimal.Parse(reader.GetString()!, NumberStyles.Any, CultureInfo.InvariantCulture);
        reader.Read();
        if (reader.TokenType != JsonTokenType.String) throw new JsonException();
        var qty = decimal.Parse(reader.GetString()!, NumberStyles.Any, CultureInfo.InvariantCulture);
        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException();
        //reader.Read();
        return new PriceQty(price, qty);
      }

      public override void Write(Utf8JsonWriter writer, PriceQty value, JsonSerializerOptions options)
        => throw new NotImplementedException();
    }
  }
}
