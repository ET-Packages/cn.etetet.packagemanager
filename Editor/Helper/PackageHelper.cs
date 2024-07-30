using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    public static class PackageHelper
    {
        public const string ETPackagePath = "Packages/cn.etetet.packagemanager";

        public const string ETPackageAssetsFolderPath = ETPackagePath + "/Editor/Assets";

        public const string ETPackageInfoAssetPath = ETPackageAssetsFolderPath + "/PackageInfoAsset.asset";

        private static PackageInfoAsset m_PackageInfoAsset;

        public static PackageInfoAsset PackageInfoAsset
        {
            get
            {
                if (m_PackageInfoAsset == null)
                {
                    var result = LoadAsset();
                    if (!result)
                    {
                        Debug.LogError($"PackageInfoAsset == null");
                        return null;
                    }
                }

                return m_PackageInfoAsset;
            }
        }

        private static readonly Dictionary<string, UnityEditor.PackageManager.PackageInfo> m_CurrentRegisteredPackages = new();
        public static           Dictionary<string, UnityEditor.PackageManager.PackageInfo> CurrentRegisteredPackages => m_CurrentRegisteredPackages;

        private static void GetAllRegisteredPackages()
        {
            m_CurrentRegisteredPackages.Clear();
            foreach (var packageInfo in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                var name = packageInfo.name;
                m_CurrentRegisteredPackages.Add(name, packageInfo);
            }
        }

        public static void Unload()
        {
            m_PackageInfoAsset = null;
        }

        private static bool LoadAsset()
        {
            GetAllRegisteredPackages();

            if (m_PackageInfoAsset != null)
            {
                return true;
            }

            m_PackageInfoAsset = AssetDatabase.LoadAssetAtPath<PackageInfoAsset>(ETPackageInfoAssetPath);

            if (m_PackageInfoAsset == null)
            {
                CreateAsset();
            }

            if (m_PackageInfoAsset == null)
            {
                Debug.LogError($"没有找到 配置资源 且自动创建失败 请检查");
                return false;
            }

            m_PackageInfoAsset.ReUpdateInfo();

            return true;
        }

        private static void CreateAsset()
        {
            m_PackageInfoAsset = ScriptableObject.CreateInstance<PackageInfoAsset>();

            var assetFolder = $"{Application.dataPath}/../{ETPackageAssetsFolderPath}";
            if (!Directory.Exists(assetFolder))
                Directory.CreateDirectory(assetFolder);

            AssetDatabase.CreateAsset(m_PackageInfoAsset, ETPackageInfoAssetPath);
        }

        public static bool IsBanPackage(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                Debug.LogError($"PackageInfoAsset == null");
                return false;
            }

            return m_PackageInfoAsset.BanPackageInfoHash.Contains(name);
        }

        public static void BanPackage(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                var result = LoadAsset();
                if (!result)
                {
                    Debug.LogError($"PackageInfoAsset == null");
                    return;
                }
            }

            if (IsBanPackage(name))
            {
                return;
            }

            m_PackageInfoAsset.BanPackageInfoHash.Add(name);
            UpdateBanPackage();
        }

        public static void ReBanPackage(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                var result = LoadAsset();
                if (!result)
                {
                    Debug.LogError($"PackageInfoAsset == null");
                    return;
                }
            }

            if (!IsBanPackage(name))
            {
                return;
            }

            m_PackageInfoAsset.BanPackageInfoHash.Remove(name);
            UpdateBanPackage();
        }

        private static void UpdateBanPackage()
        {
            var count = m_PackageInfoAsset.BanPackageInfoHash.Count;
            m_PackageInfoAsset.BanPackageInfo = new string[count];
            var index = 0;
            foreach (var value in m_PackageInfoAsset.BanPackageInfoHash)
            {
                m_PackageInfoAsset.BanPackageInfo[index] = value;
                index++;
            }

            EditorUtility.SetDirty(m_PackageInfoAsset);
        }

        public static string GetPackageLastVersion(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                var result = LoadAsset();
                if (!result)
                {
                    Debug.LogError($"PackageInfoAsset == null");
                    return "";
                }
            }

            if (m_PackageInfoAsset.AllLastPackageInfoDic.TryGetValue(name, out var version))
            {
                return version;
            }

            return "";
        }

        public static UnityEditor.PackageManager.PackageInfo GetPackageInfo(string name)
        {
            return m_CurrentRegisteredPackages.GetValueOrDefault(name);
        }

        public static string GetPackageCurrentVersion(string name)
        {
            if (m_CurrentRegisteredPackages.TryGetValue(name, out var info))
            {
                return info.version;
            }

            return "";
        }

        private static void ResetPackageLastInfo(string name, string version)
        {
            if (m_PackageInfoAsset == null)
            {
                var result = LoadAsset();
                if (!result)
                {
                    Debug.LogError($"PackageInfoAsset == null");
                    return;
                }
            }

            m_PackageInfoAsset.AllLastPackageInfoDic[name] = version;
        }

        private static void UpdateAllLastPackageInfo()
        {
            Dictionary<string, PackageLastVersionData> packageInfo = new();
            if (m_PackageInfoAsset.AllLastPackageInfo != null)
            {
                foreach (var pair in m_PackageInfoAsset.AllLastPackageInfo)
                {
                    var name = pair.Name;
                    if (m_PackageInfoAsset.AllLastPackageInfoDic.TryGetValue(name, out string version))
                    {
                        pair.Version      = version;
                        packageInfo[name] = pair;
                    }
                }
            }

            foreach (var info in m_PackageInfoAsset.AllLastPackageInfoDic)
            {
                var name = info.Key;
                if (!packageInfo.ContainsKey(name))
                {
                    var version = info.Value;
                    var pair    = new PackageLastVersionData();
                    pair.Name         = name;
                    pair.Version      = version;
                    packageInfo[name] = pair;
                }
            }

            m_PackageInfoAsset.AllLastPackageInfo = new PackageLastVersionData[packageInfo.Count];
            var index = 0;
            foreach (var pair in packageInfo.Values)
            {
                m_PackageInfoAsset.AllLastPackageInfo[index] = pair;
                index++;
            }

            EditorUtility.SetDirty(m_PackageInfoAsset);
        }

        private static bool         m_Requesting;
        private static ListRequest  m_ListRequest;
        private static Action<bool> m_RequestAllCallback;

        public static void CheckUpdateAll(Action<bool> callback)
        {
            m_RequestAllCallback = null;

            if (m_Requesting)
            {
                Debug.Log($"请求中请稍等...请勿频繁请求");
                callback?.Invoke(false);
                return;
            }

            if (m_PackageInfoAsset == null)
            {
                var result = LoadAsset();
                if (!result)
                {
                    callback?.Invoke(false);
                    return;
                }
            }

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (currentTime - m_PackageInfoAsset.LastUpdateTime < m_PackageInfoAsset.UpdateInterval)
            {
                callback?.Invoke(true);
                return;
            }

            m_PackageInfoAsset.LastUpdateTime = currentTime;
            EditorUtility.DisplayProgressBar("同步信息", $"请求中...", 0);
            m_RequestAllCallback     =  callback;
            m_Requesting             =  true;
            m_ListRequest            =  Client.List();
            EditorApplication.update += CheckUpdateAllProgress;
        }

        private static void CheckUpdateAllProgress()
        {
            if (!m_ListRequest.IsCompleted) return;

            if (m_ListRequest.Status == StatusCode.Success)
            {
                m_PackageInfoAsset.ReSetAllLastPackageInfo();

                foreach (var package in m_ListRequest.Result.Reverse())
                {
                    var lastVersion = package.version;
                    if (package.versions != null)
                    {
                        lastVersion = package.versions.latest;
                    }

                    ResetPackageLastInfo(package.name, lastVersion);
                }

                UpdateAllLastPackageInfo();
                m_RequestAllCallback?.Invoke(true);
            }
            else
            {
                Debug.LogError(m_ListRequest.Error.message);
                m_RequestAllCallback?.Invoke(false);
            }

            EditorApplication.update -= CheckUpdateAllProgress;
            m_Requesting             =  false;
            m_RequestAllCallback     =  null;
            EditorUtility.ClearProgressBar();
        }

        private static HashSet<string> m_RequestTargets = new();

        public static void CheckUpdateTarget(string name, Action<string> callback, bool showBar = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError($"不可查询 null包 请传入名称");
                callback?.Invoke("");
                return;
            }

            if (IsBanPackage(name))
            {
                callback?.Invoke("");
                return;
            }

            var version = GetPackageLastVersion(name);
            if (!string.IsNullOrEmpty(version))
            {
                callback?.Invoke(version);
                return;
            }

            if (showBar)
            {
                EditorUtility.DisplayProgressBar("同步信息", $"{name} 请求中...", 0);
            }

            if (m_RequestTargets.Contains(name))
            {
                callback?.Invoke("");
                return;
            }

            m_RequestTargets.Add(name);

            new PackageRequestTarget(name, (packageInfo) =>
            {
                m_RequestTargets.Remove(name);
                if (showBar)
                {
                    EditorUtility.ClearProgressBar();
                }

                if (packageInfo == null) return;
                var lastVersion = packageInfo.version;
                if (packageInfo.versions != null)
                {
                    lastVersion = packageInfo.versions.latest;
                }

                ResetPackageLastInfo(packageInfo.name, lastVersion);
                UpdateAllLastPackageInfo();
                callback?.Invoke(lastVersion);
            });
        }
    }
}
