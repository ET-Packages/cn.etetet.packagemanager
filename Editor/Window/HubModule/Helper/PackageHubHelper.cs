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
        public static async Task Init()
        {
            string url = "https://github.com/orgs/ET-Packages/repositories?type=all";

            int page = 1;

            int allCount = 0;

            var packages = new HashSet<string>();

            var whileCount = 0;

            while (true)
            {
                var pageUrl = $"{url}&page={page}";

                var content = await GetHtmlContent(pageUrl);

                if (page == 1)
                {
                    allCount = GetAllCount(content);
                    Debug.LogError($"总数 {allCount}");
                }

                GetCurrentPackages(content, ref packages);

                Debug.LogError($"第 {page} 页  获取到: {packages.Count}");

                if (allCount == 0)
                {
                    break;
                }

                if (packages.Count >= allCount)
                {
                    break;
                }

                if (whileCount >= 100)
                {
                    break;
                }

                page++;
                whileCount++;
            }

            Debug.LogError($"长度 {packages.Count}");

            foreach (var value in packages)
            {
                Debug.LogError(value);
            }
        }

        static async Task<string> GetHtmlContent(string url)
        {
            string html = "";
            using (var client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    html = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException e)
                {
                    Debug.LogError("\n异常: " + e.Message);
                }
            }

            return html;
        }

        static int GetAllCount(string content)
        {
            string pattern = @"ET-Packages has (\d+) repositories";

            Match match = Regex.Match(content, pattern);

            if (match.Success)
            {
                int repositoryCount = int.Parse(match.Groups[1].Value);
                return repositoryCount;
            }
            else
            {
                Debug.LogError("没有找到总数");
            }

            return 0;
        }

        static void GetCurrentPackages(string content, ref HashSet<string> repositoryNames)
        {
            string pattern = @"""name"":\s*""cn\.etetet\.(\w+)""";

            MatchCollection matches = Regex.Matches(content, pattern);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    repositoryNames.Add(match.Groups[1].Value);
                }
            }
        }
    }
}
