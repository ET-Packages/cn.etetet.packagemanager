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
        public override void Initialize()
        {
        }

        public override void OnDestroy()
        {
        }
    }
}
#endif