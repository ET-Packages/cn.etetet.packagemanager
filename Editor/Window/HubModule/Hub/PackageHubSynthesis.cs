#if ODIN_INSPECTOR
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// 整合信息
    /// </summary>
    [HideReferenceObjectPicker]
    public class PackageHubSynthesis
    {
        [TableList(DrawScrollView = true, AlwaysExpanded = true, IsReadOnly = true)]
        [BoxGroup("所有包", centerLabel: true)]
        [HideLabel]
        [ShowInInspector]
        public List<PackageHubData> AllPackages = new();

        public PackageHubSynthesis()
        {
            AllPackages.Clear();
            foreach (var package in PackageHubHelper.PackageHubAsset.AllPackageData.Values)
            {
                AllPackages.Add(package);
            }
        }

        [Button("请求刷新所有数据", 50)]
        [PropertyOrder(-999)]
        [GUIColor(0f, 1f, 0f)]
        private void RefreshAll()
        {
            _refreshMaxCount      = AllPackages.Count;
            _refreshCompleteCount = 0;

            EditorUtility.DisplayProgressBar("同步信息", $"请求中... {_refreshCompleteCount} / {_refreshMaxCount}", 0);

            foreach (var package in AllPackages)
            {
                var packageName = package.PackageName;

                new PackageRequestTarget(packageName, (info) =>
                {
                    RequestComplete();
                    package.RefreshInfo(info);
                });
            }
        }

        private int _refreshCompleteCount;
        private int _refreshMaxCount;

        private void RequestComplete()
        {
            _refreshCompleteCount++;

            EditorUtility.DisplayProgressBar("同步信息", $"请求中... {_refreshCompleteCount} / {_refreshMaxCount}",
                (float)_refreshCompleteCount / _refreshMaxCount);

            if (_refreshCompleteCount >= _refreshMaxCount)
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
#endif