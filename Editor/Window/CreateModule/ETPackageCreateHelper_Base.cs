namespace ET.PackageManager.Editor
{
    public static partial class ETPackageCreateHelper
    {
        public static bool CreateBase(ETPackageCreateData codeData)
        {
            new ETPackageCreatePackageJsonCode(codeData);
            new ETPackageCreatePackageGitCode(codeData);

            return true;
        }
    }
}
