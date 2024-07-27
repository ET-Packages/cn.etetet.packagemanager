#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    public class PackageCategoryModule : BasePackageToolModule
    {
        [Button("请求刷新所有数据", 50)]
        [PropertyOrder(-999)]
        [GUIColor(0f, 1f, 0f)]
        private void RefreshAll()
        {
            PackageHubHelper.RefreshRequestAll(AllPackages);
        }

        [TableList(DrawScrollView = true, IsReadOnly = true)]
        [BoxGroup("所有包", centerLabel: true)]
        [HideLabel]
        [ShowInInspector]
        public List<PackageHubData> AllPackages = new();

        private PackageCategoryData m_CategoryData;

        private EPackageCategoryType m_CategoryType = EPackageCategoryType.All;

        public override void Initialize()
        {
            if (UserData is PackageCategoryData data)
            {
                m_CategoryData = data;
            }
            else
            {
                Debug.LogError("错误的数据: UserData is not PackageCategoryData");
                return;
            }

            m_CategoryType = m_CategoryData.CategoryType;
        }

        public override void SelectionMenu()
        {
            if (PackageHubHelper.PackageHubAsset == null)
            {
                Debug.LogError($" PackageHubAsset == null ");
                return;
            }

            AllPackages.Clear();
            foreach (var package in PackageHubHelper.PackageHubAsset.AllPackageData.Values)
            {
                package.InitRequestInfo();

                switch (m_CategoryType)
                {
                    case EPackageCategoryType.All:
                        AllPackages.Add(package);
                        break;
                    case EPackageCategoryType.Other:
                        if (string.IsNullOrEmpty(package.PackageCategory) || PackageHubHelper.GetCategoryType(package) == m_CategoryType)
                        {
                            AllPackages.Add(package);
                        }

                        break;
                    default:
                        var categoryType = PackageHubHelper.GetCategoryType(package);
                        if (categoryType == m_CategoryType)
                        {
                            AllPackages.Add(package);
                        }

                        break;
                }
            }
        }

        public override void OnDestroy()
        {
        }
    }
}
#endif