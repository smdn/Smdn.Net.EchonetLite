// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text.Json.Serialization;

using Smdn.Net.EchonetLite.Serialization.Json;

namespace Smdn.Net.EchonetLite.Protocol
{
    public readonly struct PropertyRequest
    {
        /// <summary>
        /// <see cref="EPC"/>のみを指定して、<see cref="PropertyRequest"/>を作成します。
        /// </summary>
        /// <remarks>
        /// <see cref="PDC"/>には<c>0</c>、<see cref="EDT"/>には<see langword="null"/>が設定されます。
        /// </remarks>
        /// <param name="epc"><see cref="EPC"/>に指定する値。</param>
        public PropertyRequest(byte epc)
        {
            EPC = epc;
            EDT = ReadOnlyMemory<byte>.Empty;
        }

        /// <summary>
        /// <see cref="EPC"/>および<see cref="EDT"/>を指定して、<see cref="PropertyRequest"/>を作成します。
        /// </summary>
        /// <remarks>
        /// <see cref="PDC"/>は、常に<see cref="EDT"/>の長さを返します。
        /// </remarks>
        /// <param name="epc"><see cref="EPC"/>に指定する値。</param>
        /// <param name="edt"><see cref="EDT"/>に指定する値。</param>
        /// <exception cref="ArgumentException"><paramref name="edt"/>の長さが、255を超えています。</exception>
        public PropertyRequest(byte epc, ReadOnlyMemory<byte> edt)
        {
            if (byte.MaxValue < edt.Length)
                throw new ArgumentException(message: "The length of the EDT exceeds the maximum allowed by the specification.", nameof(edt));

            EPC = epc;
            EDT = edt;
        }

        /// <summary>
        /// ECHONET Liteプロパティ(1B)
        /// </summary>
        [JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public byte EPC { get; }
        /// <summary>
        /// EDTのバイト数(1B)
        /// </summary>
        [JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public byte PDC => (byte)EDT.Length;
        /// <summary>
        /// プロパティ値データ(PDCで指定)
        /// </summary>
        [JsonConverter(typeof(ByteSequenceJsonConverter))]
        public ReadOnlyMemory<byte> EDT { get; }
    }
}
