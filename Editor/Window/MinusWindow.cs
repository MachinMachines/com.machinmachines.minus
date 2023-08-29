// Copyright 2022 MachinMachines
//
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;

using UnityEngine;

namespace MachinMachines
{
    namespace Minus
    {
        public class MinusWindow : EditorWindow
        {
            private static readonly int WIDTH_CASE_PACKAGE_NAME = 350;
            private static readonly int WIDTH_CASE_PACKAGE_VERSION = 450;
            private static readonly int WIDTH_CASE_UPDATE_BUTTON = 100;

            private static readonly int WIDTH_CASE_PROJECT_SETTINGS = 100;            

            private static readonly string STR_NAME_PACKAGES_COLUMN = "Package Name";
            private static readonly string STR_PRIMARY_PACKAGES_COLUMN = "Primary Project";
            private static readonly string STR_THIS_PACKAGES_COLUMN = "This Project";
            private static readonly string STR_UPDATE_PACKAGES_COLUMN = "Update";


            private List<PackageManifestItem> primaryPackageList;
            private List<PackageManifestItem> thisPackageList;

            private Dictionary<string, string> primaryProjectSettingFiles;
            private Dictionary<string, string> thisProjectSettingFiles;

            /* ui properties */
            private GUIStyle validStyle;
            private GUIStyle wrongStyle;
            private GUIStyle missingStyle;
            private bool showPackages;
            private Vector2 scrollPosPackages;
            private bool showProjectSettings;
            private Vector2 scrollPosProjectSettings;

            /* settings */ 
            public static readonly string SETTINGS_PRIMARY_PROJECT_PATH = "primaryProjectPath";
            public static readonly string SETTINGS_ASSETS_PACKAGES_PREFIX = "assetPackagePrefix";
            private string primaryProjectPath;

            private bool isNeededRefreshAfterSync = false;

            /* Getters */
            public string PrimaryPackagesDirectory
            {
                get { return primaryProjectPath + "/Packages"; }
            }

            public string PrimarySettingsDirectory
            {
                get { return primaryProjectPath + "/ProjectSettings"; }
            }

            public string ThisPackagesDirectory
            {
                get { return Directory.GetCurrentDirectory() + "/Packages"; }
            }

            public string ThisSettingsDirectory
            {
                get { return Directory.GetCurrentDirectory() + "/ProjectSettings"; }
            }

            /*
             * INITIALIZATION
             */

            [MenuItem("MachinMachines/Minus/Package Sync Window")]
            public static void ShowWindow()
            {
                EditorWindow.GetWindow(typeof(MinusWindow), false, "Minus - MachinMachines");
            }

            public void OnEnable()
            {
                Init();
            }

            private void Init()
            {
                primaryProjectPath = MinusSettings.instance.Get<string>(SETTINGS_PRIMARY_PROJECT_PATH, SettingsScope.User);

                //styles
                validStyle = new GUIStyle(EditorStyles.label);
                validStyle.normal.textColor = Color.green;
                wrongStyle = new GUIStyle(EditorStyles.label);
                wrongStyle.normal.textColor = Color.yellow;
                missingStyle = new GUIStyle(EditorStyles.label);
                missingStyle.normal.textColor = new Color(1.0f,0.5f,0.0f);
            }

            public void OnGUI()
            {
                //GROUP 1 : PROPERTIES
                EditorGUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.LabelField("PROPERTIES", EditorStyles.boldLabel);

                    EditorGUI.BeginChangeCheck();
                    primaryProjectPath = EditorGUILayout.TextField("Primary Project Path", primaryProjectPath);
                    if (EditorGUI.EndChangeCheck())
                    {
                        MinusSettings.instance.Set<string>(SETTINGS_PRIMARY_PROJECT_PATH, primaryProjectPath, SettingsScope.User);
                    }
                }
                EditorGUILayout.EndVertical();

                //GROUP 2 : BUTTONS
                if (GUILayout.Button("Synchronize"))
                {
                    if (!Directory.Exists(PrimaryPackagesDirectory))
                    {
                        EditorUtility.DisplayDialog("Minus Error", "Directory not found : " + PrimaryPackagesDirectory, "ok");
                    }
                    else
                    {
                        Synchronize();
                    }
                }

                if (PackagingOperations.isRunningAsyncOperation)
                {
                    EditorGUILayout.LabelField("Waiting for synchronization...");
                }
                else
                {
                    if (isNeededRefreshAfterSync)
                    {
                        isNeededRefreshAfterSync = false;
                        Synchronize();
                    }
                    //GROUP 3 : DISPLAY PACKAGES INFO

                    EditorGUILayout.BeginVertical("box");
                    showPackages = EditorGUILayout.Foldout(showPackages, "PACKAGES", EditorStyles.foldoutHeader);
                    if (showPackages)
                    {
                        if (primaryPackageList != null && primaryPackageList.Count > 0 && primaryPackageList[0].packageName != null)
                        {
                            DisplayPackages(primaryPackageList, ref scrollPosPackages);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("please synchronize first.");
                        }
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

                    if (GUILayout.Button("Log Comparaison"))
                    {
                        if (primaryPackageList != null && primaryPackageList.Count > 0 && primaryPackageList[0].packageName != null)
                        {
                            Debug.Log(Synchronization.LogCompareFilesInfo(primaryPackageList, thisPackageList, true));
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Minus Error", "Please synchronize first.", "ok");
                        }
                    }
                }
            }

