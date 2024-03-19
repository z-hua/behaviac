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
using System.IO;
using Behaviac.Design;
using Behaviac.Design.Nodes;
using Behaviac.Design.Attributes;
using PluginBehaviac.Properties;
using PluginBehaviac.NodeExporters;
using Behaviac.Design.Attachments;

namespace PluginBehaviac.Exporters
{
    public class ExporterGo: Behaviac.Design.Exporters.Exporter
    {
        public ExporterGo(BehaviorNode node, string outputFolder, string filename, List<string> includedFilenames = null)
            : base(node, outputFolder, filename + ".go", includedFilenames)
        {
            _outputFolder = Path.Combine(Path.GetFullPath(_outputFolder), "behaviac_generated");
        }

        public override Behaviac.Design.FileManagers.SaveResult Export(List<BehaviorNode> behaviors, bool exportBehaviors, bool exportMeta, int exportFileCount)
        {
            string behaviorFilename = "generated_behaviors.go";
            string typesFolder = string.Empty;
            Behaviac.Design.FileManagers.SaveResult result = VerifyFilename(exportBehaviors, ref behaviorFilename, ref typesFolder);

            if (Behaviac.Design.FileManagers.SaveResult.Succeeded == result)
            {
                if (exportBehaviors)
                {
                    ExportBehaviors(behaviors, behaviorFilename, exportFileCount);
                }

                if (exportMeta)
                {
                    ExportAgents(typesFolder);
                    ExportCustomizedTypes(typesFolder);
                }
            }

            return result;
        }

        public override void PreviewAgentFile(AgentType agent)
        {
            string behaviacTypesDir = GetBehaviacTypesDir();
            string tmpFilename = Path.Combine(behaviacTypesDir, Utilities.ToSnakeCase(agent.BasicName) + ".go");

            ExportAgentGoFile(agent, tmpFilename, true);

            PreviewFile(tmpFilename);
        }

        public override void PreviewEnumFile(EnumType enumType)
        {
            string behaviacTypesDir = GetBehaviacTypesDir();
            string tmpFilename = Path.Combine(behaviacTypesDir, enumType.Name + ".go");

            ExportEnumFile(null, enumType, tmpFilename);

            PreviewFile(tmpFilename);
        }

        public override void PreviewStructFile(StructType structType)
        {
            string behaviacTypesDir = GetBehaviacTypesDir();
            string tmpFilename = Path.Combine(behaviacTypesDir, structType.Name + ".go");

            ExportStructFile(null, null, structType, tmpFilename);

            PreviewFile(tmpFilename);
        }

        private void ExportBehaviors(List<BehaviorNode> behaviors, string filename, int exportFileCount)
        {
            using (StringWriter file = new StringWriter())
            {
                if (exportFileCount == 1)
                {
                    ExportHead(file, filename);

                    Dictionary<string, bool> imported = new Dictionary<string, bool>();

                    foreach (BehaviorNode behavior in behaviors)
                    {
                        ExportImport(file, behavior, imported);
                    }

                    foreach (BehaviorNode behavior in behaviors)
                    {
                        ExportBody(file, behavior);
                    }
                }
                else
                {
                    foreach (BehaviorNode behavior in behaviors)
                    {
                        string behaviorFilename = behavior.RelativePath;
                        behaviorFilename = behaviorFilename.Replace("\\", "/");
                        behaviorFilename = Path.ChangeExtension(behaviorFilename, "go");

                        string agentFolder = string.Empty;

                        Behaviac.Design.FileManagers.SaveResult result = VerifyFilename(true, ref behaviorFilename, ref agentFolder);

                        if (Behaviac.Design.FileManagers.SaveResult.Succeeded == result)
                        {
                            using (StringWriter behaviorFile = new StringWriter())
                            {
                                ExportHead(behaviorFile, behaviorFilename);

                                Dictionary<string, bool> imported = new Dictionary<string, bool>();
                                ExportImport(behaviorFile, behavior, imported);

                                ExportBody(behaviorFile, behavior);

                                UpdateFile(behaviorFile, behaviorFilename);
                            }
                        }
                    }
                }

                UpdateFile(file, filename);
            }
        }

