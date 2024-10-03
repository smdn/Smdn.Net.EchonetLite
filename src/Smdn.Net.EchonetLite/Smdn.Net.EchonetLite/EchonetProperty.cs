// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Buffers;

using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.ComponentModel;
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
  /// このインスタンスのプロパティ値データ(EDT)に変更があった場合に発生するイベント。
  /// </summary>
  /// <remarks>
  /// このイベントは、プロパティに異なる値が設定された場合にのみ発生します。
  /// プロパティに値が設定される際、その値が以前と同じ値だった場合には発生しません。
  /// </remarks>
  /// <seealso cref="ValueMemory"/>
  /// <seealso cref="ValueSpan"/>
  public event EventHandler<(ReadOnlyMemory<byte> OldValue, ReadOnlyMemory<byte> NewValue)>? ValueChanged;

  /// <summary>
  /// このインスタンスが属するECHONETオブジェクトを表す<see cref="EchonetObject"/>を取得します。
  /// </summary>
  public abstract EchonetObject Device { get; }

  /// <summary>
  /// このインスタンスでイベントを発生させるために使用される<see cref="IEventInvoker"/>を取得します。
  /// </summary>
  /// <exception cref="InvalidOperationException"><see cref="IEventInvoker"/>を取得することができません。</exception>
  protected virtual IEventInvoker EventInvoker
    => Device.OwnerNode?.EventInvoker ?? throw new InvalidOperationException($"{nameof(EventInvoker)} can not be null.");

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
  /// <seealso cref="ValueChanged"/>
  public ReadOnlyMemory<byte> ValueMemory => value is null ? ReadOnlyMemory<byte>.Empty : value.WrittenMemory;

  /// <summary>
  /// プロパティ値データ(EDT)を表す<see cref="ReadOnlySpan{Byte}"/>を取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．９ ECHONET プロパティ値データ（EDT）
  /// </seealso>
  /// <seealso cref="ValueChanged"/>
  public ReadOnlySpan<byte> ValueSpan => value is null ? ReadOnlySpan<byte>.Empty : value.WrittenSpan;

  /// <summary>
  /// プロパティ値データ(EDT)を更新した時刻を表す<see cref="DateTimeOffset"/>を取得します。
  /// </summary>
  /// <remarks>
  /// このプロパティは、他ノードに属するECHONETオブジェクトのプロパティ値を更新した時刻を保持します。
  /// 具体的には、次の状況でプロパティ値を取得した時刻・通知された時刻を保持します。
  /// <list type="bullet">
  ///   <item>他ノードに属するECHONETオブジェクトに対するプロパティ値の読み出し・通知要求に対する応答</item>
  ///   <item>他ノードに属するECHONETオブジェクトからのプロパティ値通知</item>
  /// </list>
  /// </remarks>
  public DateTimeOffset LastUpdatedTime { get; private set; }

  /// <summary>
  /// コンストラクタ。
  /// </summary>
  /// <remarks>
  /// このコンストラクタはテスト目的で公開されています。　コードから直接使用することを意図したものではありません。
  /// </remarks>
  protected /* private protected */ EchonetProperty()
  {
  }

  /// <summary>
  /// プロパティ値を設定します。
  /// </summary>
  /// <param name="newValue">プロパティ値として設定する値を表す<see cref="ReadOnlyMemory{Byte}"/>。</param>
  /// <param name="raiseValueChangedEvent">値が変更された場合に<see cref="ValueChanged"/>イベントを発生させるかどうかを指定する<see cref="bool"/>値。</param>
  /// <param name="setLastUpdatedTime"><see cref="LastUpdatedTime"/>を更新するかどうかを指定する<see cref="bool"/>値。</param>
  /// <remarks>
  /// <paramref name="raiseValueChangedEvent"/>に<see langword="true"/>が指定された場合は、<paramref name="newValue"/>が以前の値と異なる場合にのみ、イベント<see cref="ValueChanged"/>を発生させます。
  /// 新たに設定される値に変更がない場合は、イベントは発生しません。
  /// </remarks>
  /// <seealso cref="ValueChanged"/>
  public void SetValue(
    ReadOnlyMemory<byte> newValue,
    bool raiseValueChangedEvent = false,
    bool setLastUpdatedTime = false
  )
    => WriteValue(
      esv: default,
      tid: default,
      write: writer => writer.Write(newValue.Span),
      newValueSize: newValue.Length,
      raiseValueChangedEvent: raiseValueChangedEvent,
      setLastUpdatedTime: setLastUpdatedTime
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
  /// <remarks>
  /// このメソッドでは、プロパティ値の更新後に<see cref="LastUpdatedTime"/>の値も更新します。
  /// また、<paramref name="newValue"/>が以前の値と異なる場合、イベント<see cref="ValueChanged"/>を発生させます。
  /// </remarks>
  /// <seealso cref="LastUpdatedTime"/>
  /// <seealso cref="ValueChanged"/>
  internal void SetValue(ESV esv, ushort tid, PropertyValue newValue)
    => WriteValue(
      esv: esv == default ? throw new InvalidOperationException("invalid ESV") : esv,
      tid: tid,
      write: writer => writer.Write(newValue.EDT.Span),
      newValueSize: newValue.PDC,
      raiseValueChangedEvent: true,
      setLastUpdatedTime: true
    );

  /// <summary>
  /// プロパティ値を書き込みます。
  /// </summary>
  /// <param name="write">
  /// プロパティ値を書き込むための<see cref="Action{T}"/>デリゲート。
  /// 引数で渡される<see cref="IBufferWriter{Byte}"/>を介してプロパティ値として設定する内容を書き込んでください。
  /// </param>
  /// <param name="raiseValueChangedEvent">値が変更された場合に<see cref="ValueChanged"/>イベントを発生させるかどうかを指定する<see cref="bool"/>値。</param>
  /// <param name="setLastUpdatedTime"><see cref="LastUpdatedTime"/>を更新するかどうかを指定する<see cref="bool"/>値。</param>
  /// <exception cref="ArgumentNullException"><paramref name="write"/>が<see langword="null"/>です。</exception>
  /// <remarks>
  /// <paramref name="raiseValueChangedEvent"/>に<see langword="true"/>が指定された場合は、<paramref name="write"/>によって設定される値が以前の値と異なる場合にのみ、イベント<see cref="ValueChanged"/>を発生させます。
  /// 新たに設定される値に変更がない場合は、イベントは発生しません。
  /// </remarks>
  /// <seealso cref="ValueChanged"/>
  public void WriteValue(
    Action<IBufferWriter<byte>> write,
    bool raiseValueChangedEvent = false,
    bool setLastUpdatedTime = false
  )
    => WriteValue(
      esv: default,
      tid: default,
      write: write ?? throw new ArgumentNullException(nameof(write)),
      newValueSize: 0,
      raiseValueChangedEvent: raiseValueChangedEvent,
      setLastUpdatedTime: setLastUpdatedTime
    );

  /// <summary>
  /// プロパティ値を書き込みます。
  /// また、書き込みによって値が変更された場合に、イベント<see cref="ValueChanged"/>が発生させます。
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
  /// <param name="raiseValueChangedEvent"><see cref="ValueChanged"/>イベントを発生させるかどうかを指定する<see cref="bool"/>値。</param>
  /// <param name="setLastUpdatedTime"><see cref="LastUpdatedTime"/>を更新するかどうかを指定する<see cref="bool"/>値。</param>
  /// <exception cref="ArgumentNullException"><paramref name="write"/>が<see langword="null"/>です。</exception>
  /// <seealso cref="ValueChanged"/>
  private void WriteValue(
    ESV esv,
    ushort tid,
    Action<IBufferWriter<byte>> write,
    int newValueSize,
    bool raiseValueChangedEvent,
    bool setLastUpdatedTime
  )
  {
    var valueChangedHandlers = ValueChanged;
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

      write(value);

      if (setLastUpdatedTime) {
        LastUpdatedTime =
#if SYSTEM_TIMEPROVIDER
          TimeProvider.GetLocalNow();
#else
          DateTimeOffset.Now;
#endif
      }

      if (esv != default) {
        Device.OwnerNode?.Owner?.Logger?.LogDebug(
          "Property value changed (ESV: {ESV}, TID: {TID:X4}, Node: {Node}, Object: {Object}, EPC: {EPC:X2})",
          esv,
          tid,
          Device.Node.Address,
          Device.EOJ,
          Code
        );
      }

      if (!raiseValueChangedEvent)
        return;

      if (valueChangedHandlers is null)
        return;

      // 値が新規に設定される場合、または以前の値から変更がある場合はValueChangedイベントを起こす
      if (oldValue is null || !oldValue.AsSpan(0, oldValueLength).SequenceEqual(value.WrittenSpan)) {
        var oldValueCopy = oldValue is null ? Array.Empty<byte>() : oldValue.AsSpan(0, oldValueLength).ToArray();
        var newValueMemory = value.WrittenMemory;

        EventInvoker.InvokeEvent(this, valueChangedHandlers, e: (oldValueCopy, newValueMemory));
      }
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
}
