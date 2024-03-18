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

namespace PluginBehaviac.NodeExporters
{
    public class DecoratorGoExporter : NodeGoExporter
    {
        protected override bool ShouldGenerateClass(Node node)
        {
            return node is Decorator;
        }

        protected override void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);

            if (!(node is Decorator decorator))
            {
                return;
            }

            // stream.WriteLine("{0}\t\t\tm_bDecorateWhenChildEnds = {1};", indent, decorator.DecorateWhenChildEnds ? "true" : "false");
        }

        public override void CollectImport(StringWriter stream, Dictionary<string, bool> imported)
        {
            ImportDecorator(stream, imported);
        }
    }
}
