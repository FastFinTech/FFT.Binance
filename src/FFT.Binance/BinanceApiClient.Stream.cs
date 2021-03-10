// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;

  public sealed partial class BinanceApiClient
  {
    private abstract class Stream : IAsyncDisposable
    {
      protected readonly List<Subscription> _subscriptions = new();

      public Stream(StreamInfo streamInfo)
      {
        StreamInfo = streamInfo;
      }

      public int SubscriptionCount => _subscriptions.Count;

      public StreamInfo StreamInfo { get; }

      public virtual ValueTask Initiate(BinanceApiClient connection) => default;

      public ValueTask Add(Subscription subscription)
      {
        _subscriptions.Add(subscription);
        return OnAdded(subscription);
      }

      public ValueTask Remove(Subscription subscription)
      {
        _subscriptions.Remove(subscription);
        return OnRemoved(subscription);
      }

      public abstract ValueTask Handle(ReadOnlyMemory<byte> data);

      public async ValueTask DisposeAsync()
      {
        foreach (var subscriber in _subscriptions)
          await subscriber.DisposeAsync();
      }

      protected virtual ValueTask OnAdded(Subscription subscription) => default;

      protected virtual ValueTask OnRemoved(Subscription subscription) => default;
    }
  }
}
