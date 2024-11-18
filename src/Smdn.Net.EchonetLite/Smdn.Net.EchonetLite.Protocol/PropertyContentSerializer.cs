// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Smdn.Net.EchonetLite.Protocol;

public static class PropertyContentSerializer {
  /// <summary>
  /// ECHONETプロパティ「インスタンスリスト通知」(EPC <c>0xD5</c>)のプロパティ内容をシリアライズします。
  /// </summary>
  /// <remarks>
  /// プロパティ内容が253バイト以上となる場合、その時点で書き込みを中断します。
  /// つまり、<paramref name="instanceList"/>の85個目以降の要素は無視されます。
  /// </remarks>
  /// <param name="instanceList">インスタンスリストを表す<see cref="IEnumerable{EOJ}"/>。</param>
  /// <param name="buffer">シリアライズした結果が書き込まれる<see cref="IBufferWriter{Byte}"/>。</param>
  /// <param name="prependPdc">プロパティ内容に先行して、PDC(プロパティ値の長さ)を書き込むかどうかを指定します。</param>
  /// <returns>
  /// <paramref name="buffer"/>に書き込まれたインスタンスリストのバイト数を返します。
  /// </returns>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  public static int SerializeInstanceListNotification(
    IEnumerable<EOJ> instanceList,
    IBufferWriter<byte> buffer,
    bool prependPdc
  )
  {
    if (buffer is null)
      throw new ArgumentNullException(nameof(buffer));

    int bytesWritten;

    if (prependPdc) {
      var destination = buffer.GetSpan(1 /* PDC */ + 253);

      if (TrySerializeInstanceListNotification(instanceList, destination[1..], out bytesWritten)) {
        destination[0] = (byte)bytesWritten;

        buffer.Advance(1 + bytesWritten);
      }
    }
    else {
      var destination = buffer.GetSpan(253);

      if (TrySerializeInstanceListNotification(instanceList, destination, out bytesWritten))
        buffer.Advance(bytesWritten);
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
  public static bool TrySerializeInstanceListNotification(
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
      if (253 <= bytesWritten)
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
  /// <param name="content">「インスタンスリスト通知」のプロパティ内容を表す<see cref="ReadOnlySpan{Byte}"/>。</param>
  /// <param name="instanceList">デシリアライズした結果を格納した<see cref="IReadOnlyList{EOJ}"/>。</param>
  /// <returns>
  /// 正常にデシリアライズされた場合は<see langword="true"/>。
  /// <paramref name="content"/>の内容に不足がある場合は<see langword="false"/>。
  /// </returns>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  public static bool TryDeserializeInstanceListNotification(
    ReadOnlySpan<byte> content,
    [NotNullWhen(true)] out IReadOnlyList<EOJ>? instanceList
  )
  {
    instanceList = default;

    // 1 バイト目：通報インスタンス数
    if (content.Length < 1)
      return false;

    var numberOfInstance = (int)content[0];
    var list = new List<EOJ>(capacity: numberOfInstance);

    content = content.Slice(1);

    // 2～253 バイト目：ECHONET オブジェクトコード（EOJ3 バイト）を列挙。
    for (var i = 0; i < numberOfInstance; i++) {
      if (content.Length < 3)
        return false;

      var eoj = new EOJ(
        classGroupCode: content[0],
        classCode: content[1],
        instanceCode: content[2]
      );

      list.Add(eoj);

      content = content.Slice(3);
    }

    instanceList = list;

    return true;
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
  /// <param name="buffer">シリアライズした結果が書き込まれる<see cref="IBufferWriter{Byte}"/>。</param>
  /// <returns>
  /// <paramref name="buffer"/>に書き込まれたプロパティマップのバイト数を返します。
  /// </returns>
  /// <seealso href="https://echonet.jp/spec_g/">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_g/">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
  /// </seealso>
  public static int SerializePropertyMap(
    IReadOnlyCollection<byte> propertyMap,
    IBufferWriter<byte> buffer
  )
  {
    if (propertyMap is null)
      throw new ArgumentNullException(nameof(propertyMap));
    if (buffer is null)
      throw new ArgumentNullException(nameof(buffer));

    var destination = buffer.GetSpan(17);

    if (TrySerializePropertyMap(propertyMap, destination, out var bytesWritten)) {
      buffer.Advance(bytesWritten);
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
  public static bool TrySerializePropertyMap(
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
  /// <param name="content">「プロパティマップ」のプロパティ内容を表す<see cref="ReadOnlySpan{Byte}"/>。</param>
  /// <param name="propertyMap">デシリアライズした結果を格納した<see cref="IReadOnlyList{Byte}"/>。</param>
  /// <returns>
  /// 正常にデシリアライズされた場合は<see langword="true"/>。
  /// <paramref name="content"/>の内容に不足がある場合は<see langword="false"/>。
  /// </returns>
  /// <seealso href="https://echonet.jp/spec_g/">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_g/">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
  /// </seealso>
  public static bool TryDeserializePropertyMap(
    ReadOnlySpan<byte> content,
    [NotNullWhen(true)] out IReadOnlyList<byte>? propertyMap
  )
  {
    propertyMap = default;

    // > APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
    // > 記述形式（1）
    // > 記述形式（2）
    // > 1 バイト目：プロパティの数。バイナリ表示。
    if (content.Length < 1)
      return false;

    var numberOfProperties = (int)content[0];

    if (numberOfProperties == 0) {
      propertyMap = Array.Empty<byte>();
      return true;
    }

    var props = new List<byte>(capacity: numberOfProperties);

    content = content.Slice(1);

    if (numberOfProperties < 0x10) {
      // > APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
      // > 記述形式（1）
      // > 1 バイト目：プロパティの数。バイナリ表示。
      // > 2 バイト目以降：プロパティのコード（1 バイトコード）をそのまま列挙する。
      if (content.Length < numberOfProperties)
        return false;

#if SYSTEM_COLLECTIONS_GENERIC_COLLECTIONEXTENSIONS_ADDRANGE
      props.AddRange(content.Slice(0, numberOfProperties));
#else
      for (var i = 0; i < numberOfProperties; i++) {
        props.Add(content[i]);
      }
#endif
    }
    else {
      // > APPENDIX ECHONET 機器オブジェクト詳細規定 付録１ プロパティマップ記述形式
      // > 記述形式（2）
      // > 1 バイト目：プロパティの数。バイナリ表示。
      // > 2～17 バイト目：下図の 16 バイトのテーブルにおいて、存在するプロパティコードを示
      // >         すビット位置に 1 をセットして 2 バイト目から順に列挙する。
      if (content.Length < 16)
        return false;

      for (var i = 0; i < 16; i++) {
        var propertyBits = content[i];
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
