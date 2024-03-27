// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using EchoDotNetLite.Common;
using EchoDotNetLite.Specifications;

namespace EchoDotNetLite.Models
{

    /// <summary>
    /// ECHONET Lite オブジェクトインスタンス
    /// </summary>
#pragma warning disable CA1708
    public sealed class EchoObjectInstance
#pragma warning restore CA1708
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

            properties = new();

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

            properties.CollectionChanged += (_, e) => OnPropertiesChanged(e);
        }

        private void OnPropertiesChanged(NotifyCollectionChangedEventArgs e)
        {
            PropertiesChanged?.Invoke(this, e);

#pragma warning disable CS0618
            // translate event args to raise obsolete OnCollectionChanged event
            var handler = OnCollectionChanged;

            if (handler is null)
                return;

            if (e.TryGetAddedItem<EchoPropertyInstance>(out var addedProperty))
                handler(this, (CollectionChangeType.Add, addedProperty));

            if (e.TryGetRemovedItem<EchoPropertyInstance>(out var removedProperty))
                handler(this, (CollectionChangeType.Remove, removedProperty));
#pragma warning restore CS0618
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
        /// プロパティの一覧<see cref="Properties"/>に変更があったときに発生するイベント。
        /// </summary>
        /// <remarks>
        /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)などによって
        /// 現在のオブジェクトにECHONET Lite プロパティが追加・削除された際にイベントが発生します。
        /// 変更の詳細は、イベント引数<see cref="NotifyCollectionChangedEventArgs"/>を参照してください。
        /// </remarks>
        public event NotifyCollectionChangedEventHandler? PropertiesChanged;

        /// <summary>
        /// イベント オブジェクトインスタンス増減通知
        /// </summary>
        [Obsolete($"Use {nameof(PropertiesChanged)} instead.")]
        public event EventHandler<(CollectionChangeType, EchoPropertyInstance)>? OnCollectionChanged;

        /// <summary>
        /// プロパティマップ取得状態
        /// </summary>
        /// <seealso cref="EchoClient.PropertyMapAcquiring"/>
        /// <seealso cref="EchoClient.PropertyMapAcquired"/>
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

        private readonly ObservableCollection<EchoPropertyInstance> properties;

        /// <summary>
        /// GETプロパティの一覧
        /// </summary>
#pragma warning disable CA1708
        public IEnumerable<EchoPropertyInstance> GetProperties => Properties.Where(static p => p.Spec.Get);

        [Obsolete($"Use {nameof(GetProperties)} instead.")]
        public IEnumerable<EchoPropertyInstance> GETProperties => GetProperties;
#pragma warning restore CA1708

        /// <summary>
        /// SETプロパティの一覧
        /// </summary>
#pragma warning disable CA1708
        public IEnumerable<EchoPropertyInstance> SetProperties => Properties.Where(static p => p.Spec.Set);

        [Obsolete($"Use {nameof(SetProperties)} instead.")]
        public IEnumerable<EchoPropertyInstance> SETProperties => SetProperties;
#pragma warning restore CA1708

        /// <summary>
        /// ANNOプロパティの一覧
        /// </summary>
#pragma warning disable CA1708
        public IEnumerable<EchoPropertyInstance> AnnoProperties => Properties.Where(static p => p.Spec.Anno);

        [Obsolete($"Use {nameof(AnnoProperties)} instead.")]
        public IEnumerable<EchoPropertyInstance> ANNOProperties => AnnoProperties;
#pragma warning restore CA1708
    }
}
