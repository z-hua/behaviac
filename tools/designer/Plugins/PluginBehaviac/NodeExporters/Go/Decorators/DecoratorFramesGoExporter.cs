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

namespace PluginBehaviac.NodeExporters
{
    public class DecoratorFramesGoExporter : DecoratorGoExporter
    {
        protected override bool ShouldGenerateClass(Node node)
        {
            return node is DecoratorFrames;
        }

        protected override void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);

            if (!(node is DecoratorFrames decoratorFrames))
            {
                return;
            }

            if (decoratorFrames.Frames != null)
            {
                RightValueGoExporter.GenerateClassConstructor(node, decoratorFrames.Frames, stream, indent, "Frames");
            }
        }

        protected override void GenerateMember(Node node, StringWriter stream, string indent)
        {
            base.GenerateMember(node, stream, indent);

            if (!(node is DecoratorFrames decoratorFrames))
            {
                return;
            }

            stream.WriteLine("\tdecorators.Frames");

            if (decoratorFrames.Frames != null)
            {
                RightValueGoExporter.GenerateClassMember(decoratorFrames.Frames, stream, indent, "Frames");
            }
        }

        protected override void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(node, stream, indent, className);

            if (!(node is DecoratorFrames decoratorFrames))
            {
                return;
            }

            if (decoratorFrames.Frames != null)
            {
                stream.WriteLine("func (n *{0}) GetTime(agent bt.Agent) int {{", className);

                string retStr = RightValueGoExporter.GenerateCode(node, decoratorFrames.Frames, stream, indent + "\t\t\t", string.Empty, string.Empty, "Frames");

                /*if (!decoratorFrames.Frames.IsPublic && (decoratorFrames.Frames.IsMethod || decoratorFrames.Frames.Var != null && decoratorFrames.Frames.Var.IsProperty))
                {
                    retStr = string.Format("int({0})", retStr);
                }*/

                stream.WriteLine("\treturn {0}", retStr);
                stream.WriteLine("}");
            }
        }
    }
}
