// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System.Text.Json.Serialization;
  using FFT.TimeStamps;

  /// <summary>
  /// Returned by the <see cref="BinanceApiClient.GetServerTime"/> method.
  /// </summary>
  public sealed class ServerTimeResponse
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerTimeResponse"/> class.
    /// </summary>
    [JsonConstructor]
    public ServerTimeResponse(TimeStamp serverTime)
    {
      ServerTime = serverTime;
    }

    /// <summary>
    /// The current server time.
    /// </summary>
    public TimeStamp ServerTime { get; }
  }
}