        private Behaviac.Design.FileManagers.SaveResult VerifyFilename(bool exportBehaviors, ref string behaviorFilename, ref string agentFolder)
        {
            behaviorFilename = Path.Combine(_outputFolder, behaviorFilename);
            agentFolder = _outputFolder;

            // get the abolute folder of the file we want to export
            string folder = Path.GetDirectoryName(behaviorFilename);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (!Directory.Exists(agentFolder))
            {
                Directory.CreateDirectory(agentFolder);
            }

            if (exportBehaviors)
            {
                // verify it can be writable
                return Behaviac.Design.FileManagers.FileManager.MakeWritable(behaviorFilename, Resources.ExportFileWarning);
            }

            return Behaviac.Design.FileManagers.SaveResult.Succeeded;
        }

        private void ExportHead(StringWriter file, string exportFilename)
        {
            string wsfolder = Path.GetDirectoryName(Workspace.Current.FileName);
            exportFilename = Behaviac.Design.FileManagers.FileManager.MakeRelative(wsfolder, exportFilename);
            exportFilename = exportFilename.Replace("\\", "/");

            // write comments
            file.WriteLine("// ---------------------------------------------------------------------");
            file.WriteLine("// THIS FILE IS AUTO-GENERATED BY BEHAVIAC DESIGNER, SO PLEASE DON'T MODIFY IT BY YOURSELF!");
            file.WriteLine("// Export file: {0}", exportFilename);
            file.WriteLine("// ---------------------------------------------------------------------");
            file.WriteLine();

            // write package name
            file.WriteLine("package behaviac_generated");
            file.WriteLine();

            file.WriteLine("import \"gitee.com/z-hua/go_ai/bt\"");
        }

        private string getValidFilename(string filename)
        {
            filename = filename.Replace('/', '_');
            filename = filename.Replace('-', '_');

            return filename;
        }

        private void ExportImport(StringWriter file, BehaviorNode behavior, Dictionary<string, bool> imported)
        {
            // write imports
            CollectImports(file, (Node)behavior, imported);
            file.WriteLine();
        }

        private void ExportBody(StringWriter file, BehaviorNode behavior)
        {
            behavior.PreExport();

            string filename = Path.ChangeExtension(behavior.RelativePath, "").Replace(".", "");
            filename = filename.Replace('\\', '/');

            // write comments
            file.WriteLine("// Source file: {0}", filename);
            file.WriteLine();

            string btClassName = Utilities.ToPascalCase(getValidFilename(filename));
            string agentType = behavior.AgentType.Name;
            string treeName = Utilities.ToPascalCase(((Node)behavior).Label);

            // create the class definition of its attachments
            ExportAttachmentClass(file, btClassName, (Node)behavior);

            // create the class definition of its children
            foreach (Node child in ((Node)behavior).GetChildNodes())
            {
                ExportNodeClass(file, btClassName, agentType, behavior, child);
            }
            file.WriteLine();

            // create the bt class
            /*file.WriteLine("// {0} {1}", treeName, ((Node)behavior).CommentText);
            file.WriteLine("type {0} struct {{", treeName);
            file.WriteLine("}");
            file.WriteLine();*/

            // export the build function
            file.WriteLine("func New{0}() *bt.Tree {{", treeName);
            file.WriteLine("\tt := bt.NewTree(\"{0}\")", treeName);

            if (((Behavior)behavior).DescriptorRefs.Count > 0)
            {
                file.WriteLine("\t\t\tbt.SetDescriptors(\"{0}\");", DesignerPropertyUtility.RetrieveExportValue(((Behavior)behavior).DescriptorRefs));
            }

            ExportPars(file, agentType, "t", (Node)behavior, "");

            // export its attachments
            ExportAttachment(file, btClassName, agentType, "t", (Node)behavior, "\t\t\t");

            file.WriteLine("\t// children");

            // export its children
            if (((Node)behavior).IsFSM)
            {
                file.WriteLine("\t\t\t{");
                file.WriteLine("\t\t\t\tFSM fsm = new FSM();");
                file.WriteLine("\t\t\t\tfsm.SetClassNameString(\"FSM\");");
                file.WriteLine("\t\t\t\tfsm.SetId(-1);");
                file.WriteLine("\t\t\t\tfsm.InitialId = {0};", behavior.InitialStateId);
                file.WriteLine("#if !BEHAVIAC_RELEASE");
                file.WriteLine("\t\t\t\tfsm.SetAgentType(\"{0}\");", agentType.Replace("::", "."));
                file.WriteLine("#endif");

                foreach (Node child in ((Node)behavior).FSMNodes)
                {
                    ExportNode(file, btClassName, agentType, "fsm", child, 4);
                }

                file.WriteLine("\t\t\t\tbt.AddChild(fsm)");
                file.WriteLine("\t\t\t}");
            }
            else
            {
                foreach (Node child in ((Node)behavior).GetChildNodes())
                {
                    ExportNode(file, btClassName, agentType, "t", child, 1);
                }
            }

            file.WriteLine("\treturn t");

            // close the build function
            file.WriteLine("}");

            behavior.PostExport();
        }

