#if ODIN_INSPECTOR
using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

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

        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelText("作者")]
        public string PackageAuthor;

        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelText("描述")]
        public string PackageDescription;

        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelText("最新版本")]
        public string PackageLastVersion;

        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelText("当前版本")]
        public string PackageCurrentVersion;

        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelText("类别")]
        public string PackageCategory;

        [Button("请求数据")]
        private void UpdateData()
        {
            new PackageRequestTarget(this.PackageName, this.RefreshInfo);
        }

        public void RefreshInfo(UnityEditor.PackageManager.PackageInfo info)
        {
            if (info == null) return;
            this.PackageAuthor      = info.author.name;
            this.PackageDescription = info.description;
            this.PackageLastVersion = info.version;
            var currentVersion = PackageHelper.GetPackageCurrentVersion(this.PackageName);
            this.PackageCurrentVersion = string.IsNullOrEmpty(currentVersion) ? "未安装" : currentVersion;
            this.PackageCategory       = info.category;

            if (PackageHelper.CurrentRegisteredPackages.ContainsKey(this.PackageName))
            {
                this.PackageCategory = PackageHelper.CurrentRegisteredPackages[this.PackageName].category;
            }
        }
    }
}
#endif