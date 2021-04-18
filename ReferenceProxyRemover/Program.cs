
#region Usings
using System;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.DotNet.Builder;
using System.Collections.Generic;
#endregion

namespace ReferenceProxyRemover
{
    public static class Program
    {
        static void Main(string[] args) {
            Console.Write("[-] Path : ");
            var Path = Console.ReadLine();
            var Module = ModuleDefinition.FromFile(Path);
            RemoveProxies(Module);
            Module.Write(Path.Insert(Path.Length - 4, "-ReferenceProxyRemove"));
        }
        private static void RemoveProxies(ModuleDefinition Module) { 
            foreach (var Type in Module.GetAllTypes().ToArray()) { 
                foreach (var Method in Type.Methods.Where(Method=> Method.HasMethodBody && !Method.Unmanaged).ToArray()) {
                    var IL = Method.CilMethodBody.Instructions;

                    for (int x = 0; x < IL.Count; x++) {
                        if (IL[x].OpCode != CilOpCodes.Call)
                            continue;
                        if (IL[x].Operand as MethodDefinition == null)
                            continue;
                        var Proxy = IL[x].Operand as MethodDefinition;
                        if (!Proxy.IsProxyMethod(out var NewOperand))
                            continue;
                        IL[x].OpCode = NewOperand.OpCode;
                        IL[x].Operand = NewOperand.Operand;
                        Console.WriteLine("Fixed Reference Proxy : {0}",
                            NewOperand.Operand.ToString());
                        Proxy.DeclaringType?.Methods.Remove(Proxy);
                    }
                }
            }
        }
        private static bool IsProxyMethod(this MethodDefinition Method, out CilInstruction Instruction) {
            Instruction = null;
            if (!Method.HasMethodBody)
                return false;
            if (Method.CilMethodBody.Instructions.Count is 0)
                return false;
            if (!Method.IsStatic)
                return false;
            var Instructions = Method.CilMethodBody.Instructions;
            var ParamertersCount = Method.Signature.GetTotalParameterCount();
            if (Instructions.Count != (ParamertersCount + 2))
                return false;
            var CallInstruction = Instructions[ParamertersCount];
            if (CallInstruction.OpCode == CilOpCodes.Call || CallInstruction.OpCode == CilOpCodes.Callvirt || CallInstruction.OpCode == CilOpCodes.Newobj) {
                Instruction = CallInstruction;
                return true;
            }
            else {
                return false;
            }
        }
    }
}