// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System.Threading.Channels;
  using System.Threading.Tasks;
  using FFT.Disposables;

  public partial class BinanceApiClient
  {
    private sealed class Subscription : AsyncDisposeBase, ISubscription
    {
      private readonly BinanceApiClient _connection;
      private readonly Channel<object> _channel = Channel.CreateBounded<object>(100);

      public Subscription(BinanceApiClient connection, StreamInfo streamInfo)
      {
        _connection = connection;
        StreamInfo = streamInfo;
      }

      public StreamInfo StreamInfo { get; }

      public ChannelReader<object> Reader => _channel.Reader;

      public void Handle(object message)
      {
        if (!_channel.Writer.TryWrite(message))
        {
          // If execution reaches here, the channel queue is full because the
          // user code is not consuming events. Rather than build up a useless
          // queue of events, we dispose ourselves to signal completion to the
          // user code have the subscription removed.
          DisposeAsync();
        }
      }

      protected override ValueTask CustomDisposeAsync()
      {
        _channel.Writer.TryComplete(DisposalReason);
        _connection.Remove(this);
        return default;
      }
    }
  }
}
