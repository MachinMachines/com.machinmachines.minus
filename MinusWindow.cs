using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

using StudioManette.Utils;

namespace StudioManette.minus
{
    public class MinusWindow : EditorWindow
    {
        private static readonly int WIDTH_CASE_PACKAGE = 100;

        /* visible properties */
        public MinusSettingsObject currentMinusSettings;

        private List<PackageManifestItem> primaryPackageList;
        private List<PackageManifestItem> thisPackageList;

        private Dictionary<string, string> primaryProjectSettingFiles;
        private Dictionary<string, string> thisProjectSettingFiles;


        /* ui properties */
        private GUIStyle validStyle;
        private GUIStyle wrongStyle;
        private bool showPackages;
        private Vector2 scrollPosPackages;
        private bool showProjectSettings;
        private Vector2 scrollPosProjectSettings;

        /* properties for async operations */
        private ListRequest _listRequest;
        private AddRequest _addRequest;
        private bool isRunningAsyncOperation = false;

        /* Getters */ 
        public string PrimarySettingsDirectory
        {
            get { return currentMinusSettings.primaryProject.path + "/ProjectSettings"; }
        }

        public string ThisSettingsDirectory
        {
            get { return Directory.GetCurrentDirectory() + "/ProjectSettings"; }
        }

        /*
         * INITIALIZATION
         */

        [MenuItem("Studio Manette/Minus")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(MinusWindow), false, "Minus - Studio Manette");
        }

        public void OnEnable()
        {
            Init();
        }

        private void Init()
        {
            //styles
            validStyle = new GUIStyle(EditorStyles.label);
            validStyle.normal.textColor = Color.green;
            wrongStyle = new GUIStyle(EditorStyles.label);
            wrongStyle.normal.textColor = Color.yellow;

            List<MinusSettingsObject> settingsList = Utils.AssetDatabaseExtensions.FindAssetsByType<MinusSettingsObject>();
            if (settingsList.Count == 0)
            {
                Debug.LogError("There is no MinusSettingsObject in the project, please create one (Create/StudioManette/MinusSettings Asset).");
            }
            else if (settingsList.Count > 1)
            {
                Debug.LogError("There is more than one MinusSettingsObject, you should have only one. Please remove the extra ones.");
            }
            else
            {
                currentMinusSettings = settingsList[0];
            }
        }

        public void OnGUI()
        {
            //GROUP 1 : PROPERTIES
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("PROPERTIES", EditorStyles.boldLabel);

                currentMinusSettings = (MinusSettingsObject)EditorGUILayout.ObjectField(currentMinusSettings, typeof(MinusSettingsObject), false);
            }
            EditorGUILayout.EndVertical();

            //GROUP 2 : BUTTONS
            if (GUILayout.Button("Synchronize"))
            {
                SynchronizeLocalPackages();
                primaryPackageList = Synchronization.GetExternalPackagesList(currentMinusSettings.primaryProject.path + "/Packages");
                primaryProjectSettingFiles = Synchronization.GetHashedFilesOfDirectory(PrimarySettingsDirectory);
                thisProjectSettingFiles = Synchronization.GetHashedFilesOfDirectory(ThisSettingsDirectory);
            }
            