            private void Synchronize() 
            {
                //SynchronizeLocalPackages();
                primaryPackageList = Synchronization.GetExternalPackagesList(PrimaryPackagesDirectory);
                thisPackageList = Synchronization.GetExternalPackagesList(ThisPackagesDirectory);
                primaryProjectSettingFiles = Synchronization.GetHashedFilesOfDirectory(PrimarySettingsDirectory);
                thisProjectSettingFiles = Synchronization.GetHashedFilesOfDirectory(ThisSettingsDirectory);
            }
            /**
             *  PACKAGE MANAGEMENT
             */

            private void DisplayPackages(IEnumerable<PackageManifestItem> _primaryList, ref Vector2 _scrollPos)
            {
                //Display Each Package
                if (_primaryList != null && _primaryList.ToList().Count > 0 && thisPackageList != null && thisPackageList.Count > 0)
                {
                    //Display Headers
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField ( STR_NAME_PACKAGES_COLUMN, EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE_NAME));
                    EditorGUILayout.LabelField ( STR_PRIMARY_PACKAGES_COLUMN, EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE_VERSION));
                    EditorGUILayout.LabelField ( STR_THIS_PACKAGES_COLUMN, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField ( STR_UPDATE_PACKAGES_COLUMN, EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_UPDATE_BUTTON));
                    EditorGUILayout.EndHorizontal();

                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                    foreach (PackageManifestItem package in _primaryList)
                    {
                        //string localPackageVersion = FindPackageVersionInThis(package.packageName);
                        string localPackageVersion = Synchronization.FindPackageVersion(thisPackageList, package.packageName);
                        bool isVersionMissing = false;
                        bool isVersionValid = localPackageVersion.Equals(package.packageVersion);
                        if (!isVersionValid)
                        {
                            isVersionMissing = localPackageVersion.Equals(Synchronization.STR_MISSING_PACKAGE);
                        }

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(package.packageName, GUILayout.Width(WIDTH_CASE_PACKAGE_NAME));
                        EditorGUILayout.LabelField(package.packageVersion, GUILayout.Width(WIDTH_CASE_PACKAGE_VERSION));
                        GUIStyle style = isVersionValid ? validStyle : (isVersionMissing ? missingStyle : wrongStyle);
                        EditorGUILayout.LabelField(localPackageVersion, style);

                        if (isVersionValid)
                        {
                            GUILayout.Label("------", GUILayout.Width(WIDTH_CASE_UPDATE_BUTTON));
                        }
                        else
                        {
                            if (GUILayout.Button("Update", GUILayout.Width(WIDTH_CASE_UPDATE_BUTTON)))
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
                    EditorGUILayout.LabelField("no packages.");
                }
            }

            private void UpdatePackage(string packageName, string newVersion)
            {
                if (EditorUtility.DisplayDialog("Warning", "Do you really want to update the package " + packageName + "to version " + newVersion + " ? ", "Yes", "No"))
                {
                    isNeededRefreshAfterSync = true;
                    PackagingOperations.UpdatePackage(packageName, newVersion);
                }
            }

            private string FindPackageVersionInThis(string _packageName)
            {
                PackageManifestItem tpmPackage = thisPackageList.FirstOrDefault(t => t.packageName == _packageName);

                return tpmPackage != null ? tpmPackage.packageVersion : Synchronization.STR_MISSING_PACKAGE;
            }

            private void SynchronizeLocalPackages()
            {
                PackagingOperations.SynchronizeLocalPackages(SynchroGetResponse);
            }

            private void SynchroGetResponse(List<PackageManifestItem> response)
            {
                thisPackageList = response;
            }

            /**
             *  PROJECT SETTINGS MANAGEMENT
             */

            private void DisplayProjectSettings()
            {
                if (primaryProjectSettingFiles != null && primaryProjectSettingFiles.Count > 0)
                {
                    //Display Headers
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("File name", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE_NAME));
                    EditorGUILayout.LabelField("State", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PROJECT_SETTINGS));
                    EditorGUILayout.LabelField("Update", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PROJECT_SETTINGS));
                    EditorGUILayout.EndHorizontal();

                    scrollPosProjectSettings = EditorGUILayout.BeginScrollView(scrollPosProjectSettings);
                    foreach (KeyValuePair<string, string> kvp in primaryProjectSettingFiles)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(WIDTH_CASE_PACKAGE_NAME));
                        //EditorGUILayout.LabelField(kvp.Value);

                        string checksumFromThis = FindLocalProjectSettingFile(kvp.Key);
                        bool isChecksumValid = kvp.Value.Equals(checksumFromThis);
                        EditorGUILayout.LabelField(isChecksumValid ? "Up to date" : "Outdated", isChecksumValid ? validStyle : wrongStyle, GUILayout.Width(WIDTH_CASE_PROJECT_SETTINGS));

                        if (isChecksumValid)
                        {
                            GUILayout.Label("------", GUILayout.Width(WIDTH_CASE_PROJECT_SETTINGS));
                        }
                        else
                        {
                            if (GUILayout.Button("Update", GUILayout.Width(WIDTH_CASE_PROJECT_SETTINGS)))
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

            private string FindLocalProjectSettingFile (string _fileName)
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
                    FileUtil.ReplaceFile(PrimarySettingsDirectory + "/" + filename, ThisSettingsDirectory + "/" + filename);

                    if (EditorUtility.DisplayDialog("info", "File copied. You need to restart the Unity Project to apply changes.", "Restart Editor", "Not yet"))
                    {
                        EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                    }
                }
            }
        }
    }
}
