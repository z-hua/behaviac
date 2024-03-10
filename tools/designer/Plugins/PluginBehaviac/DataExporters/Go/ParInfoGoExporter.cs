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
using Behaviac.Design;
using Behaviac.Design.Attributes;

namespace PluginBehaviac.DataExporters
{
    public class ParInfoGoExporter
    {
        public static string GenerateCode(Behaviac.Design.PropertyDef property, bool isRefParam, StringWriter stream, string indent, string typename, string var, string caller)
        {
            bool shouldDefineType = true;

            if (string.IsNullOrEmpty(typename))
            {
                shouldDefineType = false;
                typename = property.NativeType;
            }
            else if (typename == "System.Object" || typename == "System.Collections.IList")
            {
                typename = property.NativeType;
            }
            else
            {
                //
            }

            typename = DataGoExporter.GetGeneratedNativeType(typename);

            if (property.IsArrayElement && !typename.StartsWith("List<"))
            {
                typename = string.Format("[]{0}", typename);
            }

            string propBasicName = property.BasicName.Replace("[]", "");
            uint id = Behaviac.Design.CRC32.CalcCRC(propBasicName);
            string retStr = string.Format("pAgent.GetVariable<{0}>({1}u)", typename, id);

            if (!string.IsNullOrEmpty(var))
            {
                stream.WriteLine("{0}Debug.Check(behaviac.Utils.MakeVariableId(\"{1}\") == {2}u);", indent, propBasicName, id);

                {
                    if (shouldDefineType)
                    {
                        stream.WriteLine("{0}{1} {2} = {3};", indent, typename, var, retStr);
                    }
                    else
                    {
                        stream.WriteLine("{0}{1} = {2};", indent, var, retStr);
                    }
                }
            }

            return retStr;
        }

        public static void PostGenerateCode(PropertyDef property, MethodDef.Param arrayIndexElement, StringWriter stream, string indent, string typename, string var, string caller)
        {
            if (string.IsNullOrEmpty(typename))
            {
                typename = property.NativeType;
            }

            typename = DataGoExporter.GetGeneratedNativeType(typename);

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
                string typename = DataGoExporter.GetGeneratedNativeType(property.NativeType);

                if (property.IsArrayElement && !typename.StartsWith("List<"))
                {
                    typename = string.Format("List<{0}>", typename);
                }

                string propBasicName = property.BasicName.Replace("[]", "");
                uint id = Behaviac.Design.CRC32.CalcCRC(propBasicName);

                stream.WriteLine("{0}Debug.Check(behaviac.Utils.MakeVariableId(\"{1}\") == {2}u);", indent, propBasicName, id);
                retStr = string.Format("{0}.GetVariable<{1}>({2}u)", agentName, typename, id);
            }

            return retStr;
        }
    }
}
