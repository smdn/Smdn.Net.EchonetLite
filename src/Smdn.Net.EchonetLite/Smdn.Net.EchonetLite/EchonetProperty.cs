// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;

using Smdn.Net.EchonetLite.Appendix;

namespace Smdn.Net.EchonetLite;


/// <summary>
/// プロパティクラス
/// </summary>
public sealed class EchonetProperty {
  public EchonetProperty(
    byte classGroupCode,
    byte classCode,
    byte epc
  )
    : this(
      classGroupCode: classGroupCode,
      classCode: classCode,
      epc: epc,
      canAnnounceStatusChange: false,
      canSet: false,
      canGet: false
    )
  {
  }

  public EchonetProperty(
    byte classGroupCode,
    byte classCode,
    byte epc,
    bool canAnnounceStatusChange,
    bool canSet,
    bool canGet
  )
    : this(
      spec: DeviceClasses.LookupOrCreateProperty(classGroupCode, classCode, epc, includeProfiles: true),
      canAnnounceStatusChange: canAnnounceStatusChange,
      canSet: canSet,
      canGet: canGet
    )
  {
  }

  public EchonetProperty(EchonetPropertySpecification spec)
    : this(
      spec: spec,
      canAnnounceStatusChange: false,
      canSet: false,
      canGet: false
    )
  {
  }

  public EchonetProperty(
    EchonetPropertySpecification spec,
    bool canAnnounceStatusChange,
    bool canSet,
    bool canGet
  )
  {
    Spec = spec ?? throw new ArgumentNullException(nameof(spec));
    CanAnnounceStatusChange = canAnnounceStatusChange;
    CanSet = canSet;
    CanGet = canGet;
  }

  /// <summary>
  /// EPC
  /// ECHONET機器オブジェクト詳細規定がある場合、詳細仕様
  /// </summary>
  public EchonetPropertySpecification Spec { get; }

  /// <summary>
  /// プロパティ値の読み出し・通知要求のサービスを処理する。
  /// プロパティ値読み出し要求(0x62)、プロパティ値書き込み・読み出し要求(0x6E)、プロパティ値通知要求(0x63)の要求受付処理を実施する。
  /// </summary>
  public bool CanGet { get; }

  /// <summary>
  /// プロパティ値の書き込み要求のサービスを処理する。
  /// プロパティ値書き込み要求(応答不要)(0x60)、プロパティ値書き込み要求(応答要)(0x61)、プロパティ値書き込み・読み出し要求(0x6E)の要求受付処理を実施する。
  /// </summary>
  public bool CanSet { get; }

  /// <summary>
  /// プロパティ値の通知要求のサービスを処理する。
  /// プロパティ値通知要求（0x63）の要求受付処理を実施する。
  /// </summary>
  public bool CanAnnounceStatusChange { get; }

  private ArrayBufferWriter<byte>? _value = null;

  /// <summary>
  /// プロパティ値を表す<see cref="ReadOnlyMemory{Byte}"/>を取得します。
  /// </summary>
  public ReadOnlyMemory<byte> ValueMemory => _value is null ? ReadOnlyMemory<byte>.Empty : _value.WrittenMemory;

  /// <summary>
  /// プロパティ値を表す<see cref="ReadOnlySpan{Byte}"/>を取得します。
  /// </summary>
  public ReadOnlySpan<byte> ValueSpan => _value is null ? ReadOnlySpan<byte>.Empty : _value.WrittenSpan;

  /// <summary>
  /// プロパティ値に変更があった場合に発生するイベント。
  /// </summary>
  /// <remarks>
  /// このイベントは、プロパティに異なる値が設定された場合にのみ発生します。
  /// プロパティに値が設定される際、その値が以前と同じ値だった場合には発生しません。
  /// </remarks>
  public event EventHandler<(ReadOnlyMemory<byte> OldValue, ReadOnlyMemory<byte> NewValue)>? ValueChanged;

  /// <summary>
  /// プロパティ値を設定します。
  /// </summary>
  /// <remarks>
  /// 設定によって値が変更された場合は、イベント<see cref="ValueChanged"/>が発生します。
  /// </remarks>
  /// <param name="newValue">プロパティ値として設定する値を表す<see cref="ReadOnlyMemory{Byte}"/>。</param>
  /// <seealso cref="ValueChanged"/>
  public void SetValue(ReadOnlyMemory<byte> newValue)
    => WriteValue(writer => writer.Write(newValue.Span), newValueSize: newValue.Length);

  /// <summary>
  /// プロパティ値を書き込みます。
  /// </summary>
  /// <remarks>
  /// 書き込みによって値が変更された場合は、イベント<see cref="ValueChanged"/>が発生します。
  /// </remarks>
  /// <param name="write">
  /// プロパティ値を書き込むための<see cref="Action{T}"/>デリゲート。
  /// 引数で渡される<see cref="IBufferWriter{Byte}"/>を介してプロパティ値として設定する内容を書き込んでください。
  /// </param>
  /// <exception cref="ArgumentNullException"><paramref name="write"/>が<see langword="null"/>です。</exception>
  /// <seealso cref="ValueChanged"/>
  public void WriteValue(Action<IBufferWriter<byte>> write)
    => WriteValue(write ?? throw new ArgumentNullException(nameof(write)), newValueSize: 0);

  private void WriteValue(Action<IBufferWriter<byte>> write, int newValueSize)
  {
    var valueChangedHandlers = ValueChanged;
    byte[]? oldValue = null;

    try {
      var oldValueLength = 0;

      if (_value is null) {
        var initialCapacity = 0 < newValueSize ? newValueSize : 8; // TODO: best initial capacity

        _value = new(initialCapacity);
      }
      else {
        oldValueLength = _value.WrittenSpan.Length;

        oldValue = ArrayPool<byte>.Shared.Rent(oldValueLength);

        _value.WrittenSpan.CopyTo(oldValue.AsSpan(0, oldValueLength));

#if SYSTEM_BUFFERS_ARRAYBUFFERWRITER_RESETWRITTENCOUNT
        _value.ResetWrittenCount();
#else
        _value.Clear();
#endif
      }

      write(_value);

      if (valueChangedHandlers is not null) {
        // 値が新規に設定される場合、以前の値から変更がある場合はValueChangedイベントを起こす
        if (oldValue is null || !oldValue.AsSpan(0, oldValueLength).SequenceEqual(_value.WrittenSpan)) {
          var oldValueMemory = oldValue is null ? ReadOnlyMemory<byte>.Empty : oldValue.AsMemory(0, oldValueLength);
          var newValueMemory = _value.WrittenMemory;

          valueChangedHandlers.Invoke(this, (oldValueMemory, newValueMemory));
        }
      }
    }
    finally {
      if (oldValue is not null)
        ArrayPool<byte>.Shared.Return(oldValue);
    }
  }
}
