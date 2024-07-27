namespace ET.PackageManager.Editor
{
    public enum EPackageCategoryType
    {
        All = 0,
        Demo,
        Core,
        UI,
        Other
    }

    public class PackageCategoryData
    {
        public EPackageCategoryType CategoryType;
        public string               Category;
    }
}