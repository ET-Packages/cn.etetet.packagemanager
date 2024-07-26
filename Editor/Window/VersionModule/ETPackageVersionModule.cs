#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    [Flags]
    public enum EPackagesFilterType
    {
        [LabelText("全部")]
        All = 1 << 30,

        [LabelText("无")]
        None = 1,

        [LabelText("ET包")]
        ET = 1 << 1,

        [LabelText("更新")]
        Update = 1 << 2,

        [LabelText("请求")]
        Req = 1 << 3,

        [LabelText("禁用")]
        Ban = 1 << 4,

        [LabelText("解禁")]
        ReBan = 1 << 5,
    }

    public enum EPackagesFilterOperationType
    {
        [LabelText("唯一")]
        Only = 0,

        [LabelText("或")]
        Or = 1,

        [LabelText("与")]
        And = 2,
    }

    /// <summary>
    /// 版本管理
    /// </summary>
    [ETPackageMenu("版本管理", 1000)]
    public class ETPackageVersionModule : BasePackageToolModule
    {
        public static ETPackageVersionModule Inst;

        [HideLabel]
        [HideIf("CheckUpdateAllEnd")]
        [ShowInInspector]
        [DisplayAsString(false, 100, TextAlignment.Center, true)]
        private static string m_CheckUpdateAllReqing = "请求所有包最新版本中...";

        [BoxGroup("信息", centerLabel: true)]
        [EnumToggleButtons]
        [HideLabel]
        [OnValueChanged("OnFilterOperationTypeChanged")]
        [ShowIf("CheckUpdateAllEnd")]
        public EPackagesFilterOperationType FilterOperationType;

        private void OnFilterOperationTypeChanged()
        {
            if (FilterOperationType == EPackagesFilterOperationType.Only)
            {
                LastFilterType = EPackagesFilterType.None;
                FilterType     = EPackagesFilterType.None;
            }

            LoadFilterPackageInfoData();
        }

        [BoxGroup("信息", centerLabel: true)]
        [EnumToggleButtons]
        [HideLabel]
        [OnValueChanged("OnFilterTypeChanged")]
        [ShowIf("CheckUpdateAllEnd")]
        public EPackagesFilterType FilterType;

        private EPackagesFilterType LastFilterType;

        private void OnFilterTypeChanged()
        {
            if (FilterOperationType == EPackagesFilterOperationType.Only)
            {
                if (FilterType != LastFilterType)
                {
                    var current = (int)FilterType;
                    var last    = (int)LastFilterType;
                    FilterType = (EPackagesFilterType)(current > last ? current - last : last - current);
                }
            }

            LastFilterType = FilterType;
            LoadFilterPackageInfoData();
        }

        [BoxGroup("信息", centerLabel: true)]
        [LabelText("循环依赖同步修改")]
        [ShowIf("CheckUpdateAllEnd")]
        public bool SyncDependency = true;

        [BoxGroup("信息", centerLabel: true)]
        [LabelText("搜索")]
        [ShowIf("CheckUpdateAllEnd")]
        [OnValueChanged("OnSearchChanged")]
        [Delayed]
        [ShowInInspector]
        private string Search = "";

        private void OnSearchChanged()
        {
            Search = Search.ToLower();
            LoadFilterPackageInfoData();
        }

        private EnumPrefs<EPackagesFilterType> FilterTypePrefs = new("ETPackageVersionModule_FilterType", null, EPackagesFilterType.ET);

        private EnumPrefs<EPackagesFilterOperationType> FilterOperationTypePrefs = new("ETPackageVersionModule_FilterOperationType");

        private BoolPrefs   SyncDependencyPrefs = new("ETPackageVersionModule_SyncDependency", null, true);
        private StringPrefs SearchPrefs         = new("ETPackageVersionModule_Search", null, "");

        public bool CheckUpdateAllEnd { get; private set; }

        public bool RequestAllResult { get; private set; }

        public override void Initialize()
        {
            CheckUpdateAllEnd = false;
            RequestAllResult  = false;
            PackageHelper.CheckUpdateAll((result) =>
            {
                CheckUpdateAllEnd = true;
                RequestAllResult  = result;
                if (!result) return;
                Search              = SearchPrefs.Value;
                FilterType          = FilterTypePrefs.Value;
                LastFilterType      = FilterType;
                SyncDependency      = SyncDependencyPrefs.Value;
                FilterOperationType = FilterOperationTypePrefs.Value;
                LoadAllPackageInfoData();
                LoadFilterPackageInfoData();
                Inst = this;
            });
        }

        public override void OnDestroy()
        {
            Inst                           = null;
            SearchPrefs.Value              = Search;
            FilterTypePrefs.Value          = FilterType;
            SyncDependencyPrefs.Value      = SyncDependency;
            FilterOperationTypePrefs.Value = FilterOperationType;
        }

        [Button("同步", 50)]
        [GUIColor(1, 1, 0)]
        [PropertyOrder(-100)]
        [ShowIf("CheckUpdateAllEnd")]
        public void SyncPackages()
        {
            UpdatePackagesInfo();
        }

        private async Task UpdatePackagesInfo()
        {
            var count = m_FilterPackageInfoDataList.Count;
            for (int i = 0; i < count; i++)
            {
                var packageInfo = m_FilterPackageInfoDataList[i];
                EditorUtility.DisplayProgressBar("同步信息", $"更新{packageInfo.Name}", i * 1f / count);
                await ChangePackageInfo(packageInfo);
            }

            EditorUtility.ClearProgressBar();
            ETPackageAutoTool.CloseWindowRefresh();
        }

        public void SyncPackageUpdate(string name, string version)
        {
            Task.Run(UpdatePackagesInfo);

            //EditorUtility.DisplayProgressBar($"更新包: {name} >> {version}", "", 0);
            //TODO 其他后续一键功能
            //EditorUtility.ClearProgressBar();
        }

        private async Task ChangePackageInfo(PackageVersionData packageInfoData)
        {
            var assetPath   = $"Packages/{packageInfoData.Name}/package.json";
            var packagePath = $"{Application.dataPath}/../{assetPath}";
            if (!File.Exists(packagePath))
            {
                //Debug.LogError($"包 {packageInfoData.Name} 路径 {packagePath} 不存在");
                return;
            }

            var changeVersion = Regex.Replace(packageInfoData.Version, Pattern, "");

            try
            {
                //TODO 这里没有判断是不是有更新全部都同步
                //首先这个频率不高 其次判断也节省不了多少时间 如果有需要在扩展

                string fileContent = await File.ReadAllTextAsync(packagePath);

                JObject json = JObject.Parse(fileContent);

                json["version"]      = changeVersion;
                json["dependencies"] = NewDependencies(packageInfoData);

                await File.WriteAllTextAsync(packagePath, json.ToString(), System.Text.Encoding.UTF8);

                //Debug.Log($"修改成功 {packageInfoData.Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"错误 修改包 {packageInfoData.Name} 失败 {e.Message}");
                return;
            }
        }

        private JObject NewDependencies(PackageVersionData packageInfoData)
        {
            JObject newDependencies = new JObject();
            foreach (var dependency in packageInfoData.Dependencies)
            {
                newDependencies[dependency.Name] = Regex.Replace(dependency.Version, Pattern, "");
            }

            return newDependencies;
        }

        [TableList(DrawScrollView = true, AlwaysExpanded = true, IsReadOnly = true)]
        [NonSerialized]
        [BoxGroup("筛选包数据", centerLabel: true)]
        [HideLabel]
        [ShowInInspector]
        [ShowIf("CheckUpdateAllEnd")]
        private List<PackageVersionData> m_FilterPackageInfoDataList = new();

        private readonly Dictionary<string, PackageVersionData> m_FilterPackageInfoDataDic = new();

        public PackageVersionData GetPackageInfoData(string packageName)
        {
            m_FilterPackageInfoDataDic.TryGetValue(packageName, out PackageVersionData packageInfoData);
            return packageInfoData;
        }

        private void LoadFilterPackageInfoData()
        {
            m_FilterPackageInfoDataList.Clear();
            m_FilterPackageInfoDataDic.Clear();
            var packagesFilterTypeValues = Enum.GetValues(typeof(EPackagesFilterType));
            foreach (var data in PackageVersionHelper.PackageVersionAsset.AllPackageVersionData)
            {
                var name = data.Key;

                if (!string.IsNullOrEmpty(Search) && !name.Contains(Search))
                {
                    continue;
                }

                var infoData = data.Value;
                var add      = false;

                switch (FilterOperationType)
                {
                    case EPackagesFilterOperationType.Only:
                        add = GetResult(FilterType);
                        break;
                    case EPackagesFilterOperationType.Or:
                        add = false;
                        foreach (var value in packagesFilterTypeValues)
                        {
                            var filterTypeValue = (EPackagesFilterType)value;
                            var hasReslut       = FilterType.HasFlag(filterTypeValue);
                            if (hasReslut)
                            {
                                add = GetResult(filterTypeValue);
                                if (add)
                                {
                                    break;
                                }
                            }
                        }

                        break;
                    case EPackagesFilterOperationType.And:
                        add = true;
                        foreach (var value in packagesFilterTypeValues)
                        {
                            var filterTypeValue = (EPackagesFilterType)value;
                            var hasReslut       = FilterType.HasFlag(filterTypeValue);
                            if (hasReslut)
                            {
                                add = GetResult(filterTypeValue);
                                if (!add)
                                {
                                    break;
                                }
                            }
                        }

                        break;
                    default:
                        //Debug.LogError($"未实现的操作类型 {FilterOperationType}");
                        break;
                }

                if (!add) continue;

                var newData = infoData.Copy();
                m_FilterPackageInfoDataList.Add(newData);
                m_FilterPackageInfoDataDic.Add(name, newData);

                continue;

                bool GetResult(EPackagesFilterType filterValue)
                {
                    var result = false;
                    switch (filterValue)
                    {
                        case EPackagesFilterType.All:
                            result = true;
                            break;
                        case EPackagesFilterType.None:
                            result = false;
                            break;
                        case EPackagesFilterType.ET:
                            result = infoData.IsETPackage;
                            break;
                        case EPackagesFilterType.Update:
                            result = infoData.CanUpdateVersion;
                            break;
                        case EPackagesFilterType.Req:
                            result = infoData.ShowIfReqVersion();
                            break;
                        case EPackagesFilterType.Ban:
                            result = infoData.ShowIfBanReqVersion();
                            break;
                        case EPackagesFilterType.ReBan:
                            result = infoData.ShowIfReBanReqVersion();
                            break;
                        default:
                            //Debug.LogError($"新增了筛选条件 请扩展 {filterValue}");
                            break;
                    }

                    return result;
                }
            }
        }

        private void LoadAllPackageInfoData()
        {
            var allPackageInfoDataList = PackageVersionHelper.PackageVersionAsset.AllPackageVersionData;

            //处理依赖检查
            foreach (var data in allPackageInfoDataList.Values)
            {
                var name         = data.Name;
                var dependencies = data.Dependencies;
                foreach (var dependency in dependencies)
                {
                    if (!allPackageInfoDataList.ContainsKey(dependency.Name))
                    {
                        Debug.LogError($"{name}依赖包{dependency.Name}不存在");
                        continue;
                    }

                    var target = allPackageInfoDataList[dependency.Name];

                    if (target.IsETPackage && !CheckVersion(dependency, target))
                    {
                        Debug.LogError($"[{name}]依赖包[{dependency.Name}] 版本不匹配，依赖版本[{dependency.Version}]，当前版本[{target.Version}]");
                    }
                }
            }
        }

        private readonly Dictionary<string, int[]> m_VersionValueDict = new();

        public const string Pattern = "[^0-9.]";

        private int[] GetVersionValue(string version)
        {
            if (m_VersionValueDict.TryGetValue(version, out int[] value))
            {
                return value;
            }

            var versionSplit = Regex.Replace(version, Pattern, "").Split('.');
            m_VersionValueDict[version] = new int[versionSplit.Length];
            for (int i = 0; i < versionSplit.Length; i++)
            {
                if (!int.TryParse(versionSplit[i], out m_VersionValueDict[version][i]))
                {
                    Debug.LogError($"{version} {i} {versionSplit[i]} 不是数字");
                }
            }

            return m_VersionValueDict[version];
        }

        private bool CheckVersion(DependencyInfo dependencyInfo, PackageVersionData targetData)
        {
            var dependencyVersion  = dependencyInfo.Version;
            var versionValue       = GetVersionValue(dependencyVersion);
            var targetVersionValue = targetData?.VersionValue;
            if (targetVersionValue == null)
            {
                Debug.Log($"{targetData.Name} Version == null");
                return false;
            }

            //长度判断
            if (targetVersionValue.Length != versionValue.Length)
            {
                Debug.Log($"{targetData.Name} Version长度不一致，实际:{targetData.Version} 与 依赖:{dependencyVersion}不匹配");
                return false;
            }

            //大版本号判断
            if (targetVersionValue.Length >= 1 && targetVersionValue[0] != versionValue[0])
            {
                Debug.Log($"{targetData.Name} 大版本号不匹配，实际:{targetData.Version} 与 依赖:{dependencyVersion}不匹配");
                return false;
            }

            //中版本号判断
            if (targetVersionValue.Length >= 2 && targetVersionValue[1] != versionValue[1])
            {
                Debug.Log($"{targetData.Name} 中版本号不匹配，实际:{targetData.Version} 与 依赖:{dependencyVersion}不匹配");
                return false;
            }

            //小版本号判断
            if (targetVersionValue.Length >= 3 && targetVersionValue[2] < versionValue[2])
            {
                Debug.Log($"{targetData.Name} 小版本号不匹配，实际:{targetData.Version} 与 依赖:{dependencyVersion}不匹配");
                return false;
            }

            return true;
        }
    }
}
#endif
