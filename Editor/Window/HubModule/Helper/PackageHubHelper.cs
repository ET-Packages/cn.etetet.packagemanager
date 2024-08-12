﻿#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;

namespace ET.PackageManager.Editor
{
    public static class PackageHubHelper
    {
        private const string PackageURL = "https://github.com/orgs/ET-Packages/packages?page=";

        public const string ETPackageHubAssetPath = PackageHelper.ETPackageAssetsFolderPath + "/PackageHubAsset.asset";

        private static PackageHubAsset m_PackageHubAsset;

        public static PackageHubAsset PackageHubAsset
        {
            get
            {
                if (m_PackageHubAsset == null)
                {
                    if (!LoadAsset())
                    {
                        Debug.LogError($"m_PackageHubAsset == null");
                        return null;
                    }
                }

                return m_PackageHubAsset;
            }
        }

        public static PackageHubData GetPackageHubData(string packageName)
        {
            return PackageHubAsset.AllPackageData.GetValueOrDefault(packageName);
        }

        public static void SaveAsset()
        {
            if (m_PackageHubAsset == null) return;
            EditorUtility.SetDirty(m_PackageHubAsset);
        }

        private static bool LoadAsset()
        {
            m_PackageHubAsset = AssetDatabase.LoadAssetAtPath<PackageHubAsset>(ETPackageHubAssetPath);

            if (m_PackageHubAsset == null)
            {
                CreateAsset();
            }

            if (m_PackageHubAsset == null)
            {
                Debug.LogError($"没有找到 配置资源 且自动创建失败 请检查");
                return false;
            }

            return true;
        }

        private static void CreateAsset()
        {
            m_PackageHubAsset = ScriptableObject.CreateInstance<PackageHubAsset>();

            var assetFolder = $"{Application.dataPath}/../{PackageHelper.ETPackageAssetsFolderPath}";
            if (!Directory.Exists(assetFolder))
                Directory.CreateDirectory(assetFolder);

            AssetDatabase.CreateAsset(m_PackageHubAsset, ETPackageHubAssetPath);
        }

        private static bool m_Requesting;

        private static Action<bool> m_CheckUpdateCallback;

        public static void CheckUpdate(Action<bool> callback)
        {
            m_CheckUpdateCallback = null;

            if (m_Requesting)
            {
                Debug.Log($"请求中请稍等...请勿频繁请求");
                callback?.Invoke(false);
                return;
            }

            if (m_PackageHubAsset == null)
            {
                var result = LoadAsset();
                if (!result)
                {
                    callback?.Invoke(false);
                    return;
                }
            }

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (currentTime - m_PackageHubAsset.LastUpdateTime < m_PackageHubAsset.UpdateInterval)
            {
                callback?.Invoke(true);
                return;
            }

            m_PackageHubAsset.LastUpdateTime = currentTime;
            m_CheckUpdateCallback            = callback;
            m_Requesting                     = true;

            RefreshPackages();
        }

        private static async Task RefreshPackages()
        {
            var tempAllPackageData = new Dictionary<string, PackageHubData>();
            int page               = 0;
            int lastCount          = 0;
            while (true)
            {
                page++;
                var pageUrl = $"{PackageURL}{page}";

                EditorUtility.DisplayProgressBar("同步信息", $"第{page}页...", 0);

                var content = await GetHtmlContent(pageUrl);
                if (string.IsNullOrEmpty(content))
                {
                    break;
                }

                if (!ExtractPackages(content, ref tempAllPackageData))
                {
                    break;
                }

                //Debug.LogError($" 第{page}页 提取到:{packagesDic.Count - lastCount} 数据");
                lastCount = tempAllPackageData.Count;
            }

            SyncAllPackageData(tempAllPackageData);

            //Debug.LogError($"总数据: {packagesDic.Count}");
            m_Requesting = false;
            EditorUtility.ClearProgressBar();
            m_CheckUpdateCallback?.Invoke(true);
            m_CheckUpdateCallback = null;
        }

        private static void SyncAllPackageData(Dictionary<string, PackageHubData> datas)
        {
            var removeData = new HashSet<string>();
            foreach (var packageData in m_PackageHubAsset.AllPackageData.Values)
            {
                var name = packageData.PackageName;
                if (!datas.ContainsKey(name))
                {
                    removeData.Add(name);
                }
            }

            foreach (var name in removeData)
            {
                m_PackageHubAsset.AllPackageData.Remove(name);
            }

            foreach (var packageData in datas.Values)
            {
                var name = packageData.PackageName;
                if (m_PackageHubAsset.AllPackageData.TryGetValue(name, out var data))
                {
                    data.DownloadValue = packageData.DownloadValue;
                }
                else
                {
                    m_PackageHubAsset.AllPackageData[name] = packageData;
                }
            }

            SaveAsset();
        }

        private static async Task<string> GetHtmlContent(string url)
        {
            string    html   = "";
            using var client = new HttpClient();
            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                html = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                Debug.LogError("异常: " + e.Message);
            }

            return html;
        }

