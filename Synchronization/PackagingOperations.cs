using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Events;

namespace StudioManette.minus
{
    public static class PackagingOperations 
    {
        public static bool isRunningAsyncOperation = false;

        private static List<PackageManifestItem> tmpPackageList;
        private static ListRequest _listRequest;
        private static UnityEvent OnQuitList;

        private static AddRequest _addRequest;


        public static void SynchronizeLocalPackages(out List<PackageManifestItem> packageList)
        {
            tmpPackageList = new List<PackageManifestItem>();

            _listRequest = Client.List();
            EditorApplication.update += ProgressListPackage;
            OnQuitList = new UnityEvent();
            OnQuitList.AddListener(ReturnPackageList(out packageList));
        }

        private static void ProgressListPackage()
        {
            isRunningAsyncOperation = true;
            if (_listRequest.IsCompleted)
            {
                if (_listRequest.Status == StatusCode.Success)
                {
                    foreach (UnityEditor.PackageManager.PackageInfo pcInfo in _listRequest.Result)
                    {
                        //Debug.Log("package info : " + pcInfo.name + " / version : " + pcInfo.version);
                        tmpPackageList.Add(new PackageManifestItem(pcInfo.name, pcInfo.version));
                    }
                }
                else if (_listRequest.Status >= StatusCode.Failure)
                {
                    Debug.Log(_listRequest.Error.message);
                }
                isRunningAsyncOperation = false;
                EditorApplication.update -= ProgressListPackage;
                OnQuitList.Invoke();
            }
        }

        private static UnityAction ReturnPackageList(out List<PackageManifestItem> packageList)
        {
            packageList = tmpPackageList;
            return null;
        }

        public static void UpdatePackage(string packageName, string newVersion)
        {
            // - To install a specific version of a package, use a package identifier ("name@version"). This is the only way to install a pre-release version.
            string identifier = packageName + "@" + newVersion;
            _addRequest = Client.Add(identifier);
            EditorApplication.update += ProgressAddPackage;
        }

        private static void ProgressAddPackage()
        {
            isRunningAsyncOperation = true;
            if (_addRequest.IsCompleted)
            {
                if (_addRequest.Status == StatusCode.Success)
                {
                    UnityEditor.PackageManager.PackageInfo pcInfo = _addRequest.Result;
                    {
                        Debug.Log("package well installed.");
                        Debug.Log("package info : " + pcInfo.name + " / version : " + pcInfo.version);
                        //minusPackageList.Add(new PackageManifestItem(pcInfo.name, pcInfo.version));
                    }
                }
                else if (_addRequest.Status >= StatusCode.Failure)
                {
                    Debug.Log(_addRequest.Error.message);
                }
                isRunningAsyncOperation = false;
                EditorApplication.update -= ProgressAddPackage;
            }
        }
    }
}
