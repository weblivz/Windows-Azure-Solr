#region Copyright Notice
/*
Copyright © Microsoft Open Technologies, Inc.
All Rights Reserved
Apache 2.0 License

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Collections.ObjectModel;

namespace SolrDeployCmdlets.Utilities
{
    public static class ExecuteCommands
    {
        public static PSDataCollection<PSObject> ExecuteCommand(String command)
        {
            Console.WriteLine("Executing command:");

            string outputLine = command;
            int ichNewline = command.IndexOfAny("\r\n".ToCharArray());
            if (ichNewline > 0)
                outputLine = command.Substring(0, ichNewline);

            Console.WriteLine(outputLine);

            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript(command);

                IAsyncResult async = ps.BeginInvoke();
                PSDataCollection<PSObject> output = ps.EndInvoke(async);
                foreach (PSObject result in output)
                {
                    Console.WriteLine(result.ToString());
                }
                return output;
            }
        }
    }
}
