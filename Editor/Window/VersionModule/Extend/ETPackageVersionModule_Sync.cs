﻿#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;
using UnityEditor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    public partial class ETPackageVersionModule
    {
        #region 强制依赖同步

        [Button("强制依赖同步", 50)]
        [ButtonGroup("同步")]
        [GUIColor(0f, 0.8f, 1)]
        [PropertyOrder(-50)]
        [ShowIf("CheckUpdateAllEnd")]
        private void SyncPackages1()
        {
            UnityTipsHelper.CallBack($"确定强制依赖同步?", SyncRefPackages);
        }

        private void SyncRefPackages()
        {
            var changeInfo = new List<PackageVersionData>();
            foreach (var data in m_AllPackageInfoDataDic.Values)
            {
                var isChange = CheckPackageChange(data);
                if (!isChange) continue;

                changeInfo.Add(data);
            }

            foreach (var data in changeInfo)
            {
                ChageDependenciesSelf(data);
            }

            foreach (var data in m_AllPackageInfoDataDic.Values)
            {
                if (!data.IsETPackage)
                {
                    continue;
                }

                foreach (var dependencie in data.Dependencies)
                {
                    if (dependencie.SyncVersionShowIf())
                    {
                        dependencie.SyncVersion();
                    }

                    if (dependencie.SyncSelfVersionShowIf())
                    {
                        dependencie.SyncSelfVersion();
                    }
                }

                foreach (var dependencie in data.DependenciesSelf)
                {
                    if (dependencie.SyncVersionShowIf())
                    {
                        dependencie.SyncVersion();
                    }

                    if (dependencie.SyncSelfVersionShowIf())
                    {
                        dependencie.SyncSelfVersion();
                    }
                }
            }
        }

        private void ChageDependenciesSelf(PackageVersionData data)
        {
            foreach (var refSelf in data.DependenciesSelf)
            {
                refSelf.SyncSelfVersion();

                var refName     = refSelf.Name;
                var packageInfo = GetPackageInfoData(refName);
                if (packageInfo == null)
                {
                    Debug.LogError($"packageInfo == null {refName}");
                    continue;
                }

                var oldAllPackage = PackageVersionHelper.PackageVersionAsset.AllPackageVersionData;
                if (!oldAllPackage.ContainsKey(refName))
                {
                    Debug.LogError($"oldAllPackage == null {refName}");
                    continue;
                }

                var oldPackageInfo = oldAllPackage[refName];

                if (packageInfo.Version != oldPackageInfo.Version)
                {
                    continue;
                }

                packageInfo.UpdateVersion6();
                ChageDependenciesSelf(packageInfo);
            }
        }

        #endregion

        #region 强制网络同步

        [Button("强制网络同步", 50)]
        [ButtonGroup("同步")]
        [GUIColor(0.4f, 0.8f, 1)]
        [PropertyOrder(-50)]
        [ShowIf("CheckUpdateAllEnd")]
        private void SyncNetPackages()
        {
            UnityTipsHelper.CallBack($"确定强制网络同步?", RefreshRequestAll);
        }

        private bool m_RequestingAll;
        private int  m_RefreshCompleteCount;
        private int  m_RefreshMaxCount;

        private void RefreshRequestAll()
        {
            if (m_RequestingAll)
            {
                UnityTipsHelper.Show($"请求中请稍等...请勿频繁请求");
                return;
            }

            CheckUpdateAllEnd      = false;
            m_RequestingAll        = true;
            m_RefreshMaxCount      = m_AllPackageInfoDataDic.Count;
            m_RefreshCompleteCount = 0;

            EditorUtility.DisplayProgressBar("同步信息", $"请求中... {m_RefreshCompleteCount} / {m_RefreshMaxCount}", 0);

            foreach (var package in m_AllPackageInfoDataDic.Values)
            {
                if (!package.IsETPackage) continue;

                var packageName = package.Name;

                new PackageRequestTarget(
                    packageName,
                    (info) =>
                    {
                        if (info != null)
                        {
                            package.Version = info.version;
                        }

                        RequestComplete();
                    });
            }
        }

        private void RequestComplete()
        {
            m_RefreshCompleteCount++;

            EditorUtility.DisplayProgressBar(
                "同步信息",
                $"请求中... {m_RefreshCompleteCount} / {m_RefreshMaxCount}",
                (float)m_RefreshCompleteCount / m_RefreshMaxCount);

            if (m_RefreshCompleteCount >= m_RefreshMaxCount)
            {
                CheckUpdateAllEnd = true;
                m_RequestingAll   = false;
                EditorUtility.ClearProgressBar();
            }
        }

        #endregion
    }
}
#endif