// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Smdn.Net.EchonetLite.Appendix
{
    internal class EchonetObject : IEchonetObject
    {
        public EchonetObject(byte classGroupCode, byte classCode)
        {
            ClassGroup =
               SpecificationMaster.GetInstance().プロファイル.FirstOrDefault(p => p.ClassGroupCode == classGroupCode) ??
               SpecificationMaster.GetInstance().機器.FirstOrDefault(p => p.ClassGroupCode == classGroupCode) ??
               throw new ArgumentException($"unknown class group: 0x{classGroupCode:X2}");

            var properties = new List<EchoProperty>();

            //スーパークラスのプロパティを列挙
            using (var stream = SpecificationMaster.GetSpecificationMasterDataStream($"{ClassGroup.SuperClass}.json"))
            {
                var superClassProperties = JsonSerializer.Deserialize<PropertyMaster>(stream) ?? throw new InvalidOperationException($"{nameof(PropertyMaster)} can not be null");
                properties.AddRange(superClassProperties.Properties);
            }

            Class = ClassGroup.ClassList?.FirstOrDefault(c => c.Status && c.ClassCode == classCode)
                ?? throw new ArgumentException($"unknown class: 0x{classCode:X2}");

            if (Class.Status)
            {
                var classGroupDirectoryName = $"0x{ClassGroup.ClassGroupCode:X2}-{ClassGroup.ClassGroupName}";
                var classFileName = $"0x{Class.ClassCode:X2}-{Class.ClassName}.json";

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
        public EchoClassGroup ClassGroup { get; }
        /// <summary>
        /// クラスコード
        /// </summary>
        public EchoClass Class { get; }

        /// <summary>
        /// 仕様上定義済みのプロパティの一覧
        /// </summary>
        internal IReadOnlyList<EchoProperty> Properties { get; }

        /// <summary>
        /// 仕様上定義済みのGETプロパティの一覧
        /// </summary>
        public IEnumerable<EchoProperty> GetProperties
        {
            get { return Properties.Where(static p => p.Get); }
        }
        /// <summary>
        /// 仕様上定義済みのSETプロパティの一覧
        /// </summary>
        public IEnumerable<EchoProperty> SetProperties
        {
            get { return Properties.Where(static p => p.Set); }
        }
        /// <summary>
        /// 仕様上定義済みのANNOプロパティの一覧
        /// </summary>
        public IEnumerable<EchoProperty> AnnoProperties
        {
            get { return Properties.Where(static p => p.Anno); }
        }
    }
}
