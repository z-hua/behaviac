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
    public class ComputeGoExporter : NodeGoExporter
    {
        protected override bool ShouldGenerateClass(Node node)
        {
            return node is Compute;
        }

        protected override void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);

            if (!(node is Compute compute))
            {
                return;
            }

            if (compute.Opr1 != null)
            {
                RightValueGoExporter.GenerateClassConstructor(node, compute.Opr1, stream, indent, "opr1");
            }

            if (compute.Opr2 != null)
            {
                RightValueGoExporter.GenerateClassConstructor(node, compute.Opr2, stream, indent, "opr2");
            }
        }

        protected override void GenerateMember(Node node, StringWriter stream, string indent)
        {
            base.GenerateMember(node, stream, indent);

            if (!(node is Compute compute))
            {
                return;
            }

            stream.WriteLine("\tperformers.Compute");

            if (compute.Opr1 != null)
            {
                RightValueGoExporter.GenerateClassMember(compute.Opr1, stream, indent, "opr1");
            }

            if (compute.Opr2 != null)
            {
                RightValueGoExporter.GenerateClassMember(compute.Opr2, stream, indent, "opr2");
            }
        }

        protected override void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(node, stream, indent, className);

            if (!(node is Compute compute))
            {
                return;
            }

            stream.WriteLine("func (n *{0}) Calculate(agent bt.IAgent) {{", className);

            if (compute.Opl != null && compute.Opr1 != null && compute.Opr2 != null)
            {
                string typeName = GoExporter.GetGeneratedNativeType(compute.Opr1.ValueType);
                typeName = typeName.Replace("::", ".");

                RightValueGoExporter.GenerateCode(node, compute.Opr1, stream, indent + "\t", typeName, "opr1", "opr1");
                RightValueGoExporter.GenerateCode(node, compute.Opr2, stream, indent + "\t", typeName, "opr2", "opr2");

                string oprStr = string.Empty;

                switch (compute.Operator)
                {
                    case ComputeOperator.Add:
                        oprStr = "opr1 + opr2";
                        break;

                    case ComputeOperator.Sub:
                        oprStr = "opr1 - opr2";
                        break;

                    case ComputeOperator.Mul:
                        oprStr = "opr1 * opr2";
                        break;

                    case ComputeOperator.Div:
                        oprStr = "opr1 / opr2";
                        break;

                    default:
                        Debug.Check(false, "The operator is wrong!");
                        break;
                }

                oprStr = string.Format("{0}({1})", typeName, oprStr);

                PropertyDef prop = compute.Opl.Property;

                if (prop != null)
                {
                    string property = PropertyGoExporter.GetProperty(node, prop, compute.Opl.ArrayIndexElement, stream, indent + "\t", "opl", "compute");

                    if (!prop.IsArrayElement && (prop.IsPar || prop.IsCustomized))
                    {
                        string propBasicName = prop.BasicName.Replace("[]", "");
                        uint id = CRC32.CalcCRC(propBasicName);
                        string agentName = PropertyGoExporter.GetGenerateAgentName(prop, "opl", "compute");
                        string typename = GoExporter.GetGeneratedNativeType(prop.NativeType);

                        stream.WriteLine("{0}\t\t\t{1}.SetVariable<{2}>(\"{3}\", {4}u, {5});", indent, agentName, typename, propBasicName, id, oprStr);
                    }
                    else
                    {
                        stream.WriteLine("{0}\t{1} = {2}", indent, property, oprStr);
                    }
                }

                if (compute.Opr1.IsMethod)
                {
                    RightValueGoExporter.PostGenerateCode(compute.Opr1, stream, indent + "\t", typeName, "opr1", string.Empty);
                }

                if (compute.Opr2.IsMethod)
                {
                    RightValueGoExporter.PostGenerateCode(compute.Opr2, stream, indent + "\t", typeName, "opr2", string.Empty);
                }
            }

            stream.WriteLine("}");
        }
    }
}
