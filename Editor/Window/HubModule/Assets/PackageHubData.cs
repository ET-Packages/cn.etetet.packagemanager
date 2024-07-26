#if ODIN_INSPECTOR
using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    [Serializable]
    [HideReferenceObjectPicker]
    public class PackageHubData
    {
        [TableColumnWidth(250, Resizable = false)]
        [VerticalGroup("信息")]
        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelWidth(70)]
        [LabelText("包名")]
        public string PackageName;

        [TableColumnWidth(250, Resizable = false)]
        [VerticalGroup("信息")]
        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelWidth(70)]
        [LabelText("作者")]
        public string PackageAuthor;

        [TableColumnWidth(250, Resizable = false)]
        [VerticalGroup("信息")]
        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelWidth(70)]
        [LabelText("累计下载")]
        public int DownloadValue;

        [TextArea]
        [VerticalGroup("描述")]
        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [HideLabel]
        public string PackageDescription;

        [LabelWidth(70)]
        [TableColumnWidth(150, Resizable = false)]
        [VerticalGroup("版本")]
        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelText("当前版本")]
        public string PackageCurrentVersion;

        [LabelWidth(70)]
        [TableColumnWidth(150, Resizable = false)]
        [VerticalGroup("版本")]
        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelText("最新版本")]
        public string PackageLastVersion;

        [LabelWidth(70)]
        [TableColumnWidth(150, Resizable = false)]
        [VerticalGroup("版本")]
        [NonSerialized]
        [OdinSerialize]
        [ReadOnly]
        [LabelText("类别")]
        public string PackageCategory;

        [TableColumnWidth(80, Resizable = false)]
        [VerticalGroup("操作")]
        [Button(60, Icon = SdfIconType.ArrowRepeat, IconAlignment = IconAlignment.LeftOfText)]
        [ShowIf("ShowIfUpdateData")]
        private void UpdateData()
        {
            this.OperationState = true;
            new PackageRequestTarget(this.PackageName, (info) =>
            {
                this.OperationState = false;
                this.RefreshInfo(info);
            });
        }

        private bool ShowIfUpdateData()
        {
            return Install && !this.OperationState;
        }

        [TableColumnWidth(80, Resizable = false)]
        [VerticalGroup("操作")]
        [Button(60, Icon = SdfIconType.ArrowDownCircleFill, IconAlignment = IconAlignment.LeftOfText)]
        [GUIColor(0.4f, 0.8f, 1)]
        [HideIf("HideIfInstallPackage")]
        private void InstallPackage()
        {
            EditorUtility.DisplayProgressBar("同步信息", $"安装 {this.PackageName}...", 0);
            this.OperationState = true;
            new PackageRequestAdd(this.PackageName, (info) =>
            {
                if (info != null)
                {
                    this.PackageCurrentVersion = info.version;
                }

                EditorUtility.ClearProgressBar();
                this.OperationState = false;
                ETPackageAutoTool.CloseWindowRefresh();
            });
        }

        private bool HideIfInstallPackage()
        {
            return Install || this.OperationState;
        }

        [NonSerialized]
        [OdinSerialize]
        [HideInInspector]
        private bool m_Install;

        [HideInInspector]
        public bool Install => this.m_Install;

        public bool OperationState { get; set; }

        [VerticalGroup("操作")]
        [HideLabel]
        [ShowIf("OperationState")]
        [ShowInInspector]
        [TextArea]
        [DisplayAsString(false, 15, TextAlignment.Center, true)]
        private static string m_CheckUpdateAllReqing = "操作中...";

        public void RefreshInfo(UnityEditor.PackageManager.PackageInfo info)
        {
            if (info == null) return;
            this.PackageAuthor      = info.author.name;
            this.PackageDescription = info.description;
            this.PackageLastVersion = info.version;
            var currentVersion = PackageHelper.GetPackageCurrentVersion(this.PackageName);
            this.m_Install             = !string.IsNullOrEmpty(currentVersion);
            this.PackageCurrentVersion = this.m_Install ? currentVersion : "未安装";
            this.PackageCategory       = info.category;
        }
    }
}
#endif