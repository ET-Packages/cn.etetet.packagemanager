using System.IO;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    public static partial class ETPackageCreateHelper
    {
        private const string Pattern = "[^a-z]";

        public static string ETPackageCreateTemplatePath = "Packages/cn.etetet.packagemanager/Editor/Window/CreateModule/Template";

        public static string GetPackagePath(ref string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                Debug.LogError($"包名不能为空");
                return "";
            }

            packageName = packageName.ToLower();
            packageName = System.Text.RegularExpressions.Regex.Replace(packageName, Pattern, "");
            var packagepath = $"Packages/cn.etetet.{packageName}";

            return packagepath;
        }

        public static bool CreateDirectory(string path, bool force = true)
        {
            if (Directory.Exists(path))
            {
                if (force)
                {
                    Directory.Delete(path, true);
                }
                else
                {
                    return false;
                }
            }

            Directory.CreateDirectory(path);
            return true;
        }
    }
}