        private void ExportPars(StringWriter file, string agentType, string nodeName, Node node, string indent)
        {
            if (node is Behavior behavior)
            {
                ExportPars(file, agentType, nodeName, behavior.LocalVars, indent);
            }
        }

        private void ExportPars(StringWriter file, string agentType, string nodeName, List<ParInfo> pars, string indent)
        {
            if (pars.Count > 0)
            {
                file.WriteLine("{0}\t// locals", indent);

                for (int i = 0; i < pars.Count; ++i)
                {
                    string name = pars[i].BasicName;
                    string value = GoExporter.GetGeneratedPropertyDefaultValue(pars[i]);

                    file.WriteLine("{0}\t{1}.AddLocal(\"{2}\", {3})", indent, nodeName, name, value);
                }
            }
        }

        private void ExportAttachmentClass(StringWriter file, string btClassName, Node node)
        {
            foreach (Attachment attach in node.Attachments)
            {
                if (!attach.Enable)
                {
                    continue;
                }

                string nodeName = Utilities.ToCamelCase(string.Format("attach{0}", attach.Id));

                AttachmentGoExporter attachmentExporter = AttachmentGoExporter.CreateInstance(attach);
                attachmentExporter.GenerateClass(attach, file, "", nodeName, btClassName);
            }
        }

        private void ExportAttachment(StringWriter file, string btClassName, string agentType, string parentName, Node node, string indent)
        {
            if (node.Attachments.Count > 0)
            {
                file.WriteLine("{0}// attachments", indent);

                foreach (Attachment attach in node.Attachments)
                {
                    if (!attach.Enable || attach.IsStartCondition)
                    {
                        continue;
                    }

                    file.WriteLine("{0}{{", indent);

                    string nodeName = Utilities.ToCamelCase(string.Format("attach{0}", attach.Id));

                    // export its instance and the properties
                    AttachmentGoExporter attachmentExporter = AttachmentGoExporter.CreateInstance(attach);
                    attachmentExporter.GenerateInstance(attach, file, indent, nodeName, agentType, btClassName);

                    if (attach is Event)
                    {
                        file.WriteLine("{0}\t{1}.AddEvent({2})", indent, parentName, nodeName);
                    }
                    else if (attach.IsPrecondition)
                    {
                        file.WriteLine("{0}\t{1}.AddPrecondition({2})", indent, parentName, nodeName);
                    }
                    else if (attach.IsEffector)
                    {
                        file.WriteLine("{0}\t{1}.AddEffector({2})", indent, parentName, nodeName);
                    }

                    file.WriteLine("}");
                }
            }
        }

        private void ExportNodeClass(StringWriter file, string btClassName, string agentType, BehaviorNode behavior, Node node)
        {
            if (!node.Enable)
            {
                return;
            }

            string nodeName = string.Format("node{0}", node.Id);

            NodeGoExporter nodeExporter = NodeGoExporter.CreateInstance(node);
            nodeExporter.GenerateClass(node, file, "", nodeName, agentType, btClassName);

            ExportAttachmentClass(file, btClassName, node);

            if (!(node is ReferencedBehavior))
            {
                foreach (Node child in node.GetChildNodes())
                {
                    ExportNodeClass(file, btClassName, agentType, behavior, child);
                }
            }
        }

