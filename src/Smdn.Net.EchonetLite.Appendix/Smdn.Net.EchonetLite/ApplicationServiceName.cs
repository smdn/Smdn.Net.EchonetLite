// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

using Smdn.Net.EchonetLite.Appendix;

namespace Smdn.Net.EchonetLite
{

    /// <summary>
    /// アプリケーションサービス
    /// </summary>
    /// <seealso href="https://echonet.jp/spec_g/">
    /// APPENDIX ECHONET機器オブジェクト詳細規定 Release R 第１章 本書の概要 表１アプリケーションサービスと「オプション必須」プロパティ表記記号一覧
    /// </seealso>
    [JsonConverter(typeof(ApplicationServiceNameJsonConverter))]
    public enum ApplicationServiceName
    {
        /// <summary>
        /// モバイルサービス(Mobile services)○M
        /// </summary>
        MobileServices,
        /// <summary>
        /// エネルギーサービス(Energy services)○E
        /// </summary>
        EnergyServices,
        /// <summary>
        /// 快適生活支援サービス(Home amenity services)○Ha
        /// </summary>
        HomeAmenityServices,
        /// <summary>
        /// ホームヘルスケアサービス(Home health-care services)○Hh
        /// </summary>
        HomeHealthcareServices,
        /// <summary>
        /// セキュリティサービス(Security services)○S
        /// </summary>
        SecurityServices,
        /// <summary>
        /// 機器リモートメンテナンスサービス(Remote appliance maintenance services)○R
        /// </summary>
        RemoteApplianceMaintenanceServices,
    }
}
