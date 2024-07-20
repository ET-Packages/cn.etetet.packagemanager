using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ET;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace ET.PackageManager.Editor
{
    public enum EPackagesFilterType
    {
        [LabelText("ET包")]
        ET = 1001,

        [LabelText("全部")]
        All = 10000,
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

        [BoxGroup("筛选条件", centerLabel: true)]
        [EnumToggleButtons]
        [HideLabel]
        [OnValueChanged("OnFilterTypeChanged")]
        [ShowIf("CheckUpdateAllEnd")]
        public EPackagesFilterType FilterType;

        private void OnFilterTypeChanged()
        {
            LoadFilterPackageInfoData();
        }

        [BoxGroup("筛选条件", centerLabel: true)]
        [OnInspectorGUI]
        [ShowIf("CheckUpdateAllEnd")]
        private void Space1() { GUILayout.Space(20); }

        [BoxGroup("筛选条件", centerLabel: true)]
        [LabelText("循环依赖同步修改")]
        [ShowIf("CheckUpdateAllEnd")]
        public bool SyncDependency = true;

        [BoxGroup("筛选条件", centerLabel: true)]
        [LabelText("仅显示可更新包")]
        [ShowIf("CheckUpdateAllEnd")]
        [OnValueChanged("OnShowUpdatePackageChanged")]
        public bool ShowUpdatePackage = true;

        private void OnShowUpdatePackageChanged()
        {
            LoadFilterPackageInfoData();
        }

        [BoxGroup("筛选条件", centerLabel: true)]
        [LabelText("搜索")]
        [ShowIf("CheckUpdateAllEnd")]
        [OnValueChanged("OnSearchChanged")]
        [Delayed]
        private string Search = "";

        private void OnSearchChanged()
        {
            Search = Search.ToLower();
            LoadFilterPackageInfoData();
        }

        private EnumPrefs<EPackagesFilterType> FilterTypePrefs        = new("YIUIPackagesModule_FilterType", null, EPackagesFilterType.ET);
        private BoolPrefs                      SyncDependencyPrefs    = new("YIUIPackagesModule_SyncDependency", null, true);
        private BoolPrefs                      ShowUpdatePackagePrefs = new("YIUIPackagesModule_ShowUpdatePackage", null, true);

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

                                             FilterType        = FilterTypePrefs.Value;
                                             SyncDependency    = SyncDependencyPrefs.Value;
                                             ShowUpdatePackage = ShowUpdatePackagePrefs.Value;
                                             LoadAllPackageInfoData();
                                             LoadFilterPackageInfoData();
                                             Inst = this;
                                         });
        }

        public override void OnDestroy()
        {
            Inst                         = null;
            FilterTypePrefs.Value        = FilterType;
            SyncDependencyPrefs.Value    = SyncDependency;
            ShowUpdatePackagePrefs.Value = ShowUpdatePackage;
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
                EditorUtility.DisplayProgressBar("同步包信息", $"更新{packageInfo.Name}", i * 1f / count);
                await ChangePackageInfo(packageInfo);
            }

            EditorUtility.ClearProgressBar();
            ETPackageAutoTool.CloseWindowRefresh();
        }

        public async Task SyncPackageUpdate(string name, string version)
        {
            await UpdatePackagesInfo();

            //EditorUtility.DisplayProgressBar($"更新包: {name} >> {version}", "", 0);
            //TODO 其他后续一键功能
            //EditorUtility.ClearProgressBar();
        }

        private async Task ChangePackageInfo(PackageInfoData packageInfoData)
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

        private JObject NewDependencies(PackageInfoData packageInfoData)
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
        private List<PackageInfoData> m_FilterPackageInfoDataList = new();

        private readonly Dictionary<string, PackageInfoData> m_FilterPackageInfoDataDic = new();

        public PackageInfoData GetPackageInfoData(string packageName)
        {
            m_FilterPackageInfoDataDic.TryGetValue(packageName, out PackageInfoData packageInfoData);
            return packageInfoData;
        }

        //所有包原始数据
        private readonly Dictionary<string, PackageInfoData> m_AllPackageInfoDataList = new();

        public PackageInfoData GetSourcePackageInfoData(string packageName)
        {
            m_AllPackageInfoDataList.TryGetValue(packageName, out PackageInfoData packageInfoData);
            return packageInfoData;
        }

        private void LoadFilterPackageInfoData()
        {
            m_FilterPackageInfoDataList.Clear();
            m_FilterPackageInfoDataDic.Clear();
            foreach (var data in m_AllPackageInfoDataList)
            {
                if (ShowUpdatePackage && !data.Value.CanUpdateVersion)
                {
                    continue;
                }

                var name = data.Key;

                if (!string.IsNullOrEmpty(Search) && !name.Contains(Search))
                {
                    continue;
                }

                var infoData = data.Value;
                var newData  = infoData.Copy();

                if (FilterType == EPackagesFilterType.All)
                {
                    m_FilterPackageInfoDataList.Add(newData);
                    m_FilterPackageInfoDataDic.Add(name, newData);
                }
                else if (FilterType == EPackagesFilterType.ET && name.Contains("cn.etetet."))
                {
                    m_FilterPackageInfoDataList.Add(newData);
                    m_FilterPackageInfoDataDic.Add(name, newData);
                }
                else
                {
                    Debug.LogError($"未实现这个类型的筛选 请注意 {FilterType}");
                }
            }
        }

        private void LoadAllPackageInfoData()
        {
            //初始化
            foreach (var packageInfo in PackageInfo.GetAllRegisteredPackages())
            {
                var name         = packageInfo.name;
                var version      = packageInfo.version;
                var dependencies = packageInfo.dependencies;

                var infoData = new PackageInfoData(name, version);
                infoData.Dependencies = new();
                foreach (var dependency in dependencies)
                {
                    infoData.Dependencies.Add(new DependencyInfo()
                                              {
                                                  SelfName         = name,
                                                  Name             = dependency.name,
                                                  Version          = dependency.version,
                                                  DependenciesSelf = false
                                              });
                }

                m_AllPackageInfoDataList.Add(name, infoData);
            }

            //处理依赖关系
            foreach (var data in m_AllPackageInfoDataList.Values)
            {
                var name         = data.Name;
                var dependencies = data.Dependencies;
                foreach (var dependency in dependencies)
                {
                    if (!m_AllPackageInfoDataList.ContainsKey(dependency.Name))
                    {
                        Debug.LogError($"{name}依赖包{dependency.Name}不存在");
                        continue;
                    }

                    var target = m_AllPackageInfoDataList[dependency.Name];

                    if (target.IsETPackage && !CheckVersion(dependency, target))
                    {
                        Debug.LogError($"[{name}]依赖包[{dependency.Name}] 版本不匹配，依赖版本[{dependency.Version}]，当前版本[{target.Version}]");
                    }

                    if (target.DependenciesSelf == null)
                    {
                        target.DependenciesSelf = new();
                    }

                    target.DependenciesSelf.Add(new DependencyInfo()
                                                {
                                                    SelfName         = dependency.Name,
                                                    Name             = name,
                                                    Version          = dependency.Version,
                                                    DependenciesSelf = true,
                                                });
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

        private bool CheckVersion(DependencyInfo dependencyInfo, PackageInfoData targetData)
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
