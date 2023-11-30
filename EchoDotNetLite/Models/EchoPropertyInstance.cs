#nullable enable
using System;
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

        private byte[]? _Value;
        /// <summary>
        /// プロパティ値
        /// </summary>
        public byte[]? Value
        {
            get => _Value;
            set
            {
                //TODO とりあえず変更がなくてもイベントを起こす
                ValueChanged?.Invoke(this, value);
                if (value == _Value)
                    return;
                _Value = value;
            }
        }
        /// <summary>
        /// プロパティ値変更イベント
        /// </summary>
        public event EventHandler<byte[]?>? ValueChanged;
    }
}
