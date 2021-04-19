// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using System;
  using System.Buffers;
  using System.Collections.Generic;
  using System.Collections.Immutable;
  using System.Threading.Tasks;
  using FFT.FileManagement;
  using FFT.Market.Providers;
  using FFT.Market.Providers.Ticks;
  using FFT.Market.Sessions.TradingHoursSessions;
  using FFT.Market.Ticks;
  using FFT.Market.TickStreams;
  using FFT.TimeStamps;
  using Nerdbank.Streams;
  using Nito.AsyncEx;

  internal sealed class HourProvider : ProviderBase, ITickProvider
  {
    private readonly IFileManager _fileManager;
    private readonly Func<BinanceApiClient> _getClient;

    private ShortTickStream? _tickStream;

    internal HourProvider(TickProviderInfo info, IFileManager fileManager, Func<BinanceApiClient> getClient)
    {
      if (info.From != info.From.ToHourFloor()) throw new ArgumentException("info.From");
      if (info.Until != info.From.AddHours(1)) throw new ArgumentException("info.Until");

      _fileManager = fileManager;
      _getClient = getClient;
      Info = info;
      Name = $"{nameof(HourProvider)} '{info.Instrument.Name}' from {info.From.ToString("yyyy-MM-dd HH:mm")}";
    }

    public TickProviderInfo Info { get; }

    public long FirstTickId { get; private set; }

    public long LastTickId { get; private set; }

    public ITickStreamReader CreateReader() => _tickStream!.CreateReader();

    public override void Start()
    {
      var startString = Info.From.ToString("yyyyMMddHHmm");
      var filename = $"BinanceHistorialTrades/{Info.Instrument.Name}/{startString}.ticks";
      var metaFilename = $"BinanceHistorialTrades/{Info.Instrument.Name}/{startString}.meta";
      var sequence = new Sequence<byte>(ArrayPool<byte>.Shared);

      Task.Run(async () =>
      {
        try
        {
          // ShortTickStream constructor requires the sequence be preloaded.
          if (await TryReadDataFromDisk())
          {
            _tickStream = new ShortTickStream(Info.Instrument, sequence);
          }
          else
          {
            _tickStream = new ShortTickStream(Info.Instrument, sequence);
            var isFirstTick = true;
            var tickSizeAsDecimal = (decimal)Info.Instrument.MinPriceIncrement;
            foreach (var trade in await _getClient().GetAggregateTrades(Info.Instrument.Name, Info.From, Info.Until!.Value))
            {
              if (isFirstTick)
              {
                FirstTickId = trade.AggregateTradeId;
                isFirstTick = false;
              }

              LastTickId = trade.AggregateTradeId;
              _tickStream.WriteTick(trade.AsTick(tickSizeAsDecimal));
            }

            if (isFirstTick)
              throw new Exception("There were no trades.");

            await WriteDataToDisk();
          }

          OnReady();
        }
        catch (Exception x)
        {
          var message = $"{nameof(HourProvider)} '{Name}' error.";
          Dispose(new Exception(message, x));
        }
      }).Ignore();

      async Task<bool> TryReadDataFromDisk()
      {
        try
        {
          var headerBytes = await _fileManager.ReadBytesAsync(metaFilename);
          if (headerBytes is { Length: 16 })
          {
            FirstTickId = BitConverter.ToInt64(headerBytes, 0);
            LastTickId = BitConverter.ToInt64(headerBytes, 8);
            if (FirstTickId < 0 || LastTickId < 0 || LastTickId < FirstTickId)
              throw new Exception("Unable to read sensible first and last tick ids from metadata file.");

            if (await _fileManager.ReadBytesAsync(filename, sequence) > 0)
            {
              return true;
            }
          }
        }
        catch
        {
        }

        return false;
      }

      async Task WriteDataToDisk()
      {
        var header = new byte[16];
        BitConverter.TryWriteBytes(header, FirstTickId);
        BitConverter.TryWriteBytes(header.AsSpan().Slice(8), LastTickId);
        await _fileManager.WriteBytesAsync(metaFilename, header);
        // TODO: Remove - just for checking
        var sequence = _tickStream!.AsReadOnlySequence();
        await _fileManager.WriteBytesAsync(filename, _tickStream!.AsReadOnlySequence());
      }
    }

    public override IEnumerable<object> GetDependencies()
    {
      yield return Info.Instrument;
    }

    public override ProviderStatus GetStatus()
    {
      throw new NotImplementedException();
    }

    protected override void OnDisposed()
    {
      _tickStream?.Dispose();
    }
  }
}
