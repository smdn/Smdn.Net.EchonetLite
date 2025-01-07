// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Smdn.Net.EchonetLite.Protocol;

public static class InstanceListSerializer {
  /// <summary>
  /// ECHONETプロパティ「インスタンスリスト通知」(EPC <c>0xD5</c>)の最大データ長(バイト単位)を表す定数値。
  /// </summary>
  public const int MaxDataLength = 253; // instance count (1 byte) + 84 instances × 3 bytes

  /// <summary>
  /// ECHONETプロパティ「インスタンスリスト通知」(EPC <c>0xD5</c>)のプロパティ内容をシリアライズします。
  /// </summary>
  /// <remarks>
  /// プロパティ内容が253バイト以上となる場合、その時点で書き込みを中断します。
  /// つまり、<paramref name="instanceList"/>の85個目以降の要素は無視されます。
  /// </remarks>
  /// <param name="writer">シリアライズした結果の書き込み先となる<see cref="IBufferWriter{Byte}"/>。</param>
  /// <param name="instanceList">インスタンスリストを表す<see cref="IEnumerable{EOJ}"/>。</param>
  /// <param name="prependPdc">プロパティ内容に先行して、PDC(プロパティ値の長さ)を書き込むかどうかを指定します。</param>
  /// <returns>
  /// <paramref name="writer"/>に書き込まれたインスタンスリストのバイト数を返します。
  /// </returns>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  public static int Serialize(
    IBufferWriter<byte> writer,
    IEnumerable<EOJ> instanceList,
    bool prependPdc
  )
  {
    if (writer is null)
      throw new ArgumentNullException(nameof(writer));

    int bytesWritten;

    if (prependPdc) {
      var destination = writer.GetSpan(1 /* PDC */ + MaxDataLength);

      if (TrySerialize(instanceList, destination[1..], out bytesWritten)) {
        destination[0] = (byte)bytesWritten;

        writer.Advance(1 + bytesWritten);
      }
    }
    else {
      var destination = writer.GetSpan(MaxDataLength);

      if (TrySerialize(instanceList, destination, out bytesWritten))
        writer.Advance(bytesWritten);
    }

    return bytesWritten;
  }

  /// <summary>
  /// ECHONETプロパティ「インスタンスリスト通知」(EPC <c>0xD5</c>)のプロパティ内容をシリアライズします。
  /// </summary>
  /// <remarks>
  /// プロパティ内容が253バイト以上となる場合、その時点で書き込みを中断して<see langword="true"/>を返します。
  /// つまり、<paramref name="instanceList"/>の85個目以降の要素は無視されます。
  /// </remarks>
  /// <param name="instanceList">インスタンスリストを表す<see cref="IEnumerable{EOJ}"/>。</param>
  /// <param name="destination">シリアライズした結果が書き込まれる<see cref="Span{Byte}"/></param>
  /// <param name="bytesWritten"><paramref name="destination"/>に書き込まれた長さ。</param>
  /// <returns>
  /// 正常に書き込まれた場合は<see langword="true"/>。
  /// <paramref name="instanceList"/>が<see langword="null"/>の場合、<paramref name="destination"/>の長さが足りない場合は<see langword="false"/>。
  /// </returns>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  public static bool TrySerialize(
    IEnumerable<EOJ> instanceList,
    Span<byte> destination,
    out int bytesWritten
  )
  {
    bytesWritten = 0;

    if (instanceList is null)
      return false;

    // 1 バイト目：通報インスタンス数
    if (destination.Length < 1)
      return false;

    ref var refNumberOfInstance = ref destination[0];

    bytesWritten++;
    destination = destination.Slice(1);

    // 2～253 バイト目：ECHONET オブジェクトコード（EOJ3 バイト）を列挙。
    var numberOfInstances = 0;

    foreach (var instance in instanceList) {
      if (MaxDataLength <= bytesWritten)
        break;

      if (destination.Length < 3)
        return false;

      destination[0] = instance.ClassGroupCode;
      destination[1] = instance.ClassCode;
      destination[2] = instance.InstanceCode;

      bytesWritten += 3;
      destination = destination.Slice(3);

      numberOfInstances++;
    }

    refNumberOfInstance = (byte)numberOfInstances;

    return true;
  }

  /// <summary>
  /// ECHONETプロパティ「インスタンスリスト通知」(EPC <c>0xD5</c>)のプロパティ内容をデシリアライズします。
  /// </summary>
  /// <param name="data">「インスタンスリスト通知」のプロパティ内容を表す<see cref="ReadOnlySpan{Byte}"/>。</param>
  /// <param name="instanceList">デシリアライズした結果を格納した<see cref="IReadOnlyList{EOJ}"/>。</param>
  /// <returns>
  /// 正常にデシリアライズされた場合は<see langword="true"/>。
  /// <paramref name="data"/>の内容に不足がある場合は<see langword="false"/>。
  /// </returns>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  public static bool TryDeserialize(
    ReadOnlySpan<byte> data,
    [NotNullWhen(true)] out IReadOnlyList<EOJ>? instanceList
  )
  {
    instanceList = default;

    // 1 バイト目：通報インスタンス数
    if (data.Length < 1)
      return false;

    var numberOfInstance = (int)data[0];
    var list = new List<EOJ>(capacity: numberOfInstance);

    data = data.Slice(1);

    // 2～253 バイト目：ECHONET オブジェクトコード（EOJ3 バイト）を列挙。
    for (var i = 0; i < numberOfInstance; i++) {
      if (data.Length < 3)
        return false;

      var eoj = new EOJ(
        classGroupCode: data[0],
        classCode: data[1],
        instanceCode: data[2]
      );

      list.Add(eoj);

      data = data.Slice(3);
    }

    instanceList = list;

    return true;
  }
}
