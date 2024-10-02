// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 詳細仕様が参照可能なECHONET プロパティを表すクラスです。
/// </summary>
internal sealed class DetailedEchonetProperty : EchonetProperty {
  public override byte Code => Detail.Code;
  public override bool CanSet => Detail.CanSet;
  public override bool CanGet => Detail.CanGet;
  public override bool CanAnnounceStatusChange => Detail.CanAnnounceStatusChange;

  /// <summary>
  /// このインスタンスが表すECHONET プロパティの詳細仕様を表す<see cref="IEchonetPropertySpecification"/>。
  /// </summary>
  public IEchonetPropertySpecification Detail { get; }

  internal DetailedEchonetProperty(
    EchonetObject device,
    IEchonetPropertySpecification propertyDetail
  )
    : base(device)
  {
    Detail = propertyDetail ?? throw new ArgumentNullException(nameof(propertyDetail));
  }

  protected internal override bool IsAcceptableValue(ReadOnlySpan<byte> edt)
    => Detail.IsAcceptableValue(edt);
}
