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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace MachinMachines
{
    namespace Minus
    {
        public class CloneWindow : EditorWindow
        {
            //constants
            private static readonly string assetsPath = "Assets";
            private static readonly string projectSettingsPath = "ProjectSettings";
            private static readonly string PackagesPath = "Packages";
            private static readonly int WIDTH_CASE_TOGGLE = 20;
            private static readonly int WIDTH_CASE_PACKAGE = 300;
            private static readonly int WIDTH_CASE_BUTTON = 100;

            private List<ActionStep> steps;
            private string newFolder;
            private int currentStep = -1;

            private readonly string SETTINGNAME_ALLOWPACKAGE = "allowLocalPackages";
            private readonly string SETTINGNAME_LOCALFOLDERS = "localFolders";
            private bool tmpAllowPackages;
            private string tmpPrimaryPath;
            private List<PackageManifestItem> tmpPackagesToClone;
            private string foldersToCopy;

            /* from clone window*/
            private static string projectPath;
            private List<PackageManifestItem> packageList;
            private Vector2 scrollPosPackages;
            private bool showPackages = false;
            private static CloneWindow instance;

            [MenuItem("MachinMachines/Minus/Create New Project...")]
            public static void ShowWindow()
            {
               instance = (CloneWindow)EditorWindow.GetWindow(typeof(CloneWindow), false, "Minus - Clone Window");
               instance.LaunchSync();
            }

            public void OnEnable()
            {
                tmpAllowPackages = MinusSettings.instance.Get<bool>(SETTINGNAME_ALLOWPACKAGE, SettingsScope.Project);
                foldersToCopy = MinusSettings.instance.Get<string>(SETTINGNAME_LOCALFOLDERS, SettingsScope.Project);
                Init();
            }

            public void OnGUI()
            {
                //GROUP 1 : PROPERTIES
                EditorGUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.LabelField("PROPERTIES", EditorStyles.boldLabel);
                    
                    EditorGUI.BeginChangeCheck();
                    tmpAllowPackages = EditorGUILayout.Toggle(SETTINGNAME_ALLOWPACKAGE, tmpAllowPackages);
                    if (EditorGUI.EndChangeCheck())
                    {
                        MinusSettings.instance.Set<bool>(SETTINGNAME_ALLOWPACKAGE, tmpAllowPackages, SettingsScope.Project);
                    }

                    EditorGUI.BeginChangeCheck();
                    foldersToCopy = EditorGUILayout.TextField(new GUIContent("Folders To Copy", "Folders To Copy to new project, please separate them with a comma"), foldersToCopy);
                    if (EditorGUI.EndChangeCheck())
                    {
                        MinusSettings.instance.Set<string>(SETTINGNAME_LOCALFOLDERS, foldersToCopy, SettingsScope.Project);
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.LabelField("Project Path : ");
                projectPath = EditorGUILayout.TextField(projectPath);


                //Display Each Package
                if (packageList != null && packageList.Count > 0)
                {
                    showPackages = EditorGUILayout.Foldout(showPackages, "Select the Packages you want to clone: ", EditorStyles.foldoutHeader);

                    if (showPackages)
                    {
                        //Button Helpers
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("select all", GUILayout.Width(WIDTH_CASE_BUTTON)))
                        {
                            foreach (PackageManifestItem package in packageList) package.selected = true;
                        }
                        if (GUILayout.Button("deselect all", GUILayout.Width(WIDTH_CASE_BUTTON)))
                        {
                            foreach (PackageManifestItem package in packageList) package.selected = false;
                        }
                        EditorGUILayout.EndHorizontal();

                        //Display Headers
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_TOGGLE));
                        EditorGUILayout.LabelField("Package name", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
                        EditorGUILayout.LabelField("Version", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
                        EditorGUILayout.EndHorizontal();

                        scrollPosPackages = EditorGUILayout.BeginScrollView(scrollPosPackages);
                        foreach (PackageManifestItem package in packageList)
                        {
                            EditorGUILayout.BeginHorizontal();
                            package.selected = EditorGUILayout.Toggle(package.selected, GUILayout.Width(WIDTH_CASE_TOGGLE));
                            EditorGUILayout.LabelField(package.packageName, GUILayout.Width(WIDTH_CASE_PACKAGE));
                            EditorGUILayout.LabelField(package.packageVersion, GUILayout.Width(WIDTH_CASE_PACKAGE));
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndScrollView();
                    }

                    if (GUILayout.Button("CLONE"))
                    {
                        try
                        {
                            CreateProject(projectPath, packageList);
                            this.Close();
                        }
                        catch (Exception e)
                        {
                            EditorUtility.DisplayDialog("Minus", "ERROR : " + e.Message, "OK");
                        }
                    }

                }
                else
                {
                    EditorGUILayout.LabelField("please wait...");
                }
            }

            private void Init()
            {
                //init steps
                steps = new List<ActionStep>();
                steps.Add(new ActionStep(-1, CheckPossibility));
                steps.Add(new ActionStep(0, CheckLocalPackages));
                steps.Add(new ActionStep(1, SetupMinusSettings));
                steps.Add(new ActionStep(2, CopyFiles));
                steps.Add(new ActionStep(4, ProcessPackageManifest));
                steps.Add(new ActionStep(6, RestoreMinusSettings));

                //sort steps (uncomment if needed)
                steps.Sort((p1, p2) => p1.priority - p2.priority);
            }

            private void CreateProject(string projectPath, List<PackageManifestItem> packagesToClone)
            {
                Debug.Log("create new project : " + projectPath);

                newFolder = projectPath;
                tmpPackagesToClone = packagesToClone;

                currentStep = 0;
                steps[currentStep].Action();
            }

            private void CallNextStep()
            {
                //Debug.Log("step " + currentStep + " / well done");

                //passer au step suivant
                currentStep++;
                if (steps.Count > currentStep)
                {
                    //Debug.Log("step " + currentStep + " BEGIN" + " / priority : " + steps[currentStep].priority);
                    steps[currentStep].Action();
                }
                else
                {
                    EditorUtility.DisplayDialog("Info", "The following project has been created : " + newFolder, "OK");
                }
            }

            private void CopyFiles()
            {
                //newFolder = newFolder.Replace("\\", "/");

                //Create new folder
                Directory.CreateDirectory(newFolder);

                //Create empty Assets Folder
                Directory.CreateDirectory(newFolder + "/" + assetsPath);

                //Copy Packages
                FileUtil.CopyFileOrDirectory(Directory.GetCurrentDirectory() + "/" + PackagesPath,
                                             newFolder + "/" + PackagesPath);

                //Copy ProjectSettings
                FileUtil.CopyFileOrDirectory(Directory.GetCurrentDirectory() + "/" + projectSettingsPath,
                                             newFolder + "/" + projectSettingsPath);

                //Copy Other Folders
                if (!string.IsNullOrEmpty(foldersToCopy))
                {
                    foreach (string strFolder in foldersToCopy.Split(foldersToCopy, ','))
                    {
                        string tmpFolder = strFolder.Trim();
                        FileUtil.CopyFileOrDirectory(Directory.GetCurrentDirectory() + "/" + tmpFolder,
                                 newFolder + "/" + tmpFolder);
                    }
                }

                CallNextStep();
            }

            private void CheckPossibility()
            {
                if (string.IsNullOrWhiteSpace(newFolder))
                {
                    throw new Exception("please provide a project path.");
                }

                newFolder = newFolder.Replace("\\", "/");

                if (EditorUtility.DisplayDialog("Info", "Do you really want to create a new project on this path : " + newFolder + " ?", "Yes", "No"))
                {
                    CallNextStep();
                }
            }

            private void CheckLocalPackages()
            {
                if (!MinusSettings.instance.Get<bool>("allowLocalPackages", SettingsScope.Project))
                {
                    string newManifestJson = Directory.GetCurrentDirectory() + "/Packages/manifest.json";
                    bool hasAtLeastOneLocalPackage = false;

                    List<string> allLines = new List<string>(File.ReadAllLines(newManifestJson));
                    for (int i = 0; i < allLines.Count; i++)
                    {
                        //vérifier si aucun package n'est en local (mot-clef "file:")
                        if (allLines[i].Contains("file:"))
                        {
                            hasAtLeastOneLocalPackage = true;
                        }
                    }

                    if (hasAtLeastOneLocalPackage)
                    {
                        if (EditorUtility.DisplayDialog("Warning", "Warning : some packages are local, do you really want to duplicate the project with plocal packages ?", "I'll do it anyway", "cancel"))
                        {
                            CallNextStep();
                        }
                        else
                        {
                            throw new Exception("step cancelled by user.");
                            // tout stopper
                        }
                    }
                    else
                    {
                        CallNextStep();
                    }
                }
                else
                {
                    CallNextStep();
                }
            }

            private void ProcessPackageManifest()
            {
                string newManifestJson = newFolder + "/Packages/manifest.json";

                List<string> allLines = new List<string>(File.ReadAllLines(newManifestJson));
                for (int i = 0; i < allLines.Count; i++)
                {
                    string[] splittedline = allLines[i].Split("\"");
                    if (splittedline.Length > 2)
                    {
                        string packageNameLine = splittedline[1];

                        /* gestion des packages selectionnés ou non*/
                        PackageManifestItem packageInList = tmpPackagesToClone.FirstOrDefault(x => x.packageName == packageNameLine);

                        if (packageInList == null || packageInList.selected)
                        {
                            //nothing, the line is staying
                        }
                        else
                        {
                            allLines[i] = "";
                        }
                    }
                }

                allLines.RemoveAll(x => string.IsNullOrWhiteSpace(x));
                File.WriteAllLines(newManifestJson, allLines);
                CallNextStep();
            }

            private void SetupMinusSettings()
            {
                tmpPrimaryPath = MinusSettings.instance.Get<string>(MinusWindow.SETTINGS_PRIMARY_PROJECT_PATH, SettingsScope.Project);
                MinusSettings.instance.Set<string>(MinusWindow.SETTINGS_PRIMARY_PROJECT_PATH, Directory.GetCurrentDirectory(), SettingsScope.Project);

                CallNextStep();
            }

            private void RestoreMinusSettings()
            {
                MinusSettings.instance.Set<string>(MinusWindow.SETTINGS_PRIMARY_PROJECT_PATH, tmpPrimaryPath, SettingsScope.Project);

                CallNextStep();
            }

            private void LaunchSync()
            {
                packageList = null;
                packageList = Synchronization.GetExternalPackagesList(Directory.GetCurrentDirectory() + "/Packages", "");
            }
        }
    }
}
