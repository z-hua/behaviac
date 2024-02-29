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
using PluginBehaviac.DataExporters;
using PluginBehaviac.NodeExporters;

namespace PluginBehaviac.Exporters
{
    public class ExporterGo: Behaviac.Design.Exporters.Exporter
    {
        private string packageNameOfTypes = "behavior_types";
        private string packageNameOfTrees = "behavior_trees";

        public ExporterGo(BehaviorNode node, string outputFolder, string filename, List<string> includedFilenames = null)
            : base(node, outputFolder, filename + ".go", includedFilenames)
        {
            _outputFolder = Path.Combine(Path.GetFullPath(_outputFolder), "behaviac_generated");
        }

        public override Behaviac.Design.FileManagers.SaveResult Export(List<BehaviorNode> behaviors, bool exportBehaviors, bool exportMeta, int exportFileCount)
        {
            string behaviorFilename = "behaviors/generated_behaviors.go";
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
            string tmpFilename = Path.Combine(behaviacTypesDir, agent.BasicName + ".cs");

            ExportAgentGoFile(agent, tmpFilename, true);

            PreviewFile(tmpFilename);
        }

        public override void PreviewEnumFile(EnumType enumType)
        {
            string behaviacTypesDir = GetBehaviacTypesDir();
            string tmpFilename = Path.Combine(behaviacTypesDir, enumType.Name + ".cs");

            ExportEnumFile(null, enumType, tmpFilename);

            PreviewFile(tmpFilename);
        }

        public override void PreviewStructFile(StructType structType)
        {
            string behaviacTypesDir = GetBehaviacTypesDir();
            string tmpFilename = Path.Combine(behaviacTypesDir, structType.Name + ".cs");

            ExportStructFile(null, null, structType, tmpFilename);

            PreviewFile(tmpFilename);
        }

