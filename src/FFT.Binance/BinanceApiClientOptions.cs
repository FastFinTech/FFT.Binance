﻿// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;

  /// <summary>
  /// Configures the <see cref="BinanceApiClient"/>.
  /// </summary>
  public sealed record BinanceApiClientOptions
  {
    /// <summary>
    /// Used for authenticating requests that require security of <see
    /// cref="EndpointSecurityType.USER_STREAM"/> or <see
    /// cref="EndpointSecurityType.MARKET_DATA"/>.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Used in addition to <see cref="ApiKey"/> for signing requests that
    /// require security of <see cref="EndpointSecurityType.TRADE"/> or <see
    /// cref="EndpointSecurityType.USER_DATA"/>.
    /// </summary>
    public string? SecretKey { get; init; }

    /// <summary>
    /// This value sets the maximum number of simultaneous requests that may be
    /// made to the rest api. Default value is 2.
    /// </summary>
    public int MaxSimultaneousRequests { get; init; } = 2;

    /// <summary>
    /// Sets the maximum amount of time that the internal HttpClient will wait
    /// for a complete response before throwing a timeout exception. Default
    /// value is 100 seconds.
    /// </summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(100);
  }
}
