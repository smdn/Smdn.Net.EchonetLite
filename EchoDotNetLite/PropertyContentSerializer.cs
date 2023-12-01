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
}
