// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using EchoDotNetLite.Models;

namespace EchoDotNetLite;

public static class PropertyContentSerializer
{
    /// <summary>
    /// ECHONETプロパティ「インスタンスリスト通知」(EPC <c>0xD5</c>)のプロパティ内容をシリアライズします。
    /// </summary>
    /// <remarks>
    /// プロパティ内容が253バイト以上となる場合、その時点で書き込みを中断して<see langword="true"/>を返します。
    /// つまり、<paramref name="instanceList"/>の85個目以降の要素は無視されます。
    /// </remarks>
    /// <param name="instanceList">インスタンスリストを表す<see cref="IEnumerable{EOJ}"/>。</param>
    /// <param name="destination">シリアライズした結果が書き込まれる<see cref="Span{byte}"/></param>
    /// <param name="bytesWritten"><paramref name="destination"/>に書き込まれた長さ。</param>
    /// <returns>
    /// 正常に書き込まれた場合は<see langword="true"/>。
    /// <paramref name="instanceList"/>が<see langword="null"/>の場合、<paramref name="destination"/>の長さが足りない場合は<see langword="false"/>。
    /// </returns>
    /// <seealso href="https://echonet.jp/spec_v114_lite/">
    /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
    /// </seealso>
    public static bool TrySerializeInstanceListNotification
    (
        IEnumerable<EOJ> instanceList,
        Span<byte> destination,
        out int bytesWritten
    )
    {
        bytesWritten = 0;

        if (instanceList is null)
            return false;

        //1 ﾊﾞｲﾄ目：通報インスタンス数
        if (destination.Length < 1)
            return false;

        ref var refNumberOfInstance = ref destination[0];

        bytesWritten++;
        destination = destination.Slice(1);

        //2～253 ﾊﾞｲﾄ目：ECHONET オブジェクトコード（EOJ3 バイト）を列挙。
        var numberOfInstances = 0;

        foreach (var instance in instanceList)
        {
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
    /// <param name="content">「インスタンスリスト通知」のプロパティ内容を表す<see cref="ReadOnlySpan{byte}"/>。</param>
    /// <param name="instanceList">デシリアライズした結果を格納した<see cref="IReadOnlyList{EOJ}"/>。</param>
    /// <returns>
    /// 正常にデシリアライズされた場合は<see langword="true"/>。
    /// <paramref name="content"/>の内容に不足がある場合は<see langword="false"/>。
    /// </returns>
    /// <seealso href="https://echonet.jp/spec_v114_lite/">
    /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
    /// </seealso>
    public static bool TryDeserializeInstanceListNotification
    (
        ReadOnlySpan<byte> content,
        [NotNullWhen(true)] out IReadOnlyList<EOJ>? instanceList
    )
    {
        instanceList = default;

        //1 ﾊﾞｲﾄ目：通報インスタンス数
        if (content.Length < 1)
            return false;

        var numberOfInstance = (int)content[0];
        var list = new List<EOJ>(capacity: numberOfInstance);

        content = content.Slice(1);

        //2～253 ﾊﾞｲﾄ目：ECHONET オブジェクトコード（EOJ3 バイト）を列挙。
        for (var i = 0; i < numberOfInstance; i++)
        {
            if (content.Length < 3)
                return false;

            var eoj = new EOJ
            (
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
    /// ECHONETプロパティ「プロパティマップ」のプロパティ内容をデシリアライズします。　以下の「プロパティマップ」プロパティのデシリアライズに使用します。
    /// <list type="bullet">
    ///   <item><description>「SetM プロパティマップ」(EPC <c>0x9B</c>)</description></item>
    ///   <item><description>「GetM プロパティマップ」(EPC <c>0x9C</c>)</description></item>
    ///   <item><description>「状変アナウンスプロパティマップ」(EPC <c>0x9D</c>)</description></item>
    ///   <item><description>「Set プロパティマップ」(EPC <c>0x9E</c>)</description></item>
    ///   <item><description>「Get プロパティマップ」(EPC <c>0x9F</c>)</description></item>
    /// </list>
    /// </summary>
    /// <param name="content">「プロパティマップ」のプロパティ内容を表す<see cref="ReadOnlySpan{byte}"/>。</param>
    /// <param name="propertyMap">デシリアライズした結果を格納した<see cref="IReadOnlyList{byte}"/>。</param>
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
    public static bool TryDeserializePropertyMap
    (
        ReadOnlySpan<byte> content,
        [NotNullWhen(true)] out IReadOnlyList<byte>? propertyMap
    )
    {
        propertyMap = default;

        //1 バイト目：プロパティの数。バイナリ表示。
        if (content.Length < 1)
            return false;

        var numberOfProperties = (int)content[0];

        if (numberOfProperties == 0)
        {
            propertyMap = Array.Empty<byte>();
            return true;
        }

        var props = new List<byte>(capacity: numberOfProperties);

        content = content.Slice(1);

        if (numberOfProperties < 0x10)
        {
            // 記述形式（1）
            // 1 バイト目：プロパティの数。バイナリ表示。
            // 2 バイト目以降：プロパティのコード（1 バイトコード）をそのまま列挙する。
            if (content.Length < numberOfProperties)
                return false;

#if NET8_0_OR_GREATER
            props.AddRange(content.Slice(0, numberOfProperties));
#else
            for (var i = 0; i < numberOfProperties; i++)
            {
                props.Add(content[i]);
            }
#endif
        }
        else
        {
            // 記述形式（2）
            // 1 バイト目：プロパティの数。バイナリ表示。
            // 2～17 バイト目：下図の 16 バイトのテーブルにおいて、存在するプロパティコードを示
            //                 すビット位置に 1 をセットして 2 バイト目から順に列挙する。
            if (content.Length < 16)
                return false;

            for (var i = 0; i < 16; i++)
            {
                var propertyBits = content[i];

                if ((propertyBits & 0b10000000) == 0b10000000)
                {
                    props.Add((byte)(0xF0 | (byte)i));
                }
                if ((propertyBits & 0b01000000) == 0b01000000)
                {
                    props.Add((byte)(0xE0 | (byte)i));
                }
                if ((propertyBits & 0b00100000) == 0b00100000)
                {
                    props.Add((byte)(0xD0 | (byte)i));
                }
                if ((propertyBits & 0b00010000) == 0b00010000)
                {
                    props.Add((byte)(0xC0 | (byte)i));
                }
                if ((propertyBits & 0b00001000) == 0b00001000)
                {
                    props.Add((byte)(0xB0 | (byte)i));
                }
                if ((propertyBits & 0b00000100) == 0b00000100)
                {
                    props.Add((byte)(0xA0 | (byte)i));
                }
                if ((propertyBits & 0b00000010) == 0b00000010)
                {
                    props.Add((byte)(0x90 | (byte)i));
                }
                if ((propertyBits & 0b00000001) == 0b00000001)
                {
                    props.Add((byte)(0x80 | (byte)i));
                }
            }
        }

        propertyMap = props;

        return true;
    }
}
