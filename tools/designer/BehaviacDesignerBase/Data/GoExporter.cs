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
using System.Windows.Forms;
using Behaviac.Design.Attributes;

namespace Behaviac.Design
{
    public class GoExporter
    {
        public static string GetExportNativeType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return string.Empty;
            }

            typeName = Plugin.GetNativeTypeName(typeName, true);

            typeName = typeName.Replace("unsigned ", "u");
            typeName = typeName.Replace("signed ", "");
            typeName = typeName.Replace("const ", "");
            typeName = typeName.Replace("behaviac::wstring", "string");
            typeName = typeName.Replace("behaviac::string", "string");
            typeName = typeName.Replace("std::string", "string");
            typeName = typeName.Replace("char*", "string");
            typeName = typeName.Replace("cszstring", "string");
            typeName = typeName.Replace("szstring", "string");
            typeName = typeName.Replace("ubyte", "byte");
            typeName = typeName.Replace("unsigned long long", "ullong");
            typeName = typeName.Replace("signed long long", "llong");
            typeName = typeName.Replace("long long", "llong");
            typeName = typeName.Replace("unsigned ", "u");

            typeName = typeName.Trim();

            return typeName;
        }

        public static string GetGeneratedNativeType(string typeName)
        {
            typeName = GetExportNativeType(typeName);

            typeName = typeName.Replace("::", ".");
            typeName = typeName.Replace("*", "");
            typeName = typeName.Replace("&", "");
            typeName = typeName.Replace("ullong", "ulong");
            typeName = typeName.Replace("llong", "long");

            typeName = typeName.Replace("long", "int64");
            typeName = typeName.Replace("ulong", "uint64");
            typeName = typeName.Replace("sbyte", "byte");
            typeName = typeName.Replace("ubyte", "byte");
            typeName = typeName.Replace("char", "byte");
            typeName = typeName.Replace("float", "float32");
            typeName = typeName.Replace("double", "float64");
            typeName = typeName.Replace("short", "int16");
            typeName = typeName.Replace("behaviac.EBTStatus", "bt.Status");
            typeName = typeName.Replace("behaviac.Agent", "bt.Agent");
            typeName = typeName.Replace("EBTStatus", "bt.Status");

            typeName = typeName.Trim();

            if (typeName.StartsWith("vector<"))
            {
                typeName = typeName.Replace("vector<", "[]");
                typeName = typeName.Replace(">", "");
            }

            if (Plugin.TypeRenames.Count > 0)
            {
                foreach (KeyValuePair<string, string> typePair in Plugin.TypeRenames)
                {
                    typeName = typeName.Replace(typePair.Key, typePair.Value);
                }
            }

            return typeName;
        }

        public static string GetGeneratedNativeType(Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            if (Plugin.IsArrayType(type))
            {
                Type itemType = type.GetGenericArguments()[0];
                return string.Format("[]{0}", GoExporter.GetGeneratedNativeType(itemType));
            }

            return GetGeneratedNativeType(type.Name);
        }

        public static string GetGeneratedParType(Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            string typeName = GetGeneratedNativeType(type);

            if (typeName.StartsWith("List<"))
            {
                typeName = typeName.Replace("List<", "[]");
                typeName = typeName.Replace(">", "");
            }

            return typeName;
        }

        public static string GetGeneratedDefaultValue(Type type, string typename, string defaultValue = null)
        {
            if (type == typeof(void))
            {
                return null;
            }

            string value = defaultValue;

            if (string.IsNullOrEmpty(defaultValue))
            {
                if (!Plugin.IsStringType(type))
                {
                    value = DesignerPropertyUtility.RetrieveExportValue(Plugin.DefaultValue(type));
                }
                else
                {
                    value = "";
                }
            }

            if (type == typeof(char))
            {
                value = "0";
            }
            else if (type == typeof(float))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    value = value.Replace("f", "");
                }
            }
            else if (Plugin.IsStringType(type))
            {
                value = "\"" + value.Trim('"') + "\"";
            }
            else if (Plugin.IsEnumType(type))
            {
                if (type.Name == "behaviac_EBTStatus" || type.Name == "EBTStatus")
                {
                    switch (value) 
                    {
                        case "BT_SUCCESS":
                            value = "bt.Success";
                            break;
                        case "BT_FAILURE":
                            value = "bt.Failure";
                            break;
                        case "BT_RUNNING":
                            value = "bt.Running";
                            break;
                         default:
                            value = "bt.Invalid";
                            break;
                    }
                }
                else
                {
                    value = string.Format("{0}.{1}", typename, value);
                }
            }
            else if (Plugin.IsArrayType(type))
            {
                value = "nil";
            }
            else if (Plugin.IsCustomClassType(type))
            {
                if (Plugin.IsRefType(type))
                {
                    value = "nil";
                }
                else
                {
                    value = "new(" + typename + ")";
                }
            }

            return value;
        }

        public static string GetGeneratedPropertyDefaultValue(PropertyDef prop, string typename)
        {
            return (prop != null) ? GetGeneratedDefaultValue(prop.Type, typename, prop.DefaultValue) : null;
        }

        public static string GetGeneratedPropertyDefaultValue(PropertyDef prop)
        {
            string propType = GetGeneratedNativeType(prop.Type);
            string defaultValue = GetGeneratedDefaultValue(prop.Type, propType, prop.DefaultValue);

            if (!string.IsNullOrEmpty(prop.DefaultValue) && Plugin.IsArrayType(prop.Type))
            {
                int index = prop.DefaultValue.IndexOf(":");
                if (index > 0)
                {
                    Type itemType = prop.Type.GetGenericArguments()[0];
                    string itemPropType = GetGeneratedNativeType(itemType);
                    if (!Plugin.IsArrayType(itemType) && !Plugin.IsCustomClassType(itemType))
                    {
                        string[] items = prop.DefaultValue.Substring(index + 1).Split('|');
                        for (int i = 0; i < items.Length; i++)
                        {
                            items[i] = GetGeneratedDefaultValue(itemType, itemPropType, items[i]);
                        }
                        defaultValue = string.Format("{0}{{{1}}}", propType, string.Join(", ", items));
                    }
                }
            }

            return defaultValue;
        }

        public static string GetPropertyBasicName(PropertyDef property, MethodDef.Param arrayIndexElement)
        {
            if (property != null)
            {
                string propName = property.BasicName;

                if (property.IsArrayElement && arrayIndexElement != null)
                {
                    propName = propName.Replace("[]", "");
                }

                return propName;
            }

            return "";
        }

        public static string GetPropertyNativeType(PropertyDef property, MethodDef.Param arrayIndexElement)
        {
            string nativeType = GetGeneratedNativeType(property.NativeType);

            return nativeType;
        }
    }
}
