
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Designer {
    public class CodeCompiler {
        private List<MetadataReference> _references;

        public CodeCompiler() {
            _references = new List<MetadataReference>();
            foreach (var r in ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)) {
                    _references.Add(MetadataReference.CreateFromFile(r));
            }
        }

        public int CompileAndRun(String path, int seed=-1) {                       
            Console.WriteLine("Compiling script.");
            Data.DebugConsole.Add(("Compiling script."));

            FileStream source = File.OpenRead(path);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source), path: path);
            source.Dispose();

            CSharpCompilation compilation = CSharpCompilation.Create(Path.GetRandomFileName(), syntaxTrees: new[] { syntaxTree }, references: _references, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAllowUnsafe(true));

            //get the scripts classname
            var model = compilation.GetSemanticModel(syntaxTree);
            var myClass = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Last();
            var myClassSymbol = model.GetDeclaredSymbol(myClass) as ISymbol;
            String className = myClassSymbol.Name;
            var namespaceSymbol = myClassSymbol.ContainingNamespace;
            if (namespaceSymbol != null) {
                className = namespaceSymbol.Name + "." + className;
            }

            using (MemoryStream ms = new MemoryStream()) {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success) {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                    foreach (Diagnostic diagnostic in failures) {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                        Data.DebugConsole.Add(String.Format("{0}: {1}", diagnostic.Id, diagnostic.GetMessage()));
                    }
                    return 0;
                } else {
                    ms.Seek(0, SeekOrigin.Begin);
                    Type type = Assembly.Load(ms.ToArray()).GetType(className);

                    Console.WriteLine("Executing script.");
                    Data.DebugConsole.Add(("Executing script."));

                    type.InvokeMember("Generate",BindingFlags.Default | BindingFlags.InvokeMethod,null,Activator.CreateInstance(type),new object[] {seed});
                }
            }
            return 1;
        }
    }
}