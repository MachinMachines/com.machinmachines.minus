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

using UnityEditor.SettingsManagement;

namespace MachinMachines
{
    namespace Minus
    {
        public static class MinusSettings
        {
            private const string k_PackageName = "com.machinmachines.minus";

            static Settings s_Instance;

            public static Settings instance
            {
                get
                {
                    if (s_Instance == null)
                        s_Instance = new Settings(k_PackageName);

                    return s_Instance;
                }
            }
        }
    }
}
