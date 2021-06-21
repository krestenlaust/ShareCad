using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deinternalizer
{
    class Program
    {
        // kilder:
        // https://stackoverflow.com/questions/4971213/how-to-use-reflection-to-determine-if-a-class-is-internal

        static void Main(string[] args)
        {
            using (AssemblyDefinition a = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters { ReadWrite = true }))
            {
                foreach (var type in a.Modules[0].Types)
                {
                    if (type.IsNestedAssembly)
                        type.IsPublic = true;
                }
                a.Write();
            }
        }
    }
}