            if (isRunningAsyncOperation)
            {
                EditorGUILayout.LabelField("Waiting for synchronization...");
            }
            else
            {
                //GROUP 3 : DISPLAY PACKAGES INFO

                EditorGUILayout.BeginVertical("box");
                showPackages = EditorGUILayout.Foldout(showPackages, "PACKAGES", EditorStyles.foldoutHeader);
                if (showPackages)
                {
                    DisplayPackages();
                }
                EditorGUILayout.EndVertical();

                //GROUP 4 : DISPLAY PROJECT SETTINGS INFO

                EditorGUILayout.BeginVertical("box");
                showProjectSettings = EditorGUILayout.Foldout(showProjectSettings, "PROJECT SETTINGS", EditorStyles.foldoutHeader);
                if (showProjectSettings)
                {
                    DisplayProjectSettings();
                }
                EditorGUILayout.EndVertical();
            }
        }

        /**
         *  PACKAGE MANAGEMENT
         */

        private void DisplayPackages()
        {
            //Display Each Package
            if (primaryPackageList !=null && primaryPackageList.Count > 0 && thisPackageList != null && thisPackageList.Count > 0)
            {
                //Display Headers
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Package name", EditorStyles.boldLabel, GUILayout.Width ( WIDTH_CASE_PACKAGE * 3));
                EditorGUILayout.LabelField("Primary", EditorStyles.boldLabel, GUILayout.Width ( WIDTH_CASE_PACKAGE ));
                EditorGUILayout.LabelField("This", EditorStyles.boldLabel, GUILayout.Width ( WIDTH_CASE_PACKAGE ));
                EditorGUILayout.LabelField("Update", EditorStyles.boldLabel, GUILayout.Width ( WIDTH_CASE_PACKAGE ));
                EditorGUILayout.EndHorizontal();

                scrollPosPackages = EditorGUILayout.BeginScrollView(scrollPosPackages);
                foreach (PackageManifestItem package in primaryPackageList)
                {
                    string localPackageVersion = FindPackageVersionInThis(package.packageName);
                    bool isVersionValid = localPackageVersion.Equals(package.packageVersion);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(package.packageName, GUILayout.Width ( WIDTH_CASE_PACKAGE * 3 ));
                    EditorGUILayout.LabelField(package.packageVersion, GUILayout.Width ( WIDTH_CASE_PACKAGE ));
                    EditorGUILayout.LabelField(localPackageVersion, isVersionValid ? validStyle : wrongStyle, GUILayout.Width( WIDTH_CASE_PACKAGE ));

                    if (isVersionValid)
                    {
                        GUILayout.Label("------", GUILayout.Width( WIDTH_CASE_PACKAGE ));
                    }
                    else
                    {
                        if (GUILayout.Button("Update", GUILayout.Width(WIDTH_CASE_PACKAGE)))
                        {
                            UpdatePackage(package.packageName, package.packageVersion);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("please synchronize first.");
            }
        }

        private void UpdatePackage(string packageName, string newVersion)
        {
            if (EditorUtility.DisplayDialog("Warning", "Do you really want to update the package " + packageName + "to version " + newVersion + " ? ", "Yes", "No"))
            {
                PackagingOperations.UpdatePackage(packageName, newVersion);
                /*
                // - To install a specific version of a package, use a package identifier ("name@version"). This is the only way to install a pre-release version.
                string identifier = packageName + "@" + newVersion;
                _addRequest = Client.Add(identifier);
                EditorApplication.update += ProgressAddPackage;
                */
            }
        }

        /*
        private void ProgressAddPackage()
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
        */

        private string FindPackageVersionInThis(string _packageName)
        {
            foreach (PackageManifestItem package in thisPackageList)
            {
                if (_packageName.Equals(package.packageName))
                { 
                    return package.packageVersion;
                }
            }
            return "missing";
        }

        private void SynchronizeLocalPackages()
        {
            PackagingOperations.SynchronizeLocalPackages(out thisPackageList);
            /*
            thisPackageList = new List<PackageManifestItem>();

            _listRequest = Client.List();
            EditorApplication.update += ProgressListPackage;
            */
        }

        /*
        private void ProgressListPackage()
        {
            isRunningAsyncOperation = true;
            if (_listRequest.IsCompleted)
            {
                if (_listRequest.Status == StatusCode.Success)
                {
                    foreach (UnityEditor.PackageManager.PackageInfo pcInfo in _listRequest.Result)
                    {
                        //Debug.Log("package info : " + pcInfo.name + " / version : " + pcInfo.version);
                        thisPackageList.Add(new PackageManifestItem(pcInfo.name, pcInfo.version));
                    }
                }
                else if (_listRequest.Status >= StatusCode.Failure)
                {
                    Debug.Log(_listRequest.Error.message);
                }
                isRunningAsyncOperation = false;
                EditorApplication.update -= ProgressListPackage;
            }
        }
        */

        /**
         *  PROJECT SETTINGS MANAGEMENT
         */

        private void DisplayProjectSettings()
        {
            if (primaryProjectSettingFiles !=null && primaryProjectSettingFiles.Count > 0)
            {
                //Display Headers
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("File name", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE * 3));
                EditorGUILayout.LabelField("", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
                EditorGUILayout.LabelField("", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
                EditorGUILayout.LabelField("Update", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
                EditorGUILayout.EndHorizontal();

                scrollPosProjectSettings = EditorGUILayout.BeginScrollView(scrollPosProjectSettings);
                foreach (KeyValuePair<string, string> kvp in primaryProjectSettingFiles)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(WIDTH_CASE_PACKAGE*4));
                    //EditorGUILayout.LabelField(kvp.Value);

                    string checksumFromThis = FindLocalProjectSettingFile(kvp.Key);
                    bool isChecksumValid = kvp.Value.Equals(checksumFromThis);
                    EditorGUILayout.LabelField(isChecksumValid ? "Up to date": "Outdated", isChecksumValid ? validStyle : wrongStyle, GUILayout.Width(WIDTH_CASE_PACKAGE));

                    if (isChecksumValid)
                    {
                        GUILayout.Label("------", GUILayout.Width(WIDTH_CASE_PACKAGE));
                    }
                    else
                    {
                        if (GUILayout.Button("Update" , GUILayout.Width(WIDTH_CASE_PACKAGE)))
                        {
                            UpdateSettingFile(kvp.Key);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("please synchronize first.");
            }
        }

        private string FindLocalProjectSettingFile
            
            (string _fileName)
        {
            foreach (KeyValuePair<string, string> kvp in thisProjectSettingFiles)
            {
                if (_fileName.Equals(kvp.Key))
                {
                    return kvp.Value;
                }
            }
            return "None";
        }

        private void UpdateSettingFile(string filename)
        {
            if (EditorUtility.DisplayDialog("Warning", "Do you really want to update the file " + filename + " ? ", "Yes", "No"))
            {
                FileUtil.ReplaceFile(PrimarySettingsDirectory+ "/" + filename, ThisSettingsDirectory + "/" + filename);

                if (EditorUtility.DisplayDialog("info", "File copied. You need to restart the Unity Project to apply changes.", "Restart Editor", "Not yet"))
                {
                    EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                }
            }
        }
    }
}
