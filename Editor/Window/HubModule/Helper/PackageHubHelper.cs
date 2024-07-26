using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    public static class PackageHubHelper
    {
        private const string PackageURL = "https://github.com/orgs/ET-Packages/packages?page=";

        public const string ETPackageHubAssetPath = PackageHelper.ETPackageAssetsFolderPath + "/PackageHubAsset.asset";

        private static PackageHubAsset m_PackageHubAsset;

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
                //Debug.Log($"请求中请稍等...请勿频繁请求");
                callback?.Invoke(false);
                return;
            }

            if (m_CheckUpdateCallback == null)
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
            m_PackageHubAsset.PackageDict.Clear();
            int page      = 0;
            int lastCount = 0;
            while (true)
            {
                page++;
                var pageUrl = $"{PackageURL}{page}";

                EditorUtility.DisplayProgressBar("同步信息", $"第{page}页", 0);

                var content = await GetHtmlContent(pageUrl);
                if (string.IsNullOrEmpty(content))
                {
                    break;
                }

                if (!ExtractPackages(content, ref m_PackageHubAsset.PackageDict))
                {
                    break;
                }

                //Debug.LogError($" 第{page}页 提取到:{packagesDic.Count - lastCount} 数据");
                lastCount = m_PackageHubAsset.PackageDict.Count;
            }

            //Debug.LogError($"总数据: {packagesDic.Count}");
            EditorUtility.SetDirty(m_PackageHubAsset);
            EditorUtility.ClearProgressBar();
            m_CheckUpdateCallback?.Invoke(true);
            m_CheckUpdateCallback = null;
            m_Requesting          = false;
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
    }
}