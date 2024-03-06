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

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using Behaviac.Design.Nodes;
using PluginBehaviac.DataExporters;

namespace PluginBehaviac.NodeExporters
{
    public class NodeGoExporter : NodeExporter
    {
        public static NodeGoExporter CreateInstance(Node node)
        {
            if (node != null)
            {
                Type exporterType = getExporterType(node.GetType());

                if (exporterType != null)
                {
                    return (NodeGoExporter)Activator.CreateInstance(exporterType);
                }
            }

            return new NodeGoExporter();
        }

        private static Type getExporterType(Type nodeType)
        {
            if (nodeType != null)
            {
                while (nodeType != typeof(Node))
                {
                    string nodeExporter = "PluginBehaviac.NodeExporters." + nodeType.Name + "GoExporter";
                    Type exporterType = Type.GetType(nodeExporter);

                    if (exporterType != null)
                    {
                        return exporterType;
                    }

                    foreach (Assembly assembly in Plugin.GetLoadedPlugins())
                    {
                        string filename = Path.GetFileNameWithoutExtension(assembly.Location);
                        nodeExporter = filename + ".NodeExporters." + nodeType.Name + "GoExporter";
                        exporterType = assembly.GetType(nodeExporter);

                        if (exporterType != null)
                        {
                            return exporterType;
                        }
                    }

                    nodeType = nodeType.BaseType;
                }
            }

            return null;
        }

        public override void GenerateClass(Node node, StringWriter stream, string indent, string nodeName, string agentType, string btClassName)
        {
            if (ShouldGenerateClass(node))
            {
                string className = GetGeneratedClassName(node, btClassName, nodeName);

                //stream.WriteLine("{0}\tclass {1} : behaviac.{2}\r\n{0}\t{{", indent, className, node.ExportClass);
                stream.WriteLine("type {0} struct {{", className);
                stream.WriteLine("\tbt.{0}", node.ExportClass);

                GenerateMember(node, stream, indent);

                stream.WriteLine("}");
                stream.WriteLine();

                stream.WriteLine("func New{0}() *{0} {{", className);
                stream.WriteLine("\t_o := new({0})", className);

                GenerateConstructor(node, stream, indent, className);

                stream.WriteLine("\t return _o");
                stream.WriteLine("}");
                stream.WriteLine();

                GenerateMethod(node, stream, indent, className);
                stream.WriteLine();
            }
        }

        public override void GenerateInstance(Node node, StringWriter stream, string indent, string nodeName, string agentType, string btClassName)
        {
            string nodeBehavior = GetNodeBehavior(node, btClassName, nodeName);

            // create a new instance of the node
            stream.WriteLine("{0}\t{1} := bt.NewNode({2}, {3});", indent, nodeName, node.Id, nodeBehavior);
        }

        protected string GetGeneratedClassName(Node node, string btClassName, string nodeName)
        {
            if (ShouldGenerateClass(node))
            {
                return string.Format("{0}_{1}_{2}", node.ExportClass, btClassName, nodeName);
            }

            return node.ExportClass;
        }

        protected virtual string GetNodeBehavior(Node node, string btClassName, string nodeName)
        {
            return string.Format("New{0}()", GetGeneratedClassName(node, btClassName, nodeName));
        }

        protected virtual bool ShouldGenerateClass(Node node)
        {
            return false;
        }

        protected virtual void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
        }

        protected virtual void GenerateMember(Node node, StringWriter stream, string indent)
        {
        }

        protected virtual void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
        }
    }
}
