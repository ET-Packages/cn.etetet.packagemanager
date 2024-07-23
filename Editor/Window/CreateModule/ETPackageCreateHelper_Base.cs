namespace ET.PackageManager.Editor
{
    public static partial class ETPackageCreateHelper
    {
        public static bool CreateBase(ETPackageCreateData codeData)
        {
            var code = new ETPackageCreatePackageJsonCode(out bool result, "", codeData);
            return result;
        }
    }

}