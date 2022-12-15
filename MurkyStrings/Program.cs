using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System;
using System.IO;

namespace MurkyStrings;

internal class Program
{
    static void Main(string[] args)
    {
        /*Obviously you can do all that in 1 Console.WriteLine*/
        Console.WriteLine("MurkyStrings by drakonia | https://github.com/dr4k0nia");
        Console.WriteLine("Port to dnlib by HideakiAtsuyo | https://github.com/HideakiAtsuyo");

        Console.WriteLine("Available arguments --mode=");
        Console.WriteLine("replace[glyph]\nreplace[simple]\nremove\ncombine[glyph]\ncombine[simple]\n");

        if (!File.Exists(args[0]))
            throw new FileNotFoundException(args[0]);

        var Module = ModuleDefMD.Load(args[0]);

        if (args.Length == 1)
        {
            Console.WriteLine($"Mode was not specified using default option: replace[glyph]");
            var obfuscator = new ReplaceObfuscator(Module);
            obfuscator.Execute();
        }
        else
        {
            if (!args[1].StartsWith("--mode="))
                throw new Exception("Invalid arguments please use --mode=");

            if (!TryHandleMode(args[1].Remove(0, 7), ref Module))
            {
                Console.ReadKey();
                return;
            }
            Console.WriteLine($"Used mode: {args[1]}");
        }

        string path = Module.Location;
        string filePath = Path.Combine(Path.GetDirectoryName(path), $"{Path.GetFileNameWithoutExtension(path)}_murked{Path.GetExtension(path)}");
        Console.WriteLine($"Strings have been obfuscated, output file: {filePath}");

        ModuleWriterOptions opts = new ModuleWriterOptions(Module);
        opts.MetadataOptions.Flags = MetadataFlags.KeepOldMaxStack;

        Module.Write(filePath/*, opts*/);
    }
    private static bool TryHandleMode(string mode, ref ModuleDefMD module)
    {
        Obfuscator obfuscator;
        switch (mode)
        {
            case "replace[glyph]":
                obfuscator = new ReplaceObfuscator(module);
                obfuscator.Execute();
                return true;
            case "replace[simple]":
                obfuscator = new ReplaceObfuscator(module, ReplaceObfuscator.Mode.Simple);
                obfuscator.Execute();
                return true;
            case "remove":
                obfuscator = new RemoveObfuscator(module);
                obfuscator.Execute();
                return true;
            case "combine[glyph]":
                obfuscator = new RemoveObfuscator(module);
                obfuscator.Execute();
                obfuscator = new ReplaceObfuscator(module);
                obfuscator.Execute();
                return true;
            case "combine[simple]":
                obfuscator = new RemoveObfuscator(module);
                obfuscator.Execute();
                obfuscator = new ReplaceObfuscator(module, ReplaceObfuscator.Mode.Simple);
                obfuscator.Execute();
                return true;
            default:
                Console.WriteLine("\nPlease use one of the available modes: replace[glyph], replace[simple], remove, combine[glyph], combine[simple]");
                return false;
        }
    }
}