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

namespace MachinMachines
{
    namespace Minus
    {
        [System.Serializable]
        public class PackageManifestItem
        {
            public bool selected = true;

            private readonly string m_packageName;
            public string packageName
            {
                get
                {
                    return m_packageName;
                }
            }

            private readonly string m_packageVersion;
            public string packageVersion
            {
                get
                {
                    return m_packageVersion;
                }
            }

            private readonly string m_scope;
            public string scope
            {
                get
                {
                    return m_scope;
                }
            }

            private static readonly char SCOPE_SEPARATOR = '.';

            public PackageManifestItem(string _packageName, string _packageVersion)
            {
                this.m_packageName = _packageName;
                this.m_packageVersion = _packageVersion;

                //scope determination (the "com.unity" in "com.unity.timeline")
                this.m_scope = _packageName;
                int lastIndexScopeSeparator = _packageName.LastIndexOf(SCOPE_SEPARATOR);
                if (lastIndexScopeSeparator != -1)
                {
                    this.m_scope = _packageName.Substring(0, lastIndexScopeSeparator);
                }
            }
        }
    }
}
