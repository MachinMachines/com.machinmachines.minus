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

using UnityEngine;

namespace StudioManette
{
    namespace Minus
    {
        public class ActionStep
        {
            public int priority;
            public delegate void DelegateVoid();
            public bool isDone = false;

            private DelegateVoid action;

            public ActionStep(int _priority, DelegateVoid _firstAction)
            {
                priority = _priority;
                action = _firstAction;
            }

            public void Action()
            {
                action();
            }
        }
    }
}
