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
using System.Reflection;
using Behaviac.Design.Attachments;
using Behaviac.Design;

namespace PluginBehaviac.NodeExporters
{
    public class AttachmentGoExporter : AttachmentExporter
    {
        public static AttachmentGoExporter CreateInstance(Attachment attachment)
        {
            if (attachment != null)
            {
                Type exporterType = getExporterType(attachment.GetType());

                if (exporterType != null)
                {
                    return (AttachmentGoExporter)Activator.CreateInstance(exporterType);
                }
            }

            return new AttachmentGoExporter();
        }

        private static Type getExporterType(Type attachmentType)
        {
            if (attachmentType != null)
            {
                while (attachmentType != typeof(Attachment))
                {
                    string attachmentExporter = "PluginBehaviac.NodeExporters." + attachmentType.Name + "GoExporter";
                    Type exporterType = Type.GetType(attachmentExporter);

                    if (exporterType != null)
                    {
                        return exporterType;
                    }

                    foreach (Assembly assembly in Plugin.GetLoadedPlugins())
                    {
                        string filename = Path.GetFileNameWithoutExtension(assembly.Location);
                        attachmentExporter = filename + ".NodeExporters." + attachmentType.Name + "GoExporter";
                        exporterType = assembly.GetType(attachmentExporter);

                        if (exporterType != null)
                        {
                            return exporterType;
                        }
                    }

                    attachmentType = attachmentType.BaseType;
                }
            }

            return null;
        }

        public override void GenerateClass(Attachment attachment, StringWriter stream, string indent, string nodeName, string btClassName)
        {
            if (ShouldGenerateClass())
            {
                string className = GetGeneratedClassName(attachment, btClassName, nodeName);

                stream.WriteLine("type {0} struct {{", className);
                GenerateMember(attachment, stream, indent);
                stream.WriteLine("}");
                stream.WriteLine();

                stream.WriteLine("func New{0}() *{0} {{", className);

                GenerateConstructor(attachment, stream, indent, className);

                stream.WriteLine("}");
                stream.WriteLine();

                GenerateMethod(attachment, stream, indent, className);
            }
        }

        public override void GenerateInstance(Attachment attachment, StringWriter stream, string indent, string nodeName, string agentType, string btClassName)
        {
            string className = GetGeneratedClassName(attachment, btClassName, nodeName);

            // create a new instance of the node
            stream.WriteLine("{0}\t{1} := New{2}()", indent, nodeName, className);
        }

        protected string GetGeneratedClassName(Attachment attachment, string btClassName, string nodeName)
        {
            string name;
            if (ShouldGenerateClass())
            {
                name = string.Format("{0}_{1}_{2}", attachment.ExportClass, btClassName, nodeName);
            }
            else
            {
                name = attachment.ExportClass;
            }
            return Utilities.ToPascalCase(name);
        }

        protected virtual bool ShouldGenerateClass()
        {
            return false;
        }

        protected virtual void GenerateConstructor(Attachment attachment, StringWriter stream, string indent, string className)
        {
        }

        protected virtual void GenerateMember(Attachment attachment, StringWriter stream, string indent)
        {
        }

        protected virtual void GenerateMethod(Attachment attachment, StringWriter stream, string indent, string className)
        {
        }
    }
}
