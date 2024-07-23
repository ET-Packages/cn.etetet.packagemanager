namespace ET.PackageManager.Editor
{
    public class ETPackageCreatePackageJsonCode : BaseTemplate
    {
        private readonly string m_EventName = "PackageJson";
        public override  string EventName => m_EventName;

        public override bool Cover => true;

        private readonly bool m_AutoRefresh = false;
        public override  bool AutoRefresh => m_AutoRefresh;

        private readonly bool m_ShowTips = false;
        public override  bool ShowTips => m_ShowTips;

        public ETPackageCreatePackageJsonCode(out bool result, string authorName, ETPackageCreateData codeData) : base(authorName)
        {
            var path     = $"{codeData.PackagePath}/package.json";
            var template = $"{ETPackageCreateHelper.ETPackageCreateTemplatePath}/package.txt";
            CreateVo = new CreateVo(template, path);

            ValueDic["PackageName"] = codeData.PackageName;
            ValueDic["DisplayName"] = codeData.DisplayName;
            ValueDic["Description"] = codeData.Description;

            result = CreateNewFile();
        }
    }
}