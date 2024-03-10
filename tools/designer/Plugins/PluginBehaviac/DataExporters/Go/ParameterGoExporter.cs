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
    public class ParameterGoExporter
    {
        public static string GenerateCode(DefaultObject defaultObj, MethodDef.Param param, StringWriter stream, string indent, string typename, string var, string caller)
        {
            if (param.Value is ParInfo par)
            {
                return ParInfoGoExporter.GenerateCode(par, param.IsRef, stream, indent, typename, var, caller);
            }

            if (param.Value is VariableDef v)
            {
                return VariableGoExporter.GenerateCode(defaultObj, v, param.IsRef, stream, indent, typename, var, caller);
            }

            return DataGoExporter.GenerateCode(param.Value, defaultObj, stream, indent, typename, var, caller);
        }

        public static void PostGenerateCode(MethodDef.Param param, StringWriter stream, string indent, string typename, string var, string caller, object parent, string setValue)
        {
            if (param.Value is ParInfo par)
            {
                ParInfoGoExporter.PostGenerateCode(par, null, stream, indent, typename, var, caller);
                return;
            }

            if (param.Value is VariableDef v)
            {
                VariableGoExporter.PostGenerateCode(v, stream, indent, typename, var, caller, parent, param.Name, setValue);
                return;
            }

            Type type = param.Value.GetType();

            if (Plugin.IsCustomClassType(type) && !DesignerStruct.IsPureConstDatum(param.Value, parent, param.Name))
            {
                StructGoExporter.PostGenerateCode(param.Value, stream, indent, var, parent, param.Name);
            }
        }
    }
}
