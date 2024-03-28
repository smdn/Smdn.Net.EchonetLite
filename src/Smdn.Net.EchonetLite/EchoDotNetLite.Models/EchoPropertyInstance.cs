// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;

namespace EchoDotNetLite.Models
{

    /// <summary>
    /// プロパティクラス
    /// </summary>
    public sealed class EchoPropertyInstance
    {
        internal static Specifications.EchoProperty GenerateUnknownProperty(byte epc)
            => new
            (
                code: epc,
                name: "Unknown",
                detail: "Unknown",
                value: null,
                dataType: "Unknown",
                logicalDataType: "Unknown",
                minSize: null,
                maxSize: null,
                get: false,
                getRequired: false,
                set: false,
                setRequired: false,
                anno: false,
                annoRequired: false,
                optionRequired: null,
                description: null,
                unit: null
            );

        public EchoPropertyInstance
        (
            byte classGroupCode,
            byte classCode,
            byte epc
        )
            : this
            (
                classGroupCode: classGroupCode,
                classCode: classCode,
                epc: epc,
                isPropertyAnno: false,
                isPropertySet: false,
                isPropertyGet: false
            )
        {
        }

        public EchoPropertyInstance
        (
            byte classGroupCode,
            byte classCode,
            byte epc,
            bool isPropertyAnno,
            bool isPropertySet,
            bool isPropertyGet
        )
            : this
            (
                spec:
                    SpecificationUtil.FindProperty(classGroupCode, classCode, epc) ??
                    GenerateUnknownProperty(epc),
                isPropertyAnno: isPropertyAnno,
                isPropertySet: isPropertySet,
                isPropertyGet: isPropertyGet
            )
        {
        }

        public EchoPropertyInstance(Specifications.EchoProperty spec)
            : this
            (
                spec: spec,
                isPropertyAnno: false,
                isPropertySet: false,
                isPropertyGet: false
            )
        {
        }

        public EchoPropertyInstance
        (
            Specifications.EchoProperty spec,
            bool isPropertyAnno,
            bool isPropertySet,
            bool isPropertyGet
        )
        {
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            Anno = isPropertyAnno;
            Set = isPropertySet;
            Get = isPropertyGet;
        }

        /// <summary>
        /// EPC
        /// ECHONET機器オブジェクト詳細規定がある場合、詳細仕様
        /// </summary>
        public Specifications.EchoProperty Spec { get; }
        /// <summary>
        /// プロパティ値の読み出し・通知要求のサービスを処理する。
        /// プロパティ値読み出し要求(0x62)、プロパティ値書き込み・読み出し要求(0x6E)、プロパティ値通知要求(0x63)の要求受付処理を実施する。
        /// </summary>
        public bool Get { get; }
        /// <summary>
        /// プロパティ値の書き込み要求のサービスを処理する。
        /// プロパティ値書き込み要求(応答不要)(0x60)、プロパティ値書き込み要求(応答要)(0x61)、プロパティ値書き込み・読み出し要求(0x6E)の要求受付処理を実施する。
        /// </summary>
        public bool Set { get; }
        /// <summary>
        /// プロパティ値の通知要求のサービスを処理する。
        /// プロパティ値通知要求（0x63）の要求受付処理を実施する。
        /// </summary>
        public bool Anno { get; }

        private ArrayBufferWriter<byte>? _value = null;

        /// <summary>
        /// プロパティ値を表す<see cref="ReadOnlyMemory{Byte}"/>を取得します。
        /// </summary>
        public ReadOnlyMemory<byte> ValueMemory => _value is null ? ReadOnlyMemory<byte>.Empty : _value.WrittenMemory;

        /// <summary>
        /// プロパティ値を表す<see cref="ReadOnlySpan{Byte}"/>を取得します。
        /// </summary>
        public ReadOnlySpan<byte> ValueSpan => _value is null ? ReadOnlySpan<byte>.Empty : _value.WrittenSpan;

