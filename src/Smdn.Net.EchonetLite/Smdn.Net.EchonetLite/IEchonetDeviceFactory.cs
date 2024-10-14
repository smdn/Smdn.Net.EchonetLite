// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite;

/// <summary>
/// 指定されたコードに対応する機器オブジェクトを作成するための機能を提供するためのインターフェイスです。
/// このインターフェイスは、他ノードの機器オブジェクトを独自に定義したクラスとして型付けして扱う場合に使用します。
/// </summary>
/// <seealso cref="EchonetDevice"/>
/// <seealso cref="EchonetOtherNode.Devices"/>
/// <seealso cref="ObjectModel.DeviceSuperClass"/>
public interface IEchonetDeviceFactory {
  /// <summary>
  /// 指定されたコードに対応する機器オブジェクトを表す<see cref="EchonetDevice"/>を作成します。
  /// </summary>
  /// <param name="classGroupCode">作成する機器オブジェクトのクラスグループコード。</param>
  /// <param name="classCode">作成する機器オブジェクトのクラスコード。</param>
  /// <param name="instanceCode">作成する機器オブジェクトのインスタンスコード。</param>
  /// <returns>作成した<see cref="EchonetDevice"/>インスタンス。</returns>
  EchonetDevice? Create(
    byte classGroupCode,
    byte classCode,
    byte instanceCode
  );
}
