using System;
using System.IO;
using System.Threading.Tasks;
using LibGit2Sharp;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    [ETPackageMenu("更新", 900)]
    public class ETPackageUpdateModule : BasePackageToolModule
    {
        public override void Initialize()
        {
        }

        public override void OnDestroy()
        {
        }

        public string repositoryUrl = "https://github.com/ET-Packages/cn.etetet.yiui.git";
        public string localPath     = "TestGit";

        [Button]
        private void Start()
        {
            DownloadRepositoryAsync(repositoryUrl, localPath);
        }

        private void DownloadRepositoryAsync(string repositoryUrl, string localPath)
        {
            Task.Run(() => DownloadRepository(repositoryUrl, localPath));
        }

        private void DownloadRepository(string repositoryUrl, string localPath)
        {
            try
            {
                var path = $"{Application.dataPath}/{localPath}";
                
                // 确保目标文件夹不存在
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }

                // 克隆仓库
                var aaa = Repository.Clone(repositoryUrl, localPath);
                
                Debug.Log($"{aaa} ");
                
                Debug.Log("Repository cloned successfully to " + localPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error cloning repository: " + ex.Message);
            }
        }
    }
}