        private static bool ExtractPackages(string html, ref Dictionary<string, PackageHubData> dic)
        {
            string          packagePattern  = @"href=""/ET-Packages/cn.etetet.(\w+)""";
            string          downloadPattern = @"</svg>\s*?(\d+)\s*?</span>";
            Regex           packageRegex    = new Regex(packagePattern);
            Regex           downloadRegex   = new Regex(downloadPattern);
            MatchCollection packageMatches  = packageRegex.Matches(html);
            MatchCollection downloadMatches = downloadRegex.Matches(html);

            if (packageMatches.Count != downloadMatches.Count || packageMatches.Count <= 0 || downloadMatches.Count <= 0)
            {
                return false;
            }

            for (int i = 0; i < packageMatches.Count; i++)
            {
                string packageName      = $"cn.etetet.{packageMatches[i].Groups[1].Value}";
                string downloadCountStr = downloadMatches[i].Groups[1].Value;
                int    downloadCount    = int.Parse(downloadCountStr);

                dic[packageName] = new()
                {
                    PackageName   = packageName,
                    DownloadValue = downloadCount
                };
            }

            return true;
        }

        public static bool CheckRemove(string packageName, bool showLog = false)
        {
            var packagePath = Application.dataPath.Replace("Assets", "Packages") + "/" + packageName;

            if (!System.IO.Directory.Exists(packagePath))
            {
                if (showLog)
                {
                    UnityTipsHelper.Show($"{packagePath} 不存在此包 无法移除");
                }

                return false;
            }

            //查询依赖 他被其他包依赖时 无法移除必须先移除依赖包
            var versionData = PackageVersionHelper.GetPackageVersionData(packageName);
            if (versionData != null)
            {
                if (versionData.DependenciesSelf != null)
                {
                    foreach (var dependencyInfo in versionData.DependenciesSelf)
                    {
                        var name    = dependencyInfo.Name;
                        var hubData = PackageHubHelper.GetPackageHubData(name);
                        if (hubData != null)
                        {
                            if (hubData.Install)
                            {
                                if (showLog)
                                {
                                    UnityTipsHelper.Show($"无法移除此包: {packageName} 因为被{hubData.PackageName}引用 必须先移除依赖");
                                }

                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        #region 请求更新所有

        private static bool m_RequestingAll;
        private static int  m_RefreshCompleteCount;
        private static int  m_RefreshMaxCount;

        public static void RefreshRequestAll(List<PackageHubData> allPackages)
        {
            if (m_RequestingAll)
            {
                UnityTipsHelper.Show($"请求中请稍等...请勿频繁请求");
                return;
            }

            m_RequestingAll        = true;
            m_RefreshMaxCount      = allPackages.Count;
            m_RefreshCompleteCount = 0;

            EditorUtility.DisplayProgressBar("同步信息", $"请求中... {m_RefreshCompleteCount} / {m_RefreshMaxCount}", 0);

            foreach (var package in allPackages)
            {
                var packageName = package.PackageName;

                package.OperationState = true;
                new PackageRequestTarget(packageName, (info) =>
                {
                    package.OperationState = false;
                    RequestComplete();
                    package.RefreshInfo(info);
                });
            }
        }

        private static void RequestComplete()
        {
            m_RefreshCompleteCount++;

            EditorUtility.DisplayProgressBar("同步信息", $"请求中... {m_RefreshCompleteCount} / {m_RefreshMaxCount}", (float)m_RefreshCompleteCount / m_RefreshMaxCount);

            if (m_RefreshCompleteCount >= m_RefreshMaxCount)
            {
                m_RequestingAll = false;
                EditorUtility.ClearProgressBar();
                ETPackageAutoTool.CloseWindowRefresh();
                ETPackageAutoTool.OpenWindow();
            }
        }

        #endregion

        public static Dictionary<string, List<PackageHubData>> GetNextCategoryData(List<PackageHubData> allPackages, int layer, string lastAllPath = "")
        {
            Dictionary<string, List<PackageHubData>> nextCategory = new();
            foreach (var package in allPackages)
            {
                var categoryAList = package.PackageCategory?.Split("|");
                if (categoryAList == null)
                {
                    if (layer == 1)
                    {
                        var other = EPackageCategoryType.Other.ToString();
                        if (!nextCategory.ContainsKey(other))
                        {
                            nextCategory.Add(other, new List<PackageHubData>());
                        }

                        nextCategory[other].Add(package);
                    }

                    continue;
                }

                foreach (var categoryA in categoryAList)
                {
                    var categoryList = categoryA.Split("/");
                    var category     = "";
                    if (categoryList != null && categoryList.Length >= layer)
                    {
                        if (layer > 1 && !string.IsNullOrEmpty(lastAllPath))
                        {
                            if (!categoryA.Contains(lastAllPath))
                            {
                                continue;
                            }
                        }

                        category = categoryList[layer - 1];
                        if (string.IsNullOrEmpty(category))
                        {
                            if (layer == 1)
                            {
                                category = EPackageCategoryType.Other.ToString();
                            }
                        }
                    }
                    else
                    {
                        if (layer == 1)
                        {
                            category = EPackageCategoryType.Other.ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(category))
                    {
                        continue;
                    }

                    if (!nextCategory.ContainsKey(category))
                    {
                        nextCategory.Add(category, new List<PackageHubData>());
                    }

                    nextCategory[category].Add(package);
                }
            }

            return nextCategory;
        }
    }
}
#endif