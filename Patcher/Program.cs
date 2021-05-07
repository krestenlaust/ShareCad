using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Patcher
{
    class Patcher
    {
        static void Main(string[] args)
        {
            AssemblyDefinition mathcadPrimeAssembly = AssemblyDefinition.ReadAssembly("MathcadPrime.exe");
            var mathcadPrimeModule = mathcadPrimeAssembly.MainModule;

            TypeDefinition type = mathcadPrimeModule.Types.First(t => t.Name == "App");
            MethodDefinition method = type.Methods.Single(m => m.Name == "PrimeStartup");

            var processor = method.Body.GetILProcessor();
            var inst1 = processor.Create(OpCodes.Call, AccessTools.TypeByName("Directory").GetMethod(""));
            processor.Append(inst1);
        }
    }
}
