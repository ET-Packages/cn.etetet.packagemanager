#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    [ETPackageMenu("库")]
    public class ETPackageHubModule : BasePackageToolModule
    {
        public static ETPackageHubModule Inst;

        [HideLabel]
        [HideIf("CheckUpdateAllEnd")]
        [ShowInInspector]
        [DisplayAsString(false, 100, TextAlignment.Center, true)]
        private static string m_CheckUpdateAllReqing = "请求所有包最新数据中...";

        [HideLabel]
        [ShowIf("RequestAllResult")]
        [ShowInInspector]
        private PackageHubSynthesis m_PackageHubSynthesis;

        public bool CheckUpdateAllEnd { get; private set; }

        public bool RequestAllResult { get; private set; }

        public override void Initialize()
        {
            CheckUpdateAllEnd = false;
            RequestAllResult  = false;

            PackageHelper.CheckUpdateAll((result) =>
            {
                if (!result)
                {
                    UnityTipsHelper.ShowError("获取所有包最新数据失败！");
                    return;
                }

                PackageHubHelper.CheckUpdate((result2) =>
                {
                    CheckUpdateAllEnd = true;
                    RequestAllResult  = result2;
                    if (!result2) return;
                    m_PackageHubSynthesis = new();
                    Inst                  = this;
                });
            });
        }

        public override void OnDestroy()
        {
            PackageHubHelper.SaveAsset();
            Inst = null;
        }
    }
}
#endif