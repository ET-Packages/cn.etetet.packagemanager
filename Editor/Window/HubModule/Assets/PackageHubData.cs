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

        [NonSerialized]
        [OdinSerialize]
        [HideInInspector]
        public string PackageAuthorURL;

        [HideLabel]
        [TableColumnWidth(30, Resizable = false)]
        [VerticalGroup("连接", -999)]
        [Button(30, Icon = SdfIconType.PersonFill, IconAlignment = IconAlignment.LeftOfText)]
        [ShowIf("ShowIfAuthorURL")]
        public void OpenPackageAuthorURL()
        {
            if (string.IsNullOrEmpty(PackageAuthorURL)) return;
            Application.OpenURL(PackageAuthorURL);
        }

        private bool ShowIfAuthorURL()
        {
            return !string.IsNullOrEmpty(PackageAuthorURL);
        }

        [NonSerialized]
        [OdinSerialize]
        [HideInInspector]
        public string PackageRepositoryURL;

        [HideLabel]
        [TableColumnWidth(30, Resizable = false)]
        [VerticalGroup("连接", -999)]
        [Button(30, Icon = SdfIconType.Github, IconAlignment = IconAlignment.LeftOfText)]
        [ShowIf("ShowIfRepositoryURL")]
        public void OpenPackageRepositoryURL()
        {
            if (string.IsNullOrEmpty(PackageRepositoryURL)) return;
            Application.OpenURL(PackageRepositoryURL);
        }

        private bool ShowIfRepositoryURL()
        {
            return !string.IsNullOrEmpty(PackageRepositoryURL);
        }

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

        [HideLabel]
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

        [HideLabel]
        [GUIColor("GetRemoveColor")]
        [TableColumnWidth(60, Resizable = false)]
        [VerticalGroup("操作")]
        [Button(40, Icon = SdfIconType.X, IconAlignment = IconAlignment.LeftOfText)]
        [ShowIf("ShowIfRemovePackage")]
        private void RemovePackage()
        {
            UnityTipsHelper.CallBackOk($"确定移除 {PackageName}", () =>
            {
                EditorUtility.DisplayProgressBar("同步信息", $"移除 {PackageName}...", 0);
                OperationState = true;
                new PackageRequestRemove(PackageName, (result) =>
                {
                    OperationState = false;
                    EditorUtility.ClearProgressBar();
                    if (!result) return;
                    m_Install = false;
                    ETPackageAutoTool.CloseWindowRefresh();
                });
            });
        }

        private bool ShowIfRemovePackage()
        {
            return Install && !OperationState;
        }

        [NonSerialized]
        [OdinSerialize]
        [HideInInspector]
        private bool m_CanRemove;

        private Color GetRemoveColor()
        {
            return m_CanRemove ? Color.HSVToRGB(0f, 0.5f, 1) : Color.HSVToRGB(0f, 0f, 0.5f);
        }

        [HideLabel]
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
        private static string m_CheckUpdateAllReqing = "操作中";

        public void RefreshInfo(UnityEditor.PackageManager.PackageInfo info)
        {
            if (info == null) return;
            PackageAuthor    = info.author?.name ?? "";
            PackageAuthorURL = info.author?.url ?? "";
            if (info.repository != null)
            {
                var url = info.repository.url ?? "";
                if (string.IsNullOrEmpty(url))
                {
                    PackageRepositoryURL = "";
                }
                else
                {
                    var http      = "http";
                    var httpIndex = url.IndexOf(http, StringComparison.Ordinal);
                    if (httpIndex > 0)
                    {
                        PackageRepositoryURL = url.Substring(httpIndex);
                    }
                    else
                    {
                        PackageRepositoryURL = "";
                    }
                }
            }
            else
            {
                PackageRepositoryURL = "";
            }

            PackageDescription = info.description;
            PackageLastVersion = info.version;
            var currentVersion = PackageHelper.GetPackageCurrentVersion(PackageName);
            m_Install             = !string.IsNullOrEmpty(currentVersion);
            PackageCurrentVersion = m_Install ? currentVersion : "未安装";
            m_CanRemove           = PackageHubHelper.CheckRemove(PackageName);

            if (string.IsNullOrEmpty(info.category))
            {
                var currentInfo = PackageHelper.GetPackageInfo(PackageName);
                PackageCategory = currentInfo == null ? "" : currentInfo.category;
            }
            else
            {
                PackageCategory = info.category;
            }
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
