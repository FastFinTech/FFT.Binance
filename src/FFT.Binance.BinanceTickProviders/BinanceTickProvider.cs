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
  using Nito.Disposables;

  internal sealed class BinanceTickProvider : ProviderBase, ITickProvider
  {
    private readonly IDisposable _disposables;
    private readonly List<ITickProvider> _providers = new();

    internal BinanceTickProvider(TickProviderInfo info, HourProviderStore hourProviderStore)
    {
      Info = info;
      Name = $"Binance Tick Provider for {info.Instrument.Name} from {info.From.GetDate()}";
      if (info.Until.HasValue)
        Name += $" until {info.Until.Value.GetDate()}";

      var now = TimeStamp.Now;
      var startOfCurrentHour = now.ToHourFloor();

      if (info.Until > startOfCurrentHour)
        throw new ArgumentException("Must be null or less than the start of the current hour in UTC timezone.", "info.Until");

      if (info.From > now)
        throw new ArgumentException("from is in the future.");

      if (info.Until < info.From)
        throw new ArgumentException("from is greater than until.");

      var from = info.From.ToHourFloor();
      while (from < startOfCurrentHour && !(from >= info.Until))
      {
        var hourProvider = hourProviderStore.GetCreate(new TickProviderInfo
        {
          Instrument = info.Instrument,
          From = from,
          Until = from.AddHours(1),
        });
        _providers.Add(hourProvider);
        from = from.AddHours(1);
      }

      if (Info.Until is null)
      {
        var liveProvider = LiveProviderStore.Instance.GetCreate(info.Instrument);

        // Remove any hour providers that have data beyond the start of the live
        // provider. (This can happen when the liveProvider is more than an hour
        // old)
        while (_providers.Count > 0)
        {
          var hourProvider = _providers[^1];
          if (hourProvider.Info.Until <= liveProvider.Info.From)
            break;
          // Bump the usage token so it knows it can shutdown if nobody else is using it.
          hourProvider.GetUserCountToken().Dispose();
          _providers.RemoveAt(_providers.Count - 1);
        }

        _providers.Add(liveProvider);
      }

      // Reserve the internal providers.
      _disposables = CollectionDisposable.Create(_providers.Select(p => p.GetUserCountToken()));
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

          // Verify contiguity of tick ids from provider to provider.
          if (_providers.Count > 1)
          {
            for (var i = _providers.Count - 1; i > 0; i--)
            {
              var provider = _providers[i];
              var previousProvider = (HourProvider)_providers[i - 1];

              if (provider is LiveProvider liveProvider)
              {
                if (liveProvider.FirstTickId != previousProvider.LastTickId + 1)
                {
                  throw new Exception("Tick ids did not match for live provider.");
                }
              }
              else
              {
                if (((HourProvider)provider).FirstTickId != previousProvider.LastTickId + 1)
                {
                  throw new Exception("Tick ids did not match for hour provider.");
                }
              }
            }
          }

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

    protected override void OnDisposed()
    {
      // Release the internal providers.
      _disposables.Dispose();
    }
  }
}
