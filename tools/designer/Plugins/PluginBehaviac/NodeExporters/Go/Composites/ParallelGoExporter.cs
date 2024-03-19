/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Tencent is pleased to support the open source community by making behaviac available.
//
// Copyright (C) 2015-2017 THL A29 Limited, a Tencent company. All rights reserved.
//
// Licensed under the BSD 3-Clause License (the "License"); you may not use this file except in compliance with
// the License. You may obtain a copy of the License at http://opensource.org/licenses/BSD-3-Clause
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Behaviac.Design.Nodes;
using PluginBehaviac.Nodes;
using System.Collections.Generic;
using System.IO;

namespace PluginBehaviac.NodeExporters
{
    public class ParallelGoExporter : NodeGoExporter
    {
        protected override string GetNodeBehavior(Node node, string btClassName, string nodeName)
        {
            if (!(node is Parallel parallel))
            {
                return "";
            }

            return string.Format(
                "composites.NewParallel(\n\t\t\t{0},\n\t\t\t{1},\n\t\t\t{2})",
                GetSuccessPolicy(parallel.SuccessPolicy),
                GetExitPolicy(parallel.ExitPolicy),
                GetChildFinishPolicy(parallel.ChildFinishPolicy)
            );
        }

        private string GetSuccessPolicy(SuccessPolicy policy)
        {
            switch (policy)
            {
                case SuccessPolicy.SUCCEED_ON_ONE:
                    return "composites.SuccessOnOne";
                case SuccessPolicy.SUCCEED_ON_ALL:
                    return "composites.SuccessOnAll";
                default:
                    return "";
            }
        }

        private string GetExitPolicy(ExitPolicy policy)
        {
            switch (policy)
            {
                case ExitPolicy.EXIT_NONE:
                    return "composites.ExitNone";
                case ExitPolicy.EXIT_ABORT_RUNNINGSIBLINGS:
                    return "composites.ExitAbort";
                default:
                    return "";
            }
        }

        private string GetChildFinishPolicy(ChildFinishPolicy policy)
        {
            switch (policy)
            {
                case ChildFinishPolicy.CHILDFINISH_ONCE:
                    return "composites.ChildFinishOnce";
                case ChildFinishPolicy.CHILDFINISH_LOOP:
                    return "composites.ChildFinishLoop";
                default:
                    return "";
            }
        }

        public override void CollectImport(StringWriter stream, Dictionary<string, bool> imported)
        {
            ImportComposite(stream, imported);
        }
    }
}
