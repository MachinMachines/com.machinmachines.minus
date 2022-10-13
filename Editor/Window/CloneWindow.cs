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
using UnityEditor;

using UnityEngine;

namespace MachinMachines
{
    namespace Minus
    {
        public class CloneWindow : EditorWindow
        {
            public delegate void OnClick(string variable, List<PackageManifestItem> packageList);
            private static OnClick onClickFunction;

            private static string text;
            private static string projectPath;

            private List<PackageManifestItem> packageList;
            private static readonly int WIDTH_CASE_TOGGLE = 20;
            private static readonly int WIDTH_CASE_PACKAGE = 300;
            private static readonly int WIDTH_CASE_BUTTON = 100;
            private Vector2 scrollPosPackages;

            private static CloneWindow instance;
            private bool showPackages = false;

            public static void ShowWindow(string _text, OnClick _OnClick)
            {
                instance = (CloneWindow)EditorWindow.GetWindow(typeof(CloneWindow), false, "Info");
                text = _text;
                onClickFunction = _OnClick;
                instance.LaunchSync();
            }

            public void LaunchSync()
            {
                packageList = null;
                packageList = Synchronization.GetExternalPackagesList(Directory.GetCurrentDirectory() + "/Packages");
            }

            void OnGUI()
            {
                EditorGUILayout.LabelField(text, EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                projectPath = EditorGUILayout.TextField(projectPath);
                EditorGUILayout.EndHorizontal();


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
                            onClickFunction(projectPath, packageList);
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
        }
    }
}
