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
using Behaviac.Design;
using Behaviac.Design.Nodes;
using PluginBehaviac.DataExporters;

namespace PluginBehaviac.NodeExporters
{
    public class ConditionGoExporter : NodeGoExporter
    {
        protected override bool ShouldGenerateClass(Node node)
        {
            return node is Nodes.Condition;
        }

        protected override void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);

            if (!(node is Nodes.Condition condition))
            {
                return;
            }

            if (condition.Opl != null)
            {
                RightValueGoExporter.GenerateClassConstructor(node, condition.Opl, stream, indent, "opl");
            }

            if (condition.Opr != null)
            {
                RightValueGoExporter.GenerateClassConstructor(node, condition.Opr, stream, indent, "opr");
            }
        }

        protected override void GenerateMember(Node node, StringWriter stream, string indent)
        {
            base.GenerateMember(node, stream, indent);

            if (!(node is Nodes.Condition condition))
            {
                return;
            }

            stream.WriteLine("\tconditions.Condition");

            if (condition.Opl != null)
            {
                RightValueGoExporter.GenerateClassMember(condition.Opl, stream, indent, "opl");
            }

            if (condition.Opr != null)
            {
                RightValueGoExporter.GenerateClassMember(condition.Opr, stream, indent, "opr");
            }
        }

        public static void GenerateOperand(DefaultObject defaultObj, StringWriter stream, string indent, RightValueDef operand, string operandName, string nodeName)
        {
            if (operand != null)
            {
                string typeName = DataGoExporter.GetGeneratedNativeType(operand.ValueType);
                typeName = typeName.Replace("::", ".");

                if (operand.IsMethod) // method
                {
                    RightValueGoExporter.GenerateCode(defaultObj, operand, stream, indent, typeName, operandName, string.Empty);
                    RightValueGoExporter.PostGenerateCode(operand, stream, indent, typeName, operandName, string.Empty);
                }
                else
                {
                    VariableDef var = operand.Var;

                    if (var != null)
                    {
                        if (var.IsProperty) // property
                        {
                            PropertyDef prop = var.Property;

                            if (prop != null)
                            {
                                string property = PropertyGoExporter.GetProperty(defaultObj, prop, var.ArrayIndexElement, stream, indent, operandName, nodeName);
                                stream.WriteLine("{0}{1} := {2}", indent, operandName, property);
                            }
                        }
                        else if (var.IsConst) // const
                        {
                            RightValueGoExporter.GenerateCode(defaultObj, operand, stream, indent, typeName, operandName, string.Empty);
                        }
                    }
                }
            }
        }

        protected override void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(node, stream, indent, className);

            if (!(node is Nodes.Condition condition))
            {
                return;
            }

            stream.WriteLine("func (n *{0}) Compare(agent bt.IAgent) bool {{", className);

            // opl
            ConditionGoExporter.GenerateOperand(node, stream, indent + "\t", condition.Opl, "opl", "condition");

            // opr
            ConditionGoExporter.GenerateOperand(node, stream, indent + "\t", condition.Opr, "opr", "condition");

            // Operator
            switch (condition.Operator)
            {
                case OperatorType.Equal:
                    stream.WriteLine("\treturn opl == opr");
                    break;

                case OperatorType.NotEqual:
                    stream.WriteLine("\treturn opl != opr");
                    break;

                case OperatorType.Greater:
                    stream.WriteLine("\treturn opl > opr");
                    break;

                case OperatorType.GreaterEqual:
                    stream.WriteLine("\treturn opl >= opr");
                    break;

                case OperatorType.Less:
                    stream.WriteLine("\treturn opl < opr");
                    break;

                case OperatorType.LessEqual:
                    stream.WriteLine("\treturn opl <= opr");
                    break;

                case OperatorType.And:
                    stream.WriteLine("\treturn opl && opr");
                    break;

                case OperatorType.Or:
                    stream.WriteLine("\treturn opl || opr");
                    break;

                default:
                    stream.WriteLine("\treturn false");
                    break;
            }

            stream.WriteLine("}");
        }

        public override void CollectImport(StringWriter stream, Dictionary<string, bool> imported)
        {
            ImportCondition(stream, imported);
        }
    }
}
