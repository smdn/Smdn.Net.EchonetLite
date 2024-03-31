// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;

using Smdn.Net.EchonetLite.Appendix;

namespace Smdn.Net.EchonetLite
{
    /// <summary>
    /// ECHONET Liteノード
    /// </summary>
    public sealed class EchonetNode
    {
        public EchonetNode(IPAddress address, EchonetObject nodeProfile)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            NodeProfile = nodeProfile ?? throw new ArgumentNullException(nameof(nodeProfile));

            var devices = new ObservableCollection<EchonetObject>();

            devices.CollectionChanged += (_, e) => OnDevicesChanged(e);

            Devices = devices;
        }

        private void OnDevicesChanged(NotifyCollectionChangedEventArgs e)
        {
            DevicesChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 下位スタックのアドレス
        /// </summary>
        public IPAddress Address { get; }

        /// <summary>
        /// ノードプロファイルオブジェクト
        /// </summary>
        public EchonetObject NodeProfile { get; }

        /// <summary>
        /// 機器オブジェクトのリスト
        /// </summary>
        public ICollection<EchonetObject> Devices { get;  }

        /// <summary>
        /// 機器オブジェクトのリスト<see cref="Devices"/>に変更があったときに発生するイベント。
        /// </summary>
        /// <remarks>
        /// 現在のノードにECHONET Lite オブジェクトが追加・削除された際にイベントが発生します。
        /// 変更の詳細は、イベント引数<see cref="NotifyCollectionChangedEventArgs"/>を参照してください。
        /// </remarks>
        public event NotifyCollectionChangedEventHandler? DevicesChanged;
    }


    internal static class SpecificationUtil
    {
        public static EchonetPropertySpecification? FindProperty(byte classGroupCode, byte classCode, byte epc)
        {
            var @class = FindClass(classGroupCode, classCode);
            if (@class is not null)
            {
                EchonetPropertySpecification? property;
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
                classGroup: new
                (
                    code: classGroupCode,
                    name: "Unknown",
                    propertyName: "Unknown",
                    classes: Array.Empty<EchonetClassSpecification>(),
                    superClassName: null
                ),
                @class: new
                (
                    isDefined: false,
                    code: classCode,
                    name: "Unknown",
                    propertyName: "Unknown"
                )
            );
        }
        private class UnknownEchoObject : IEchonetObject
        {
            public UnknownEchoObject(EchonetClassGroupSpecification classGroup, EchonetClassSpecification @class)
            {
                ClassGroup = classGroup ?? throw new ArgumentNullException(nameof(classGroup));
                Class = @class ?? throw new ArgumentNullException(nameof(@class));
            }

            public EchonetClassGroupSpecification ClassGroup { get; }
            public EchonetClassSpecification Class { get; }

            public IEnumerable<EchonetPropertySpecification> GetProperties => Enumerable.Empty<EchonetPropertySpecification>();

            public IEnumerable<EchonetPropertySpecification> SetProperties => Enumerable.Empty<EchonetPropertySpecification>();

            public IEnumerable<EchonetPropertySpecification> AnnoProperties => Enumerable.Empty<EchonetPropertySpecification>();
        }

        public static IEchonetObject? FindClass(byte classGroupCode, byte classCode)
        {
            var profileClass = Profiles.All.FirstOrDefault(
                                g => g.ClassGroup.Code == classGroupCode
                                && g.Class.Code == classCode);
            if (profileClass != null)
            {
                return profileClass;
            }
            var deviceClass = DeviceClasses.All.FirstOrDefault(
                                g => g.ClassGroup.Code == classGroupCode
                                && g.Class.Code == classCode);
            if (deviceClass != null)
            {
                return deviceClass;
            }
            return null;
        }
    }
}
