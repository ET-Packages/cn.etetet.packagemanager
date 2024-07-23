﻿#if ODIN_INSPECTOR
using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// 一键生成属于自己的Package
    /// </summary>
    [ETPackageMenu("生成")]
    public class ETPackageCreateModule : BasePackageToolModule
    {
        public override void Initialize()
        {
        }

        public override void OnDestroy()
        {
        }

        [BoxGroup("生成类型", centerLabel: true)]
        [EnumToggleButtons]
        [HideLabel]
        public EPackageCreateType PackageCreateType;

        [BoxGroup("Runtime引用类型", centerLabel: true)]
        [EnumToggleButtons]
        [HideLabel]
        [ShowIf("OnRuntimeRefTypeShowIf")]
        public EPackageRuntimeRefType RuntimeRefType;

        private bool OnRuntimeRefTypeShowIf()
        {
            return PackageCreateType.HasFlag(EPackageCreateType.Runtime);
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

        [Button("生成", 50)]
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

                            break;
                        case EPackageCreateType.Editor:
                            break;
                        case EPackageCreateType.Hotfix:
                            break;
                        case EPackageCreateType.HotfixView:
                            break;
                        case EPackageCreateType.Model:
                            break;
                        case EPackageCreateType.ModelView:
                            break;
                        default:
                            Debug.LogError($"未实现的功能 {createType}");
                            break;
                    }
                }
            }

            ETPackageCreateHelper.CreateBase(data);
        }
    }
}
#endif
