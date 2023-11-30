﻿using EchoDotNetLite.Common;
using EchoDotNetLite.Specifications;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace EchoDotNetLite.Models
{
#nullable enable
    /// <summary>
    /// ECHONET Liteノード
    /// </summary>
    public sealed class EchoNode: INotifyCollectionChanged<EchoObjectInstance>
    {
        public EchoNode(IPAddress address, EchoObjectInstance nodeProfile)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            NodeProfile = nodeProfile ?? throw new ArgumentNullException(nameof(nodeProfile));
            Devices = new NotifyChangeCollection<EchoNode,EchoObjectInstance>(this);
        }
        /// <summary>
        /// 下位スタックのアドレス
        /// </summary>
        public IPAddress Address { get; }

        /// <summary>
        /// ノードプロファイルオブジェクト
        /// </summary>
        public EchoObjectInstance NodeProfile { get; }

        /// <summary>
        /// 機器オブジェクトのリスト
        /// </summary>
        public ICollection<EchoObjectInstance> Devices { get;  }

        /// <summary>
        /// イベント オブジェクトインスタンス増減通知
        /// </summary>
        public event EventHandler<(CollectionChangeType, EchoObjectInstance)>? OnCollectionChanged;

        void INotifyCollectionChanged<EchoObjectInstance>.RaiseCollectionChanged(CollectionChangeType type, EchoObjectInstance item)
        {
            OnCollectionChanged?.Invoke(this, (type, item));
        }
    }


    public static class SpecificationUtil
    {
        public static Specifications.EchoProperty? FindProperty(byte classGroupCode, byte classCode, byte epc)
        {
            var @class = FindClass(classGroupCode, classCode);
            if (@class is not null)
            {
                Specifications.EchoProperty? property;
                 property = @class.AnnoProperties.FirstOrDefault(p => p.Code == epc);
                if (property is not null)
                {
                    return property;
                }
                property = @class.GetProperties.FirstOrDefault(p => p.Code == epc);
                if (property is not null)
                {
                    return property;
                }
                property = @class.SetProperties.FirstOrDefault(p => p.Code == epc);
                if (property is not null)
                {
                    return property;
                }
            }
            return null;
        }

        internal static IEchonetObject GenerateUnknownClass(byte classGroupCode, byte classCode)
        {
            return new UnknownEchoObject
            (
                classGroup: new EchoClassGroup
                (
                    classGroupCode: classGroupCode,
                    classGroupName: "Unknown",
                    classGroupNameOfficial: "Unknown",
                    classList: Array.Empty<EchoClass>(),
                    superClass: null
                ),
                @class: new EchoClass
                (
                    classCode: classCode,
                    className: "Unknown",
                    classNameOfficial: "Unknown",
                    status: false
                )
            );
        }
        private class UnknownEchoObject : IEchonetObject
        {
            public UnknownEchoObject(EchoClassGroup classGroup, EchoClass @class)
            {
                ClassGroup = classGroup ?? throw new ArgumentNullException(nameof(classGroup));
                Class = @class ?? throw new ArgumentNullException(nameof(@class));
            }

            public EchoClassGroup ClassGroup { get; }
            public EchoClass Class { get; }

            public IEnumerable<EchoProperty> GetProperties => Enumerable.Empty<EchoProperty>();

            public IEnumerable<EchoProperty> SetProperties => Enumerable.Empty<EchoProperty>();

            public IEnumerable<EchoProperty> AnnoProperties => Enumerable.Empty<EchoProperty>();
        }

        public static Specifications.IEchonetObject? FindClass(byte classGroupCode, byte classCode)
        {
            var profileClass = Specifications.プロファイル.クラス一覧.FirstOrDefault(
                                g => g.ClassGroup.ClassGroupCode == classGroupCode
                                && g.Class.ClassCode == classCode);
            if (profileClass != null)
            {
                return profileClass;
            }
            var deviceClass = Specifications.機器.クラス一覧.FirstOrDefault(
                                g => g.ClassGroup.ClassGroupCode == classGroupCode
                                && g.Class.ClassCode == classCode);
            if (deviceClass != null)
            {
                return deviceClass;
            }
            return null;
        }
    }
#nullable restore
}
