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
using Behaviac.Design.Attachments;
using PluginBehaviac.DataExporters;

namespace PluginBehaviac.NodeExporters
{
    public class AttachActionGoExporter : AttachmentGoExporter
    {
        protected override bool ShouldGenerateClass()
        {
            return true;
        }

        protected override void GenerateConstructor(Attachment attachment, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(attachment, stream, indent, className);

            if (!(attachment is AttachAction attach))
            {
                return;
            }

            RightValueGoExporter.GenerateClassConstructor(attachment, attach.Opl, stream, indent, "opl");

            if (!attach.IsAction())
            {
                if (attach.IsCompute() && attach.Opr1 != null)
                {
                    RightValueGoExporter.GenerateClassConstructor(attachment, attach.Opr1, stream, indent, "opr1");
                }

                if (attach.Opr2 != null)
                {
                    RightValueGoExporter.GenerateClassConstructor(attachment, attach.Opr2, stream, indent, "opr2");
                }
            }
        }

        protected override void GenerateMember(Attachment attachment, StringWriter stream, string indent)
        {
            base.GenerateMember(attachment, stream, indent);

            if (!(attachment is AttachAction attach))
            {
                return;
            }

            RightValueGoExporter.GenerateClassMember(attach.Opl, stream, indent, "opl");

            if (!attach.IsAction())
            {
                if (attach.IsCompute() && attach.Opr1 != null)
                {
                    RightValueGoExporter.GenerateClassMember(attach.Opr1, stream, indent, "opr1");
                }

                if (attach.Opr2 != null)
                {
                    RightValueGoExporter.GenerateClassMember(attach.Opr2, stream, indent, "opr2");
                }
            }
        }

        protected override void GenerateMethod(Attachment attachment, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(attachment, stream, indent, className);

            if (!(attachment is AttachAction attach))
            {
                return;
            }

            stream.WriteLine("func (n *{0}) Update(agent bt.IAgent) bt.Status {{", className);
            stream.WriteLine("\tresult := bt.Success", indent);

            if (attach.IsAction())
            {
                string method = MethodGoExporter.GenerateCode(attachment, attach.Opl.Method, stream, indent + "\t", string.Empty, string.Empty, "opl");

                stream.WriteLine("{0}\t{1}", indent, method);
                MethodGoExporter.PostGenerateCode(attach.Opl.Method, stream, indent + "\t", string.Empty, string.Empty, "opl");
            }
            else if (attach.IsAssign())
            {
                if (attach.Opl != null && !attach.Opl.IsMethod && attach.Opl.Var != null && attach.Opr2 != null)
                {
                    PropertyDef prop = attach.Opl.Var.Property;

                    if (prop != null)
                    {
                        RightValueGoExporter.GenerateCode(attachment, attach.Opr2, stream, indent + "\t", attach.Opr2.NativeType.Replace("::", "."), "opr2", "opr2");

                        string propBasicName = Utilities.ToPascalCase(prop.BasicName.Replace("[]", ""));

                        stream.WriteLine("\t{0} = opr2", propBasicName);

                        if (attach.Opr2.IsMethod)
                        {
                            RightValueGoExporter.PostGenerateCode(attach.Opr2, stream, indent + "\t", attach.Opr2.NativeType.Replace("::", "."), "opr2", string.Empty);
                        }
                    }
                }
            }
            else if (attach.IsCompare())
            {
                ConditionGoExporter.GenerateOperand(attachment, stream, indent + "\t", attach.Opl, "opl", "");
                ConditionGoExporter.GenerateOperand(attachment, stream, indent + "\t", attach.Opr2, "opr2", "");

                switch (attach.Operator)
                {
                    case OperatorTypes.Equal:
                        stream.WriteLine("{0}\top := opl == opr2", indent);
                        break;

                    case OperatorTypes.NotEqual:
                        stream.WriteLine("{0}\top := opl != opr2", indent);
                        break;

                    case OperatorTypes.Greater:
                        stream.WriteLine("{0}\top := opl > opr2", indent);
                        break;

                    case OperatorTypes.GreaterEqual:
                        stream.WriteLine("{0}\top := opl >= opr2", indent);
                        break;

                    case OperatorTypes.Less:
                        stream.WriteLine("{0}\top := opl < opr2", indent);
                        break;

                    case OperatorTypes.LessEqual:
                        stream.WriteLine("{0}\top := opl <= opr2", indent);
                        break;

                    default:
                        stream.WriteLine("{0}\top := false", indent);
                        break;
                }

                stream.WriteLine("{0}\tif !op {{", indent);
                stream.WriteLine("{0}\t\tresult = bt.Failure", indent);
                stream.WriteLine("\t}");
            }
            else if (attach.IsCompute())
            {
                if (attach.Opl != null && !attach.Opl.IsMethod && attach.Opl.Var != null && attach.Opr1 != null && attach.Opr2 != null)
                {
                    PropertyDef prop = attach.Opl.Var.Property;

                    if (prop != null)
                    {
                        string typeName = Plugin.GetNativeTypeName(attach.Opr1.ValueType);
                        typeName = typeName.Replace("::", ".");

                        RightValueGoExporter.GenerateCode(attachment, attach.Opr1, stream, indent + "\t", typeName, "opr1", "opr1");
                        RightValueGoExporter.GenerateCode(attachment, attach.Opr2, stream, indent + "\t", typeName, "opr2", "opr2");

                        string oprStr = string.Empty;

                        switch (attach.Operator)
                        {
                            case OperatorTypes.Add:
                                oprStr = "opr1 + opr2";
                                break;

                            case OperatorTypes.Sub:
                                oprStr = "opr1 - opr2";
                                break;

                            case OperatorTypes.Mul:
                                oprStr = "opr1 * opr2";
                                break;

                            case OperatorTypes.Div:
                                oprStr = "opr1 / opr2";
                                break;

                            default:
                                Debug.Check(false, "The operator is wrong!");
                                break;
                        }

                        oprStr = string.Format("{0}({1})", typeName, oprStr);

                        string property = PropertyGoExporter.GetProperty(attachment, prop, attach.Opl.Var.ArrayIndexElement, stream, indent + "\t", "opl", "attach");

                        stream.WriteLine("\t{0} = {1}", property, oprStr);

                        if (attach.Opr1.IsMethod)
                        {
                            RightValueGoExporter.PostGenerateCode(attach.Opr1, stream, indent + "\t", typeName, "opr1", string.Empty);
                        }

                        if (attach.Opr2.IsMethod)
                        {
                            RightValueGoExporter.PostGenerateCode(attach.Opr2, stream, indent + "\t", typeName, "opr2", string.Empty);
                        }
                    }
                }
            }

            stream.WriteLine("{0}\treturn result", indent);
            stream.WriteLine("}");
        }
    }
}
