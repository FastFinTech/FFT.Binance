// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  /// <summary>
  /// Configures the <see cref="BinanceApiClient"/>.
  /// </summary>
  public sealed record BinanceApiClientOptions
  {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public string ApiKey { get; init; }

    /// <summary>
    /// Used for signing requests that require security of <see
    /// cref="EndpointSecurityType.TRADE"/> or <see
    /// cref="EndpointSecurityType.USER_DATA"/>.
    /// </summary>
    public string SecretKey { get; init; }
  }
}
