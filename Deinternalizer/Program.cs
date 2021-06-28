using System;
using Mono.Cecil;

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
                int current = 0;
                foreach (var type in a.Modules[0].Types)
                {
                    if (type.IsNotPublic)
                    {
                        current++;
                        type.IsPublic = true;
                        Console.WriteLine(type.FullName);
                    }
                }

                a.Write();
            }
        }
    }
}
