// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Buffers;

using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET オブジェクトのプロパティを表す抽象クラスです。
/// このクラスでは、プロパティのコード(EPC)、プロパティ値データ(EDT)、およびプロパティのアクセスルール(Get/Set/Anno)のみを規定します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ２．１ 基本的な考え方
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．７ ECHONET プロパティ（EPC）
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．９ ECHONET プロパティ値データ（EDT）
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．２．５ アクセスルール
/// </seealso>
public abstract class EchonetProperty {
  /// <summary>
  /// このインスタンスのプロパティ値データ(EDT)が更新された場合に発生するイベント。
  /// </summary>
  /// <remarks>
  /// イベント引数には、更新前と更新後の<seealso cref="ValueMemory"/>の値が設定されます。
  /// このイベントは、プロパティに更新された値が以前と同じ値であっても発生します。
  /// </remarks>
  /// <seealso cref="ValueMemory"/>
  /// <seealso cref="ValueSpan"/>
  /// <seealso cref="EchonetPropertyValueUpdatedEventArgs"/>
  public event EventHandler<EchonetPropertyValueUpdatedEventArgs>? ValueUpdated;

  /// <summary>
  /// このインスタンスが属するECHONETオブジェクトを表す<see cref="EchonetObject"/>を取得します。
  /// </summary>
  public abstract EchonetObject Device { get; }

#if SYSTEM_TIMEPROVIDER
  /// <summary>
  /// <see cref="LastUpdatedTime"/>に設定する時刻の取得元となる<see cref="TimeProvider"/>を取得します。
  /// </summary>
  protected virtual TimeProvider TimeProvider
    => Device.OwnerNode?.Owner?.TimeProvider ?? TimeProvider.System;
#endif

  /// <summary>
  /// プロパティ値を保持するバッファとなる<see cref="IBufferWriter{T}"/> 。
  /// </summary>
  private ArrayBufferWriter<byte>? value;

  /// <summary>
  /// ECHONETプロパティを規定するコード(EPC)を表す<see cref="byte"/>型の値を返します。
  /// </summary>
  public abstract byte Code { get; }

  /// <summary>
  /// ECHONETプロパティが<c>Set</c>アクセス可能であるかどうかを表す<see cref="bool"/>型の値を返します。
  /// このアクセスルールでは、プロパティ値の書き込み要求のサービスを処理します。
  /// </summary>
  /// <remarks>
  /// <c>Set</c>アクセスが可能な場合、プロパティ値書き込み要求(応答不要)(0x60)、プロパティ値書き込み要求(応答要)(0x61)、プロパティ値書き込み・読み出し要求(0x6E)の要求受付処理を実施します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．２．５ アクセスルール
  /// </seealso>
  public abstract bool CanSet { get; }

  /// <summary>
  /// ECHONETプロパティが<c>Get</c>アクセス可能であるかどうかを表す<see cref="bool"/>型の値を返します。
  /// このアクセスルールでは、プロパティ値の読み出し・通知要求のサービスを処理します。
  /// </summary>
  /// <remarks>
  /// <c>Get</c>アクセスが可能な場合、プロパティ値読み出し要求(0x62)、プロパティ値書き込み・読み出し要求(0x6E)、プロパティ値通知要求(0x63)の要求受付処理を実施します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．２．５ アクセスルール
  /// </seealso>
  public abstract bool CanGet { get; }

  /// <summary>
  /// ECHONETプロパティが<c>Anno</c>アクセス可能であるかどうかを表す<see cref="bool"/>型の値を返します。
  /// このアクセスルールでは、プロパティ値の通知要求のサービスを処理します。
  /// </summary>
  /// <remarks>
  /// <c>Anno</c>アクセスが可能な場合、プロパティ値通知要求（0x63）の要求受付処理を実施します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．２．５ アクセスルール
  /// </seealso>
  public abstract bool CanAnnounceStatusChange { get; }

  /// <summary>
  /// プロパティ値データ(EDT)を表す<see cref="ReadOnlyMemory{Byte}"/>を取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．９ ECHONET プロパティ値データ（EDT）
  /// </seealso>
  /// <seealso cref="ValueUpdated"/>
  public ReadOnlyMemory<byte> ValueMemory => value is null ? ReadOnlyMemory<byte>.Empty : value.WrittenMemory;

