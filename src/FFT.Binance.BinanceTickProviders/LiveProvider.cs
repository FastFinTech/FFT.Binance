// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using FFT.Market.Instruments;
  using FFT.Market.Providers;
  using FFT.Market.Providers.Ticks;
  using FFT.Market.Sessions.TradingHoursSessions;
  using FFT.Market.TickStreams;
  using FFT.TimeStamps;
  using Nito.AsyncEx;

  internal sealed class LiveProvider : ProviderBase, ITickProvider
  {
    private readonly ShortTickStream _tickStream;
    private readonly Func<BinanceApiClient> _getClient;
    private readonly Func<BinanceApiStreamingClient> _getStreamingClient;

    internal LiveProvider(IInstrument instrument, Func<BinanceApiClient> getClient, Func<BinanceApiStreamingClient> getStreamingClient)
    {
      _tickStream = new ShortTickStream(instrument);
      _getClient = getClient;
      _getStreamingClient = getStreamingClient;
      Info = new TickProviderInfo()
      {
        From = TimeStamp.Now.ToHourFloor(),
        Until = null,
        Instrument = instrument,
      };
    }

    public TickProviderInfo Info { get; }

    public long FirstTickId { get; private set; }

    public override IEnumerable<object> GetDependencies()
    {
      yield break;
    }

    public override ProviderStatus GetStatus()
    {
      throw new NotImplementedException();
    }

    public ITickStreamReader CreateReader()
      => _tickStream.CreateReader();

    public override void Start()
    {
      Task.Run(async () =>
      {
        try
        {
          var tickSizeAsDecimal = (decimal)Info.Instrument.MinPriceIncrement;
          using var subscription = await _getStreamingClient().Subscribe(StreamInfo.AggregatedTrade(Info.Instrument.Name));
          var firstLiveTrade = (AggregateTrade)await subscription.Reader.ReadAsync(DisposedToken);

          var historicalTrades = await _getClient().GetAggregateTrades(Info.Instrument.Name, Info.From, firstLiveTrade.Timestamp);
          if (historicalTrades.Count > 0)
          {
            FirstTickId = historicalTrades[0].AggregateTradeId;
          }

          foreach (var trade in historicalTrades)
          {
            if (trade.AggregateTradeId >= firstLiveTrade.AggregateTradeId)
              break;
            _tickStream.WriteTick(trade.AsTick(tickSizeAsDecimal));
          }

          _tickStream.WriteTick(firstLiveTrade.AsTick(tickSizeAsDecimal));
          while (subscription.Reader.TryRead(out var trade))
          {
            _tickStream.WriteTick((trade as AggregateTrade)!.AsTick(tickSizeAsDecimal));
          }

          OnReady();
          while (true)
          {
            var trade = (AggregateTrade)await subscription.Reader.ReadAsync(DisposedToken);
            _tickStream.WriteTick(trade.AsTick(tickSizeAsDecimal));
          }
        }
        catch (Exception x)
        {
          var message = $"Error in {nameof(LiveProvider)} '{Name}'.";
          Dispose(new Exception(message, x));
        }
      }).Ignore();
    }
  }
}
