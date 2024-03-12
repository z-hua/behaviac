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
    public class AssignmentGoExporter : NodeGoExporter
    {
        protected override bool ShouldGenerateClass(Node node)
        {
            return node is Assignment;
        }

        protected override void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);

            if (!(node is Assignment assignment))
            {
                return;
            }

            if (assignment.Opr != null)
            {
                RightValueGoExporter.GenerateClassConstructor(node, assignment.Opr, stream, indent, "opr");
            }
        }

        protected override void GenerateMember(Node node, StringWriter stream, string indent)
        {
            base.GenerateMember(node, stream, indent);

            if (!(node is Assignment assignment))
            {
                return;
            }

            stream.WriteLine("\tperformers.Assignment");

            if (assignment.Opr != null)
            {
                RightValueGoExporter.GenerateClassMember(assignment.Opr, stream, indent, "opr");
            }
        }

        protected override void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(node, stream, indent, className);

            if (!(node is Assignment assignment))
            {
                return;
            }

            stream.WriteLine("func (b *{0}) Assign(agent bt.Agent) {{", className);

            if (assignment.Opl != null && assignment.Opr != null)
            {
                PropertyDef prop = assignment.Opl.Property;

                if (prop != null)
                {
                    RightValueGoExporter.GenerateCode(node, assignment.Opr, stream, indent + "\t", assignment.Opr.NativeType.Replace("::", "."), "opr", "opr");

                    string property = PropertyGoExporter.GetProperty(node, prop, assignment.Opl.ArrayIndexElement, stream, indent + "\t", "opl", "assignment");

                    string oprStr = "opr";

/*                    if (!Plugin.IsArrayType(prop.Type))
                    {
                        if (assignment.Opr.Var != null && assignment.Opr.Var.ArrayIndexElement != null)
                        {
                            ParameterGoExporter.GenerateCode(node, assignment.Opr.Var.ArrayIndexElement, stream, indent + "\t", "int", "opr_index", "assignment_opr");
                            oprStr = string.Format("({0})[opr_index]", oprStr);
                        }
                    }*/

                    if (!prop.IsArrayElement && (prop.IsPar || prop.IsCustomized))
                    {
                        string v = prop.BasicName.Replace("[]", "");
                        string propBasicName = v;
                        uint id = CRC32.CalcCRC(propBasicName);
                        string agentName = PropertyGoExporter.GetGenerateAgentName(prop, "opl", "assignment");
                        string typename = GoExporter.GetGeneratedNativeType(prop.NativeType);

                        stream.WriteLine("{0}\t\t\t{1}.SetVariable<{2}>(\"{3}\", {4}u, {5});", indent, agentName, typename, propBasicName, id, oprStr);
                    }
                    else
                    {
                        if (assignment.IsCasting)
                        {
                            stream.WriteLine("{0}\t{1} = {2}({3})", indent, property, GoExporter.GetGeneratedNativeType(assignment.Opl.ValueType), oprStr);
                        }
                        else
                        {
                            stream.WriteLine("{0}\t{1} = {2}", indent, property, oprStr);
                        }
                    }
                }

                if (assignment.Opr.IsMethod)
                {
                    RightValueGoExporter.PostGenerateCode(assignment.Opr, stream, indent + "\t", assignment.Opr.NativeType.Replace("::", "."), "opr", string.Empty);
                }
            }

            stream.WriteLine("}");
        }
    }
}
