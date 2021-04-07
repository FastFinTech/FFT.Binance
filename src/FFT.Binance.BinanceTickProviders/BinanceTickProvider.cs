// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using System;
  using System.Buffers;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.Collections.Immutable;
  using System.Linq;
  using System.Runtime.ExceptionServices;
  using System.Threading;
  using System.Threading.Tasks;
  using FFT.Disposables;
  using FFT.FileManagement;
  using FFT.Market;
  using FFT.Market.Instruments;
  using FFT.Market.Providers;
  using FFT.Market.Providers.Ticks;
  using FFT.Market.Services;
  using FFT.Market.Sessions.TradingHoursSessions;
  using FFT.Market.TickStreams;
  using FFT.TimeStamps;
  using Nerdbank.Streams;
  using Nito.AsyncEx;

  internal sealed class BinanceTickProvider : ProviderBase, ITickProvider
  {
    private readonly List<ITickProvider> _providers = new();

    internal BinanceTickProvider(TickProviderInfo info, HourProviderStore hourProviderStore)
    {
      var now = TimeStamp.Now;
      var currentDay = now.GetDate();

      if (info.From > now) throw new ArgumentException("from is in the future.");
      if (info.Until < info.From) throw new ArgumentException("from is greater than until.");

      var liveStartTime = currentDay.GetStartTime();
      if (info.From < liveStartTime)
      {
        var from = info.From.ToHourFloor();
        while (from < liveStartTime)
        {
          _providers.Add(hourProviderStore.GetCreate(new TickProviderInfo
          {
            Instrument = info.Instrument,
            From = from,
            Until = from.AddHours(1),
          }));
          from = from.AddHours(1);
        }
      }

      if (info.Until is null || info.Until > liveStartTime)
      {
        _providers.Add(LiveProviderStore.Instance.GetCreate(info.Instrument));
      }

      Info = info;
      Name = $"Binance Tick Provider for {info.Instrument.Name} from {info.From.GetDate()}";
      if (info.Until is not null)
        Name += $" until {info.Until.Value.GetDate()}";
    }

    public TickProviderInfo Info { get; }

    public override IEnumerable<object> GetDependencies()
    {
      yield return Info.Instrument;
    }

    public override void Start()
    {
      Task.Run(async () =>
      {
        try
        {
          await _providers.WaitForReadyAsync(DisposedToken);
          OnReady();
          await _providers.WaitForErrorAsync(DisposedToken);
        }
        catch (Exception x)
        {
          var message = $"{nameof(BinanceTickProvider)} '{Name}' error.";
          Dispose(new Exception(message, x));
        }
      }).Ignore();
    }

    public ITickStreamReader CreateReader()
    {
      // TODO: Does not end the reader at Info.Until
      var reader = new ConcatenatedTickStreamReader(_providers.Select(p => p.CreateReader()).ToArray());
      reader.ReadUntil(Info.From).Count(); // Don't forget to actually execute that enumerable.
      return reader;
    }

    public override ProviderStatus GetStatus()
    {
      throw new NotImplementedException();
    }
  }
}
