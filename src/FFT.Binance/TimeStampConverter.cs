// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Text.Json;
  using System.Text.Json.Serialization;
  using FFT.TimeStamps;

  internal sealed class TimeStampConverter : JsonConverter<TimeStamp>
  {
    public override TimeStamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      => TimeStamp.FromUnixMilliseconds(reader.GetInt64());

    public override void Write(Utf8JsonWriter writer, TimeStamp value, JsonSerializerOptions options)
      => writer.WriteNumberValue(value.ToUnixMillieconds());
  }
}
