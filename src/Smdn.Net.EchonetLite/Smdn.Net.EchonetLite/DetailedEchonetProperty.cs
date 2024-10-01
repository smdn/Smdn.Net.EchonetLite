// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.Net.EchonetLite.Appendix;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 詳細仕様が参照可能なECHONET プロパティを表すクラスです。
/// </summary>
internal sealed class DetailedEchonetProperty : EchonetProperty {
  public override byte Code => Spec.Code;
  public override bool CanSet => Spec.CanSet;
  public override bool CanGet => Spec.CanGet;
  public override bool CanAnnounceStatusChange => Spec.CanAnnounceStatusChange;

  /// <summary>
  /// このインスタンスが表すECHONET プロパティの詳細仕様を表す<see cref="EchonetPropertySpecification"/>。
  /// </summary>
  public EchonetPropertySpecification Spec { get; }

  internal DetailedEchonetProperty(
    EchonetObject device,
    EchonetPropertySpecification spec
  )
    : base(device)
  {
    Spec = spec ?? throw new ArgumentNullException(nameof(spec));
  }

  protected internal override bool IsAcceptableValue(ReadOnlySpan<byte> edt)
  {
    if (Spec.MaxSize < edt.Length)
      return false;

    if (Spec.MinSize > edt.Length)
      return false;

    return true;
  }
}
