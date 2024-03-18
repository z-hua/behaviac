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

using System.Collections.Generic;
using System.IO;
using Behaviac.Design.Nodes;
using PluginBehaviac.DataExporters;

namespace PluginBehaviac.NodeExporters
{
    public class ReferencedBehaviorGoExporter : NodeGoExporter
    {
        protected override bool ShouldGenerateClass(Node node)
        {
            return node is ReferencedBehavior;
        }

        protected override void GenerateMember(Node node, StringWriter stream, string indent)
        {
            base.GenerateMember(node, stream, indent);

            if (!(node is ReferencedBehavior pReferencedBehavior))
            {
                return;
            }

            stream.WriteLine("\tcomposites.Subtree");

            if (pReferencedBehavior.ReferenceBehavior != null)
            {
                RightValueGoExporter.GenerateClassMember(pReferencedBehavior.ReferenceBehavior, stream, indent, "Behavior");
            }
        }

        protected override void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(node, stream, indent, className);

            if (!(node is ReferencedBehavior pReferencedBehavior))
            {
                return;
            }

            if (pReferencedBehavior.ReferenceBehavior != null)
            {
                stream.WriteLine("func (n *{0}) GetSubtree(agent bt.IAgent) string {{", className);

                string retStr = RightValueGoExporter.GenerateCode(node, pReferencedBehavior.ReferenceBehavior, stream, indent + "\t", string.Empty, string.Empty, "Behavior");

                bool bConst = false;

                if (pReferencedBehavior.ReferenceBehavior.Var != null && pReferencedBehavior.ReferenceBehavior.Var.IsConst)
                {
                    bConst = true;
                }

                if (!bConst)
                {
                    stream.WriteLine("{0}\tif (agent != nil) {{", indent);
                    stream.WriteLine("{0}\t\treturn {1}", indent, retStr);
                    stream.WriteLine("{0}\t}}", indent);
                    stream.WriteLine("{0}\tpanic(\"subtree not found\")", indent);
                }
                else
                {
                    stream.WriteLine("{0}\treturn {1}", indent, retStr);
                }

                stream.WriteLine("}");
            }
        }

        protected override void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);
        }

        public override void CollectImport(StringWriter stream, Dictionary<string, bool> imported)
        {
            ImportComposite(stream, imported);
        }
    }
}
