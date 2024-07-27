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
        public List<PackageHubData> AllPackages;

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

            AllPackages    = m_CategoryData.AllPackages;
            m_CategoryType = m_CategoryData.CategoryType;

            if (m_CategoryType is EPackageCategoryType.All or EPackageCategoryType.Other)
            {
                return;
            }

            //不限制可以无限层
            GetNextCategory(m_CategoryData.Layer + 1);
        }

        public override void OnDestroy()
        {
        }

        private void GetNextCategory(int layer)
        {
            var nextCategory = PackageHubHelper.GetNextCategoryData(AllPackages, layer);

            if (nextCategory is not { Count: > 0 }) return;

            foreach (var category in nextCategory.Keys)
            {
                var categoryPath = $"{m_CategoryData.CategoryPath}/{category}";

                var menuItem = new TreeMenuItem<PackageCategoryModule>(AutoTool, Tree, categoryPath, EditorIcons.Folder);
                menuItem.UserData = new PackageCategoryData
                {
                    CategoryType = m_CategoryType,
                    Category     = category,
                    CategoryPath = categoryPath,
                    Layer        = layer,
                    AllPackages  = nextCategory[category],
                };
            }
        }
    }
}
#endif