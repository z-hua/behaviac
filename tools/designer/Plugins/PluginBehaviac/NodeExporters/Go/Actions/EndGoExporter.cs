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
using Behaviac.Design.Nodes;
using PluginBehaviac.Nodes;
using PluginBehaviac.DataExporters;
using System.Collections.Generic;

namespace PluginBehaviac.NodeExporters
{
    public class EndGoExporter : NodeGoExporter
    {
        protected override bool ShouldGenerateClass(Node node)
        {
            return node is End;
        }

        protected override void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);

            if (!(node is End end))
            {
                return;
            }

            stream.WriteLine("\tn.Behavior = n");
            stream.WriteLine("\tn.EndOutside = {0}", end.EndOutside ? "true" : "false");

            if (end.EndStatus != null)
            {
                RightValueGoExporter.GenerateClassConstructor(node, end.EndStatus, stream, indent, "EndStatus");
            }
        }

        protected override void GenerateMember(Node node, StringWriter stream, string indent)
        {
            base.GenerateMember(node, stream, indent);

            if (!(node is End end))
            {
                return;
            }

            stream.WriteLine("\tactions.End");

            if (end.EndStatus != null)
            {
                RightValueGoExporter.GenerateClassMember(end.EndStatus, stream, indent, "EndStatus");
            }
        }

        protected override void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(node, stream, indent, className);

            if (!(node is End end))
            {
                return;
            }

            if (end.EndStatus != null)
            {
                stream.WriteLine("func (n *{0}) GetStatus(agent bt.IAgent) bt.Status {{", className);

                string retStr = RightValueGoExporter.GenerateCode(node, end.EndStatus, stream, indent + "\t", string.Empty, string.Empty, "EndStatus");

                stream.WriteLine("\treturn {0}", retStr);
                stream.WriteLine("}");
            }
        }

        public override void CollectImport(StringWriter stream, Dictionary<string, bool> imported)
        {
            ImportAction(stream, imported);
        }
    }
}
