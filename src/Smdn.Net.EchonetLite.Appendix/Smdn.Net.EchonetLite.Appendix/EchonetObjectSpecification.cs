// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Smdn.Net.EchonetLite.Appendix
{
    internal class EchonetObjectSpecification : IEchonetObject
    {
        public EchonetObjectSpecification(byte classGroupCode, byte classCode)
        {
            ClassGroup =
               SpecificationMaster.GetInstance().プロファイル.FirstOrDefault(p => p.Code == classGroupCode) ??
               SpecificationMaster.GetInstance().機器.FirstOrDefault(p => p.Code == classGroupCode) ??
               throw new ArgumentException($"unknown class group: 0x{classGroupCode:X2}");

            var properties = new List<EchonetPropertySpecification>();

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

            Properties = properties;
        }
        /// <summary>
        /// クラスグループコード
        /// </summary>
        public EchonetClassGroupSpecification ClassGroup { get; }
        /// <summary>
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
            get { return Properties.Where(static p => p.Get); }
        }
        /// <summary>
        /// 仕様上定義済みのSETプロパティの一覧
        /// </summary>
        public IEnumerable<EchonetPropertySpecification> SetProperties
        {
            get { return Properties.Where(static p => p.Set); }
        }
        /// <summary>
        /// 仕様上定義済みのANNOプロパティの一覧
        /// </summary>
        public IEnumerable<EchonetPropertySpecification> AnnoProperties
        {
            get { return Properties.Where(static p => p.Anno); }
        }
    }
}
