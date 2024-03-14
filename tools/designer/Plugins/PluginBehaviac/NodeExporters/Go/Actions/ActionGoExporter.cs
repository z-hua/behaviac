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
using PluginBehaviac.DataExporters;

namespace PluginBehaviac.NodeExporters
{
    public class ActionGoExporter : NodeGoExporter
    {
        private bool isNullMethod(MethodDef method)
        {
            return (method != null && method.BasicName == "null_method");
        }

        private string getResultOptionStr(EBTStatus status)
        {
            switch (status)
            {
                case EBTStatus.BT_SUCCESS:
                    return "bt.Success";

                case EBTStatus.BT_FAILURE:
                    return "bt.Failure";

                case EBTStatus.BT_RUNNING:
                    return "bt.Running";
            }

            return "bt.Invalid";
        }

        protected override bool ShouldGenerateClass(Node node)
        {
            return node is Action;
        }

        protected override void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);


            if (!(node is Action action))
            {
                return;
            }

            if (action.Method != null && !isNullMethod(action.Method))
            {
                MethodGoExporter.GenerateClassConstructor(node, action.Method, stream, indent, "method");
            }
        }

        protected override void GenerateMember(Node node, StringWriter stream, string indent)
        {
            base.GenerateMember(node, stream, indent);

            if (!(node is Action action))
            {
                return;
            }

            stream.WriteLine("\tperformers.Action");

            if (action.Method != null && !isNullMethod(action.Method))
            {
                MethodGoExporter.GenerateClassMember(action.Method, stream, indent, "method");
            }
        }

        protected override void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(node, stream, indent, className);

            if (!(node is Action action))
            {
                return;
            }

            stream.WriteLine("func (n *{0}) Execute(agent bt.IAgent) bt.Status {{", className);

            string resultStatus = getResultOptionStr(action.ResultOption);

            if (action.Method != null && !isNullMethod(action.Method))
            {
                string nativeReturnType = GoExporter.GetGeneratedNativeType(action.Method.NativeReturnType);
                string method = MethodGoExporter.GenerateCode(node, action.Method, stream, indent + "\t", string.Empty, string.Empty, "method");

                if ("behaviac.EBTStatus" == nativeReturnType)
                {
                    resultStatus = "result";

                    stream.WriteLine("\tresult := {0}", method);
                    MethodGoExporter.PostGenerateCode(action.Method, stream, indent + "\t", string.Empty, string.Empty, "method");
                }
                else
                {
                    if (("void" == nativeReturnType) || (EBTStatus.BT_INVALID != action.ResultOption) || action.ResultFunctor == null)
                    {
                        stream.WriteLine("\t{0}", method);
                    }
                    else
                    {
                        stream.WriteLine("\tresult := {0}", method);
                    }

                    MethodGoExporter.PostGenerateCode(action.Method, stream, indent + "\t", string.Empty, string.Empty, "method");

                    if (EBTStatus.BT_INVALID != action.ResultOption)
                    {
                        resultStatus = getResultOptionStr(action.ResultOption);
                    }
                    else if (Behaviac.Design.Plugin.IsMatchedStatusMethod(action.Method, action.ResultFunctor))
                    {
                        if ("void" == nativeReturnType)
                        {
                            resultStatus = MethodGoExporter.GenerateCode(node, action.ResultFunctor, stream, indent + "\t", string.Empty, string.Empty, "functor");
                        }
                        else
                        {
                            resultStatus = string.Format("agent.(*types.{0}).{1}(result)", action.Method.ClassName, action.ResultFunctor.BasicName);
                        }
                    }
                }
            }

            stream.WriteLine("\treturn {0}", resultStatus);
            stream.WriteLine("}");
        }
    }
}
