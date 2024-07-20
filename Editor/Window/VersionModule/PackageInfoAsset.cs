using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace ET.PackageManager.Editor
{
    [LabelText("所有包信息")]
    public class PackageInfoAsset : SerializedScriptableObject
    {
        [OdinSerialize]
        [ReadOnly]
        [ShowInInspector]
        public Dictionary<string, string> AllLastPackageInfo = new(); //所有包信息的最新版本号

        [OdinSerialize]
        [ReadOnly]
        [ShowInInspector]
        public HashSet<string> BanPackageInfo = new(); //黑名单 可不请求数据

        [ShowInInspector]
        public long LastUpdateTime; //上一次更新时间 防止重复更新 如果想强制更新可以手动设置改为0即可

        [ShowInInspector]
        public long UpdateInterval = 3600; //更新间隔 真不需要很高的频率 哪里有插件一直更新的
    }
}