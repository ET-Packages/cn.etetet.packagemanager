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

        [TableColumnWidth(60, Resizable = false)]
        [VerticalGroup("操作")]
        [Button(40, Icon = SdfIconType.ArrowRepeat, IconAlignment = IconAlignment.LeftOfText)]
        [HideIf("OperationState")]
        private void UpdateData()
        {
            OperationState = true;
            new PackageRequestTarget(PackageName, (info) =>
            {
                OperationState = false;
                RefreshInfo(info);
            });
        }

        [TableColumnWidth(60, Resizable = false)]
        [VerticalGroup("操作")]
        [Button(40, Icon = SdfIconType.X, IconAlignment = IconAlignment.LeftOfText)]
        [GUIColor(1f, 0.5f, 0.5f)]
        [ShowIf("ShowIfRemovePackage")]
        private void RemovePackage()
        {
            UnityTipsHelper.CallBackOk($"确定移除 {PackageName}", () =>
            {
                UnityTipsHelper.CallBackOk($"请到 Package Manager 中手动移除 {PackageName}",
                    () =>
                    {
                        PackageAuthor = null;
                        EditorApplication.ExecuteMenuItem("Window/Package Manager");
                        ETPackageAutoTool.CloseWindowRefresh();
                    });

                /* 测试无法移除 所以改其他方法
                EditorUtility.DisplayProgressBar("同步信息", $"移除 {PackageName}...", 0);
                OperationState = true;
                new PackageRequestRemove(PackageName, (result) =>
                {
                    m_Install = false;
                    EditorUtility.ClearProgressBar();
                    OperationState = false;
                    ETPackageAutoTool.CloseWindowRefresh();
                });
                */
            });
        }

        private bool ShowIfRemovePackage()
        {
            return Install && !OperationState;
        }

        [TableColumnWidth(60, Resizable = false)]
        [VerticalGroup("操作")]
        [Button(40, Icon = SdfIconType.ArrowDownShort, IconAlignment = IconAlignment.LeftOfText)]
        [GUIColor(0.4f, 0.8f, 1)]
        [HideIf("HideIfInstallPackage")]
        private void InstallPackage()
        {
            UnityTipsHelper.CallBackOk($"确定安装 {PackageName}", () =>
            {
                EditorUtility.DisplayProgressBar("同步信息", $"安装 {PackageName}...", 0);
                OperationState = true;
                new PackageRequestAdd(PackageName, (info) =>
                {
                    if (info != null)
                    {
                        m_Install             = true;
                        PackageCurrentVersion = info.version;
                    }

                    EditorUtility.ClearProgressBar();
                    OperationState = false;
                    ETPackageAutoTool.CloseWindowRefresh();
                });
            });
        }

        private bool HideIfInstallPackage()
        {
            return Install || OperationState;
        }

        [NonSerialized]
        [OdinSerialize]
        [HideInInspector]
        private bool m_Install;

        [HideInInspector]
        public bool Install => m_Install;

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
            PackageAuthor      = info.author.name;
            PackageDescription = info.description;
            PackageLastVersion = info.version;
            var currentVersion = PackageHelper.GetPackageCurrentVersion(PackageName);
            m_Install             = !string.IsNullOrEmpty(currentVersion);
            PackageCurrentVersion = m_Install ? currentVersion : "未安装";
            PackageCategory       = info.category;
        }

        public void InitRequestInfo()
        {
            if (string.IsNullOrEmpty(PackageAuthor))
            {
                UpdateData();
            }
        }
    }
}
#endif
