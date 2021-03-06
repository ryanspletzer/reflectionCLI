using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime;
using System.Runtime.Loader;

namespace reflectionCli {
    public class LoadAssembly : ICommand    {

        public LoadAssembly(String path) {

            Assembly TempAsm = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(File.ReadAllBytes(path)));

            //need to check and
            var tempicommand = TempAsm.GetTypes().ToList().Where(x => (x.Name == "ICommand")).ToList();

            if (tempicommand.Count == 0) {
                throw new Exception("Unable to find ICommand");
            }

            if (tempicommand.Count > 1) {
                throw new Exception("Multiple ICommands Found");
            }

            Type type = tempicommand[0];

            var extvalinfo = type.GetMethods().ToList()
                                .Where(x => (x.Name == "ExitVal"))
                                .Where(x => (x.ReturnType == typeof(bool)))
                                .ToList();

            if (extvalinfo.Count == 0) {
                throw new Exception("ICommand did not return the correct exit value");
            }

            Program.activeasm.Add(Guid.NewGuid() ,TempAsm);

        }
        public bool ExitVal()   {
            return false;
        }
    }

}