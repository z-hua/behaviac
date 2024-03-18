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

namespace PluginBehaviac.DataExporters
{
    public class ParInfoGoExporter
    {
        public static string GenerateCode(DefaultObject defaultObj, PropertyDef property, bool isRefParam, StringWriter stream, string indent, string typename, string var, string caller)
        {
            if (string.IsNullOrEmpty(typename))
            {
                typename = property.NativeType;
            }
            else if (typename == "System.Object" || typename == "System.Collections.IList")
            {
                typename = property.NativeType;
            }

            typename = GoExporter.GetGeneratedNativeType(typename);

            if (property.IsArrayElement && !typename.StartsWith("List<"))
            {
                typename = string.Format("[]{0}", typename);
            }

            string propBasicName = property.BasicName.Replace("[]", "");
            string retStr = string.Format("agent.GetLocal(\"{0}\").({1})", propBasicName, typename);

            if (property.IsArrayElement)
            {
                string index = "";
                if (property.Variable.IsConst)
                {
                    index = property.Variable.Value.ToString();
                }
                else if (property.Variable.ArrayIndexElement != null)
                {
                    index = property.Variable.ArrayIndexElement.Value.ToString();
                }
                retStr += string.Format("[{0}]", index);
            }

            if (!string.IsNullOrEmpty(var))
            {
                stream.WriteLine("{0}{1} = {2}", indent, var, retStr);
            }

            return retStr;
        }

        public static void PostGenerateCode(PropertyDef property, MethodDef.Param arrayIndexElement, StringWriter stream, string indent, string typename, string var, string caller)
        {
            if (string.IsNullOrEmpty(typename))
            {
                typename = property.NativeType;
            }

            typename = GoExporter.GetGeneratedNativeType(typename);

            string propBasicName = property.BasicName.Replace("[]", "");
            uint id = CRC32.CalcCRC(propBasicName);

            stream.WriteLine("{0}Debug.Check(behaviac.Utils.MakeVariableId(\"{1}\") == {2}u);", indent, propBasicName, id);
            stream.WriteLine("{0}pAgent.SetVariable<{1}>(\"{2}\", {3}u, ({1}){4});", indent, typename, property.Name, id, var);
        }

        public static string GetProperty(string agentName, PropertyDef property, MethodDef.Param arrayIndexElement, StringWriter stream, string indent)
        {
            string retStr = string.Empty;

            if (property != null)
            {
                string typename = GoExporter.GetGeneratedNativeType(property.NativeType);

                if (property.IsArrayElement && !typename.StartsWith("List<"))
                {
                    typename = string.Format("List<{0}>", typename);
                }

                string propBasicName = property.BasicName.Replace("[]", "");
                uint id = CRC32.CalcCRC(propBasicName);

                stream.WriteLine("{0}Debug.Check(behaviac.Utils.MakeVariableId(\"{1}\") == {2}u);", indent, propBasicName, id);
                retStr = string.Format("{0}.GetVariable<{1}>({2}u)", agentName, typename, id);
            }

            return retStr;
        }
    }
}
