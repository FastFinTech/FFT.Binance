// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  /// <summary>
  /// Defines the different kinds of requests that can be made of the api, with
  /// respect to the required security.
  /// </summary>
  public enum EndpointSecurityType
  {
    /// <summary>
    /// Endpoint can be accessed freely.
    /// </summary>
    NONE,

    /// <summary>
    /// Endpoint requires sending a valid API-Key and signature.
    /// </summary>
    TRADE,

    /// <summary>
    /// Endpoint requires sending a valid API-Key and signature.
    /// </summary>
    USER_DATA,

    /// <summary>
    /// Endpoint requires sending a valid API-Key.
    /// </summary>
    USER_STREAM,

    /// <summary>
    /// Endpoint requires sending a valid API-Key.
    /// </summary>
    MARKET_DATA,
  }
}
