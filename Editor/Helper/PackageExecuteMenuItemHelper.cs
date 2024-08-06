using UnityEditor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    //为了防止其他包的按钮可能改名字等操作 这里需要使用前深度检查
    //因为这个管理包 不可能每次其他包更新都去检查一下 万一哪天没检查到不就BUG了
    //所以这哪里必须要做好防护措施
    public static class PackageExecuteMenuItemHelper
    {
        public static void ETAll()
        {
            //有顺序的 别乱动
            ET_Init_RepairDependencies();
            ET_Loader_ReGenerateProjectFile();
            ET_Loader_ReGenerateProjectAssemblyReference();
            ET_Loader_UpdateScriptsReferences();
            ET_Excel_ExcelExporter();
            ET_Proto_Proto2CS();
        }

        public static void ET_Init_RepairDependencies()
        {
            var menuItem = "ET/Init/RepairDependencies";
            if (IsMenuItemExists("com.etetet.init", "DependencyResolver", menuItem))
            {
                ExecuteMenuItem(menuItem);
            }
        }

        public static void ET_Loader_ReGenerateProjectFile()
        {
            var menuItem = "ET/Loader/ReGenerateProjectFile";
            if (IsMenuItemExists("cn.etetet.loader", "ReGenerateProjectFilesHelper", menuItem))
            {
                ExecuteMenuItem(menuItem);
            }
        }

        public static void ET_Loader_ReGenerateProjectAssemblyReference()
        {
            var menuItem = "ET/Loader/ReGenerateProjectAssemblyReference";
            if (IsMenuItemExists("cn.etetet.loader", "CodeModeChangeHelper", menuItem))
            {
                ExecuteMenuItem(menuItem);
            }
        }

        public static void ET_Loader_UpdateScriptsReferences()
        {
            var menuItem = "ET/Loader/UpdateScriptsReferences";
            if (IsMenuItemExists("cn.etetet.loader", "ScriptsReferencesHelper", menuItem))
            {
                ExecuteMenuItem(menuItem);
            }
        }

        public static void ET_Excel_ExcelExporter()
        {
            var menuItem = "ET/Excel/ExcelExporter";
            if (IsMenuItemExists("cn.etetet.excel", "ExcelEditor", menuItem))
            {
                ExecuteMenuItem(menuItem);
            }
        }

        public static void ET_Proto_Proto2CS()
        {
            var menuItem = "ET/Proto/Proto2CS";
            if (IsMenuItemExists("cn.etetet.proto", "ProtoEditor", menuItem))
            {
                ExecuteMenuItem(menuItem);
            }
        }

        private static void ExecuteMenuItem(string menuItem)
        {
            EditorApplication.ExecuteMenuItem(menuItem);
        }

        private static bool IsMenuItemExists(string packageName, string csName, string menuName)
        {
            var packagePath = $"{Application.dataPath}/../Packages/{packageName}";

            if (!System.IO.Directory.Exists(packagePath))
            {
                Debug.LogError($"没有找到这个包 {packageName} 请检查 {packagePath}");
                return false;
            }

            foreach (var file in System.IO.Directory.GetFiles(packagePath, "*.cs", System.IO.SearchOption.AllDirectories))
            {
                if (file.Contains(csName))
                {
                    var content = System.IO.File.ReadAllText(file);
                    if (content.Contains(menuName))
                    {
                        return true;
                    }

                    Debug.LogError($"这个包 {packageName} 这个文件 {csName} 没有找到 {menuName}");
                    return false;
                }
            }

            Debug.LogError($"这个包 {packageName} 没有找到这个文件 {csName}");
            return false;
        }
    }
}