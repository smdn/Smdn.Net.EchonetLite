// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;

using Smdn.Net.EchonetLite.Appendix;

namespace Smdn.Net.EchonetLite
{
    /// <summary>
    /// ECHONET Lite クラスグループ定義
    /// プロファイルクラスグループ
    /// </summary>
    public static class Profiles
    {
        /// <summary>
        /// 0xF0 ノードプロファイル
        /// </summary>
        public static EchonetObjectSpecification NodeProfile { get; } = new(0x0E, 0xF0);

        /// <summary>
        /// クラス一覧
        /// </summary>
        public static IReadOnlyList<EchonetObjectSpecification> All { get; } =
        [
            NodeProfile,
        ];
    }
}
