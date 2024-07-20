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
        public const string YIUIPackagePath = "Packages/cn.etetet.packagemanager";

        public const string YIUIAssetFolderPath = YIUIPackagePath + "/Editor/Assets";

        public const string YIUIAssetPath = YIUIAssetFolderPath + "/PackageInfoAsset.asset";

        private static PackageInfoAsset m_PackageInfoAsset;

        private static bool LoadAsset()
        {
            m_PackageInfoAsset = AssetDatabase.LoadAssetAtPath<PackageInfoAsset>(YIUIAssetPath);

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

            var assetFolder = $"{Application.dataPath}/../{YIUIAssetFolderPath}";
            if (!Directory.Exists(assetFolder))
                Directory.CreateDirectory(assetFolder);

            AssetDatabase.CreateAsset(m_PackageInfoAsset, YIUIAssetPath);
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
                Debug.LogError($"PackageInfoAsset == null");
                return;
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
                Debug.LogError($"PackageInfoAsset == null");
                return;
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

            AssetDatabase.SaveAssetIfDirty(m_PackageInfoAsset);
            AssetDatabase.SaveAssets();
        }

        public static string GetPackageLastVersion(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                Debug.LogError($"PackageInfoAsset == null");
                return "";
            }

            if (m_PackageInfoAsset.AllLastPackageInfoDic.TryGetValue(name, out var version))
            {
                return version;
            }

            return "";
        }

        private static void ResetPackageLastInfo(string name, string version)
        {
            if (m_PackageInfoAsset == null)
            {
                Debug.LogError($"PackageInfoAsset == null");
                return;
            }

            m_PackageInfoAsset.AllLastPackageInfoDic[name] = version;

            //Debug.LogError($"{name} 最新版本 {version}");
        }

        private static void UpdateAllLastPackageInfo()
        {
            Dictionary<string, PackageInfoKeyValuePair> packageInfo = new();
            if (m_PackageInfoAsset.AllLastPackageInfo != null)
            {
                foreach (var pair in m_PackageInfoAsset.AllLastPackageInfo)
                {
                    var name = pair.name;
                    if (m_PackageInfoAsset.AllLastPackageInfoDic.TryGetValue(name, out string version))
                    {
                        pair.Value        = version;
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
                    var pair    = ScriptableObject.CreateInstance<PackageInfoKeyValuePair>();
                    pair.Key          = name;
                    pair.Value        = version;
                    packageInfo[name] = pair;
                }
            }

            m_PackageInfoAsset.AllLastPackageInfo = new PackageInfoKeyValuePair[packageInfo.Count];
            var index = 0;
            foreach (var pair in packageInfo.Values)
            {
                m_PackageInfoAsset.AllLastPackageInfo[index] = pair;
                index++;
            }

            AssetDatabase.SaveAssetIfDirty(m_PackageInfoAsset);
            AssetDatabase.SaveAssets();
        }

        private static bool         m_Requesting;
        private static ListRequest  m_ListRequest;
        private static Action<bool> m_RequestAllCallback;

        public static void CheckUpdateAll(Action<bool> callback)
        {
            m_RequestAllCallback = null;

            if (m_Requesting)
            {
                //Debug.Log($"请求中请稍等...请勿频繁请求");
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
            EditorUtility.DisplayProgressBar("同步包信息", $"请求中...", 0);
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

            AssetDatabase.SaveAssetIfDirty(m_PackageInfoAsset);
            AssetDatabase.SaveAssets();
            EditorApplication.update -= CheckUpdateAllProgress;
            m_Requesting             =  false;
            m_RequestAllCallback     =  null;
            EditorUtility.ClearProgressBar();
        }

        private static SearchRequest  m_TargetRequest;
        private static Action<string> m_RequestTargetCallback;

        public static void CheckUpdateTarget(string name, Action<string> callback)
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

            if (m_Requesting)
            {
                //Debug.Log($"请求中请稍等...请勿频繁请求");
                callback?.Invoke("");
                return;
            }

            EditorUtility.DisplayProgressBar("同步包信息", $"{name} 请求中...", 0);

            m_RequestTargetCallback = callback;

            m_Requesting = true;

            m_TargetRequest = Client.Search(name);

            EditorApplication.update += CheckUpdateTargetProgress;
        }

        private static void CheckUpdateTargetProgress()
        {
            if (!m_TargetRequest.IsCompleted) return;

            if (m_TargetRequest.Status == StatusCode.Success)
            {
                if (m_TargetRequest.Result is { Length: >= 1 })
                {
                    var packageInfo = m_TargetRequest.Result[0];
                    var lastVersion = packageInfo.version;
                    if (packageInfo.versions != null)
                    {
                        lastVersion = packageInfo.versions.latest;
                    }

                    ResetPackageLastInfo(packageInfo.name, lastVersion);
                    UpdateAllLastPackageInfo();
                    m_RequestTargetCallback?.Invoke(lastVersion);
                    AssetDatabase.SaveAssetIfDirty(m_PackageInfoAsset);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    m_RequestTargetCallback?.Invoke("");
                }
            }
            else
            {
                Debug.LogError(m_TargetRequest.Error.message);
                m_RequestTargetCallback?.Invoke("");
            }

            EditorApplication.update -= CheckUpdateTargetProgress;
            m_Requesting             =  false;
            m_RequestTargetCallback  =  null;
            EditorUtility.ClearProgressBar();
        }
    }
}
