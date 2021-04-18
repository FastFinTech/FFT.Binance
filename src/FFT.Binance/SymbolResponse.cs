// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System.Collections.Immutable;

  public sealed record SymbolResponse
  {
    public string Symbol { get; init; }
    public string Status { get; init; }
    public string BaseAsset { get; init; }
    public int BaseAssetPrecision { get; init; }
    public string QuoteAsset { get; init; }
    public int QuotePrecision { get; init; }
    public int QuoteAssetPrecision { get; init; }
    public ImmutableList<string> OrderTypes { get; init; }
    public bool IcebergAllowed { get; init; }
    public bool OcoAllowed { get; init; }
    public bool IsSpotTradingAllowed { get; init; }
    public bool IsMarginTradingAllowed { get; init; }
    public ImmutableList<object> Filters { get; init; }
    public ImmutableList<string> Permissions { get; init; }
  }
}
