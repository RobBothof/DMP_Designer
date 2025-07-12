
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Designer
{
    public class CodeCompiler
    {
        private List<MetadataReference> _references;

        private bool _isCompiling = false;
        private bool _isGenerating = false;
        public bool IsGenerating { get => _isGenerating; set => _isGenerating = value; }
        public bool IsCompiling { get => _isCompiling; set => _isCompiling = value; }

        public CancellationTokenSource GeneratorTokenSource { get; private set; }

        public CodeCompiler()
        {
            _references = new List<MetadataReference>();
            foreach (var r in ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator))
            {
                _references.Add(MetadataReference.CreateFromFile(r));
            }
        }

        public Type Compile(String path)
        {
            IsCompiling = true;
            FileStream source = File.OpenRead(path);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source), path: path);
            source.Dispose();

            // CSharpCompilation compilation = CSharpCompilation.Create(Path.GetRandomFileName(), syntaxTrees: new[] { syntaxTree }, references: _references, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAllowUnsafe(true));
            CSharpCompilation compilation = CSharpCompilation.Create(
                Path.GetRandomFileName(),
                syntaxTrees: new[] { syntaxTree },
                references: _references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(OptimizationLevel.Debug) // ensure debug info
                    .WithAllowUnsafe(true));

            // Emit DLL and PDB into memory streams.
            using MemoryStream dllStream = new MemoryStream();
            using MemoryStream pdbStream = new MemoryStream();
            EmitResult result = compilation.Emit(dllStream, pdbStream: pdbStream);

            if (!result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(
                    diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                foreach (Diagnostic diagnostic in failures)
                {
                    Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    Data.DebugConsole.Add($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                }
                IsCompiling = false;
                return null;
            }

            dllStream.Seek(0, SeekOrigin.Begin);
            pdbStream.Seek(0, SeekOrigin.Begin);

            // Load the assembly with symbols.
            Assembly assembly = Assembly.Load(dllStream.ToArray(), pdbStream.ToArray());

            //get the scripts classname
            var model = compilation.GetSemanticModel(syntaxTree);
            var myClass = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Last();
            var myClassSymbol = model.GetDeclaredSymbol(myClass) as ISymbol;
            String className = myClassSymbol.Name;
            var namespaceSymbol = myClassSymbol.ContainingNamespace;
            if (namespaceSymbol != null)
            {
                className = namespaceSymbol.Name + "." + className;
            }

            IsCompiling = false;
            return assembly.GetType(className);
        }

        public void Run(Type scriptType, int seed)
        {
            if (scriptType == null)
            {

                return;
            }

            IsGenerating = true;

            GeneratorTokenSource = new CancellationTokenSource();
            CancellationToken token = GeneratorTokenSource.Token;

            Task.Run(() =>
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                try
                {
                    scriptType.InvokeMember("Generate",
                        BindingFlags.Default | BindingFlags.InvokeMethod,
                        null, Activator.CreateInstance(scriptType), new object[] { seed, token });
                    Console.WriteLine("Script execution complete.");
                    Data.DebugConsole.Add(("Script execution complete."));
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        Console.WriteLine("Script execution canceled.");
                        Data.DebugConsole.Add("Script execution canceled.");
                    }
                    else
                    {
                        string scriptName = scriptType.FullName;
                        Exception innerEx = (ex is TargetInvocationException && ex.InnerException != null)
                            ? ex.InnerException
                            : ex;
                        string errorMessage = innerEx.Message;
                        string errorStack = innerEx.StackTrace;

                        string errorDetail = $"Script error in '{scriptName}'\n" +
                        $"Error Message: {errorMessage}\n\n" +
                        $"Stack Trace: {errorStack}";
                        Console.WriteLine(errorDetail);
                        Data.DebugConsole.Add(errorDetail);
                    }

                    stopwatch.Stop();
                    TimeSpan elapsed = stopwatch.Elapsed;
                    IsGenerating = false;
                }
                finally
                {
                    stopwatch.Stop();
                    TimeSpan elapsed = stopwatch.Elapsed;
                    Console.WriteLine($"Total time: {elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s {elapsed.Milliseconds}ms");
                    Data.DebugConsole.Add($"Total time: {elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s {elapsed.Milliseconds}ms");
                    IsGenerating = false;
                }

            }, token);

        }

        public void Stop()
        {
            GeneratorTokenSource?.Cancel();
        }
    }
}