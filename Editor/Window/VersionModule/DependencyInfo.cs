﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    [HideLabel]
    [HideReferenceObjectPicker]
    public class DependencyInfo
    {
        [ReadOnly]
        [LabelText("名称")]
        public string Name;

        [ReadOnly]
        [LabelText("版本")]
        [OnValueChanged("OnVersionChanged")]
        public string Version;

        [HideInInspector]
        public string SelfName;

        [HideInInspector]
        public bool DependenciesSelf;

        private void OnVersionChanged()
        {
            Version = Regex.Replace(Version, ETPackageVersionModule.Pattern, "");
        }

        #region 我依赖的版本不是最新时

        [Button("同步")]
        [GUIColor(0.7f, 0.4f, 0.8f)]
        [ShowIf("SyncVersionShowIf")]
        public void SyncVersion()
        {
            if (string.IsNullOrEmpty(Name))
            {
                return;
            }

            var packageInfo = ETPackageVersionModule.Inst.GetPackageInfoData(Name);
            if (packageInfo == null)
            {
                return;
            }

            Version = packageInfo.Version;

            if (!ETPackageVersionModule.Inst.SyncDependency) return;

            foreach (var info in packageInfo.DependenciesSelf)
            {
                if (info.Name == SelfName)
                {
                    info.Version = Version;
                    break;
                }
            }
        }

        //同步目标的版本 当目标name的版本与当前版本不一致时显示同步按钮
        private bool SyncVersionShowIf()
        {
            if (DependenciesSelf)
            {
                return false;
            }

            if (string.IsNullOrEmpty(Name))
            {
                return false;
            }

            var packageInfo = ETPackageVersionModule.Inst.GetPackageInfoData(Name);
            if (packageInfo == null)
            {
                return false;
            }

            return packageInfo.Version != Version;
        }

        #endregion

        #region 依赖我的版本不是最新时

        [Button("同步")]
        [GUIColor(0.4f, 0.8f, 1)]
        [ShowIf("SyncSelfVersionShowIf")]
        public void SyncSelfVersion()
        {
            if (string.IsNullOrEmpty(SelfName))
            {
                return;
            }

            var packageInfo = ETPackageVersionModule.Inst.GetPackageInfoData(SelfName);
            if (packageInfo == null)
            {
                return;
            }

            Version = packageInfo.Version;

            if (!ETPackageVersionModule.Inst.SyncDependency) return;

            packageInfo = ETPackageVersionModule.Inst.GetPackageInfoData(Name);
            if (packageInfo == null)
            {
                return;
            }

            foreach (var info in packageInfo.Dependencies)
            {
                if (info.Name == SelfName)
                {
                    info.Version = Version;
                    break;
                }
            }
        }

        //依赖我  但是我的版本已经升级了 他哪里的版本还是旧的时候显示同步按钮
        private bool SyncSelfVersionShowIf()
        {
            if (!DependenciesSelf)
            {
                return false;
            }

            var packageInfo = ETPackageVersionModule.Inst.GetPackageInfoData(SelfName);
            if (packageInfo == null)
            {
                return false;
            }

            return packageInfo.Version != Version;
        }

        #endregion
    }
}
