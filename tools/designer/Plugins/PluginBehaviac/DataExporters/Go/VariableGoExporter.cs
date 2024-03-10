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
    public class VariableGoExporter
    {
        public static void GenerateClassConstructor(DefaultObject defaultObj, VariableDef variable, StringWriter stream, string indent, string var)
        {
            if (variable.ValueClass == VariableDef.kConst)
            {
                Type type = variable.Value.GetType();

                if (/*Plugin.IsArrayType(type) || */Plugin.IsCustomClassType(type) || (Plugin.IsStringType(type) && !variable.IsConst))
                {
                    if (Plugin.IsArrayType(type))
                    {
                        string nativeType = DataGoExporter.GetGeneratedNativeType(variable.NativeType);
                        //int startIndex = nativeType.IndexOf('<');
                        //int endIndex = nativeType.LastIndexOf('>');
                        //string itemType = nativeType.Substring(startIndex + 1, endIndex - startIndex - 1);

                        ArrayGoExporter.GenerateCode(variable.Value, defaultObj, stream, indent + "\t\t\t", nativeType, var);
                    }
                    else if (Plugin.IsCustomClassType(type))
                    {
                        string nativeType = DataGoExporter.GetGeneratedNativeType(variable.NativeType);
                        StructGoExporter.GenerateCode(variable.Value, defaultObj, stream, indent + "\t\t\t", var, nativeType, null, "");
                    }
                    else if ((Plugin.IsStringType(type) && !variable.IsConst))
                    {
                        string nativeType = DataGoExporter.GetGeneratedNativeType(variable.NativeType);
                        string retStr = DataGoExporter.GenerateCode(variable.Value, defaultObj, stream, indent + "\t\t\t", nativeType, string.Empty, string.Empty);
                        stream.WriteLine("{0}\t\t\t{1} = {2};", indent, var, retStr);
                    }
                }
            }
        }

        public static void GenerateClassMember(VariableDef variable, StringWriter stream, string indent, string var)
        {
            if (variable.ValueClass == VariableDef.kConst)
            {
                Type type = variable.Value.GetType();

                if (/*Plugin.IsArrayType(type) || */Plugin.IsCustomClassType(type) || (Plugin.IsStringType(type) && !variable.IsConst))
                {
                    string nativeType = DataGoExporter.GetGeneratedNativeType(variable.NativeType);

                    /*bool setNull = Plugin.IsArrayType(type);

                    if (!setNull && Plugin.IsCustomClassType(type))
                    {
                        Attribute[] attributes = (Attribute[])type.GetCustomAttributes(typeof(ClassDescAttribute), false);

                        if (attributes.Length > 0)
                        {
                            ClassDescAttribute classDesc = (ClassDescAttribute)attributes[0];

                            if (!classDesc.IsStruct)
                            {
                                setNull = true;
                            }
                        }
                    }

                    if (setNull)
                    {
                        var += " = null";
                    }*/

                    stream.WriteLine("\t{0} {1}", var, nativeType);
                }
            }
        }

        public static string GenerateCode(DefaultObject defaultObj, VariableDef variable, bool isRefParam, StringWriter stream, string indent, string typename, string var, string caller, string setValue = null)
        {
            string retStr = string.Empty;

            if (variable.ValueClass == VariableDef.kConst)
            {
                bool shouldGenerate = true;
                Type type = variable.Value.GetType();

/*                if (Plugin.IsArrayType(type))
                {
                    string nativeType = DataGoExporter.GetGeneratedNativeType(variable.NativeType);
                    ArrayGoExporter.GenerateCode(variable.Value, defaultObj, stream, indent + "\t\t\t", nativeType, var);
                }*/

                if (/*Plugin.IsArrayType(type) || */Plugin.IsCustomClassType(type) || (Plugin.IsStringType(type) && !variable.IsConst))
                {
                    shouldGenerate = false;
                }

                if (shouldGenerate)
                {
                    retStr = DataGoExporter.GenerateCode(variable.Value, defaultObj, stream, indent, typename, var, caller, setValue);
                }
            }
            else if (variable.Property != null)
            {
                retStr = PropertyGoExporter.GenerateCode(defaultObj, variable.Property, variable.ArrayIndexElement, isRefParam, stream, indent, typename, var, caller, setValue);
            }

            return retStr;
        }

        public static void PostGenerateCode(VariableDef variable, StringWriter stream, string indent, string typename, string var, string caller, object parent = null, string paramName = "", string setValue = null)
        {
            if (variable.ValueClass == VariableDef.kConst)
            {
                Type type = variable.Value.GetType();

                if (Plugin.IsCustomClassType(type) && !DesignerStruct.IsPureConstDatum(variable.Value, parent, paramName))
                {
                    StructGoExporter.PostGenerateCode(variable.Value, stream, indent, var, parent, paramName);
                }
            }
            else if (variable.Property != null)
            {
                PropertyGoExporter.PostGenerateCode(variable.Property, variable.ArrayIndexElement, stream, indent, typename, var, caller, setValue);
            }
        }
    }
}
