using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EchoDotNetLite.Common;
using EchoDotNetLite.Specifications;

namespace EchoDotNetLite.Models
{

    /// <summary>
    /// ECHONET Lite オブジェクトインスタンス
    /// </summary>
    public sealed class EchoObjectInstance: INotifyCollectionChanged<EchoPropertyInstance>
    {
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public EchoObjectInstance(EOJ eoj)
            : this
            (
                classObject:
                    SpecificationUtil.FindClass(eoj.ClassGroupCode, eoj.ClassCode) ??
                    SpecificationUtil.GenerateUnknownClass(eoj.ClassGroupCode, eoj.ClassCode),
                instanceCode: eoj.InstanceCode
            )
        {
        }

        /// <summary>
        /// スペック指定のコンストラクタ
        /// プロパティは仕様から取得する
        /// </summary>
        /// <param name="classObject">オブジェクトクラス</param>
        /// <param name="instanceCode"></param>
        public EchoObjectInstance(IEchonetObject classObject,byte instanceCode)
        {
            Spec = classObject ?? throw new ArgumentNullException(nameof(classObject));
            InstanceCode = instanceCode;

            properties = new(this);

            foreach (var prop in classObject.GetProperties)
            {
                properties.Add(new EchoPropertyInstance(prop));
            }
            foreach (var prop in classObject.SetProperties)
            {
                properties.Add(new EchoPropertyInstance(prop));
            }
            foreach (var prop in classObject.AnnoProperties)
            {
                properties.Add(new EchoPropertyInstance(prop));
            }
        }

        internal void AddProperty(EchoPropertyInstance prop)
            => properties.Add(prop);

        internal void ResetProperties(IEnumerable<EchoPropertyInstance> props)
        {
            properties.Clear();

            foreach (var prop in props)
            {
                properties.Add(prop);
            }
        }

        /// <summary>
        /// EOJ
        /// </summary>
        public EOJ EOJ => new
        (
            classGroupCode: Spec.ClassGroup.ClassGroupCode,
            classCode: Spec.Class.ClassCode,
            instanceCode: InstanceCode
        );

        /// <summary>
        /// イベント プロパティインスタンス増減通知
        /// </summary>
        public event EventHandler<(CollectionChangeType, EchoPropertyInstance)>? OnCollectionChanged;

        void INotifyCollectionChanged<EchoPropertyInstance>.RaiseCollectionChanged(CollectionChangeType type, EchoPropertyInstance item)
        {
            OnCollectionChanged?.Invoke(this, (type, item));
        }

        /// <summary>
        /// プロパティマップ取得状態
        /// </summary>
        public bool HasPropertyMapAcquired { get; internal set; } = false;

        [Obsolete($"Use {nameof(HasPropertyMapAcquired)} instead.")]
        public bool IsPropertyMapGet => HasPropertyMapAcquired;

        /// <summary>
        /// クラスグループコード、クラスグループ名
        /// ECHONET機器オブジェクト詳細規定がある場合、詳細仕様
        /// </summary>
        public Specifications.IEchonetObject Spec { get; }
        /// <summary>
        /// インスタンスコード
        /// </summary>
        public byte InstanceCode { get; }

        /// <summary>
        /// プロパティの一覧
        /// </summary>
        public IReadOnlyCollection<EchoPropertyInstance> Properties => properties;

        private readonly NotifyChangeCollection<EchoObjectInstance, EchoPropertyInstance> properties;

        /// <summary>
        /// GETプロパティの一覧
        /// </summary>
        public IEnumerable<EchoPropertyInstance> GetProperties => Properties.Where(static p => p.Spec.Get);

        [Obsolete($"Use {nameof(GetProperties)} instead.")]
        public IEnumerable<EchoPropertyInstance> GETProperties => GetProperties;

        /// <summary>
        /// SETプロパティの一覧
        /// </summary>
        public IEnumerable<EchoPropertyInstance> SetProperties => Properties.Where(static p => p.Spec.Set);

        [Obsolete($"Use {nameof(SetProperties)} instead.")]
        public IEnumerable<EchoPropertyInstance> SETProperties => SetProperties;

        /// <summary>
        /// ANNOプロパティの一覧
        /// </summary>
        public IEnumerable<EchoPropertyInstance> AnnoProperties => Properties.Where(static p => p.Spec.Anno);

        [Obsolete($"Use {nameof(AnnoProperties)} instead.")]
        public IEnumerable<EchoPropertyInstance> ANNOProperties => AnnoProperties;

    }
}