        private void CollectImports(StringWriter file, Node node, Dictionary<string, bool> imported)
        {
            if (!node.Enable)
            {
                return;
            }

            NodeGoExporter nodeExporter = NodeGoExporter.CreateInstance(node);
            nodeExporter.CollectImport(file, imported);

            if (!(node is ReferencedBehavior))
            {
                foreach (Node child in node.GetChildNodes())
                {
                    CollectImports(file, child, imported);
                }
            }
        }

        private void ExportNode(StringWriter file, string btClassName, string agentType, string parentName, Node node, int indentDepth)
        {
            if (!node.Enable)
            {
                return;
            }

            // generate the indent string
            string indent = string.Empty;

            for (int i = 0; i < indentDepth; ++i)
            {
                indent += '\t';
            }

            string nodeName = string.Format("node{0}", node.Id);

            // open some brackets for a better formatting in the generated code
            file.WriteLine("{0}{{", indent);

            // export its instance and the properties
            NodeGoExporter nodeExporter = NodeGoExporter.CreateInstance(node);
            nodeExporter.GenerateInstance(node, file, indent, nodeName, agentType, btClassName);
            
            ExportPars(file, agentType, nodeName, node, indent);

            ExportAttachment(file, btClassName, agentType, nodeName, node, indent + "\t");

            bool isAsChild = true;

            if (node.Parent != null)
            {
                BaseNode.Connector connector = node.Parent.GetConnector(node);

                if (connector != null && !connector.IsAsChild)
                {
                    isAsChild = false;
                }
            }

            if (isAsChild)
            {
                // add the node to its parent
                file.WriteLine("{0}\t{1}.AddChild({2})", indent, parentName, nodeName);
            }
            else
            {
                // add the node as its customized children
                file.WriteLine("{0}\t{1}.SetCustomCondition({2})", indent, parentName, nodeName);
            }

            // export the child nodes
            if (!node.IsFSM && !(node is ReferencedBehavior))
            {
                foreach (Node child in node.GetChildNodes())
                {
                    ExportNode(file, btClassName, agentType, nodeName, child, indentDepth + 1);
                }
            }

            file.WriteLine("{0}}}", indent);
        }

