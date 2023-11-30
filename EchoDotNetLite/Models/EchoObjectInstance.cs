#nullable enable
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
            Properties = new NotifyChangeCollection<EchoObjectInstance,EchoPropertyInstance>(this);
            foreach (var prop in classObject.GetProperties)
            {
                Properties.Add(new EchoPropertyInstance(prop));
            }
            foreach (var prop in classObject.SetProperties)
            {
                Properties.Add(new EchoPropertyInstance(prop));
            }
            foreach (var prop in classObject.AnnoProperties)
            {
                Properties.Add(new EchoPropertyInstance(prop));
            }
        }

        /// <summary>
        /// EOJ
        /// </summary>
        /// <returns></returns>
        public EOJ GetEOJ()
        {
            return new EOJ
            (
                classGroupCode: Spec.ClassGroup.ClassGroupCode,
                classCode: Spec.Class.ClassCode,
                instanceCode: InstanceCode
            );
        }

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
        public bool IsPropertyMapGet { get; internal set; } = false;

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
        public ICollection<EchoPropertyInstance> Properties { get; }

        /// <summary>
        /// GETプロパティの一覧
        /// </summary>
        public IEnumerable<EchoPropertyInstance> GETProperties
        {
            get { return Properties.Where(static p => p.Spec.Get); }
        }
        /// <summary>
        /// SETプロパティの一覧
        /// </summary>
        public IEnumerable<EchoPropertyInstance> SETProperties
        {
            get { return Properties.Where(static p => p.Spec.Set); }
        }
        /// <summary>
        /// ANNOプロパティの一覧
        /// </summary>
        public IEnumerable<EchoPropertyInstance> ANNOProperties
        {
            get { return Properties.Where(static p => p.Spec.Anno); }
        }
    }
}
