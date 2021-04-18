// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System.Collections.Immutable;
  using FFT.TimeStamps;

  public sealed record ExchangeInfoResponse
  {
    public string Timezone { get; init; }
    public TimeStamp ServerTime { get; init; }
    public ImmutableList<object> RateLimits { get; init; }
    public ImmutableList<object> ExchangeFilters { get; init; }
    public ImmutableList<SymbolResponse> Symbols { get; init; }
  }
}
