using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace EchoDotNetLite.Models
{

    /// <summary>
    /// プロパティクラス
    /// </summary>
    public sealed class EchoPropertyInstance
    {
        internal static Specifications.EchoProperty GenerateUnknownProperty(byte epc)
            => new Specifications.EchoProperty
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

        private readonly ArrayBufferWriter<byte> _value = new(initialCapacity: 8); // TODO: best initial capacity

        /// <summary>
        /// プロパティ値を表す<see cref="ReadOnlyMemory{byte}"/>を取得します。
        /// </summary>
        public ReadOnlyMemory<byte> ValueMemory => _value.WrittenMemory;

        /// <summary>
        /// プロパティ値を表す<see cref="ReadOnlySpan{byte}"/>を取得します。
        /// </summary>
        public ReadOnlySpan<byte> ValueSpan => _value.WrittenSpan;

        /// <summary>
        /// プロパティ値変更イベント
        /// </summary>
        /// <remarks>
        /// このイベントは、プロパティ値の設定が行われる場合に発生します。
        /// このイベントは、設定される値が以前と同じ値の場合でも発生します。
        /// </remarks>
        public event EventHandler<ReadOnlyMemory<byte>>? ValueSet;

        /// <summary>
        /// プロパティ値を設定します。
        /// </summary>
        /// <remarks>
        /// プロパティ値の設定が行われたあと、イベント<see cref="ValueSet"/>が発生します。
        /// </remarks>
        /// <param name="newValue">プロパティ値として設定する値を表す<see cref="ReadOnlySpan{byte}"/>。</param>
        /// <seealso cref="ValueSet"/>
        public void SetValue(ReadOnlySpan<byte> newValue)
        {
#if NET8_0_OR_GREATER
            _value.ResetWrittenCount();
#else
            _value.Clear();
#endif

            _value.Write(newValue);

            //TODO とりあえず変更がなくてもイベントを起こす
            ValueSet?.Invoke(this, _value.WrittenMemory);
        }

        /// <summary>
        /// プロパティ値を書き込みます。
        /// </summary>
        /// <remarks>
        /// プロパティ値の設定が行われたあと、イベント<see cref="ValueSet"/>が発生します。
        /// </remarks>
        /// <param name="write">
        /// プロパティ値を書き込むための<see cref="Action{IBufferWriter{byte}}"/>デリゲート。
        /// 引数で渡される<see cref="IBufferWriter{byte}"/>を介してプロパティ値として設定する内容を書き込んでください。
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="write"/>が<see langword="null"/>です。</exception>
        /// <seealso cref="ValueSet"/>
        public void WriteValue(Action<IBufferWriter<byte>> write)
        {
            if (write is null)
                throw new ArgumentNullException(nameof(write));

#if NET8_0_OR_GREATER
            _value.ResetWrittenCount();
#else
            _value.Clear();
#endif

            write(_value);

            //TODO とりあえず変更がなくてもイベントを起こす
            ValueSet?.Invoke(this, _value.WrittenMemory);
        }
    }
}
