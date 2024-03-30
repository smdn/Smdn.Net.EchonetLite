// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace EchoDotNetLite.Specifications
{

    /// <summary>
    /// アプリケーションサービス
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApplicationService
    {
        /// <summary>
        /// (Mobile services)○M
        /// </summary>
        モバイルサービス,
        /// <summary>
        /// (Energy services)○E
        /// </summary>
        エネルギーサービス,
        /// <summary>
        /// (Home amenity services)○Ha
        /// </summary>
        快適生活支援サービス,
        /// <summary>
        /// (Home health-care services)○Hh
        /// </summary>
        ホームヘルスケアサービス,
        /// <summary>
        /// (Security services)○S
        /// </summary>
        セキュリティサービス,
        /// <summary>
        /// (Remote appliance maintenance services)○R
        /// </summary>
        機器リモートメンテナンスサービス,
    }
}