  /// <summary>
  /// プロパティ値データ(EDT)を表す<see cref="ReadOnlySpan{Byte}"/>を取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．９ ECHONET プロパティ値データ（EDT）
  /// </seealso>
  /// <seealso cref="ValueUpdated"/>
  public ReadOnlySpan<byte> ValueSpan => value is null ? ReadOnlySpan<byte>.Empty : value.WrittenSpan;

  /// <summary>
  /// プロパティ値データ(EDT)を更新した時刻を表す<see cref="DateTime"/>を取得します。
  /// </summary>
  /// <remarks>
  /// このプロパティは、他ノードに属するECHONETオブジェクトのプロパティ値を更新した時刻(ローカル時刻)を保持します。
  /// 具体的には、次の状況でプロパティ値を取得した時刻・通知された時刻を保持します。
  /// <list type="bullet">
  ///   <item>他ノードに属するECHONETオブジェクトに対するプロパティ値の読み出し・通知要求に対する応答</item>
  ///   <item>他ノードに属するECHONETオブジェクトからのプロパティ値通知</item>
  /// </list>
  /// </remarks>
  public DateTime LastUpdatedTime { get; private set; }

  /// <summary>
  /// プロパティ値データ(EDT)が変更されているかどうかを表す<see cref="bool"/>型の値を返します。
  /// </summary>
  /// <value>
  /// <see cref="SetValue(ReadOnlyMemory{byte}, bool, bool)"/>などのメソッドによってプロパティ値データが変更されているが、
  /// その変更が未送信である場合、または、変更されたプロパティ値の送信を要求したが、要求が受理されなかった場合は<see langword="true"/> 。
  /// プロパティ値が変更されていない場合、あるいは変更したプロパティ値の送信が受理されている場合は<see langword="false"/> 。
  /// </value>
  /// <seealso cref="SetValue(ReadOnlyMemory{byte}, bool, bool)"/>
  /// <seealso cref="WriteValue(Action{IBufferWriter{byte}}, bool, bool)"/>
  /// <seealso cref="EchonetClient.RequestWriteAsync"/>
  /// <seealso cref="EchonetClient.RequestWriteOneWayAsync"/>
  /// <seealso cref="EchonetClient.RequestWriteReadAsync"/>
  public bool HasModified { get; private set; }

  /// <summary>
  /// コンストラクタ。
  /// </summary>
  private protected EchonetProperty()
  {
  }

  /// <summary>
  /// プロパティのアクセスルールを設定します。
  /// このメソッドはECHONET Lite サービスによって取得・通知されたプロパティマップを反映するために使用します。
  /// </summary>
  /// <param name="canSet"><c>Set</c>アクセス可能であるかどうかを設定する値を指定します。</param>
  /// <param name="canGet"><c>Get</c>アクセス可能であるかどうかを設定する値を指定します。</param>
  /// <param name="canAnnounceStatusChange"><c>Anno</c>アクセス可能であるかどうかを設定する値を指定します。</param>
  protected internal abstract void UpdateAccessRule(
    bool canSet,
    bool canGet,
    bool canAnnounceStatusChange
  );

  /// <summary>
  /// プロパティ値を設定します。
  /// </summary>
  /// <param name="newValue">プロパティ値として設定する値を表す<see cref="ReadOnlyMemory{Byte}"/>。</param>
  /// <param name="raiseValueUpdatedEvent">
  /// <see cref="ValueUpdated"/>イベントを発生させるかどうかを指定する<see cref="bool"/>値。
  /// </param>
  /// <param name="setLastUpdatedTime"><see cref="LastUpdatedTime"/>を更新するかどうかを指定する<see cref="bool"/>値。</param>
  /// <seealso cref="ValueUpdated"/>
  public void SetValue(
    ReadOnlyMemory<byte> newValue,
    bool raiseValueUpdatedEvent = false,
    bool setLastUpdatedTime = false
  )
    => WriteValue(
      esv: default,
      tid: default,
      write: writer => writer.Write(newValue.Span),
      newValueSize: newValue.Length,
      raiseValueUpdatedEvent: raiseValueUpdatedEvent,
      setLastUpdatedTime: setLastUpdatedTime,
      newModificationState: true
    );

