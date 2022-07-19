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
        /*
         *  public variables
         */
        public static bool isRunningAsyncOperation = false;

        /*
         *  private Common variables
         */
        public delegate void ResponseString ( string response );

        /*
         *  private variables for SynchronizeLocalPackages method
         */
        private static ListRequest _listRequest;
        public delegate void ResponsePackageList(List<PackageManifestItem> response);
        private static ResponsePackageList delegateResponsePackageList;

        /*
         *  private variables for UpdatePackage method
         */
        private static AddRequest _addRequest;

        /*
         *  private variables for GetVersionInDependencies method
         */
        private static ListRequest _listDependenciesRequest;
        private static string _packageName;
        private static string _dependencyName;
        private static ResponseString delegateResponseDependenciesRequest;

        //public static void SynchronizeLocalPackages(out List<PackageManifestItem> packageList)
        public static void SynchronizeLocalPackages(ResponsePackageList delegateResponse)
        {

            _listRequest = Client.List();
            EditorApplication.update += ProgressListPackage;
            delegateResponsePackageList = delegateResponse;
        }

        private static void ProgressListPackage()
        {
            isRunningAsyncOperation = true;
            if (_listRequest.IsCompleted)
            {
                List<PackageManifestItem> tmpPackageList = new List<PackageManifestItem>();
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

                delegateResponsePackageList(tmpPackageList);

                //clean variables
                _listRequest = null;
            }
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

                //clean variables
                _addRequest = null;
            }
        }

        public static void GetVersionInDependencies(string packageName, string dependencyName, ResponseString delegateResponse)
        {
            _packageName = packageName;
            _dependencyName = dependencyName;

            _listDependenciesRequest = Client.List();
            EditorApplication.update += ProgressDepedencies;

            delegateResponseDependenciesRequest = delegateResponse;
        }

        private static void ProgressDepedencies()
        {
            isRunningAsyncOperation = true;
            if (_listDependenciesRequest.IsCompleted)
            {
                string dependencyVersion = null;

                if (_listDependenciesRequest.Status == StatusCode.Success)
                {
                    foreach (UnityEditor.PackageManager.PackageInfo pcInfo in _listDependenciesRequest.Result)
                    {
                        if (string.Equals(pcInfo.name, _packageName))
                        {
                            foreach (DependencyInfo di in pcInfo.dependencies)
                            {
                                if (di.name == _dependencyName)
                                {
                                    dependencyVersion = di.version;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else if (_listDependenciesRequest.Status >= StatusCode.Failure)
                {
                    Debug.Log(_listDependenciesRequest.Error.message);
                }
                isRunningAsyncOperation = false;
                EditorApplication.update -= ProgressDepedencies;

                delegateResponseDependenciesRequest(dependencyVersion);

                //clean variables
                _listDependenciesRequest = null;
                _packageName = null;
                _dependencyName = null;
            }
        }
    }
}
