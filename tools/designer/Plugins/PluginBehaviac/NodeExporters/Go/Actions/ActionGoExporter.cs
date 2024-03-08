﻿/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
            Action action = node as Action;
            return (action != null);
        }

        protected override void GenerateConstructor(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);

            Action action = node as Action;

            if (action == null)
            {
                return;
            }

            stream.WriteLine("\t_o.Callback = _o.callbackFn");

            if (action.Method != null && !isNullMethod(action.Method))
            {
                MethodGoExporter.GenerateClassConstructor(node, action.Method, stream, indent, "method");
            }
        }

        protected override void GenerateMember(Node node, StringWriter stream, string indent)
        {
            base.GenerateMember(node, stream, indent);

            Action action = node as Action;

            if (action == null)
            {
                return;
            }

            if (action.Method != null && !isNullMethod(action.Method))
            {
                MethodGoExporter.GenerateClassMember(action.Method, stream, indent, "method");
            }
        }

        protected override void GenerateMethod(Node node, StringWriter stream, string indent, string className)
        {
            base.GenerateMethod(node, stream, indent, className);

            Action action = node as Action;

            if (action == null)
            {
                return;
            }

            stream.WriteLine("func (_o *{0}) callbackFn() func(agent bt.Agent) bt.Status {{", className);
            stream.WriteLine("\treturn func(agent bt.Agent) bt.Status {");

            indent += "\t";

            string resultStatus = getResultOptionStr(action.ResultOption);

            if (action.Method != null && !isNullMethod(action.Method))
            {
                string nativeReturnType = DataGoExporter.GetGeneratedNativeType(action.Method.NativeReturnType);
                string method = MethodGoExporter.GenerateCode(node, action.Method, stream, indent + "\t\t\t", string.Empty, string.Empty, "method");

                if ("behaviac.EBTStatus" == nativeReturnType)
                {
                    resultStatus = "result";

                    stream.WriteLine("\t\tresult := {0}", method);
                    MethodGoExporter.PostGenerateCode(action.Method, stream, indent + "\t\t\t", string.Empty, string.Empty, "method");
                }
                else
                {
                    if (("void" == nativeReturnType) || (EBTStatus.BT_INVALID != action.ResultOption) || action.ResultFunctor == null)
                    {
                        stream.WriteLine("\t\t{0}", method);
                    }
                    else
                    {
                        stream.WriteLine("\t\tresult := {0}", method);
                    }

                    MethodGoExporter.PostGenerateCode(action.Method, stream, indent + "\t\t\t", string.Empty, string.Empty, "method");

                    if (EBTStatus.BT_INVALID != action.ResultOption)
                    {
                        resultStatus = getResultOptionStr(action.ResultOption);
                    }
                    else if (Plugin.IsMatchedStatusMethod(action.Method, action.ResultFunctor))
                    {
                        if ("void" == nativeReturnType)
                        {
                            resultStatus = MethodGoExporter.GenerateCode(node, action.ResultFunctor, stream, indent + "\t\t\t", string.Empty, string.Empty, "functor");
                        }
                        else
                        {
                            string agentName = "agent";

                            if (action.ResultFunctor.Owner != VariableDef.kSelf &&
                                (!action.ResultFunctor.IsPublic || !action.ResultFunctor.IsStatic))
                            {
                                string instanceName = action.ResultFunctor.Owner.Replace("::", ".");
                                agentName = "agent_functor";

                                stream.WriteLine("{0}behaviac.Agent {1} = behaviac.Utils.GetParentAgent(pAgent, \"{2}\");", indent, agentName, instanceName);
                                //stream.WriteLine("{0}Debug.Check(!System.Object.ReferenceEquals({1}, null) || Utils.IsStaticClass(\"{2}\"));", indent, agentName, instanceName);
                            }

                            resultStatus = string.Format("{0}.({1}).{2}(result)", agentName, action.Method.ClassName, action.ResultFunctor.BasicName);
                        }
                    }
                }
            }

            stream.WriteLine("\t\treturn {0}", resultStatus);
            stream.WriteLine("\t}");
            stream.WriteLine("}");
        }
    }
}
