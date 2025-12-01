using System;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Basic.Reference.Assemblies;

namespace RoslynWrapper;

public partial class Program
{
    private static List<MetadataReference>? _references;
    
    public static void Main()
    {
        Console.WriteLine("C# Compiler Ready");
        InitializeReferences();
    }
    
    private static void InitializeReferences()
    {
        if (_references != null) return;
        
        // Use Basic.Reference.Assemblies - this provides in-memory reference assemblies
        _references = new List<MetadataReference>(Net80.References.All);
        
        Console.WriteLine($"Initialized with {_references.Count} references");
    }

    [JSExport]
    public static string CompileAndRun(string code)
    {
        try
        {
            if (_references == null)
                InitializeReferences();
            
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            
            // For now, use an empty reference list - user code must be self-contained
            // Or we need to embed mscorlib, System.Runtime, etc. as resources
            var references = _references ?? new List<MetadataReference>();

            var compilation = CSharpCompilation.Create(
                "DynamicAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(
                    OutputKind.ConsoleApplication,
                    concurrentBuild: false,  // Disable concurrent build (uses threads)
                    deterministic: true,
                    checkOverflow: false));

            using var ms = new MemoryStream();
            EmitResult result = compilation.Emit(ms, options: new EmitOptions(
                debugInformationFormat: DebugInformationFormat.Embedded,
                includePrivateMembers: false));

            if (!result.Success)
            {
                var errors = string.Join("\n", result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => $"{d.Id}: {d.GetMessage()}"));
                return $"COMPILATION_ERROR\n{errors}";
            }

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            
            // Find the entry point - try different combinations
            var type = assembly.GetType("Program");
            if (type == null)
            {
                // Try to find any type with a Main method
                type = assembly.GetTypes().FirstOrDefault(t => 
                    t.GetMethod("Main", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null);
            }
            
            if (type == null)
                return "ERROR: No Program class or Main method found";
            
            var method = type.GetMethod("Main", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (method == null)
                return "ERROR: No static Main method found in Program class";

            var oldOut = Console.Out;
            using var sw = new StringWriter();
            Console.SetOut(sw);
            
            try
            {
                // Invoke Main with appropriate parameters
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    method.Invoke(null, null);
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string[]))
                {
                    method.Invoke(null, new object[] { new string[0] });
                }
                else
                {
                    return "ERROR: Main method has unsupported signature";
                }
            }
            finally
            {
                Console.SetOut(oldOut);
            }
            
            return sw.ToString();
        }
        catch (Exception ex)
        {
            return $"RUNTIME_ERROR\n{ex.Message}\n{ex.StackTrace}";
        }
    }
}
