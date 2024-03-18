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
using System.IO;
using Behaviac.Design;
using Behaviac.Design.Attributes;

namespace PluginBehaviac.DataExporters
{
    public class MethodGoExporter
    {
        public static void GenerateClassConstructor(DefaultObject defaultObj, MethodDef method, StringWriter stream, string indent, string var)
        {
            Debug.Check(!string.IsNullOrEmpty(var));

            for (int i = 0; i < method.Params.Count; ++i)
            {
                // const value
                if (!method.Params[i].IsProperty && !method.Params[i].IsLocalVar)
                {
                    object obj = method.Params[i].Value;

                    if (obj != null)
                    {
                        string param = getParamName(var, "", i);

                        Type type = obj.GetType();

                        if (Behaviac.Design.Plugin.IsArrayType(type))
                        {
                            string typename = GoExporter.GetGeneratedNativeType(method.Params[i].NativeType);
                            int startIndex = typename.IndexOf('<');
                            int endIndex = typename.LastIndexOf('>');
                            string itemType = typename.Substring(startIndex + 1, endIndex - startIndex - 1);

                            ArrayGoExporter.GenerateCode(obj, defaultObj, stream, indent + "\t\t\t", itemType, param);
                        }
                        else if (Behaviac.Design.Plugin.IsCustomClassType(type))
                        {
                            if (DesignerStruct.IsPureConstDatum(obj, method, method.Params[i].Name))
                            {
                                string paramType = GoExporter.GetGeneratedNativeType(method.Params[i].NativeType);
                                StructGoExporter.GenerateCode(obj, defaultObj, stream, indent + "\t\t\t", param, paramType, null, "");
                            }
                        }
                        else
                        {
                            string retStr = DataGoExporter.GenerateCode(obj, defaultObj, stream, string.Empty, method.Params[i].NativeType, string.Empty, string.Empty);

                            stream.WriteLine("\tn.{0} = {1}", param, retStr);
                        }
                    }
                }
            }
        }

        public static void GenerateClassMember(MethodDef method, StringWriter stream, string indent, string var)
        {
            Debug.Check(!string.IsNullOrEmpty(var));

            for (int i = 0; i < method.Params.Count; ++i)
            {
                // const value
                if (/*!method.Params[i].IsProperty && !method.Params[i].IsLocalVar && */method.Params[i].Value != null)
                {
                    string param = getParamName(var, "", i);
                    string nativeType = GoExporter.GetGeneratedNativeType(method.Params[i].NativeType);
                    stream.WriteLine("\t{0} {1}", param, nativeType);
                }
            }
        }

        public static string GenerateCode(DefaultObject defaultObj, MethodDef method, StringWriter stream, string indent, string typename, string var, string caller)
        {
            Debug.Check(!string.IsNullOrEmpty(var) || !string.IsNullOrEmpty(caller));

            string allParams = string.Empty;

            for (int i = 0; i < method.Params.Count; ++i)
            {
                string nativeType = GoExporter.GetGeneratedNativeType(method.Params[i].NativeType);
                string param = "n." + getParamName(var, caller, i);

                if (method.Params[i].IsProperty || method.Params[i].IsLocalVar) // property
                {
                    if ((method.Params[i].Property != null && method.Params[i].Property.IsCustomized) || method.Params[i].IsLocalVar)
                    {
                        ParameterGoExporter.GenerateCode(defaultObj, method.Params[i], stream, indent, nativeType, param, caller);
                    }
                    else
                    {
                        param = ParameterGoExporter.GenerateCode(defaultObj, method.Params[i], stream, indent, nativeType, "", param);
                    }
                }
                else // const value
                {
                    object obj = method.Params[i].Value;

                    if (obj != null)
                    {
                        Type type = obj.GetType();

                        if (Behaviac.Design.Plugin.IsCustomClassType(type) && !DesignerStruct.IsPureConstDatum(obj, method, method.Params[i].Name))
                        {
                            string paramName = getParamName(var, caller, i);
                            string paramType = GoExporter.GetGeneratedNativeType(method.Params[i].NativeType);

                            StructGoExporter.GenerateCode(obj, defaultObj, stream, indent, paramName, paramType, method, method.Params[i].Name);
                        }
                    }
                }

                if (i > 0)
                {
                    allParams += ", ";
                }

                allParams += param;
            }

            string className = method.ClassName.Replace("::", ".");
            string retStr;
            if (className == "behaviac.Agent")
            {
                retStr = string.Format("agent.{0}({1})", Utilities.ToPascalCase(method.BasicName), allParams);
            }
            else
            {
                retStr = string.Format("agent.(*{0}).{1}({2})", className, Utilities.ToPascalCase(method.BasicName), allParams);
            }
            

            if (!string.IsNullOrEmpty(var))
            {
                stream.WriteLine("{0}{1} := {2}", indent, var, retStr);
            }

            return retStr;
        }

        public static void PostGenerateCode(MethodDef method, StringWriter stream, string indent, string typename, string var, string caller)
        {
            string paramsName = getParamsName(var, caller);

            for (int i = 0; i < method.Params.Count; ++i)
            {
                if (method.Params[i].IsRef || method.Params[i].IsOut)
                {
                    object obj = method.Params[i].Value;

                    if (obj != null)
                    {
                        string nativeType = DataGoExporter.GetGeneratedNativeType(method.Params[i].NativeType);
                        string param = getParamName(var, caller, i);

                        string paramName = string.Format("(({0}){1}[{2}])", nativeType, paramsName, i);

                        if (!method.Params[i].IsProperty && !method.Params[i].IsLocalVar)
                        {
                            Type type = obj.GetType();

                            if (!Plugin.IsArrayType(type) && !Plugin.IsCustomClassType(type))
                            {
                                param = paramName;
                            }
                        }
                        else
                        {
                            paramName = null;
                        }

                        ParameterGoExporter.PostGenerateCode(method.Params[i], stream, indent, nativeType, param, caller, method, paramName);
                    }
                }
            }
        }

        private static string getParamsName(string var, string caller)
        {
            Debug.Check(!string.IsNullOrEmpty(var) || !string.IsNullOrEmpty(caller));

            return (string.IsNullOrEmpty(var) ? caller : var) + "_params";
        }

        private static string getParamName(string var, string caller, int index)
        {
            Debug.Check(!string.IsNullOrEmpty(var) || !string.IsNullOrEmpty(caller));

            return (string.IsNullOrEmpty(var) ? caller : var) + "P" + index;
        }
    }
}