        private void ExportAgents(string defaultAgentFolder)
        {
            foreach (AgentType agent in Plugin.AgentTypes)
            {
                if (agent.IsImplemented || agent.Name == "behaviac::Agent")
                {
                    continue;
                }

                //string agentFolder = string.IsNullOrEmpty(agent.ExportLocation) ? defaultAgentFolder : Workspace.Current.MakeAbsolutePath(agent.ExportLocation);
                string agentFolder = defaultAgentFolder;
                string filename = Path.Combine(agentFolder, Utilities.ToSnakeCase(agent.BasicName) + ".go");
                string oldFilename = "";

                if (!string.IsNullOrEmpty(agent.OldName) && agent.OldName != agent.Name)
                {
                    oldFilename = Path.Combine(agentFolder, agent.BasicOldName + ".go");
                }

                Debug.Check(filename != oldFilename);
                agent.OldName = null;

                try
                {
                    if (!Directory.Exists(agentFolder))
                    {
                        Directory.CreateDirectory(agentFolder);
                    }

                    if (!File.Exists(filename))
                    {
                        ExportAgentGoFile(agent, filename, false);

                        if (File.Exists(oldFilename))
                        {
                            MergeFiles(oldFilename, filename, filename);
                        }
                    }
                    else
                    {
                        string behaviacAgentDir = GetBehaviacTypesDir();
                        string newFilename = Path.GetFileName(filename);
                        newFilename = Path.ChangeExtension(newFilename, ".new.go");
                        newFilename = Path.Combine(behaviacAgentDir, newFilename);

                        ExportAgentGoFile(agent, newFilename, false);
                        Debug.Check(File.Exists(newFilename));

                        MergeFiles(filename, newFilename, filename);
                    }

                    if (File.Exists(oldFilename))
                    {
                        File.Delete(oldFilename);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Merge Go Files Error : {0} {1}", filename, e.Message);
                }
            }
        }

        private void ExportAgentGoFile(AgentType agent, string filename, bool preview)
        {
            using (StringWriter file = new StringWriter())
            {
                string indent = "";

                ExportFileWarningHeader(file);

                // 生成包名
                file.WriteLine("package behaviac_generated");
                file.WriteLine();

                if (!preview)
                {
                    ExportBeginComment(file, indent, file_init_part);
                    file.WriteLine();
                    ExportEndComment(file, indent);
                    file.WriteLine();
                }

                string structName = Utilities.ToPascalCase(agent.BasicName);

                // 生成结构体
                file.WriteLine("type {0} struct {{", structName);
                file.WriteLine("\tbt.Agent");

                IList<PropertyDef> properties = agent.GetProperties(true);

                int propNameWidth = 0;
                int propTypeWidth = 0;
                foreach (PropertyDef prop in properties)
                {
                    if ((preview || !agent.IsImplemented) && !prop.IsInherited && !prop.IsPar && !prop.IsArrayElement)
                    {
                        if (prop.BasicName.Length > propNameWidth)
                        {
                            propNameWidth = prop.BasicName.Length;
                        }

                        string propType = GoExporter.GetGeneratedNativeType(prop.Type);
                        if (propType.Length > propTypeWidth)
                        {
                            propTypeWidth = propType.Length;
                        }
                    }
                }

                foreach (PropertyDef prop in properties)
                {
                    if ((preview || !agent.IsImplemented) && !prop.IsInherited && !prop.IsPar && !prop.IsArrayElement)
                    {
                        string propType = GoExporter.GetGeneratedNativeType(prop.Type);
                        file.WriteLine(
                            "\t{0,-" + propNameWidth + "} {1,-" + propTypeWidth + "} // {2}", 
                            Utilities.ToPascalCase(prop.BasicName), propType, prop.BasicDescription
                        );
                    }
                }

                file.WriteLine("}");
                file.WriteLine();

                // 生成 new 函数
                file.WriteLine("func New{0}() *{0} {{", structName);
                file.WriteLine("\t_o := new({0})", structName);
                foreach (PropertyDef prop in properties)
                {
                    if ((preview || !agent.IsImplemented) && !prop.IsInherited && !prop.IsPar && !prop.IsArrayElement)
                    {
                        string defaultValue = GoExporter.GetGeneratedPropertyDefaultValue(prop);

                        if (defaultValue != null)
                        {
                            file.WriteLine("\t_o.{0} = {1}", Utilities.ToPascalCase(prop.BasicName), defaultValue);
                        }
                    }
                }
                file.WriteLine("\treturn _o");
                file.WriteLine("}");
                file.WriteLine();

                // 生成方法
                IList<MethodDef> methods = agent.GetMethods(true);

                foreach (MethodDef method in methods)
                {
                    if ((preview || !agent.IsImplemented) && !method.IsInherited && !method.IsNamedEvent)
                    {
                        string allParams = "";

                        foreach (MethodDef.Param param in method.Params)
                        {
                            if (!string.IsNullOrEmpty(allParams))
                            {
                                allParams += ", ";
                            }

                            string paramType = GoExporter.GetGeneratedNativeType(param.NativeType);

                            allParams += Utilities.ToCamelCase(param.Name) + " " + paramType;
                        }

                        string returnType = GoExporter.GetGeneratedNativeType(method.ReturnType);
                        string returnValue = GoExporter.GetGeneratedDefaultValue(method.ReturnType, returnType);

                        ExportMethodComment(file, "\t" + indent);

                        string methodName = Utilities.ToPascalCase(method.BasicName);

                        if (returnType == "void")
                        {
                            file.WriteLine("func (_o *{0}) {1}({2}) {{", structName, methodName, allParams);
                        }
                        else
                        {
                            file.WriteLine("func (_o *{0}) {1}({2}) {3} {{", structName, methodName, allParams, returnType);
                        }
                        

                        if (!preview)
                        {
                            ExportBeginComment(file, "\t\t" + indent, method.BasicName);
                        }

                        if (returnValue != null)
                        {
                            file.WriteLine("\treturn {0}", returnValue);
                        }

                        if (!preview)
                        {
                            ExportEndComment(file, "\t\t" + indent);
                        }

                        file.WriteLine("}");
                        file.WriteLine();
                    }
                }

                UpdateFile(file, filename);
            }
        }

        private void ExportCustomizedTypes(string agentFolder)
        {
            if (TypeManager.Instance.HasNonImplementedTypes())
            {
                using (StringWriter file = new StringWriter())
                {
                    file.WriteLine("// ---------------------------------------------------------------------");
                    file.WriteLine("// THIS FILE IS AUTO-GENERATED BY BEHAVIAC DESIGNER, SO PLEASE DON'T MODIFY IT BY YOURSELF!");
                    file.WriteLine("// ---------------------------------------------------------------------");
                    file.WriteLine();

                    if (TypeManager.Instance.HasNonImplementedEnums())
                    {
                        file.WriteLine("// -------------------");
                        file.WriteLine("// Customized enums");
                        file.WriteLine("// -------------------");

                        for (int e = 0; e < TypeManager.Instance.Enums.Count; ++e)
                        {
                            EnumType enumType = TypeManager.Instance.Enums[e];

                            if (enumType.IsImplemented)
                            {
                                continue;
                            }

                            ExportEnumFile(file, enumType, null);
                        }

                        file.WriteLine();
                    }

                    if (TypeManager.Instance.HasNonImplementedStructs())
                    {
                        file.WriteLine("// -------------------");
                        file.WriteLine("// Customized structs");
                        file.WriteLine("// -------------------");

                        Dictionary<string, bool> baseClasses = new Dictionary<string, bool>();

                        for (int s = 0; s < TypeManager.Instance.Structs.Count; s++)
                        {
                            StructType structType = TypeManager.Instance.Structs[s];

                            if (!string.IsNullOrEmpty(structType.BaseName))
                            {
                                baseClasses[structType.BaseName] = true;
                                baseClasses[structType.Name] = true;
                            }
                        }

                        for (int s = 0; s < TypeManager.Instance.Structs.Count; s++)
                        {
                            StructType structType = TypeManager.Instance.Structs[s];

                            if (structType.IsImplemented)
                            {
                                continue;
                            }

                            ExportStructFile(file, baseClasses, structType, null);
                        }
                    }

                    string filename = Path.Combine(agentFolder, "customized_types.go");

                    UpdateFile(file, filename);
                }
            }
        }

        private void ExportEnumFile(StringWriter file, EnumType enumType, string filename)
        {
            StringWriter enumfile = file;
            bool hasSetExportLocation = (!string.IsNullOrEmpty(filename) || !string.IsNullOrEmpty(enumType.ExportLocation));

            if (hasSetExportLocation)
            {
                enumfile = new StringWriter();

                enumfile.WriteLine("// ---------------------------------------------------------------------");
                enumfile.WriteLine("// THIS FILE IS AUTO-GENERATED BY BEHAVIAC DESIGNER, SO PLEASE DON'T MODIFY IT BY YOURSELF!");
                enumfile.WriteLine("// ---------------------------------------------------------------------");
                enumfile.WriteLine();

                enumfile.WriteLine("package types");
                enumfile.WriteLine();
            }

            Debug.Check(enumfile != null);

            if (enumfile != null)
            {
                enumfile.WriteLine();

                string enumName = Utilities.ToPascalCase(enumType.Name);

                enumfile.WriteLine("// {0}", enumType.Description);
                enumfile.WriteLine("type {0} int", enumName);
                enumfile.WriteLine("const (");

                int fieldWidth = 0;
                int valueWidth = 0;
                for (int m = 0; m < enumType.Members.Count; ++m)
                {
                    EnumType.EnumMemberType member = enumType.Members[m];
                    if (member.Name.Length > fieldWidth)
                    {
                        fieldWidth = member.Name.Length;
                    }
                    string valueStr = member.Value.ToString();
                    if (valueStr.Length > valueWidth)
                    {
                        valueWidth = valueStr.Length;
                    }
                }

                for (int m = 0; m < enumType.Members.Count; ++m)
                {
                    EnumType.EnumMemberType member = enumType.Members[m];

                    enumfile.WriteLine(
                        "\t{0,-" + fieldWidth + "} {1} = {2,-" + valueWidth + "} // {3}",
                        Utilities.ToPascalCase(member.Name), enumName, member.Value, member.DisplayName
                    );
                }

                enumfile.WriteLine(")");

                if (hasSetExportLocation)
                {
                    string enumFilename = filename;

                    if (string.IsNullOrEmpty(enumFilename))
                    {
                        string enumLocation = Workspace.Current.MakeAbsolutePath(enumType.ExportLocation);

                        if (!enumLocation.StartsWith(Workspace.Current.DefaultExportFolder))
                        {
                            throw new Exception(string.Format("The export location of {0} must under {1}", enumLocation, Workspace.Current.DefaultExportFolder));
                        }

                        if (!Directory.Exists(enumLocation))
                        {
                            Directory.CreateDirectory(enumLocation);
                        }

                        enumFilename = Path.Combine(enumLocation, enumType.Name + ".go");
                    }

                    UpdateFile(enumfile, enumFilename);
                }
            }
        }

        private void ExportStructFile(StringWriter file, Dictionary<string, bool> baseClasses, StructType structType, string filename)
        {
            StringWriter structfile = file;
            bool hasSetExportLocation = (!string.IsNullOrEmpty(filename) || !string.IsNullOrEmpty(structType.ExportLocation));

            if (hasSetExportLocation)
            {
                structfile = new StringWriter();

                structfile.WriteLine("// ---------------------------------------------------------------------");
                structfile.WriteLine("// THIS FILE IS AUTO-GENERATED BY BEHAVIAC DESIGNER, SO PLEASE DON'T MODIFY IT BY YOURSELF!");
                structfile.WriteLine("// ---------------------------------------------------------------------");
                structfile.WriteLine("package types");
                structfile.WriteLine();
            }
            structfile.WriteLine();

            string structName = Utilities.ToPascalCase(structType.Name);

            structfile.WriteLine("// {0}", structType.Description);
            structfile.WriteLine("type {0} struct {{", structName);

            if (!string.IsNullOrEmpty(structType.BaseName))
            {
                structfile.WriteLine("\t{0}", structType.BaseName);
            }

            int nameWidth = 0;
            int typeWidth = 0;
            for (int m = 0; m < structType.Properties.Count; ++m)
            {
                PropertyDef member = structType.Properties[m];
                if (member.BasicName.Length > nameWidth)
                {
                    nameWidth = member.BasicName.Length;
                }
                string typeStr = GoExporter.GetGeneratedNativeType(member.NativeType);
                if (typeStr.Length > typeWidth)
                {
                    typeWidth = typeStr.Length;
                }
            }
            for (int m = 0; m < structType.Properties.Count; ++m)
            {
                PropertyDef member = structType.Properties[m];
                if (!member.IsInherited)
                {
                    structfile.WriteLine(
                        "\t{0,-" + nameWidth + "} {1,-" + typeWidth + "} // {2}", 
                        Utilities.ToPascalCase(member.BasicName), GoExporter.GetGeneratedNativeType(member.NativeType), member.BasicDescription
                    );
                }
            }

            structfile.WriteLine("}");

            if (hasSetExportLocation)
            {
                string structFilename = filename;

                if (string.IsNullOrEmpty(structFilename))
                {
                    string structLocation = Workspace.Current.MakeAbsolutePath(structType.ExportLocation);

                    if (!Directory.Exists(structLocation))
                    {
                        Directory.CreateDirectory(structLocation);
                    }

                    structFilename = Path.Combine(structLocation, structType.Name + ".go");
                }

                UpdateFile(structfile, structFilename);
            }
        }
    }
}
