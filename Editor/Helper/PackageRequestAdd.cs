using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    public class PackageRequestAdd
    {
        private AddRequest                                     m_Request;
        private Action<UnityEditor.PackageManager.PackageInfo> m_RequestCallback;

        public PackageRequestAdd(string name, Action<UnityEditor.PackageManager.PackageInfo> callback)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError($"null包 请传入名称");
                callback?.Invoke(null);
                return;
            }

            this.m_RequestCallback = callback;

            this.m_Request = Client.Add(name);

            EditorApplication.update += UpdateRequest;
        }

        private void UpdateRequest()
        {
            if (!this.m_Request.IsCompleted) return;

            if (this.m_Request.Status == StatusCode.Success)
            {
                if (this.m_Request.Result != null)
                {
                    var packageInfo = this.m_Request.Result;
                    this.m_RequestCallback?.Invoke(packageInfo);
                }
                else
                {
                    this.m_RequestCallback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError(this.m_Request.Error.message);
                this.m_RequestCallback?.Invoke(null);
            }

            EditorApplication.update -= UpdateRequest;
            this.m_RequestCallback   =  null;
        }
    }
}