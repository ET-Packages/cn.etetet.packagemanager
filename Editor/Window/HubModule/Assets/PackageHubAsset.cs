using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace ET.PackageManager.Editor
{
    public class PackageHubAsset : SerializedScriptableObject
    {
        public List<PackageHubData> AllPackages = new();
    }
}