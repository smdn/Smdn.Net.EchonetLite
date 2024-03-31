// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.Protocol
{
    public enum EHD1 : byte
    {
        //図 ３-２ EHD1 詳細規定
        //プロトコル種別
        //1* * * :従来のECHONET規格
        //0001:ECHONET Lite規格
        EchonetLite = 0x10,
        //0000:使用不可
        //その他:future reserved
    }
}
