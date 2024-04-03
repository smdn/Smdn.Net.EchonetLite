// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.Protocol;

/// <summary>
/// ECHONET Lite ヘッダ2 (EDH2)の値を表す列挙体です。
/// ECHONET Lite サービスコードを規定します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 図 ３-５ ESV コードの詳細規定
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 表 ３-９ 要求用 ESV コード一覧表
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 表 ３-１０ 応答・通知用 ESV コード一覧表
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 表 ３-１１ 不可応答用 ESV コード一覧表
/// </seealso>
// #pragma warning disable CA1027
public enum ESV : byte {
  /// <summary>
  /// 未定義または無効なサービスコードを表す値を示します。
  /// </summary>
  Invalid = 0,

  /// <summary>
  /// サービスコード<c>0x60</c>、「プロパティ値書き込み要求（応答不要）」(記号:<c>SetI</c>) を表す値を示します。
  /// このサービスは、一斉同報可です。
  /// </summary>
  SetI = 0x60,

  /// <summary>
  /// サービスコード<c>0x61</c>、「プロパティ値書き込み要求（応答要）」(記号:<c>SetC</c>) を表す値を示します。
  /// このサービスは、一斉同報可です。
  /// </summary>
  SetC = 0x61,

  /// <summary>
  /// サービスコード<c>0x62</c>、「プロパティ値読み出し要求」(記号:<c>Get</c>) を表す値を示します。
  /// このサービスは、一斉同報可です。
  /// </summary>
  Get = 0x62,

  /// <summary>
  /// サービスコード<c>0x63</c>、「プロパティ値通知要求」(記号:<c>INF_REQ</c>) を表す値を示します。
  /// このサービスは、一斉同報可です。
  /// </summary>
  InfRequest = 0x63,

  /*
   * 0x64-0x6D: for future reserved
   */

  /// <summary>
  /// サービスコード<c>0x6E</c>、「プロパティ値書き込み・読み出し要求」(記号:<c>SetGet</c>) を表す値を示します。
  /// このサービスは、一斉同報可です。
  /// </summary>
  SetGet = 0x6E,

  /*
   * 0x6F: for future reserved
   */

  /// <summary>
  /// サービスコード<c>0x71</c>、「プロパティ値書き込み応答」(記号:<c>Set_Res</c>) を表す値を示します。
  /// <see cref="SetC">ESV=0x61</see>の応答、個別応答です。
  /// </summary>
  SetResponse = 0x71,

  /// <summary>
  /// サービスコード<c>0x72</c>、「プロパティ値読み出し応答」(記号:<c>Get_Res</c>) を表す値を示します。
  /// <see cref="Get">ESV=0x62</see>の応答、個別応答です。
  /// </summary>
  GetResponse = 0x72,

  /// <summary>
  /// サービスコード<c>0x73</c>、「プロパティ値通知」(記号:<c>INF</c>) を表す値を示します。
  /// 自発的なプロパティ値通知、及び、<see cref="InfRequest">ESV=0x63</see>の応答に使用します。
  /// このサービスは、個別通知、一斉同報通知共に可です。
  /// </summary>
  Inf = 0x73,

  /// <summary>
  /// サービスコード<c>0x74</c>、「プロパティ値通知（応答要）」(記号:<c>INFC</c>) を表す値を示します。
  /// このサービスは、個別通知のみです。
  /// </summary>
  InfC = 0x74,

  /*
   * 0x75-0x79: for future reserved
   */

  /// <summary>
  /// サービスコード<c>0x7A</c>、「プロパティ値通知応答」(記号:<c>INFC_Res</c>) を表す値を示します。
  /// <see cref="InfC">ESV=0x74</see>の応答、個別応答です。
  /// </summary>
  InfCResponse = 0x7A,

  /*
   * 0x7B-0x7D: for future reserved
   */

  /// <summary>
  /// サービスコード<c>0x7E</c>、「プロパティ値書き込み・読み出し応答」(記号:<c>SetGet_Res</c>) を表す値を示します。
  /// <see cref="SetGet">ESV=0x6E</see>の応答、個別応答です。
  /// </summary>
  SetGetResponse = 0x7E,

  /*
   * 0x7F: for future reserved
   */

  /// <summary>
  /// サービスコード<c>0x50</c>、「プロパティ値書き込み要求不可応答」(記号:<c>SetI_SNA</c>) を表す値を示します。
  /// <see cref="SetI">ESV=0x60</see>の不可応答、個別応答です。
  /// </summary>
  SetIServiceNotAvailable = 0x50,

  /// <summary>
  /// サービスコード<c>0x51</c>、「プロパティ値書き込み要求不可応答」(記号:<c>SetC_SNA</c>) を表す値を示します。
  /// <see cref="SetC">ESV=0x61</see>の不可応答、個別応答です。
  /// </summary>
  SetCServiceNotAvailable = 0x51,

  /// <summary>
  /// サービスコード<c>0x52</c>、「プロパティ値読み出し不可応答」(記号:<c>Get_SNA</c>) を表す値を示します。
  /// <see cref="Get">ESV=0x62</see>の不可応答、個別応答です。
  /// </summary>
  GetServiceNotAvailable = 0x52,

  /// <summary>
  /// サービスコード<c>0x53</c>、「プロパティ値通知不可応答」(記号:<c>INF_SNA</c>) を表す値を示します。
  /// <see cref="InfRequest">ESV=0x63</see>の不可応答、個別応答です。
  /// </summary>
  InfServiceNotAvailable = 0x53,

  /*
   * 0x54-0x5D: for future reserved
   */

  /// <summary>
  /// サービスコード<c>0x5E</c>、「プロパティ値書き込み・読み出し不可応答」(記号:<c>SetGet_SNA</c>) を表す値を示します。
  /// <see cref="SetGet">ESV=0x6E</see>の不可応答、個別応答です。
  /// </summary>
  SetGetServiceNotAvailable = 0x5E,

  /*
   * 0x5F: for future reserved
   */
}