        private void ExportBehaviors(List<BehaviorNode> behaviors, string filename, int exportFileCount)
        {
            using (StringWriter file = new StringWriter())
            {
                ExportHead(file, filename);

                if (exportFileCount == 1)
                {
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
                        behaviorFilename = Path.Combine("behaviors", behaviorFilename);

                        string agentFolder = string.Empty;

                        Behaviac.Design.FileManagers.SaveResult result = VerifyFilename(true, ref behaviorFilename, ref agentFolder);

                        if (Behaviac.Design.FileManagers.SaveResult.Succeeded == result)
                        {
                            using (StringWriter behaviorFile = new StringWriter())
                            {
                                ExportHead(behaviorFile, behaviorFilename);

                                ExportBody(behaviorFile, behavior);

                                ExportTail(behaviorFile);

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
            agentFolder = Path.Combine(_outputFolder, "types");

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
            file.WriteLine("package {0}", packageNameOfTrees);
        }

        private string getValidFilename(string filename)
        {
            filename = filename.Replace('/', '_');
            filename = filename.Replace('-', '_');

            return filename;
        }

        private void ExportBody(StringWriter file, BehaviorNode behavior)
        {
            behavior.PreExport();

            string filename = Path.ChangeExtension(behavior.RelativePath, "").Replace(".", "");
            filename = filename.Replace('\\', '/');

            file.WriteLine();
            // write comments
            file.WriteLine("// Source file: {0}\r\n", filename);

            string btClassName = getValidFilename(filename);
            string agentType = behavior.AgentType.Name;

            // create the bt class
            file.WriteLine("// {0}", ((Node)behavior).CommentText);
            file.WriteLine("type {0} struct {{", ((Node)behavior).Label);
            file.WriteLine("}");
            file.WriteLine();

            // create the class definition of its attachments
            ExportAttachmentClass(file, btClassName, (Node)behavior);

            // create the class definition of its children
            foreach (Node child in ((Node)behavior).GetChildNodes())
            {
                ExportNodeClass(file, btClassName, agentType, behavior, child);
            }

            // export the build function
            file.WriteLine("func (_o *{0}) NewTree() *bt.Tree {{", ((Node)behavior).Label);
            file.WriteLine("\ttree := bt.NewTree(\"{0}\")", ((Node)behavior).Label);

            if (((Behavior)behavior).DescriptorRefs.Count > 0)
            {
                file.WriteLine("\t\t\tbt.SetDescriptors(\"{0}\");", DesignerPropertyUtility.RetrieveExportValue(((Behavior)behavior).DescriptorRefs));
            }

            ExportPars(file, agentType, "tree", (Node)behavior, "\t\t");

            // export its attachments
            ExportAttachment(file, btClassName, agentType, "tree", (Node)behavior, "\t\t\t");

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

                file.WriteLine("\t\t\t\tbt.AddChild(fsm);");
                file.WriteLine("\t\t\t}");
            }
            else
            {
                foreach (Node child in ((Node)behavior).GetChildNodes())
                {
                    ExportNode(file, btClassName, agentType, "tree", child, 1);
                }
            }

            file.WriteLine("\treturn tree");

            // close the build function
            file.WriteLine("}");

            behavior.PostExport();
        }

        private void ExportTail(StringWriter file)
        {
            // close namespace
            file.WriteLine("}");
        }

        private void ExportPars(StringWriter file, string agentType, string nodeName, Node node, string indent)
        {
            if (node is Behavior)
            {
                ExportPars(file, agentType, nodeName, ((Behavior)node).LocalVars, indent);
            }
        }

        private void ExportPars(StringWriter file, string agentType, string nodeName, List<Behaviac.Design.ParInfo> pars, string indent)
        {
            if (pars.Count > 0)
            {
                file.WriteLine("{0}\t// locals", indent);

                for (int i = 0; i < pars.Count; ++i)
                {
                    string name = pars[i].BasicName;
                    string type = DataCsExporter.GetGeneratedParType(pars[i].Type);
                    string value = pars[i].DefaultValue.Replace("\"", "\\\"");

                    file.WriteLine("{0}\t{1}.AddLocal(\"{2}\", \"{3}\", \"{4}\", \"{5}\");", indent, nodeName, agentType, type, name, value);
                }
            }
        }

        private void ExportAttachmentClass(StringWriter file, string btClassName, Node node)
        {
            foreach (Behaviac.Design.Attachments.Attachment attach in node.Attachments)
            {
                if (!attach.Enable)
                {
                    continue;
                }

                string nodeName = string.Format("attach{0}", attach.Id);

                AttachmentCsExporter attachmentExporter = AttachmentCsExporter.CreateInstance(attach);
                attachmentExporter.GenerateClass(attach, file, "", nodeName, btClassName);
            }
        }

        private void ExportAttachment(StringWriter file, string btClassName, string agentType, string parentName, Node node, string indent)
        {
            if (node.Attachments.Count > 0)
            {
                file.WriteLine("{0}// attachments", indent);

                foreach (Behaviac.Design.Attachments.Attachment attach in node.Attachments)
                {
                    if (!attach.Enable || attach.IsStartCondition)
                    {
                        continue;
                    }

                    file.WriteLine("{0}{{", indent);

                    string nodeName = string.Format("attach{0}", attach.Id);

                    // export its instance and the properties
                    AttachmentCsExporter attachmentExporter = AttachmentCsExporter.CreateInstance(attach);
                    attachmentExporter.GenerateInstance(attach, file, indent, nodeName, agentType, btClassName);

                    string isPrecondition = attach.IsPrecondition && !attach.IsTransition ? "true" : "false";
                    string isEffector = attach.IsEffector && !attach.IsTransition ? "true" : "false";
                    string isTransition = attach.IsTransition ? "true" : "false";
                    file.WriteLine("{0}\t{1}.Attach({2}, {3}, {4}, {5});", indent, parentName, nodeName, isPrecondition, isEffector, isTransition);

                    if (attach is Behaviac.Design.Attachments.Event)
                    {
                        file.WriteLine("{0}\t{1}.SetHasEvents({1}.HasEvents() | ({2} is Event));", indent, parentName, nodeName);
                    }

                    file.WriteLine("{0}}}", indent);
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
                file.WriteLine("{0}\t{1}.AddChild({2});", indent, parentName, nodeName);
            }
            else
            {
                // add the node as its customized children
                file.WriteLine("{0}\t{1}.SetCustomCondition({2});", indent, parentName, nodeName);
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

                string agentFolder = string.IsNullOrEmpty(agent.ExportLocation) ? defaultAgentFolder : Workspace.Current.MakeAbsolutePath(agent.ExportLocation);
                string filename = Path.Combine(agentFolder, agent.BasicName + ".go");
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
                        newFilename = Path.ChangeExtension(newFilename, ".new.cs");
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

                // ���ɰ���
                string pkgName = packageNameOfTypes;
                if (string.IsNullOrEmpty(agent.Namespace))
                {
                    pkgName = agent.Namespace;
                }
                file.WriteLine("package {0}", pkgName);
                file.WriteLine();

                // ���ɽṹ��
                file.WriteLine("type {0} struct {{", agent.BasicName);
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

                        string propType = DataGoExporter.GetGeneratedNativeType(prop.Type);
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
                        string propType = DataGoExporter.GetGeneratedNativeType(prop.Type);
                        file.WriteLine("\t{0,-" + propNameWidth + "} {1,-" + propTypeWidth + "} // {2}", prop.BasicName, propType, prop.BasicDescription);
                    }
                }

                file.WriteLine("}");
                file.WriteLine();

                // ���� new ����
                file.WriteLine("func New{0}() *{0} {{", agent.BasicName);
                file.WriteLine("\t_o := new({0})", agent.BasicName);
                foreach (PropertyDef prop in properties)
                {
                    if ((preview || !agent.IsImplemented) && !prop.IsInherited && !prop.IsPar && !prop.IsArrayElement)
                    {
                        string defaultValue = DataGoExporter.GetGeneratedPropertyDefaultValue(prop);

                        if (defaultValue != null)
                        {
                            file.WriteLine("\t_o.{0} = {1}", prop.BasicName, defaultValue);
                        }
                    }
                }
                file.WriteLine("\treturn _o");
                file.WriteLine("}");
                file.WriteLine();

                // ���ɷ���
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

                            string paramType = DataGoExporter.GetGeneratedNativeType(param.NativeType);

                            allParams += param.Name + " " + paramType;
                        }

                        string returnType = DataGoExporter.GetGeneratedNativeType(method.ReturnType);
                        string returnValue = DataGoExporter.GetGeneratedDefaultValue(method.ReturnType, returnType);

                        ExportMethodComment(file, "\t" + indent);

                        if (returnType == "void")
                        {
                            file.WriteLine("func (_o *{0}) {1}({2}) {{", agent.BasicName, method.BasicName, allParams);
                        }
                        else
                        {
                            file.WriteLine("func (_o *{0}) {1}({2}) {3} {{", agent.BasicName, method.BasicName, allParams, returnType);
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

                string pkgName = packageNameOfTypes;
                if (!string.IsNullOrEmpty(enumType.Namespace))
                {
                    pkgName = enumType.Namespace.Replace("::", ".");
                }
                enumfile.WriteLine("package {0}", pkgName);
                enumfile.WriteLine();
            }

            Debug.Check(enumfile != null);

            if (enumfile != null)
            {
                enumfile.WriteLine();

                enumfile.WriteLine("// {0}", enumType.Description);
                enumfile.WriteLine("type {0} int", enumType.Name);
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

                    enumfile.WriteLine("\t{0,-" + fieldWidth + "} {1} = {2,-" + valueWidth + "} // {3}", member.Name, enumType.Name, member.Value, member.DisplayName);
                }

                enumfile.WriteLine(")");

                if (hasSetExportLocation)
                {
                    string enumFilename = filename;

                    if (string.IsNullOrEmpty(enumFilename))
                    {
                        string enumLocation = Workspace.Current.MakeAbsolutePath(enumType.ExportLocation);

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
                string pkgName = packageNameOfTypes;
                if (!string.IsNullOrEmpty(structType.Namespace))
                {
                    pkgName = structType.Namespace.Replace("::", ".");
                }
                structfile.WriteLine("package {0}", pkgName);
                structfile.WriteLine();
            }

            structfile.WriteLine();

            structfile.WriteLine("// {0}", structType.Description);
            structfile.WriteLine("type {0} struct {{", structType.Name);

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
                string typeStr = DataGoExporter.GetGeneratedNativeType(member.NativeType);
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
                    structfile.WriteLine("\t{0,-" + nameWidth + "} {1,-" + typeWidth + "} // {2}", member.BasicName, DataGoExporter.GetGeneratedNativeType(member.NativeType), member.BasicDescription);
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

                    structFilename = Path.Combine(structLocation, structType.Name + ".cs");
                }

                UpdateFile(structfile, structFilename);
            }
        }

        private bool IsStructType(MethodDef.Param param)
        {
            string paramType = DataCsExporter.GetGeneratedNativeType(param.NativeType);
            bool isStruct = Plugin.IsCustomClassType(param.Type);

            if (isStruct)
            {
                StructType structType = TypeManager.Instance.FindStruct(paramType);
                if (structType == null || structType.IsRef || structType.Properties.Count == 0)
                {
                    isStruct = false;
                }
            }

            return isStruct;
        }

        private void PreExportMeta(StringWriter file)
        {
            // all structs
            Dictionary<string, bool> allStructs = new Dictionary<string, bool>();

            foreach (StructType structType in TypeManager.Instance.Structs)
            {
                if (structType.IsRef || structType.Properties.Count == 0)
                {
                    continue;
                }

                string structTypeName = structType.Fullname.Replace("::", "_");
                structTypeName = structTypeName.Replace(".", "_");

                // class
                file.WriteLine("\t\tclass CInstanceConst_{0} : CInstanceConst<{1}>", structTypeName, structType.Fullname.Replace("::", "."));
                file.WriteLine("\t\t{");

                foreach (PropertyDef prop in structType.Properties)
                {
                    if (!prop.IsReadonly)
                    {
                        file.WriteLine("\t\t\tIInstanceMember _{0};", prop.BasicName);
                    }
                }

                file.WriteLine();

                // Constructors
                file.WriteLine("\t\t\tpublic CInstanceConst_{0}(string typeName, string valueStr) : base(typeName, valueStr)", structTypeName);
                file.WriteLine("\t\t\t{");

                file.WriteLine("\t\t\t\tList<string> paramStrs = behaviac.StringUtils.SplitTokensForStruct(valueStr);");
                
                int validPropCount = 0;
                foreach (PropertyDef prop in structType.Properties)
                {
                    if (!prop.IsReadonly)
                    {
                        validPropCount++;
                    }
                }

                file.WriteLine("\t\t\t\tDebug.Check(paramStrs != null && paramStrs.Count == {0});", validPropCount);
                file.WriteLine();

                validPropCount = 0;
                foreach (PropertyDef prop in structType.Properties)
                {
                    if (!prop.IsReadonly)
                    {
                        string propType = DataCsExporter.GetGeneratedNativeType(prop.NativeType);

                        file.WriteLine("\t\t\t\t_{0} = (CInstanceMember<{1}>)AgentMeta.ParseProperty<{1}>(paramStrs[{2}]);", prop.BasicName, propType, validPropCount);

                        validPropCount++;
                    }
                }

                file.WriteLine("\t\t\t}");
                file.WriteLine();

                // Run()
                file.WriteLine("\t\t\tpublic override void Run(Agent self)");
                file.WriteLine("\t\t\t{");

                if (structType.Properties.Count > 0)
                {
                    foreach (PropertyDef prop in structType.Properties)
                    {
                        if (!prop.IsReadonly)
                        {
                            file.WriteLine("\t\t\t\tDebug.Check(_{0} != null);", prop.BasicName);
                        }
                    }

                    file.WriteLine();
                }

                foreach (PropertyDef prop in structType.Properties)
                {
                    if (!prop.IsReadonly)
                    {
                        string propType = DataCsExporter.GetGeneratedNativeType(prop.NativeType);

                        if (Plugin.IsRefType(prop.Type))
                        {
                            file.WriteLine("\t\t\t\t_value.{0} = ({1})_{0}.GetValueObject(self);", prop.BasicName, propType);
                        }
                        else
                        {
                            file.WriteLine("\t\t\t\t_value.{0} = ((CInstanceMember<{1}>)_{0}).GetValue(self);", prop.BasicName, propType);
                        }
                    }
                }

                file.WriteLine("\t\t\t}"); // Run()

                file.WriteLine("\t\t};"); // end of class
                file.WriteLine();
            }

            // all methods
            Dictionary<string, bool> allMethods = new Dictionary<string, bool>();

            foreach (AgentType agent in Plugin.AgentTypes)
            {
                string agentTypeName = agent.Name.Replace("::", ".");

                IList<MethodDef> methods = agent.GetMethods(true);

                foreach (MethodDef method in methods)
                {
                    if (!method.IsNamedEvent)
                    {
                        bool hasRefParam = false;

                        foreach (MethodDef.Param param in method.Params)
                        {
                            if (param.IsRef || param.IsOut || Plugin.IsRefType(param.Type) || IsStructType(param))
                            {
                                hasRefParam = true;
                                break;
                            }
                        }

                        if (hasRefParam)
                        {
                            string methodFullname = method.Name.Replace("::", "_");

                            if (allMethods.ContainsKey(methodFullname))
                            {
                                continue;
                            }
                            else
                            {
                                allMethods[methodFullname] = true;
                            }

                            string methodReturnType = DataCsExporter.GetGeneratedNativeType(method.NativeReturnType);
                            string baseClass = (methodReturnType == "void") ? "CAgentMethodVoidBase" : string.Format("CAgentMethodBase<{0}>", methodReturnType);

                            // class
                            file.WriteLine("\t\tprivate class CMethod_{0} : {1}", methodFullname, baseClass);
                            file.WriteLine("\t\t{");

                            foreach (MethodDef.Param param in method.Params)
                            {
                                if (Plugin.IsRefType(param.Type))
                                {
                                    file.WriteLine("\t\t\tIInstanceMember _{0};", param.Name);
                                }
                                else
                                {
                                    string paramType = DataCsExporter.GetGeneratedNativeType(param.NativeType);
                                    file.WriteLine("\t\t\tCInstanceMember<{0}> _{1};", paramType, param.Name);
                                }
                            }

                            if (method.Params.Count > 0)
                            {
                                file.WriteLine();
                            }

                            // Constructors
                            file.WriteLine("\t\t\tpublic CMethod_{0}()", methodFullname);
                            file.WriteLine("\t\t\t{");
                            file.WriteLine("\t\t\t}");
                            file.WriteLine();

                            file.WriteLine("\t\t\tpublic CMethod_{0}(CMethod_{0} rhs) : base(rhs)", methodFullname);
                            file.WriteLine("\t\t\t{");
                            file.WriteLine("\t\t\t}");
                            file.WriteLine();

                            // Clone()
                            file.WriteLine("\t\t\tpublic override IMethod Clone()");
                            file.WriteLine("\t\t\t{");
                            file.WriteLine("\t\t\t\treturn new CMethod_{0}(this);", methodFullname);
                            file.WriteLine("\t\t\t}"); // Clone()
                            file.WriteLine();

                            // Load()
                            file.WriteLine("\t\t\tpublic override void Load(string instance, string[] paramStrs)");
                            file.WriteLine("\t\t\t{");

                            file.WriteLine("\t\t\t\tDebug.Check(paramStrs.Length == {0});", method.Params.Count);
                            file.WriteLine();
                            file.WriteLine("\t\t\t\t_instance = instance;");

                            for (int i = 0; i < method.Params.Count; ++i)
                            {
                                MethodDef.Param param = method.Params[i];
                                string paramType = DataCsExporter.GetGeneratedNativeType(param.NativeType);

                                if (IsStructType(param))
                                {
                                    file.WriteLine("\t\t\t\tif (paramStrs[{0}].StartsWith(\"{{\"))", i);
                                    file.WriteLine("\t\t\t\t{");
                                    file.WriteLine("\t\t\t\t\t_{0} = new CInstanceConst_{1}(\"{2}\", paramStrs[{3}]);", param.Name, paramType.Replace(".", "_"), paramType, i);
                                    file.WriteLine("\t\t\t\t}");
                                    file.WriteLine("\t\t\t\telse");
                                    file.WriteLine("\t\t\t\t{");
                                    file.WriteLine("\t\t\t\t\t_{0} = (CInstanceMember<{1}>)AgentMeta.ParseProperty<{1}>(paramStrs[{2}]);", param.Name, paramType, i);
                                    file.WriteLine("\t\t\t\t}");
                                }
                                else
                                {
                                    if (Plugin.IsRefType(param.Type))
                                    {
                                        file.WriteLine("\t\t\t\t_{0} = AgentMeta.ParseProperty<{1}>(paramStrs[{2}]);", param.Name, paramType, i);
                                    }
                                    else
                                    {
                                        file.WriteLine("\t\t\t\t_{0} = (CInstanceMember<{1}>)AgentMeta.ParseProperty<{1}>(paramStrs[{2}]);", param.Name, paramType, i);
                                    }
                                }
                            }

                            file.WriteLine("\t\t\t}"); // Load()
                            file.WriteLine();

                            // Run()
                            file.WriteLine("\t\t\tpublic override void Run(Agent self)");
                            file.WriteLine("\t\t\t{");

                            if (method.Params.Count > 0)
                            {
                                foreach (MethodDef.Param param in method.Params)
                                {
                                    file.WriteLine("\t\t\t\tDebug.Check(_{0} != null);", param.Name);
                                }

                                file.WriteLine();
                            }

                            string paramValues = "";

                            foreach (MethodDef.Param param in method.Params)
                            {
                                if (IsStructType(param))
                                {
                                    file.WriteLine("\t\t\t\t_{0}.Run(self);", param.Name);
                                }

                                if (!string.IsNullOrEmpty(paramValues))
                                {
                                    paramValues += ", ";
                                }

                                string paramType = DataCsExporter.GetGeneratedNativeType(param.NativeType);
                                string paramName = string.Format("((CInstanceMember<{0}>)_{1}).GetValue(self)", paramType, param.Name);

                                if (Plugin.IsRefType(param.Type))
                                {
                                    paramName = string.Format("({0})_{1}.GetValueObject(self)", paramType, param.Name);
                                }

                                if (param.IsRef || param.IsOut)
                                {
                                    file.WriteLine("\t\t\t\t{0} {1} = {2};", paramType, param.Name, paramName);

                                    if (method.IsPublic)
                                    {
                                        paramValues += param.IsRef ? "ref " : "out ";
                                    }

                                    paramValues += param.Name;
                                }
                                else
                                {
                                    paramValues += paramName;
                                }
                            }

                            if (!method.IsStatic)
                            {
                                file.WriteLine("\t\t\t\tAgent agent = Utils.GetParentAgent(self, _instance);");
                                file.WriteLine();
                            }

                            string instanceName = method.IsStatic ? agentTypeName : string.Format("(({0})agent)", agentTypeName);

                            if (methodReturnType == "void")
                            {
                                if (method.IsPublic)
                                {
                                    file.WriteLine("\t\t\t\t{0}.{1}({2});", instanceName, method.BasicName, paramValues);
                                }
                                else
                                {
                                    file.WriteLine("\t\t\t\tobject[] paramArray = new object[] {{ {0} }};", paramValues);
                                    file.WriteLine("\t\t\t\tAgentMetaVisitor.ExecuteMethod({0}, \"{1}\", paramArray);", instanceName, method.BasicName);
                                }
                            }
                            else
                            {
                                if (method.IsPublic)
                                {
                                    file.WriteLine("\t\t\t\t_returnValue.value = {0}.{1}({2});", instanceName, method.BasicName, paramValues);
                                }
                                else
                                {
                                    file.WriteLine("\t\t\t\tobject[] paramArray = new object[] {{ {0} }};", paramValues);
                                    file.WriteLine("\t\t\t\t_returnValue.value = ({0})AgentMetaVisitor.ExecuteMethod({1}, \"{2}\", paramArray);", methodReturnType, instanceName, method.BasicName);
                                }
                            }

                            for (int i = 0; i < method.Params.Count; ++i)
                            {
                                MethodDef.Param param = method.Params[i];

                                if (param.IsRef || param.IsOut)
                                {
                                    if (method.IsPublic)
                                    {
                                        file.WriteLine("\t\t\t\t_{0}.SetValue(self, {0});", param.Name);
                                    }
                                    else
                                    {
                                        string paramType = DataCsExporter.GetGeneratedNativeType(param.NativeType);
                                        file.WriteLine("\t\t\t\t_{0}.SetValue(self, ({1})paramArray[{2}]);", param.Name, paramType, i);
                                    }
                                }
                            }

                            file.WriteLine("\t\t\t}"); // Run()

                            if (method.Params.Count == 1 && methodReturnType == "behaviac.EBTStatus")
                            {
                                // GetIValue()
                                string paramType = DataCsExporter.GetGeneratedNativeType(method.Params[0].NativeType);

                                file.WriteLine();
                                file.WriteLine("\t\t\tpublic override IValue GetIValue(Agent self, IInstanceMember firstParam)");
                                file.WriteLine("\t\t\t{");
                                file.WriteLine("\t\t\t\tAgent agent = Utils.GetParentAgent(self, _instance);");
                                file.WriteLine();
                                file.WriteLine("\t\t\t\t{0} result = ((CInstanceMember<{0}>)firstParam).GetValue(self);", paramType);

                                MethodDef.Param param = method.Params[0];

                                if (method.IsPublic)
                                {
                                    string refStr = "";

                                    if (param.IsRef || param.IsOut)
                                    {
                                        refStr = param.IsRef ? "ref " : "out ";
                                    }

                                    file.WriteLine("\t\t\t\t_returnValue.value = {0}.{1}({2}result);", instanceName, method.BasicName, refStr);
                                }
                                else
                                {
                                    file.WriteLine("\t\t\t\tobject[] paramArray = new object[] { result };");
                                    file.WriteLine("\t\t\t\t_returnValue.value = ({0})AgentMetaVisitor.ExecuteMethod({1}, \"{2}\", paramArray);", methodReturnType, instanceName, method.BasicName);
                                }

                                if (param.IsRef || param.IsOut)
                                {
                                    if (method.IsPublic)
                                    {
                                        file.WriteLine("\t\t\t\tfirstParam.SetValue(self, result);");
                                    }
                                    else
                                    {
                                        string paramNativeType = DataCsExporter.GetGeneratedNativeType(param.NativeType);
                                        file.WriteLine("\t\t\t\tfirstParam.SetValue(self, ({0})paramArray[0]);", paramNativeType);
                                    }
                                }

                                file.WriteLine("\t\t\t\treturn _returnValue;");
                                file.WriteLine("\t\t\t}");
                            }

                            file.WriteLine("\t\t}"); // class
                            file.WriteLine();
                        }
                    }
                }
            }
        }

        private void ExportMeta(StringWriter file)
        {
            file.WriteLine("\t\t\tAgentMeta.TotalSignature = {0};", CRC32.CalcCRC(Plugin.Signature));
            file.WriteLine();
            file.WriteLine("\t\t\tAgentMeta meta;");

            foreach (AgentType agent in Plugin.AgentTypes)
            {
                string agentTypeName = agent.Name.Replace("::", ".");
                string signature = agent.GetSignature(true);

                file.WriteLine("\n\t\t\t// {0}", agentTypeName);
                file.WriteLine("\t\t\tmeta = new AgentMeta({0});", CRC32.CalcCRC(signature));
                file.WriteLine("\t\t\tAgentMeta._AgentMetas_[{0}] = meta;", CRC32.CalcCRC(agentTypeName));

                IList<PropertyDef> properties = agent.GetProperties(true);

                foreach (PropertyDef prop in properties)
                {
                    if (!prop.IsPar)
                    {
                        string bindingProperty = "";
                        string registerName = "RegisterMemberProperty";
                        string propType = DataCsExporter.GetGeneratedNativeType(prop.NativeItemType);

                        string propItemName = prop.BasicName;

                        if (prop.IsArrayElement)
                        {
                            propItemName = propItemName.Replace("[]", "[index]");
                        }

                        string instanceName = string.Format("(({0})self)", agentTypeName);

                        if (agent.IsStatic || prop.IsStatic)
                        {
                            instanceName = agentTypeName;
                        }

                        string setValue = "";
                        string getValue = "";

                        if (prop.IsPublic)
                        {
                            setValue = string.Format("{0}.{1} = value;", instanceName, propItemName);
                            getValue = string.Format("{0}.{1}", instanceName, propItemName);
                        }
                        else
                        {
                            if (prop.IsArrayElement)
                            {
                                setValue = string.Format("{0}._get_{1}()[index] = value;", instanceName, prop.BasicName.Replace("[]", ""));
                                getValue = string.Format("{0}._get_{1}()[index]", instanceName, prop.BasicName.Replace("[]", ""));
                            }
                            else
                            {
                                setValue = string.Format("{0}._set_{1}(value);", instanceName, propItemName);
                                getValue = string.Format("{0}._get_{1}()", instanceName, propItemName);
                            }
                        }

                        if (prop.IsReadonly)
                        {
                            setValue = "";
                        }

                        bool isMemberProp = prop.IsMember || agent.IsCustomized;

                        if (agent.IsStatic || isMemberProp && prop.IsStatic)
                        {
                            if (isMemberProp)
                            {
                                if (prop.IsArrayElement)
                                {
                                    bindingProperty = string.Format("new CStaticMemberArrayItemProperty<{0}>(\"{1}\", delegate({0} value, int index) {{ {2} }}, delegate(int index) {{ return {3}; }})",
                                                                    propType, prop.BasicName, setValue, getValue);
                                }
                                else
                                {
                                    bindingProperty = string.Format("new CStaticMemberProperty<{0}>(\"{1}\", delegate({0} value) {{ {2} }}, delegate() {{ return {3}; }})",
                                                                    propType, prop.BasicName, setValue, getValue);
                                }
                            }
                            else
                            {
                                Debug.Check(false);
                            }
                        }
                        else
                        {
                            if (isMemberProp)
                            {
                                if (!prop.IsStatic)
                                {
                                    if (prop.IsArrayElement)
                                    {
                                        bindingProperty = string.Format("new CMemberArrayItemProperty<{0}>(\"{1}\", delegate(Agent self, {0} value, int index) {{ {2} }}, delegate(Agent self, int index) {{ return {3}; }})",
                                                                        propType, prop.BasicName, setValue, getValue);
                                    }
                                    else
                                    {
                                        bindingProperty = string.Format("new CMemberProperty<{0}>(\"{1}\", delegate(Agent self, {0} value) {{ {2} }}, delegate(Agent self) {{ return {3}; }})",
                                                                        propType, prop.BasicName, setValue, getValue);
                                    }
                                }
                            }
                            else
                            {
                                registerName = prop.IsStatic ? "RegisterStaticCustomizedProperty" : "RegisterCustomizedProperty";

                                if (prop.IsArrayElement)
                                {
                                    string propName = prop.BasicName.Replace("[]", "");
                                    bindingProperty = string.Format("new CCustomizedArrayItemProperty<{0}>({1}, \"{2}\")",
                                                                    propType, CRC32.CalcCRC(propName), propName);
                                }
                                else
                                {
                                    bindingProperty = string.Format("new CCustomizedProperty<{0}>({1}, \"{2}\", \"{3}\")",
                                                                    propType, CRC32.CalcCRC(prop.BasicName), prop.BasicName, prop.DefaultValue);
                                }
                            }
                        }

                        file.WriteLine("\t\t\tmeta.{0}({1}, {2});", registerName, CRC32.CalcCRC(prop.BasicName), bindingProperty);
                    }
                }

                IList<MethodDef> methods = agent.GetMethods(true);

                foreach (MethodDef method in methods)
                {
                    bool hasRefParam = false;

                    foreach (MethodDef.Param param in method.Params)
                    {
                        if (param.IsRef || param.IsOut || Plugin.IsRefType(param.Type) || IsStructType(param))
                        {
                            hasRefParam = true;
                            break;
                        }
                    }

                    string agentMethod = "";
                    string paramTypes = "";
                    string paramTypeValues = "";
                    string paramValues = "";

                    if (method.IsNamedEvent || !hasRefParam)
                    {
                        foreach (MethodDef.Param param in method.Params)
                        {
                            if (!string.IsNullOrEmpty(paramTypes))
                            {
                                paramTypes += ", ";
                            }

                            if (!string.IsNullOrEmpty(paramValues))
                            {
                                paramValues += ", ";
                            }

                            string paramType = DataCsExporter.GetGeneratedNativeType(param.NativeType);
                            paramTypes += paramType;
                            paramTypeValues += ", " + paramType + " " + param.Name;
                            paramValues += param.Name;
                        }
                    }

                    string methodReturnType = DataCsExporter.GetGeneratedNativeType(method.NativeReturnType);

                    if (method.IsNamedEvent)
                    {
                        if (!string.IsNullOrEmpty(paramTypes))
                        {
                            paramTypes = string.Format("<{0}>", paramTypes);
                        }

                        agentMethod = string.Format("new CAgentMethodVoid{0}(delegate(Agent self{1}) {{ }}) /* {2} */", paramTypes, paramTypeValues, method.BasicName);

                        file.WriteLine("\t\t\tmeta.RegisterMethod({0}, {1});", CRC32.CalcCRC(method.BasicName), agentMethod);
                    }
                    else
                    {
                        if (hasRefParam)
                        {
                            string methodFullname = method.Name.Replace("::", "_");
                            agentMethod = string.Format("new CMethod_{0}()", methodFullname);
                        }
                        else
                        {
                            if (method.IsStatic)
                            {
                                if (paramTypeValues.StartsWith(", "))
                                {
                                    paramTypeValues = paramTypeValues.Substring(2);
                                }

                                if (methodReturnType == "void")
                                {
                                    if (!string.IsNullOrEmpty(paramTypes))
                                    {
                                        paramTypes = string.Format("<{0}>", paramTypes);
                                    }

                                    agentMethod = string.Format("new CAgentStaticMethodVoid{0}(delegate({1}) {{ {2}.{3}({4}); }})",
                                                                paramTypes, paramTypeValues, agentTypeName, method.BasicName, paramValues);
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(paramTypes))
                                    {
                                        paramTypes = ", " + paramTypes;
                                    }

                                    agentMethod = string.Format("new CAgentStaticMethod<{0}{1}>(delegate({2}) {{ return {3}.{4}({5}); }})",
                                                                methodReturnType, paramTypes, paramTypeValues, agentTypeName, method.BasicName, paramValues);
                                }
                            }
                            else
                            {
                                string methodStr = "";

                                if (method.IsPublic)
                                {
                                    methodStr = string.Format("(({0})self).{1}({2})", agentTypeName, method.BasicName, paramValues);
                                }
                                else
                                {
                                    paramValues = string.IsNullOrEmpty(paramValues) ? "null" : string.Format("new object[]{{ {0} }}", paramValues);
                                    methodStr = string.Format("AgentMetaVisitor.ExecuteMethod(self, \"{0}\", {1})", method.BasicName, paramValues);
                                }

                                if (methodReturnType == "void")
                                {
                                    if (!string.IsNullOrEmpty(paramTypes))
                                    {
                                        paramTypes = string.Format("<{0}>", paramTypes);
                                    }

                                    agentMethod = string.Format("new CAgentMethodVoid{0}(delegate(Agent self{1}) {{ {2}; }})",
                                                                paramTypes, paramTypeValues, methodStr);
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(paramTypes))
                                    {
                                        paramTypes = ", " + paramTypes;
                                    }

                                    if (!method.IsPublic)
                                    {
                                        methodStr = string.Format("({0}){1}", methodReturnType, methodStr);
                                    }

                                    agentMethod = string.Format("new CAgentMethod<{0}{1}>(delegate(Agent self{2}) {{ return {3}; }})",
                                                                methodReturnType, paramTypes, paramTypeValues, methodStr);
                                }
                            }
                        }

                        file.WriteLine("\t\t\tmeta.RegisterMethod({0}, {1});", CRC32.CalcCRC(method.BasicName), agentMethod);
                    }
                }
            }
        }
    }
}