  /// <summary>
  /// プロパティ値を設定します。
  /// このメソッドはECHONET Lite サービスによって取得・通知されたプロパティ値を反映するために使用します。
  /// </summary>
  /// <param name="esv">
  /// このプロパティ値を設定する契機となったECHONET Lite サービスを表す<see cref="ESV"/>。
  /// </param>
  /// <param name="tid">
  /// このプロパティ値を設定する契機となったECHONET Lite フレームのトランザクションIDを表す<see cref="ushort"/>。
  /// </param>
  /// <param name="newValue">
  /// プロパティ値として設定する値を表す<see cref="PropertyValue"/>。
  /// </param>
  /// <param name="newModificationState">
  /// <see cref="HasModified"/>に値を設定する場合は<see langword="true"/>または<see langword="false"/>、
  /// そのままにする場合は<see langword="null"/>。
  /// </param>
  /// <remarks>
  /// このメソッドでは、プロパティ値の更新後に<see cref="LastUpdatedTime"/>の値も更新します。
  /// また、イベント<see cref="ValueUpdated"/>を発生させます。
  /// </remarks>
  /// <seealso cref="LastUpdatedTime"/>
  /// <seealso cref="ValueUpdated"/>
  /// <seealso cref="HasModified"/>
  internal void SetValue(
    ESV esv,
    ushort tid,
    PropertyValue newValue,
    bool? newModificationState
  )
    => WriteValue(
      esv: esv == default ? throw new InvalidOperationException("invalid ESV") : esv,
      tid: tid,
      write: writer => writer.Write(newValue.EDT.Span),
      newValueSize: newValue.PDC,
      raiseValueUpdatedEvent: true,
      setLastUpdatedTime: true, // TODO: switch by esv
      newModificationState: newModificationState
    );

  /// <summary>
  /// プロパティ値を書き込みます。
  /// </summary>
  /// <param name="write">
  /// プロパティ値を書き込むための<see cref="Action{T}"/>デリゲート。
  /// 引数で渡される<see cref="IBufferWriter{Byte}"/>を介してプロパティ値として設定する内容を書き込んでください。
  /// </param>
  /// <param name="raiseValueUpdatedEvent">
  /// <see cref="ValueUpdated"/>イベントを発生させるかどうかを指定する<see cref="bool"/>値。
  /// </param>
  /// <param name="setLastUpdatedTime"><see cref="LastUpdatedTime"/>を更新するかどうかを指定する<see cref="bool"/>値。</param>
  /// <exception cref="ArgumentNullException"><paramref name="write"/>が<see langword="null"/>です。</exception>
  /// <seealso cref="ValueUpdated"/>
  public void WriteValue(
    Action<IBufferWriter<byte>> write,
    bool raiseValueUpdatedEvent = false,
    bool setLastUpdatedTime = false
  )
    => WriteValue(
      esv: default,
      tid: default,
      write: write ?? throw new ArgumentNullException(nameof(write)),
      newValueSize: 0,
      raiseValueUpdatedEvent: raiseValueUpdatedEvent,
      setLastUpdatedTime: setLastUpdatedTime,
      newModificationState: true
    );

