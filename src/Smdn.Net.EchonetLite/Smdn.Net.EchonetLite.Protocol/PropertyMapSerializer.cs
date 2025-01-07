// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Smdn.Net.EchonetLite.Protocol;

public static class PropertyMapSerializer {
  /// <summary>
  /// ECHONETプロパティ「プロパティマップ」のプロパティ内容をシリアライズします。　以下の「プロパティマップ」プロパティのシリアライズに使用します。
  /// <list type="bullet">
  ///   <item><description>「SetM プロパティマップ」(EPC <c>0x9B</c>)</description></item>
  ///   <item><description>「GetM プロパティマップ」(EPC <c>0x9C</c>)</description></item>
  ///   <item><description>「状変アナウンスプロパティマップ」(EPC <c>0x9D</c>)</description></item>
  ///   <item><description>「Set プロパティマップ」(EPC <c>0x9E</c>)</description></item>
  ///   <item><description>「Get プロパティマップ」(EPC <c>0x9F</c>)</description></item>
  /// </list>
  /// </summary>
  /// <param name="writer">シリアライズした結果の書き込み先となる<see cref="IBufferWriter{Byte}"/>。</param>
  /// <param name="propertyMap">「プロパティマップ」を表す<see cref="IReadOnlyCollection{Byte}"/>。</param>
  /// <returns>
  /// <paramref name="writer"/>に書き込まれたプロパティマップのバイト数を返します。
  /// </returns>
  /// <seealso href="https://echonet.jp/spec_g/">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_g/">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
  /// </seealso>
  public static int Serialize(
    IBufferWriter<byte> writer,
    IReadOnlyCollection<byte> propertyMap
  )
  {
    if (writer is null)
      throw new ArgumentNullException(nameof(writer));
    if (propertyMap is null)
      throw new ArgumentNullException(nameof(propertyMap));

    var destination = writer.GetSpan(17);

    if (TrySerialize(propertyMap, destination, out var bytesWritten)) {
      writer.Advance(bytesWritten);
      return bytesWritten;
    }

    return 0; // unreachable
  }

  /// <summary>
  /// ECHONETプロパティ「プロパティマップ」のプロパティ内容をシリアライズします。　以下の「プロパティマップ」プロパティのシリアライズに使用します。
  /// <list type="bullet">
  ///   <item><description>「SetM プロパティマップ」(EPC <c>0x9B</c>)</description></item>
  ///   <item><description>「GetM プロパティマップ」(EPC <c>0x9C</c>)</description></item>
  ///   <item><description>「状変アナウンスプロパティマップ」(EPC <c>0x9D</c>)</description></item>
  ///   <item><description>「Set プロパティマップ」(EPC <c>0x9E</c>)</description></item>
  ///   <item><description>「Get プロパティマップ」(EPC <c>0x9F</c>)</description></item>
  /// </list>
  /// </summary>
  /// <param name="propertyMap">「プロパティマップ」を表す<see cref="IReadOnlyCollection{Byte}"/>。</param>
  /// <param name="destination">「プロパティマップ」をシリアライズした結果を書き込む先となる<see cref="ReadOnlySpan{Byte}"/>。</param>
  /// <param name="bytesWritten"><paramref name="destination"/>に書き込まれた長さ。</param>
  /// <returns>
  /// 正常にシリアライズされた場合は<see langword="true"/>。
  /// <paramref name="propertyMap"/>が<see langword="null"/>の場合、<paramref name="destination"/>の長さが足りない場合は<see langword="false"/>。
  /// </returns>
  /// <seealso href="https://echonet.jp/spec_g/">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_g/">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
  /// </seealso>
  public static bool TrySerialize(
    IReadOnlyCollection<byte> propertyMap,
    Span<byte> destination,
    out int bytesWritten
  )
  {
    bytesWritten = default;

    if (propertyMap is null || propertyMap.Count < 0)
      return false; // do nothing

    if (propertyMap.Count == 0) {
      if (destination.Length < 1)
        return false; // destination too short

      // > APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
      // > 記述形式（1）
      // > 記述形式（2）
      // > 1 バイト目：プロパティの数。バイナリ表示。
      bytesWritten = 1;
      destination[0] = 0;

      return true;
    }

    if (propertyMap.Count < 0x10) {
      // > APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
      // > 記述形式（1）
      // > 1 バイト目：プロパティの数。バイナリ表示。
      // > 2 バイト目以降：プロパティのコード（1 バイトコード）をそのまま列挙する。
      if (destination.Length < 1 + propertyMap.Count)
        return false; // destination too short

      destination[bytesWritten++] = (byte)propertyMap.Count;

      foreach (var p in propertyMap) {
        destination[bytesWritten++] = p;
      }
    }
    else {
      // > APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
      // > 記述形式（2）
      // > 1 バイト目：プロパティの数。バイナリ表示。
      // > 2～17 バイト目：下図の 16 バイトのテーブルにおいて、存在するプロパティコードを示
      // >         すビット位置に 1 をセットして 2 バイト目から順に列挙する。
      if (destination.Length < 17)
        return false; // destination too short

      destination[0] = (byte)propertyMap.Count;

      foreach (var p in propertyMap) {
        if (p < 0x80)
          continue; // ignore EPCs less than 0x80

        var index = p & 0x0F;
        var bit = (byte)(1 << (((p & 0xF0) - 0x80) >> 4));

        destination[1 + index] |= bit;
      }

      bytesWritten = 17;
    }

    return true;
  }

