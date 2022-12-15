using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MurkyStrings
{
    internal class RemoveObfuscator : Obfuscator
    {
        private readonly ModuleDefMD _module;

        private readonly Random _random;

        private readonly List<string> _names;

        public RemoveObfuscator(ModuleDefMD module)
        {
            _module = module;
            _names = new List<string>();
            _random = new Random(Guid.NewGuid().GetHashCode());
        }

        public override void Execute()
        {
            var importer = new Importer(_module);
            foreach (var type in _module.GetTypes().Where(t => t.Methods.Count != 0))
            {
                foreach (var method in type.Methods)
                {
                    if (method.Body == null)
                        continue;

                    var instructions = method.Body.Instructions;

                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].OpCode != OpCodes.Ldstr)
                            continue;

                        var replaceMethod = importer.Import(typeof(string).GetMethod("Remove", new[] { typeof(int), typeof(int) }) ?? throw new InvalidDataException());

                        string operand = (string)instructions[i].Operand!;

                        var result = ObfuscateString(ref operand);

                        instructions[i].Operand = operand;

                        var implant = new List<Instruction>();
                        int count = 0;
                        foreach (var pair in result)
                        {
                            implant.Add(Instruction.CreateLdcI4(pair.Item1));
                            implant.Add(Instruction.CreateLdcI4(pair.Item2));
                            implant.Add(count == 0 ? new Instruction(OpCodes.Call, importer.Import(replaceMethod)) : new Instruction(OpCodes.Callvirt, importer.Import(replaceMethod)));
                            count++;
                        }

                        int iii = i + 1;
                        for (int ii = 0; ii < implant.Count(); ii++)
                        {
                            instructions.Insert(iii, implant[ii]);
                            iii++;
                        }

                        i += implant.Count;
                    }

                    instructions.OptimizeMacros();
                    instructions.OptimizeBranches();
                    instructions.SimplifyBranches();
                }
            }
        }

        private List<Tuple<int, int>> ObfuscateString(ref string input)
        {
            int insertsCount = _random.Next(5);
            var result = new List<Tuple<int, int>>();

            for (int i = 0; i < insertsCount; i++)
            {
                int index = _random.Next(0, input.Length);
                string insert = GetRandomName();
                int insertLength = insert.Length;

                input = input.Insert(index, insert);

                result.Add(new Tuple<int, int>(index, insertLength));
            }

            result.Reverse();
            return result;
        }

        private string GetRandomName()
        {
            if (_names.Count != 0)
            {
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < _random.Next(0, 5); i++)
                {
                    builder.Append(_names[_random.Next(_names.Count)]);
                    if (_random.Next(0, 2) == 0)
                        builder.Append((char)0x20, _random.Next(1, 4)); // Append random spaces
                }

                return builder.ToString();
            }

            var types = typeof(string).Module.GetTypes();

            foreach (var type in types)
            {
                foreach (var method in type.GetMethods())
                {
                    if (!_names.Contains(method.Name))
                        _names.Add(method.Name);
                }
            }

            return GetRandomName();
        }
    }
}