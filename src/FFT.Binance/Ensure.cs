// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Runtime.CompilerServices;

  internal static class Ensure
  {
    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T EnsureNotNull<T>(this T value, string name)
    {
      if (value is null) throw new ArgumentNullException($"{name} is null.", name);
      return value;
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? EnsureNotNull<T>(this T? value, string name) where T : struct
    {
      if (value is null) throw new ArgumentNullException($"{name} is null.", name);
      return value;
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EnsureNotNullOrEmpty(this string value, string name)
    {
      if (string.IsNullOrEmpty(value)) throw new ArgumentNullException($"{name} is null or empty.", name);
      return value;
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EnsureNotNullOrWhiteSpace(this string value, string name)
    {
      if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException($"{name} is null or whitespace.", name);
      return value;
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T EnsureIs<T>(this T value, string name, string error, Func<T, bool> predicate)
    {
      if (!predicate(value)) throw new ArgumentException($"{name} {error}", name);
      return value;
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T EnsureHasValues<T>(this T values, string name) where T : IEnumerable<T>
    {
      values.EnsureNotNull(name);
      var hasValues = false;
      foreach (var value in values)
      {
        hasValues = true;
        break;
      }
      if (!hasValues)
        throw new ArgumentException($"{name} does not have any values.", name);
      return values;
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T>? EnsureNoNullValues<T>(this IEnumerable<T>? values, string name)
    {
      if (values is not null)
      {
        foreach (var value in values)
          if (value is null)
            throw new ArgumentException($"{name} contains a null value.", name);
      }
      return values;
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? EnsureNoEmptyValues<T>(this T? values, string name) where T : IEnumerable<string>
    {
      if (values is not null)
      {
        foreach (var value in values)
          value.EnsureNotNullOrEmpty(name);
      }
      return values;
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? EnsureNoWhitespaceValues<T>(this T? values, string name) where T : IEnumerable<string>
    {
      if (values is not null)
      {
        foreach (var value in values)
          value.EnsureNotNullOrWhiteSpace(name);
      }
      return values;
    }

    /// <summary>
    /// Ensures that the <paramref name="symbol"/> is not null or whitespace and
    /// returns it converted to lower invariant case.
    /// </summary>
    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EnsureStreamSymbol(this string symbol)
      => symbol.EnsureNotNullOrWhiteSpace(nameof(symbol)).ToLowerInvariant();
  }
}
