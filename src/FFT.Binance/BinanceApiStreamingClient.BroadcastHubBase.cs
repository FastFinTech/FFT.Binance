// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Buffers;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using FFT.Subscriptions;

  public sealed partial class BinanceApiStreamingClient
  {
    private abstract class BroadcastHubBase : IBroadcastHub
    {
      protected readonly List<IWritable> _subscribers = new();

      public int SubscriberCount => _subscribers.Count;

      public void AddSubscriber(IWritable subscriber)
        => _subscribers.Add(subscriber);

      public void RemoveSubscriber(IWritable subscriber)
        => _subscribers.Remove(subscriber);

      public void Complete()
      {
        foreach (var subscriber in _subscribers)
          subscriber.Complete();
      }

      public virtual void Handle(object message)
      {
        message = Convert((ReadOnlyMemory<byte>)message);
        foreach (var subscriber in _subscribers)
          subscriber.Write(message);
      }

      public virtual ValueTask Initialize(BinanceApiStreamingClient client, string streamId) => default;

      protected abstract object Convert(ReadOnlyMemory<byte> message);
    }
  }
}
