using EchoDotNetLite.Common;
using EchoDotNetLite.Specifications;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace EchoDotNetLite.Models
{
    /// <summary>
    /// ECHONET Liteノード
    /// </summary>
    public class EchoNode: INotifyCollectionChanged<EchoObjectInstance>
    {
        public EchoNode()
        {
            Devices = new NotifyChangeCollection<EchoNode,EchoObjectInstance>(this);
        }
        /// <summary>
        /// 下位スタックのアドレス
        /// </summary>
        public IPAddress Address { get; set;}

        /// <summary>
        /// ノードプロファイルオブジェクト
        /// </summary>
        public EchoObjectInstance NodeProfile { get; set; }

        /// <summary>
        /// 機器オブジェクトのリスト
        /// </summary>
        public ICollection<EchoObjectInstance> Devices { get;  }

        /// <summary>
        /// イベント オブジェクトインスタンス増減通知
        /// </summary>
        public event EventHandler<(CollectionChangeType, EchoObjectInstance)> OnCollectionChanged;

        public void RaiseCollectionChanged(CollectionChangeType type, EchoObjectInstance item)
        {
            OnCollectionChanged?.Invoke(this, (type, item));
        }
    }


    public static class SpecificationUtil
    {
        public static Specifications.EchoProperty FindProperty(byte classGroupCode, byte classCode, byte epc)
        {
            var @class = FindClass(classGroupCode, classCode);
            if (@class != null)
            {
                Specifications.EchoProperty property;
                 property = @class.AnnoProperties.FirstOrDefault(p => p.Code == epc);
                if (property != null)
                {
                    return property;
                }
                property = @class.GetProperties.FirstOrDefault(p => p.Code == epc);
                if (property != null)
                {
                    return property;
                }
                property = @class.SetProperties.FirstOrDefault(p => p.Code == epc);
                if (property != null)
                {
                    return property;
                }
            }
            return null;
        }

        internal static IEchonetObject GenerateUnknownClass(byte classGroupCode, byte classCode)
        {
            return new UnknownEchoObject()
            {
                ClassGroup = new EchoClassGroup
                (
                    classGroupCode: classGroupCode,
                    classGroupName: "Unknown",
                    classGroupNameOfficial: "Unknown",
                    classList: Array.Empty<EchoClass>(),
                    superClass: null
                ),
                Class = new EchoClass
                (
                    classCode: classCode,
                    className: "Unknown",
                    classNameOfficial: "Unknown",
                    status: false
                )
            };
        }
        private class UnknownEchoObject : IEchonetObject
        {
            public EchoClassGroup ClassGroup { get; set; }
            public EchoClass Class { get; set; }

            public IEnumerable<EchoProperty> GetProperties => Enumerable.Empty<EchoProperty>();

            public IEnumerable<EchoProperty> SetProperties => Enumerable.Empty<EchoProperty>();

            public IEnumerable<EchoProperty> AnnoProperties => Enumerable.Empty<EchoProperty>();
        }

        public static Specifications.IEchonetObject FindClass(byte classGroupCode, byte classCode)
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
}
