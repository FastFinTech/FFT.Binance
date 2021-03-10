// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.Serialization
{
  using System.Text.Json;
  using System.Text.Json.Serialization;

  /// <summary>
  /// Contains a reference to <see cref="JsonSerializerOptions"/> specifically
  /// matched to the Binance api.
  /// </summary>
  internal class SerializationOptions
  {
    static SerializationOptions()
    {
      Instance = new JsonSerializerOptions(JsonSerializerDefaults.Web);
      Instance.Converters.Add(new TimeStampConverter());
      Instance.Converters.Add(new LadderConverter());
      Instance.NumberHandling = JsonNumberHandling.AllowReadingFromString;
      Instance.PropertyNameCaseInsensitive = false;
    }

    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> specifically matched to the
    /// Binance api.
    /// </summary>
    public static JsonSerializerOptions Instance { get; }
  }
}
