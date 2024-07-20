using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    [HideReferenceObjectPicker]
    public class PackageInfoData
    {
        [ReadOnly]
        [HideLabel]
        [VerticalGroup("信息")]
        [ShowInInspector]
        [PropertyOrder(-50)]
        public string Name { get; private set; }

        [ReadOnly]
        [HideLabel]
        [OnValueChanged("OnVersionChanged")]
        [VerticalGroup("信息")]
        [PropertyOrder(-45)]
        public string Version;

        private void OnVersionChanged()
        {
            Version        = Regex.Replace(Version, ETPackageVersionModule.Pattern, "");
            m_VersionValue = null;
        }

        public bool IsETPackage { get; set; }

        private int[] m_VersionValue;

        public int[] VersionValue
        {
            get
            {
                if (m_VersionValue == null)
                {
                    if (string.IsNullOrEmpty(Version))
                    {
                        Debug.LogError($"{Name} Version == null");
                        return null;
                    }

                    var versionSplit = Regex.Replace(Version, ETPackageVersionModule.Pattern, "").Split('.');
                    m_VersionValue = new int[versionSplit.Length];
                    for (int i = 0; i < versionSplit.Length; i++)
                    {
                        if (!int.TryParse(versionSplit[i], out m_VersionValue[i]))
                        {
                            Debug.LogError($"{versionSplit[i]}不是数字");
                        }
                    }
                }

                return m_VersionValue;
            }
        }

        [LabelText(" ")]
        [VerticalGroup("我依赖")]
        [ListDrawerSettings(DefaultExpandedState = true)]
        public List<DependencyInfo> Dependencies;

        [LabelText(" ")]
        [VerticalGroup("依赖我")]
        [ListDrawerSettings(DefaultExpandedState = true)]
        public List<DependencyInfo> DependenciesSelf;

        [Button("↓大")]
        [GUIColor(1f, 0.5f, 0.5f)]
        [VerticalGroup("信息")]
        [ButtonGroup("信息/更新版本")]
        public void UpdateVersion()
        {
            if (VersionValue.Length < 3) return;
            VersionValue[0]--;
            if (VersionValue[0] < 0)
            {
                VersionValue[0] = 0;
            }

            Version = string.Join(".", VersionValue);
        }

        [Button("↑大")]
        [GUIColor(1f, 0.65f, 0f)]
        [VerticalGroup("信息")]
        [ButtonGroup("信息/更新版本")]
        public void UpdateVersion2()
        {
            if (VersionValue.Length < 3) return;
            VersionValue[0]++;
            Version = string.Join(".", VersionValue);
        }

        [Button("↓中")]
        [GUIColor(1f, 0.5f, 0.5f)]
        [VerticalGroup("信息")]
        [ButtonGroup("信息/更新版本")]
        public void UpdateVersion3()
        {
            if (VersionValue.Length < 2) return;
            VersionValue[1]--;
            if (VersionValue[1] < 0)
            {
                VersionValue[1] = 0;
            }

            Version = string.Join(".", VersionValue);
        }

        [Button("↑中")]
        [GUIColor(1f, 1f, 0.5f)]
        [VerticalGroup("信息")]
        [ButtonGroup("信息/更新版本")]
        public void UpdateVersion4()
        {
            if (VersionValue.Length < 2) return;
            VersionValue[1]++;
            Version = string.Join(".", VersionValue);
        }

        [Button("↓小")]
        [GUIColor(1f, 0.5f, 0.5f)]
        [VerticalGroup("信息")]
        [ButtonGroup("信息/更新版本")]
        public void UpdateVersion5()
        {
            VersionValue[^1]--;
            if (VersionValue[^1] < 0)
            {
                VersionValue[^1] = 0;
            }

            Version = string.Join(".", VersionValue);
        }

        [Button("↑小")]
        [GUIColor(0.5f, 1f, 0.7f)]
        [VerticalGroup("信息")]
        [ButtonGroup("信息/更新版本")]
        public void UpdateVersion6()
        {
            VersionValue[^1]++;
            Version = string.Join(".", VersionValue);
        }

        [Button("重置")]
        [VerticalGroup("信息")]
        public void ResetVersion()
        {
            var packageInfo = ETPackageVersionModule.Inst.GetSourcePackageInfoData(Name);
            if (packageInfo == null)
            {
                return;
            }

            Version = packageInfo.Version;
        }

        [Button("请求")]
        [GUIColor(0f, 1f, 1f)]
        [VerticalGroup("信息")]
        [ShowIf("ShowIfReqVersion")]
        [ButtonGroup("信息/请求")]
        public void ReqVersion()
        {
            ReqCheckUpdate();
        }

        public bool ShowIfReqVersion()
        {
            return !IsBan &&IsETPackage && string.IsNullOrEmpty(LastVersion);
        }

        [Button("禁")]
        [GUIColor(1f, 0.3f, 0.3f)]
        [VerticalGroup("信息")]
        [ShowIf("ShowIfBanReqVersion")]
        [ButtonGroup("信息/请求")]
        public void BanReqVersion()
        {
            PackageHelper.BanPackage(Name);
            IsBan = true;
        }

        public bool ShowIfBanReqVersion()
        {
            return !IsBan && IsETPackage && string.IsNullOrEmpty(LastVersion);
        }

        [Button("解")]
        [GUIColor(0.3f, 1f, 0.3f)]
        [VerticalGroup("信息")]
        [ShowIf("ShowIfReBanReqVersion")]
        [ButtonGroup("信息/请求")]
        public void ReBanReqVersion()
        {
            PackageHelper.ReBanPackage(Name);
            IsBan = false;
        }

        public bool ShowIfReBanReqVersion()
        {
            return IsBan && IsETPackage && string.IsNullOrEmpty(LastVersion);
        }

        [GUIColor(0f, 1f, 0f)]
        [Button("有最新版本可更新", 50)]
        [VerticalGroup("信息")]
        [ShowIf("CanUpdateVersion")]
        public void CheckUpdateVersion()
        {
            UnityTipsHelper.CallBack($"{Name} 确定更新版本 {Version} >> {LastVersion}", UpdateDependencies);
        }

        public void UpdateDependencies()
        {
            var packagePath = Application.dataPath.Replace("Assets", "Packages") + "/" + Name;

            try
            {
                if (!System.IO.Directory.Exists(packagePath))
                {
                    return;
                }

                System.IO.Directory.Delete(packagePath, true);

                if (System.IO.Directory.Exists(packagePath))
                {
                    Debug.LogError("删除失败 文件还存在");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }

            foreach (var data in DependenciesSelf)
            {
                var packageInfo = ETPackageVersionModule.Inst.GetPackageInfoData(data.Name);
                if (packageInfo == null)
                {
                    return;
                }

                data.Version = LastVersion;

                foreach (var info in packageInfo.Dependencies)
                {
                    if (info.Name == Name)
                    {
                        info.Version = LastVersion;
                        break;
                    }
                }
            }

            ETPackageVersionModule.Inst.SyncPackageUpdate(Name, LastVersion);
        }

        public bool CanUpdateVersion { get; private set; }
        public bool IsBan            { get; private set; }

        [ReadOnly]
        [LabelText("最新版本")]
        [DisplayAsString(false, 20, TextAlignment.Left, true)]
        [VerticalGroup("信息")]
        [ShowInInspector]
        [PropertyOrder(-100)]
        private string DisplayLastVersion
        {
            get
            {
                return $"<color=#00FF00>{LastVersion}</color>";
            }
        }

        public string LastVersion { get; private set; }

        public PackageInfoData(string name, string version)
        {
            Name        = name;
            Version     = version;
            IsETPackage = name.Contains("cn.etetet.");
            ReqCheckUpdate();
        }

        private void ReqCheckUpdate()
        {
            if (!IsETPackage) return;

            IsBan = PackageHelper.IsBanPackage(Name);

            if (IsBan) return;

            PackageHelper.CheckUpdateTarget(Name, (lastVersion) =>
            {
                CanUpdateVersion = false;
                if (string.IsNullOrEmpty(lastVersion))
                {
                    return;
                }

                LastVersion = lastVersion;

                if (Version != lastVersion)
                {
                    CanUpdateVersion = true;
                }
            });
        }
    }

    public static class PackageInfoDataExtension
    {
        public static PackageInfoData Copy(this PackageInfoData data)
        {
            var copyData = new PackageInfoData(data.Name, data.Version)
            {
                Dependencies     = new(),
                DependenciesSelf = new()
            };

            if (data.Dependencies != null)
            {
                foreach (var dependency in data.Dependencies)
                {
                    copyData.Dependencies.Add(new DependencyInfo()
                    {
                        SelfName         = dependency.SelfName,
                        Name             = dependency.Name,
                        Version          = dependency.Version,
                        DependenciesSelf = dependency.DependenciesSelf,
                    });
                }
            }

            if (data.DependenciesSelf != null)
            {
                foreach (var dependency in data.DependenciesSelf)
                {
                    copyData.DependenciesSelf.Add(new DependencyInfo()
                    {
                        SelfName         = dependency.SelfName,
                        Name             = dependency.Name,
                        Version          = dependency.Version,
                        DependenciesSelf = dependency.DependenciesSelf,
                    });
                }
            }

            return copyData;
        }
    }
}