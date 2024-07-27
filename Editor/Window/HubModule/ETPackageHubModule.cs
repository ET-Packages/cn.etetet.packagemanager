#if ODIN_INSPECTOR
using System;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    [ETPackageMenu("库")]
    public class ETPackageHubModule : BasePackageToolModule
    {
        public static ETPackageHubModule Inst;

        [HideLabel]
        [HideIf("CheckUpdateAllEnd")]
        [ShowInInspector]
        [DisplayAsString(false, 100, TextAlignment.Center, true)]
        private static string m_CheckUpdateAllReqing = "请求所有包最新数据中...";

        public bool CheckUpdateAllEnd { get; private set; }

        public bool RequestAllResult { get; private set; }

        public override void Initialize()
        {
            CheckUpdateAllEnd = false;
            RequestAllResult  = false;

            PackageHelper.CheckUpdateAll((result) =>
            {
                if (!result)
                {
                    UnityTipsHelper.ShowError("获取所有包最新数据失败！请检查网络或关闭工具后重试");
                    return;
                }

                PackageHubHelper.CheckUpdate((result2) =>
                {
                    if (!result2)
                    {
                        UnityTipsHelper.ShowError("获取所有包最新数据失败！请检查网络或关闭工具后重试");
                        return;
                    }

                    CheckUpdateAllEnd = true;
                    RequestAllResult  = true;
                    Inst              = this;
                    CreateCategory();
                });
            });
        }

        public override void OnDestroy()
        {
            PackageHubHelper.SaveAsset();
            Inst = null;
        }

        private void CreateCategory()
        {
            var allCategoryType = Enum.GetValues(typeof(EPackageCategoryType));
            foreach (EPackageCategoryType categoryType in allCategoryType)
            {
                var categoryName = categoryType.ToString();
                var categoryPath = $"{ModuleName}/{categoryName}";
                var menuItem     = new TreeMenuItem<PackageCategoryModule>(AutoTool, Tree, categoryPath, EditorIcons.Folder);
                menuItem.UserData = new PackageCategoryData
                {
                    CategoryType = categoryType,
                    Category     = categoryName,
                };
            }

            foreach (var menu in Tree.MenuItems)
            {
                if (menu.Name == ModuleName)
                {
                    foreach (var item in menu.ChildMenuItems)
                    {
                        if (item.Name == EPackageCategoryType.All.ToString())
                        {
                            item.Select();
                            break;
                        }
                    }

                    break;
                }
            }
        }
    }
}
#endif