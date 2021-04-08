# FFT.Binance

[![Source code](https://img.shields.io/static/v1?style=flat&label=&message=Source%20Code&logo=read-the-docs&color=informational)](https://github.com/FastFinTech/FFT.Binance)
[![NuGet
package - FFT.Binance](https://img.shields.io/nuget/v/FFT.Binance.svg)](https://nuget.org/packages/FFT.Binance)
[![NuGet
package - FFT.Binance.BinanceTickProviders](https://img.shields.io/nuget/v/FFT.Binance.svg)](https://nuget.org/packages/FFT.Binance.BinanceTickProviders)
[![Full documentation](https://img.shields.io/static/v1?style=flat&label=&message=Documentation&logo=read-the-docs&color=green)](https://fastfintech.github.io/FFT.Binance/)

`FFT.Binance` is a .Net client for the [Binance
api](https://github.com/binance/binance-spot-api-docs/blob/master/rest-api.md#general-api-information)

Use the latest version 3.x.x package to connect to the Binance V3 api. When Binance
releases new api versions, this package will adjust new major versions to match
the Binance api version. For example, when Binance releases their V4 version, you
can use the latest 4.x.x package to connect to it.

### Usage
The basic idea is to create a long-lived singleton instance of the api client
which you reuse throughout your application. It is threadsafe.

```csharp
// TODO: basic code sample;
```

[See complete documentation including the list of `BinanceApiClient` methods.](https://fastfintech.github.io/FFT.Binance/api/FFT.Binance.BinanceApiClient.html)