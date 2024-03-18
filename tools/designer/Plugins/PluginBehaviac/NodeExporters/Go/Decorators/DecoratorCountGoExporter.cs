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
    public class DecoratorCountGoExporter : DecoratorGoExporter
    {
        protected override bool ShouldGenerateClass(Node node)
        {
            return node is DecoratorCount;
        }

        protected override void GenerateMember(Node node, StringWriter stream, string indent)
        {
            base.GenerateMember(node, stream, indent);

            if (!(node is DecoratorCount))
            {
                return;
            }

            stream.WriteLine("\tdecorators.Count");
        }

        protected override void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(node, stream, indent, className);

            if (!(node is DecoratorCount decoratorCount))
            {
                return;
            }

            if (decoratorCount.Count != null)
            {
                stream.WriteLine("func (n *{0}) GetCount(agent bt.IAgent) int {{", className);

                string retStr = VariableGoExporter.GenerateCode(node, decoratorCount.Count, false, stream, indent + "\t", string.Empty, string.Empty, string.Empty);

                stream.WriteLine("\treturn {0}", retStr);
                stream.WriteLine("}");
            }
        }

        public override void CollectImport(StringWriter stream, Dictionary<string, bool> imported)
        {
            ImportDecorator(stream, imported);
        }
    }
}
