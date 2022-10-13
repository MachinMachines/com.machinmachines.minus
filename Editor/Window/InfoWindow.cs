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

using UnityEditor;

using UnityEngine;

namespace MachinMachines
{
    namespace cortex
    {
        public class InfoWindow : EditorWindow
        {
            public delegate void OnClick(string variable);
            private static OnClick onClickFunction;

            private static string text;
            private static string value;

            public static void ShowWindow(string _text, OnClick _OnClick)
            {
                EditorWindow.GetWindow(typeof(InfoWindow), false, "Info");
                text = _text;
                onClickFunction = _OnClick;
            }

            void OnGUI()
            {
                EditorGUILayout.LabelField(text, EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                value = EditorGUILayout.TextField(value);
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("OK"))
                {
                    try
                    {
                        onClickFunction(value);
                        this.Close();
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("Cortex", "ERROR : " + e.Message, "OK");
                    }
                }
            }
        }
    }
}