  /// <summary>
  /// プロパティ値を書き込みます。
  /// また、書き込みによって値が変更された場合に、イベント<see cref="ValueUpdated"/>を発生させます。
  /// </summary>
  /// <param name="esv">
  /// このプロパティ値を設定する契機となったECHONET Lite サービスを表す<see cref="ESV"/>。
  /// もしくは<see langword="default"/>。
  /// </param>
  /// <param name="tid">
  /// このプロパティ値を設定する契機となったECHONET Lite フレームのトランザクションIDを表す<see cref="ushort"/>。
  /// もしくは<see langword="default"/>。
  /// </param>
  /// <param name="write">
  /// プロパティ値を書き込むための<see cref="Action{T}"/>デリゲート。
  /// 引数で渡される<see cref="IBufferWriter{Byte}"/>を介してプロパティ値として設定する内容を書き込んでください。
  /// </param>
  /// <param name="newValueSize">
  /// プロパティに書き込む値のサイズ。
  /// 書き込みによってバッファが確保される場合に、初期容量として確保するサイズとしてこの値を使用します。
  /// <c>0</c>を指定した場合は、デフォルトのサイズが初期容量として確保されます。
  /// </param>
  /// <param name="raiseValueUpdatedEvent"><see cref="ValueUpdated"/>イベントを発生させるかどうかを指定する<see cref="bool"/>値。</param>
  /// <param name="setLastUpdatedTime"><see cref="LastUpdatedTime"/>を更新するかどうかを指定する<see cref="bool"/>値。</param>
  /// <param name="newModificationState">
  /// <see cref="HasModified"/>に値を設定する場合は<see langword="true"/>または<see langword="false"/>、
  /// そのままにする場合は<see langword="null"/>。
  /// </param>
  /// <exception cref="ArgumentNullException"><paramref name="write"/>が<see langword="null"/>です。</exception>
  /// <seealso cref="ValueUpdated"/>
  private void WriteValue(
    ESV esv,
    ushort tid,
    Action<IBufferWriter<byte>> write,
    int newValueSize,
    bool raiseValueUpdatedEvent,
    bool setLastUpdatedTime,
    bool? newModificationState
  )
  {
    byte[]? oldValue = null;

    try {
      var oldValueLength = 0;

      if (value is null) {
        var initialCapacity = 0 < newValueSize ? newValueSize : 8; // TODO: best initial capacity

        value = new(initialCapacity);
      }
      else {
        oldValueLength = value.WrittenSpan.Length;

        oldValue = ArrayPool<byte>.Shared.Rent(oldValueLength);

        value.WrittenSpan.CopyTo(oldValue.AsSpan(0, oldValueLength));

#if SYSTEM_BUFFERS_ARRAYBUFFERWRITER_RESETWRITTENCOUNT
        value.ResetWrittenCount();
#else
        value.Clear();
#endif
      }

      try {
        write(value);
      }
      catch (Exception ex) {
        Device.OwnerNode?.Owner?.Logger?.LogError(
          ex,
          "Failed to set property value (ESV: {ESV}, TID: {TID:X4}, Node: {Node}, Object: {Object}, EPC: {EPC:X2})",
          esv.ToSymbolString(),
          tid,
          Device.Node.Address,
          Device.EOJ,
          Code
        );

        throw;
      }

      if (newModificationState.HasValue)
        HasModified = newModificationState.Value;

      var previousUpdatedTime = LastUpdatedTime;

      if (setLastUpdatedTime) {
        LastUpdatedTime =
#if SYSTEM_TIMEPROVIDER
          TimeProvider.GetLocalNow().LocalDateTime;
#else
          DateTime.Now;
#endif
      }

      if (esv != default) {
        Device.OwnerNode?.Owner?.Logger?.LogDebug(
          "Property value changed (ESV: {ESV}, TID: {TID:X4}, Node: {Node}, Object: {Object}, EPC: {EPC:X2})",
          esv.ToSymbolString(),
          tid,
          Device.Node.Address,
          Device.EOJ,
          Code
        );
      }

      if (!raiseValueUpdatedEvent)
        return;

      Device.RaisePropertyValueUpdated(
        property: this,
        valueUpdatedEventHandler: ValueUpdated,
        oldValue: oldValue.AsSpan(0, oldValueLength),
        previousUpdatedTime: previousUpdatedTime
      );
    }
    finally {
      if (oldValue is not null)
        ArrayPool<byte>.Shared.Return(oldValue);
    }
  }

  /// <summary>
  /// プロパティ値が詳細仕様で定められている値となっているかどうかを検証し、その結果を返します。
  /// </summary>
  /// <remarks>
  /// 既定の実装では、すべての値が詳細仕様と適合すると判断されます。
  /// </remarks>
  /// <param name="edt">
  /// 受信したECHONET プロパティ値データ(EDT)を表す<see cref="ReadOnlySpan{Byte}"/>。　このインスタンスが表すECHONET プロパティの値として適合するかどうか検証される値を表します。
  /// </param>
  /// <returns>
  /// <paramref name="edt"/>が詳細仕様で定められているサイズ・値域などに適合すると判断される場合は<see langword="true"/>、そうでなければ<see langword="false"/>。
  /// </returns>
  protected internal virtual bool IsAcceptableValue(ReadOnlySpan<byte> edt) => true;

  public override string ToString()
    => $"{GetType().FullName} (Code: 0x{Code:X2}, Value: {ValueMemory.ToHexString()})";
}
