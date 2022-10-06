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

using MachinMachines.Utils;

using UnityEditor;

using UnityEngine;

namespace MachinMachines
{
    namespace minus
    {
        public class MinusWindow : EditorWindow
        {
            private static readonly int WIDTH_CASE_PACKAGE = 100;

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

            /* settings */ 
            private readonly string SETTINGS_PRIMARY_PROJECT_PATH = "primaryProjectPath";
            private string tmpPrimaryProjectPath;

            /* Getters */
            public string PrimaryPackagesDirectory
            {
                get { return MinusSettings.instance.Get<string>(SETTINGS_PRIMARY_PROJECT_PATH, SettingsScope.Project) + "/Packages"; }
            }

            public string PrimarySettingsDirectory
            {
                get { return MinusSettings.instance.Get<string>(SETTINGS_PRIMARY_PROJECT_PATH, SettingsScope.Project) + "/ProjectSettings"; }
            }

            public string ThisSettingsDirectory
            {
                get { return Directory.GetCurrentDirectory() + "/ProjectSettings"; }
            }

            /*
             * INITIALIZATION
             */

            [MenuItem("MachinMachines/Minus")]
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
                //styles
                validStyle = new GUIStyle(EditorStyles.label);
                validStyle.normal.textColor = Color.green;
                wrongStyle = new GUIStyle(EditorStyles.label);
                wrongStyle.normal.textColor = Color.yellow;
            }

            public void OnGUI()
            {
                //GROUP 1 : PROPERTIES
                EditorGUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.LabelField("PROPERTIES", EditorStyles.boldLabel);

                    EditorGUI.BeginChangeCheck();
                    tmpPrimaryProjectPath = EditorGUILayout.TextField("Primary Project Path", tmpPrimaryProjectPath);
                    if (EditorGUI.EndChangeCheck())
                    {
                        MinusSettings.instance.Set<string>(SETTINGS_PRIMARY_PROJECT_PATH, tmpPrimaryProjectPath, SettingsScope.Project);
                    }
                }
                EditorGUILayout.EndVertical();

                //GROUP 2 : BUTTONS
                if (GUILayout.Button("Synchronize"))
                {
                    SynchronizeLocalPackages();
                    primaryPackageList = Synchronization.GetExternalPackagesList(PrimaryPackagesDirectory);
                    primaryProjectSettingFiles = Synchronization.GetHashedFilesOfDirectory(PrimarySettingsDirectory);
                    thisProjectSettingFiles = Synchronization.GetHashedFilesOfDirectory(ThisSettingsDirectory);
                }

                if (PackagingOperations.isRunningAsyncOperation)
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
                if (primaryPackageList != null && primaryPackageList.Count > 0 && thisPackageList != null && thisPackageList.Count > 0)
                {
                    //Display Headers
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Package name", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE * 3));
                    EditorGUILayout.LabelField("Primary", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
                    EditorGUILayout.LabelField("This", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
                    EditorGUILayout.LabelField("Update", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
                    EditorGUILayout.EndHorizontal();

                    scrollPosPackages = EditorGUILayout.BeginScrollView(scrollPosPackages);
                    foreach (PackageManifestItem package in primaryPackageList)
                    {
                        string localPackageVersion = FindPackageVersionInThis(package.packageName);
                        bool isVersionValid = localPackageVersion.Equals(package.packageVersion);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(package.packageName, GUILayout.Width(WIDTH_CASE_PACKAGE * 3));
                        EditorGUILayout.LabelField(package.packageVersion, GUILayout.Width(WIDTH_CASE_PACKAGE));
                        EditorGUILayout.LabelField(localPackageVersion, isVersionValid ? validStyle : wrongStyle, GUILayout.Width(WIDTH_CASE_PACKAGE));

                        if (isVersionValid)
                        {
                            GUILayout.Label("------", GUILayout.Width(WIDTH_CASE_PACKAGE));
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
                }
            }

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
                    EditorGUILayout.LabelField("File name", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE * 3));
                    EditorGUILayout.LabelField("", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
                    EditorGUILayout.LabelField("", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
                    EditorGUILayout.LabelField("Update", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
                    EditorGUILayout.EndHorizontal();

                    scrollPosProjectSettings = EditorGUILayout.BeginScrollView(scrollPosProjectSettings);
                    foreach (KeyValuePair<string, string> kvp in primaryProjectSettingFiles)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(WIDTH_CASE_PACKAGE * 4));
                        //EditorGUILayout.LabelField(kvp.Value);

                        string checksumFromThis = FindLocalProjectSettingFile(kvp.Key);
                        bool isChecksumValid = kvp.Value.Equals(checksumFromThis);
                        EditorGUILayout.LabelField(isChecksumValid ? "Up to date" : "Outdated", isChecksumValid ? validStyle : wrongStyle, GUILayout.Width(WIDTH_CASE_PACKAGE));

                        if (isChecksumValid)
                        {
                            GUILayout.Label("------", GUILayout.Width(WIDTH_CASE_PACKAGE));
                        }
                        else
                        {
                            if (GUILayout.Button("Update", GUILayout.Width(WIDTH_CASE_PACKAGE)))
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
