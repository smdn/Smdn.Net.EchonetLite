// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;

namespace Smdn.Net.EchonetLite.Appendix
{
    /// <summary>
    /// ECHONET Lite オブジェクト
    /// </summary>
    public sealed class EchonetObjectSpecification
    {
        /// <summary>
        /// 指定されたクラスグループコード・クラスコードをもつ、未知のECHONET Lite オブジェクトを作成します。
        /// </summary>
        internal static EchonetObjectSpecification CreateUnknown(byte classGroupCode, byte classCode)
            => new(
                (
                    ClassGroup: new(
                        code: classGroupCode,
                        name: "Unknown",
                        propertyName: "Unknown",
                        classes: Array.Empty<EchonetClassSpecification>(),
                        superClassName: null
                    ),
                    Class: new(
                        isDefined: false,
                        code: classCode,
                        name: "Unknown",
                        propertyName: "Unknown"
                    ),
                    Properties: Array.Empty<EchonetPropertySpecification>()
                )
            );

        internal EchonetObjectSpecification(
            byte classGroupCode,
            byte classCode
        )
            : this(SpecificationMaster.LoadObjectSpecification(classGroupCode, classCode))
        {
        }

        private EchonetObjectSpecification(
            (
                EchonetClassGroupSpecification ClassGroup,
                EchonetClassSpecification Class,
                IReadOnlyList<EchonetPropertySpecification> Properties
            ) objectSpecification
        )
        {
            (ClassGroup, Class, Properties) = objectSpecification;
        }

        /// <summary>
        /// クラスグループ情報
        /// クラスグループコード
        /// </summary>
        public EchonetClassGroupSpecification ClassGroup { get; }
        /// <summary>
        /// クラス情報
        /// クラスコード
        /// </summary>
        public EchonetClassSpecification Class { get; }

        /// <summary>
        /// 仕様上定義済みのプロパティの一覧
        /// </summary>
        internal IReadOnlyList<EchonetPropertySpecification> Properties { get; }

        /// <summary>
        /// 仕様上定義済みのGETプロパティの一覧
        /// </summary>
        public IEnumerable<EchonetPropertySpecification> GetProperties
        {
            get { return Properties.Where(static p => p.CanGet); }
        }
        /// <summary>
        /// 仕様上定義済みのSETプロパティの一覧
        /// </summary>
        public IEnumerable<EchonetPropertySpecification> SetProperties
        {
            get { return Properties.Where(static p => p.CanSet); }
        }
        /// <summary>
        /// 仕様上定義済みのANNOプロパティの一覧
        /// </summary>
        public IEnumerable<EchonetPropertySpecification> AnnoProperties
        {
            get { return Properties.Where(static p => p.CanAnnounceStatusChange); }
        }
    }
}
