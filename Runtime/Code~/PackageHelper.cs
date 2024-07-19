using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;

namespace YIUIFramework.Editor
{
    public static class PackageHelper
    {
        public const string YIUIPackagePath = "Packages/cn.etetet.yiuipackagetool";

        public const string YIUIAssetFolderPath = YIUIPackagePath + "/Editor/Asset";

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

            return m_PackageInfoAsset.BanPackageInfo.Contains(name);
        }

        public static void BanPackage(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                Debug.LogError($"PackageInfoAsset == null");
                return;
            }

            m_PackageInfoAsset.BanPackageInfo.Add(name);
            AssetDatabase.SaveAssets();
        }

        public static void ReBanPackage(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                Debug.LogError($"PackageInfoAsset == null");
                return;
            }

            m_PackageInfoAsset.BanPackageInfo.Remove(name);
            AssetDatabase.SaveAssets();
        }

        public static string GetPackageLastVersion(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                Debug.LogError($"PackageInfoAsset == null");
                return "";
            }

            if (m_PackageInfoAsset.AllLastPackageInfo.TryGetValue(name, out var version))
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

            m_PackageInfoAsset.AllLastPackageInfo[name] = version;

            //Debug.LogError($"{name} 最新版本 {version}");
        }

        private static bool         m_Requesting;
        private static bool         m_CheckUpdateAllEnd;
        private static ListRequest  m_ListRequest;
        private static Action<bool> m_RequestAllCallback;

        public static void CheckUpdateAll(Action<bool> callback)
        {
            m_RequestAllCallback = null;
            if (m_CheckUpdateAllEnd)
            {
                callback?.Invoke(true);
                return;
            }

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
                foreach (var package in m_ListRequest.Result.Reverse())
                {
                    var lastVersion = package.version;
                    if (package.versions != null)
                    {
                        lastVersion = package.versions.latest;
                    }

                    ResetPackageLastInfo(package.name, lastVersion);
                }

                m_RequestAllCallback?.Invoke(true);
            }
            else
            {
                Debug.LogError(m_ListRequest.Error.message);
                m_RequestAllCallback?.Invoke(false);
            }

            AssetDatabase.SaveAssets();
            EditorApplication.update -= CheckUpdateAllProgress;
            m_Requesting             =  false;
            m_CheckUpdateAllEnd      =  true;
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
                    m_RequestTargetCallback?.Invoke(lastVersion);
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