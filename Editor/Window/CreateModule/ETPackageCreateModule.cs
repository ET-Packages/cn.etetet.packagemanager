#if ODIN_INSPECTOR
using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// 一键生成属于自己的Package
    /// </summary>
    [ETPackageMenu("创建")]
    public class ETPackageCreateModule : BasePackageToolModule
    {
        [Button("文档", 30, Icon = SdfIconType.Link45deg, IconAlignment = IconAlignment.LeftOfText)]
        [PropertyOrder(-999)]
        public void OpenDocument()
        {
            ETPackageDocumentModule.ETPackageCreate();
        }

        public override void Initialize()
        {
        }

        public override void OnDestroy()
        {
        }

        [BoxGroup("生成类型", centerLabel: true)]
        [EnumToggleButtons]
        [HideLabel]
        public EPackageCreateType PackageCreateType = EPackageCreateType.All;

        [BoxGroup("Runtime引用类型", centerLabel: true)]
        [EnumToggleButtons]
        [HideLabel]
        [ShowIf("OnRuntimeRefTypeShowIf")]
        public EPackageRuntimeRefType RuntimeRefType = EPackageRuntimeRefType.All;

        private bool OnRuntimeRefTypeShowIf()
        {
            return PackageCreateType.HasFlag(EPackageCreateType.Runtime);
        }

        [BoxGroup("CodeMode类型", centerLabel: true)]
        [EnumToggleButtons]
        [HideLabel]
        [ShowIf("OnFolderTypeShowIf")]
        public EPackageCreateFolderType FolderType = EPackageCreateFolderType.All;

        private bool OnFolderTypeShowIf()
        {
            return PackageCreateType.HasFlag(EPackageCreateType.Hotfix) || PackageCreateType.HasFlag(EPackageCreateType.Model);
        }

        [Required]
        [LabelText("作者")]
        public string PackageAuthor;

        [Required]
        [LabelText("模块名称 cn.etetet.{0}")]
        public string PackageName;

        [InfoBox("模块ID 用于区分模块 1000以下为ET官方保有ID 其他ID请向ET官方申请")]
        [LabelText("模块ID")]
        public int PackageId;

        [Required]
        [InfoBox("推荐ET.{0} 也可以自定义")]
        [LabelText("显示名称")]
        public string DisplayName;

        [Required]
        [LabelText("程序集名称")]
        [ShowIf("OnRuntimeRefTypeShowIf")]
        public string AssemblyName;

        [LabelText("描述")]
        public string Description;

        [LabelText("强制创建")]
        public bool ForceCreate;

        private string PackagePath;

        [Button("创建", 50)]
        [GUIColor(0f, 1f, 0f)]
        [PropertyOrder(-999)]
        public void CreatePackage()
        {
            if ((int)PackageCreateType == 0)
            {
                UnityTipsHelper.Show("必须选择生成类型");
                return;
            }

            if (string.IsNullOrEmpty(PackageAuthor))
            {
                UnityTipsHelper.Show("必须输入 作者名称");
                return;
            }

            if (OnRuntimeRefTypeShowIf() && string.IsNullOrEmpty(AssemblyName))
            {
                UnityTipsHelper.Show("必须输入 程序集名称");
                return;
            }

            if (string.IsNullOrEmpty(DisplayName))
            {
                UnityTipsHelper.Show("必须输入 显示名称");
                return;
            }

            if (string.IsNullOrEmpty(PackageName))
            {
                UnityTipsHelper.Show("必须输入 模块名称");
                return;
            }

            PackagePath = ETPackageCreateHelper.GetPackagePath(ref PackageName);
            var projPath = EditorHelper.GetProjPath(PackagePath);

            if (!ETPackageCreateHelper.CreateDirectory(projPath, ForceCreate))
            {
                UnityTipsHelper.Show($"模块[ {PackageName} ] 已存在 请勿重复创建");
                return;
            }

            CreateTargetModule();

            UnityTipsHelper.Show($"创建成功 [ {PackageName} ]");

            PackageExecuteMenuItemHelper.ETAll();

            ETPackageAutoTool.CloseWindowRefresh();
        }

        private void CreateTargetModule()
        {
            var data = new ETPackageCreateData
            {
                PackageAuthor  = this.PackageAuthor,
                PackagePath    = this.PackagePath,
                PackageId      = this.PackageId,
                PackageName    = this.PackageName,
                AssemblyName   = this.AssemblyName,
                DisplayName    = this.DisplayName,
                Description    = this.Description,
                RuntimeRefType = this.RuntimeRefType,
                FolderType     = this.FolderType,
            };

            var createTypeValues = Enum.GetValues(typeof(EPackageCreateType));

            foreach (var value in createTypeValues)
            {
                var createType = (EPackageCreateType)value;
                if ((int)createType <= 0)
                {
                    continue;
                }

                var hasReslut = PackageCreateType.HasFlag(createType);
                if (hasReslut)
                {
                    switch (createType)
                    {
                        case EPackageCreateType.Runtime:
                            new ETPackageCreatePackageAsmdefCode(data);
                            new ETPackageCreatePackageIgnoreAsmdefCode(data);
                            break;
                        case EPackageCreateType.Editor:
                            new ETPackageCreatePackageAsmdefEditorCode(data);
                            break;
                        case EPackageCreateType.Hotfix:
                            var hotfixClient = FolderType.HasFlag(EPackageCreateFolderType.Client);
                            if (hotfixClient)
                            {
                                ETPackageCreateHelper.CreateDirectory(EditorHelper.GetProjPath($"{PackagePath}/Scripts/Hotfix/Client"));
                            }

                            var hotfixServer = FolderType.HasFlag(EPackageCreateFolderType.Server);
                            if (hotfixServer)
                            {
                                ETPackageCreateHelper.CreateDirectory(EditorHelper.GetProjPath($"{PackagePath}/Scripts/Hotfix/Server"));
                            }

                            var hotfixShare = FolderType.HasFlag(EPackageCreateFolderType.Share);
                            if (hotfixShare)
                            {
                                ETPackageCreateHelper.CreateDirectory(EditorHelper.GetProjPath($"{PackagePath}/Scripts/Hotfix/Share"));
                            }

                            break;
                        case EPackageCreateType.HotfixView:
                            ETPackageCreateHelper.CreateDirectory(EditorHelper.GetProjPath($"{PackagePath}/Scripts/HotfixView/Client"));
                            break;
                        case EPackageCreateType.Model:
                            var modelClient = FolderType.HasFlag(EPackageCreateFolderType.Client);
                            if (modelClient)
                            {
                                ETPackageCreateHelper.CreateDirectory(EditorHelper.GetProjPath($"{PackagePath}/Scripts/Model/Client"));
                            }

                            var modelServer = FolderType.HasFlag(EPackageCreateFolderType.Server);
                            if (modelServer)
                            {
                                ETPackageCreateHelper.CreateDirectory(EditorHelper.GetProjPath($"{PackagePath}/Scripts/Model/Server"));
                            }

                            var modelShare = FolderType.HasFlag(EPackageCreateFolderType.Share);
                            if (modelShare)
                            {
                                ETPackageCreateHelper.CreateDirectory(EditorHelper.GetProjPath($"{PackagePath}/Scripts/Model/Share"));
                            }

                            break;
                        case EPackageCreateType.ModelView:
                            ETPackageCreateHelper.CreateDirectory(EditorHelper.GetProjPath($"{PackagePath}/Scripts/ModelView/Client"));
                            break;
                        default:
                            Debug.LogError($"未实现的功能 {createType}");
                            break;
                    }
                }
            }

            new ETPackageCreatePackageJsonCode(data);
            new ETPackageCreatePackageGitCode(data);
        }
    }
}
#endif
