using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace ET.PackageManager.Editor
{
    [Serializable]
    [HideReferenceObjectPicker]
    public class PackageHubData
    {
        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelText("包名")]
        public string PackageName;

        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelText("累计下载")]
        public int DownloadValue;
    }
}