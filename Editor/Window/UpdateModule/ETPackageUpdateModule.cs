﻿#if ODIN_INSPECTOR
using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    [ETPackageMenu("更新")]
    public class ETPackageUpdateModule : BasePackageToolModule
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