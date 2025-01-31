﻿// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Text;

  internal static class Extensions
  {
    public static byte[] ToUtf8(this string value)
      => Encoding.UTF8.GetBytes(value);

    public static NotImplementedException UnknownValueException<T>(this T value)
      where T : struct, Enum
      => new NotImplementedException($"Unknown '{typeof(T)}' value '{value}'.");
  }
}
