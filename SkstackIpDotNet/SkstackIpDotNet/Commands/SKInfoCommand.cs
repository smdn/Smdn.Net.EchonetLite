// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-License-Identifier: MIT
using SkstackIpDotNet.Responses;

namespace SkstackIpDotNet.Commands
{

    /// <summary>
    /// 現在の主要な通信設定値を表示します。
    /// </summary>
    internal class SKInfoCommand : AbstractSKCommand<EINFO>
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SKInfoCommand() : base("SKINFO")
        {

        }

        bool isResponseBodyReceived = false;
        bool isResponseCommandEndReceived = false;
        EINFO response = null;
        public override void ReceiveHandler(object sender, string eventRow)
        {
            base.ReceiveHandler(sender, eventRow);
            if (eventRow.StartsWith("EINFO"))
            {
                isResponseBodyReceived = true;
                response = new EINFO(eventRow);
            }
            else if (eventRow.StartsWith("OK"))
            {
                isResponseCommandEndReceived = true;
            }
            if (isResponseBodyReceived && isResponseCommandEndReceived)
            {
                TaskCompletionSource.SetResult(response);
            }
        }
    }
}
