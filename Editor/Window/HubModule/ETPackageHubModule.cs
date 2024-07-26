#if ODIN_INSPECTOR
using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    [ETPackageMenu("库")]
    public class ETPackageHubModule : BasePackageToolModule
    {
        public static ETPackageHubModule Inst;

        public bool CheckUpdateAllEnd { get; private set; }

        public bool RequestAllResult { get; private set; }

        public override void Initialize()
        {
            CheckUpdateAllEnd = false;
            RequestAllResult  = false;
            PackageHubHelper.CheckUpdate((result) =>
            {
                CheckUpdateAllEnd = true;
                RequestAllResult  = result;
                if (!result) return;

                Inst = this;
            });
        }

        public override void OnDestroy()
        {
            Inst = null;
        }
    }
}
#endif