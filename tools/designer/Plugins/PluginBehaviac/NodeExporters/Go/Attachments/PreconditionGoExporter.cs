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

using System.IO;
using Behaviac.Design.Attachments;
using PluginBehaviac.Events;

namespace PluginBehaviac.NodeExporters
{
    public class PreconditionGoExporter : AttachActionGoExporter
    {
        protected override bool ShouldGenerateClass()
        {
            return true;
        }

        protected override void GenerateMember(Attachment attachment, StringWriter stream, string indent)
        {
            stream.WriteLine("\tbt.Precondition");

            base.GenerateMember(attachment, stream, indent);
        }

        protected override void GenerateConstructor(Attachment attachment, StringWriter stream, string indent, string className)
        {
            if (!(attachment is Precondition precondition))
            {
                return;
            }

            stream.WriteLine("\tn := new({0})", className);
            stream.WriteLine("\tn.Id = {0}", precondition.Id);

            string phase = "bt.PreconditionPhaseEnter";

            switch (precondition.Phase)
            {
                case PreconditionPhase.Update:
                    phase = "bt.PreconditionPhaseUpdate";
                    break;

                case PreconditionPhase.Both:
                    phase = "bt.PreconditionPhaseBoth";
                    break;
            }

            stream.WriteLine("\tn.Phase = {0}", phase);

            string isAnd = (precondition.BinaryOperator == BinaryOperator.And) ? "true" : "false";
            stream.WriteLine("\tn.And = {0}", isAnd);

            base.GenerateConstructor(attachment, stream, indent, className);

            stream.WriteLine("\treturn n");
        }
    }
}
