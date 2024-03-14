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
using PluginBehaviac.DataExporters;
using PluginBehaviac.Nodes;

namespace PluginBehaviac.NodeExporters
{
    public class DecoratorLoopUntilGoExporter : DecoratorGoExporter
    {
        protected override bool ShouldGenerateClass(Node node)
        {
            return node is DecoratorLoopUntil;
        }

        protected override void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);

            if (!(node is DecoratorLoopUntil loopUntil))
            {
                return;
            }

            stream.WriteLine("{0}\tb.Until = {1}", indent, loopUntil.Until ? "true" : "false");
        }

        protected override void GenerateMember(Node node, StringWriter stream, string indent)
        {
            base.GenerateMember(node, stream, indent);

            if (!(node is DecoratorLoopUntil))
            {
                return;
            }

            stream.WriteLine("\tdecorators.LoopUntil");
        }

        protected override void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(node, stream, indent, className);

            if (!(node is DecoratorLoopUntil loopUntil))
            {
                return;
            }

            if (loopUntil.Count != null)
            {
                stream.WriteLine("func (n *{0}) GetCount(agent bt.IAgent) int {{", className);

                string retStr = VariableGoExporter.GenerateCode(node, loopUntil.Count, false, stream, indent + "\t", string.Empty, string.Empty, string.Empty);

                stream.WriteLine("\treturn {0}", retStr);
                stream.WriteLine("}");
            }
        }
    }
}
