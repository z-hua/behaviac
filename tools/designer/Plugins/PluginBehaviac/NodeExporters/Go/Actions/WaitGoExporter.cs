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

using System.IO;
using Behaviac.Design;
using Behaviac.Design.Nodes;
using PluginBehaviac.Nodes;
using PluginBehaviac.DataExporters;

namespace PluginBehaviac.NodeExporters
{
    public class WaitGoExporter : NodeGoExporter
    {
        protected override bool ShouldGenerateClass(Node node)
        {
            return node is Wait;
        }

        protected override void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);

            if (!(node is Wait wait))
            {
                return;
            }

            if (wait.Time != null)
            {
                RightValueGoExporter.GenerateClassConstructor(node, wait.Time, stream, indent, "Time");
            }
        }

        protected override void GenerateMember(Node node, StringWriter stream, string indent)
        {
            base.GenerateMember(node, stream, indent);

            if (!(node is Wait wait))
            {
                return;
            }

            stream.WriteLine("\tperformers.Wait");

            if (wait.Time != null)
            {
                RightValueGoExporter.GenerateClassMember(wait.Time, stream, indent, "Time");
            }
        }

        protected override void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(node, stream, indent, className);

            if (!(node is Wait wait))
            {
                return;
            }

            if (wait.Time != null)
            {
                stream.WriteLine("func (n *{0}) GetTime(agent bt.Agent) int {{", className);

                string retStr = RightValueGoExporter.GenerateCode(node, wait.Time, stream, indent + "\t\t\t", string.Empty, string.Empty, "Time");

                if (!Workspace.Current.UseIntValue)
                {
                    retStr = string.Format("int({0})", retStr);
                }

                stream.WriteLine("\treturn {0}", retStr);
                stream.WriteLine("}");
            }
        }
    }
}