        /// <summary>
        /// プロパティ値変更イベント
        /// </summary>
        /// <remarks>
        /// このイベントは、プロパティ値の設定が行われる場合に発生します。
        /// このイベントは、設定される値が以前と同じ値の場合でも発生します。
        /// </remarks>
        /// <seealso cref="ValueChanged"/>
        [Obsolete($"Use {nameof(ValueChanged)} instead.")]
        public event EventHandler<ReadOnlyMemory<byte>>? ValueSet;

        /// <summary>
        /// プロパティ値に変更があった場合に発生するイベント。
        /// </summary>
        /// <remarks>
        /// このイベントは、プロパティに異なる値が設定された場合にのみ発生します。
        /// プロパティに値が設定される際、その値が以前と同じ値だった場合には発生しません。
        /// </remarks>
        /// <seealso cref="ValueSet"/>
        public event EventHandler<(ReadOnlyMemory<byte> OldValue, ReadOnlyMemory<byte> NewValue)>? ValueChanged;

        /// <summary>
        /// プロパティ値を設定します。
        /// </summary>
        /// <remarks>
        /// プロパティ値の設定が行われたあと、イベント<see cref="ValueSet"/>が発生します。
        /// 設定によって値が変更された場合は、イベント<see cref="ValueChanged"/>も発生します。
        /// </remarks>
        /// <param name="newValue">プロパティ値として設定する値を表す<see cref="ReadOnlyMemory{Byte}"/>。</param>
        /// <seealso cref="ValueSet"/>
        /// <seealso cref="ValueChanged"/>
        public void SetValue(ReadOnlyMemory<byte> newValue)
            => WriteValue(writer => writer.Write(newValue.Span), newValueSize: newValue.Length);

        /// <summary>
        /// プロパティ値を書き込みます。
        /// </summary>
        /// <remarks>
        /// プロパティ値の設定が行われたあと、イベント<see cref="ValueSet"/>が発生します。
        /// 書き込みによって値が変更された場合は、イベント<see cref="ValueChanged"/>も発生します。
        /// </remarks>
        /// <param name="write">
        /// プロパティ値を書き込むための<see cref="Action{T}"/>デリゲート。
        /// 引数で渡される<see cref="IBufferWriter{Byte}"/>を介してプロパティ値として設定する内容を書き込んでください。
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="write"/>が<see langword="null"/>です。</exception>
        /// <seealso cref="ValueSet"/>
        /// <seealso cref="ValueChanged"/>
        public void WriteValue(Action<IBufferWriter<byte>> write)
            => WriteValue(write ?? throw new ArgumentNullException(nameof(write)), newValueSize: 0);

        private void WriteValue(Action<IBufferWriter<byte>> write, int newValueSize)
        {
            var valueChangedHandlers = ValueChanged;
            byte[]? oldValue = null;

            try
            {
                var oldValueLength = 0;

                if (_value is null)
                {
                    var initialCapacity = 0 < newValueSize ? newValueSize : 8; // TODO: best initial capacity

                    _value = new(initialCapacity);
                }
                else
                {
                    oldValueLength = _value.WrittenSpan.Length;

                    oldValue = ArrayPool<byte>.Shared.Rent(oldValueLength);

                    _value.WrittenSpan.CopyTo(oldValue.AsSpan(0, oldValueLength));

#if SYSTEM_BUFFERS_ARRAYBUFFERWRITER_RESETWRITTENCOUNT
                    _value.ResetWrittenCount();
#else
                    _value.Clear();
#endif
                }

                write(_value);

                // 変更がなくてもValueSetイベントを起こす
                ValueSet?.Invoke(this, _value.WrittenMemory);

                if (valueChangedHandlers is not null)
                {
                    // 値が新規に設定される場合、以前の値から変更がある場合はValueChangedイベントを起こす
                    if (oldValue is null || !oldValue.AsSpan(0, oldValueLength).SequenceEqual(_value.WrittenSpan))
                    {
                        var oldValueMemory = oldValue is null ? ReadOnlyMemory<byte>.Empty : oldValue.AsMemory(0, oldValueLength);
                        var newValueMemory = _value.WrittenMemory;

                        valueChangedHandlers.Invoke(this, (oldValueMemory, newValueMemory));
                    }
                }
            }
            finally
            {
                if (oldValue is not null)
                    ArrayPool<byte>.Shared.Return(oldValue);
            }
        }
    }
}
