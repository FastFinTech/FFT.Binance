// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.Tests
{
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

  using FFT.Market.Instruments;
  using FFT.TimeStamps;

  internal sealed record Instrument : IInstrument
  {
    public string Name { get; init; }
    public Asset BaseAsset { get; init; }
    public Asset QuoteAsset { get; init; }
    public Exchange Exchange { get; init; }
    public SettlementTime SettlementTime { get; init; }
    public double MinPriceIncrement { get; init; }
    public double MinQtyIncrement { get; init; }

    public bool IsTradingDay(DateStamp date) => true;

    public DateStamp ThisOrNextTradingDay(DateStamp date) => date;

    public DateStamp ThisOrPreviousTradingDay(DateStamp date) => date;
  }
}
