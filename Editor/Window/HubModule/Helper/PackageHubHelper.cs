using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace ET
{
    public static class PackageHubHelper
    {
        private const string PackageURL = "https://github.com/orgs/ET-Packages/packages?page=";

        public static async Task Init()
        {
            var packagesDic = new Dictionary<string, PackageInfo>();
            int page        = 0;
            int lastCount   = 0;
            while (true)
            {
                page++;
                var pageUrl = $"{PackageURL}{page}";

                var content = await GetHtmlContent(pageUrl);
                if (string.IsNullOrEmpty(content))
                {
                    break;
                }

                if (!ExtractPackages(content, ref packagesDic))
                {
                    break;
                }

                Debug.LogError($" 第{page}页 提取到:{packagesDic.Count - lastCount} 数据");
                lastCount = packagesDic.Count;
            }

            Debug.LogError($"总数据: {packagesDic.Count}");

            foreach (var info in packagesDic.Values)
            {
                Debug.LogError($"{info.PackageName} {info.DownloadValue}");
            }
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

        private static bool ExtractPackages(string html, ref Dictionary<string, PackageInfo> dic)
        {
            string packagePattern = @"href=""/ET-Packages/cn.etetet.(\w+)""";

            string downloadPattern = @"</svg>\s*?(\d+)\s*?</span>";

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
                string packageName      = packageMatches[i].Groups[1].Value;
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

        public class PackageInfo
        {
            public string PackageName   { get; set; }
            public int    DownloadValue { get; set; }
        }
    }
}