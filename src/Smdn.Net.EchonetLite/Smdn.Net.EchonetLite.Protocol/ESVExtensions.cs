// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.Protocol;

/// <summary>
/// <see cref="ESV"/>列挙体に対する拡張メソッドを提供します。
/// </summary>
public static class ESVExtensions {
  /// <summary>
  /// <see cref="ESV"/>の値を、対応するECHONET サービスの記号を表す文字列に変換します。
  /// </summary>
  /// <param name="esv">文字列へ変換する<see cref="ESV"/>の値。</param>
  /// <returns>文字列へ変換した結果。</returns>
  public static string ToSymbolString(this ESV esv)
    => esv switch {
      ESV.SetI => "SetI",
      ESV.SetC => "SetC",
      ESV.Get => "Get",
      ESV.InfRequest => "INF_REQ",
      ESV.SetGet => "SetGet",
      ESV.SetResponse => "Set_Res",
      ESV.GetResponse => "Get_Res",
      ESV.Inf => "INF",
      ESV.InfC => "INFC",
      ESV.InfCResponse => "INFC_Res",
      ESV.SetGetResponse => "SetGet_Res",
      ESV.SetIServiceNotAvailable => "SetI_SNA",
      ESV.SetCServiceNotAvailable => "SetC_SNA",
      ESV.GetServiceNotAvailable => "Get_SNA",
      ESV.InfServiceNotAvailable => "INF_SNA",
      ESV.SetGetServiceNotAvailable => "SetGet_SNA",
      _ => ((byte)esv).ToString("X2", provider: null),
    };
}
