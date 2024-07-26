#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    [HideReferenceObjectPicker]
    public abstract class BasePackageToolModule : BaseCreateModule
    {
        [HideInInspector]
        public OdinMenuEditorWindow AutoTool { get; internal set; }

        [HideInInspector]
        public OdinMenuTree Tree { get; internal set; }

        [HideInInspector]
        public string ModuleName { get; internal set; }

        [HideInInspector]
        public object UserData { get; internal set; }

        protected OdinMenuItem MenuItem;

        public void SelectSelf()
        {
            if (MenuItem == null) return;
            MenuItem.Select();
            if (Tree.Selection.SelectedValue is BaseTreeMenuItem menuItem)
            {
                menuItem.SelectionMenu();
            }
        }

        public virtual void SelectionMenu()
        {
        }
    }
}
#endif
