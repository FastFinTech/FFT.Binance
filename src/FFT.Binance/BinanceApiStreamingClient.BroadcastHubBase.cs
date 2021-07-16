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
      public int SubscriberCount => Subscribers.Count;

      protected List<IWritable> Subscribers { get; } = new();

      public void AddSubscriber(IWritable subscriber)
        => Subscribers.Add(subscriber);

      public void RemoveSubscriber(IWritable subscriber)
        => Subscribers.Remove(subscriber);

      public void Complete(Exception? error)
      {
        foreach (var subscriber in Subscribers)
          subscriber.Complete(error);
      }

      public virtual void Handle(object message)
      {
        message = Convert((ReadOnlyMemory<byte>)message);
        foreach (var subscriber in Subscribers)
          subscriber.Write(message);
      }

      public virtual ValueTask Initialize(BinanceApiStreamingClient client, string streamId) => default;

      protected abstract object Convert(ReadOnlyMemory<byte> message);
    }
  }
}
