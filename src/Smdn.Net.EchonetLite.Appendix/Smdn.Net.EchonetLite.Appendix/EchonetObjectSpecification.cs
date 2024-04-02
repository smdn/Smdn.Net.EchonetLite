// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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
                classGroup: new(
                    code: classGroupCode,
                    name: "Unknown",
                    propertyName: "Unknown",
                    classes: Array.Empty<EchonetClassSpecification>(),
                    superClassName: null
                ),
                @class: new(
                    isDefined: false,
                    code: classCode,
                    name: "Unknown",
                    propertyName: "Unknown"
                ),
                properties: Array.Empty<EchonetPropertySpecification>()
            );

        internal EchonetObjectSpecification(byte classGroupCode, byte classCode)
        {
            ClassGroup =
               SpecificationMaster.GetInstance().Profiles.FirstOrDefault(p => p.Code == classGroupCode) ??
               SpecificationMaster.GetInstance().DeviceClasses.FirstOrDefault(p => p.Code == classGroupCode) ??
               throw new ArgumentException($"unknown class group: 0x{classGroupCode:X2}");

            const int MaxNumberOfProperty = 0x80; // EPC: 0b_1XXX_XXXX (0x80~0xFF)

            var properties = new List<EchonetPropertySpecification>(capacity: MaxNumberOfProperty);

            //スーパークラスのプロパティを列挙
            using (var stream = SpecificationMaster.GetSpecificationMasterDataStream($"{ClassGroup.SuperClassName}.json"))
            {
                var superClassProperties = JsonSerializer.Deserialize<PropertyMaster>(stream) ?? throw new InvalidOperationException($"{nameof(PropertyMaster)} can not be null");
                properties.AddRange(superClassProperties.Properties);
            }

            Class = ClassGroup.Classes?.FirstOrDefault(c => c.IsDefined && c.Code == classCode)
                ?? throw new ArgumentException($"unknown class: 0x{classCode:X2}");

            if (Class.IsDefined)
            {
                var classGroupDirectoryName = $"0x{ClassGroup.Code:X2}-{ClassGroup.PropertyName}";
                var classFileName = $"0x{Class.Code:X2}-{Class.PropertyName}.json";

                //クラスのプロパティを列挙
                using (var stream = SpecificationMaster.GetSpecificationMasterDataStream(classGroupDirectoryName, classFileName))
                {
                    if (stream is not null)
                    {
                        var classProperties = JsonSerializer.Deserialize<PropertyMaster>(stream) ?? throw new InvalidOperationException($"{nameof(PropertyMaster)} can not be null");
                        properties.AddRange(classProperties.Properties);
                    }
                }
            }

            properties.TrimExcess(); // reduce capacity

            Properties = properties;
        }

        private EchonetObjectSpecification(
            EchonetClassGroupSpecification classGroup,
            EchonetClassSpecification @class,
            IReadOnlyList<EchonetPropertySpecification> properties
        )
        {
            ClassGroup = classGroup;
            Class = @class;
            Properties = properties;
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
