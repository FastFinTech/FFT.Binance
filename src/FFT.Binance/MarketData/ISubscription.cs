// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.MarketDataStreams
{
  using System;
  using System.Threading.Channels;

  /// <summary>
  /// Represents the subscription to a particular stream.
  /// Dispose this subscription to end it.
  /// </summary>
  public interface ISubscription : IAsyncDisposable
  {
    /// <summary>
    /// Read this reader to get messages from the stream. This reader will be
    /// completed after you dispose the subscription or if the underlying
    /// connection had an issue. If the completion is due to an underlying
    /// connection issue, you will need to request a new subscription if you
    /// want to keep receiving data.
    /// </summary>
    ChannelReader<object> Reader { get; }
  }
}
