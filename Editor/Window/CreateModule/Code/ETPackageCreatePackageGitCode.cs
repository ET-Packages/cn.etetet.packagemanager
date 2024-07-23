namespace ET.PackageManager.Editor
{
    public class ETPackageCreatePackageGitCode : BaseTemplate
    {
        private readonly string m_EventName = "PackageGit";
        public override  string EventName => m_EventName;

        public override bool Cover => true;

        private readonly bool m_AutoRefresh = false;
        public override  bool AutoRefresh => m_AutoRefresh;

        private readonly bool m_ShowTips = false;
        public override  bool ShowTips => m_ShowTips;

        public ETPackageCreatePackageGitCode(out bool result, string authorName, ETPackageCreateData codeData) : base(authorName)
        {
            var path     = $"{codeData.PackagePath}/packagegit.json";
            var template = $"{ETPackageCreateHelper.ETPackageCreateTemplatePath}/package.txt";
            CreateVo = new CreateVo(template, path);

            ValueDic["PackageName"] = codeData.PackageName;
            ValueDic["DisplayName"] = codeData.DisplayName;
            ValueDic["Description"] = codeData.Description;

            result = CreateNewFile();
        }
    }
}