  /// <summary>
  /// ECHONETプロパティ「プロパティマップ」のプロパティ内容をデシリアライズします。　以下の「プロパティマップ」プロパティのデシリアライズに使用します。
  /// <list type="bullet">
  ///   <item><description>「SetM プロパティマップ」(EPC <c>0x9B</c>)</description></item>
  ///   <item><description>「GetM プロパティマップ」(EPC <c>0x9C</c>)</description></item>
  ///   <item><description>「状変アナウンスプロパティマップ」(EPC <c>0x9D</c>)</description></item>
  ///   <item><description>「Set プロパティマップ」(EPC <c>0x9E</c>)</description></item>
  ///   <item><description>「Get プロパティマップ」(EPC <c>0x9F</c>)</description></item>
  /// </list>
  /// </summary>
  /// <param name="data">「プロパティマップ」のプロパティ内容を表す<see cref="ReadOnlySpan{Byte}"/>。</param>
  /// <param name="propertyMap">デシリアライズした結果を格納した<see cref="IReadOnlyList{Byte}"/>。</param>
  /// <returns>
  /// 正常にデシリアライズされた場合は<see langword="true"/>。
  /// <paramref name="data"/>の内容に不足がある場合は<see langword="false"/>。
  /// </returns>
  /// <seealso href="https://echonet.jp/spec_g/">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_g/">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
  /// </seealso>
  public static bool TryDeserialize(
    ReadOnlySpan<byte> data,
    [NotNullWhen(true)] out IReadOnlyList<byte>? propertyMap
  )
  {
    propertyMap = default;

    // > APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
    // > 記述形式（1）
    // > 記述形式（2）
    // > 1 バイト目：プロパティの数。バイナリ表示。
    if (data.Length < 1)
      return false;

    var numberOfProperties = (int)data[0];

    if (numberOfProperties == 0) {
      propertyMap = Array.Empty<byte>();
      return true;
    }

    var props = new List<byte>(capacity: numberOfProperties);

    data = data.Slice(1);

    if (numberOfProperties < 0x10) {
      // > APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
      // > 記述形式（1）
      // > 1 バイト目：プロパティの数。バイナリ表示。
      // > 2 バイト目以降：プロパティのコード（1 バイトコード）をそのまま列挙する。
      if (data.Length < numberOfProperties)
        return false;

#if SYSTEM_COLLECTIONS_GENERIC_COLLECTIONEXTENSIONS_ADDRANGE
      props.AddRange(data.Slice(0, numberOfProperties));
#else
      for (var i = 0; i < numberOfProperties; i++) {
        props.Add(data[i]);
      }
#endif
    }
    else {
      // > APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
      // > 記述形式（2）
      // > 1 バイト目：プロパティの数。バイナリ表示。
      // > 2～17 バイト目：下図の 16 バイトのテーブルにおいて、存在するプロパティコードを示
      // >         すビット位置に 1 をセットして 2 バイト目から順に列挙する。
      if (data.Length < 16)
        return false;

      for (var i = 0; i < 16; i++) {
        var propertyBits = data[i];
        var lower = i;

        for (var j = 0; j < 8; j++) {
          var upper = 0x80 + (0x10 * j);
          var bitMask = 1 << j;

          if ((propertyBits & bitMask) != 0) {
            props.Add((byte)(upper | lower));
          }
        }
      }
    }

    propertyMap = props;

    return true;
  }
}
