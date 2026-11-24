using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

// Headless xUnit runner.
//
// `dotnet test` cannot run in the confined sandbox this repo is developed in: VSTest's
// testhost calls OpenProcess on its parent and gets Win32Exception (5). This runner loads
// the already-built test assembly and invokes the same [Fact]/[Theory] methods, so the
// logic suite still gates changes.
//
// The output directory is Fida.Tests/bin/<Configuration>/net8.0, because the game resolves
// Content/ through AppContext.BaseDirectory.
//
//   dotnet TestRunner.dll                 run every test class next to this assembly
//   dotnet TestRunner.dll Fida.Tests.dll Fida.Tests.RelayTests ...
//
// Set RUNNER_DETAIL=1 to print inner exception stacks for failures.
string assemblyPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "Fida.Tests.dll");
assemblyPath = Path.GetFullPath(assemblyPath);
if (!File.Exists(assemblyPath))
{
    Console.WriteLine($"Test assembly not found: {assemblyPath}");
    return 2;
}

AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var dependency = Path.Combine(Path.GetDirectoryName(assemblyPath)!, name.Name + ".dll");
    return File.Exists(dependency) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(dependency) : null;
};
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

bool detail = Environment.GetEnvironmentVariable("RUNNER_DETAIL") == "1";
bool verbose = Environment.GetEnvironmentVariable("RUNNER_VERBOSE") == "1";
string[] requested = args.Skip(1).ToArray();
IEnumerable<Type> types = requested.Length == 0 || (requested.Length == 1 && requested[0] == "*")
    ? assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && HasTests(type))
    : requested.Select(name => assembly.GetType(name, true)!);

int passed = 0;
var failures = new List<string>();
foreach (var type in types)
{
    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    {
        var inlineCases = method.GetCustomAttributesData()
            .Where(attribute => attribute.AttributeType.Name == "InlineDataAttribute")
            .Select(attribute => ((IEnumerable<CustomAttributeTypedArgument>)attribute.ConstructorArguments[0].Value!)
                .Select(value => value.Value).ToArray())
            .ToList();
        if (method.GetParameters().Length == 0) inlineCases.Add(Array.Empty<object>());
        foreach (var data in inlineCases)
        {
            var instance = Activator.CreateInstance(type);
            try
            {
                method.Invoke(instance, data);
                passed++;
                if (verbose) Console.WriteLine($"ok   {type.Name}.{method.Name}");
            }
            catch (TargetInvocationException e)
            {
                string message = detail
                    ? e.InnerException?.ToString() ?? e.ToString()
                    : $"{e.InnerException?.GetType().Name}: {e.InnerException?.Message}";
                failures.Add($"{type.Name}.{method.Name}: {message}");
                Console.WriteLine($"FAIL {type.Name}.{method.Name}");
            }
            finally
            {
                (instance as IDisposable)?.Dispose();
            }
        }
    }
}

foreach (string failure in failures) Console.WriteLine(failure);
Console.WriteLine($"Passed {passed}, failed {failures.Count}.");
return failures.Count == 0 ? 0 : 1;

static bool HasTests(Type type) => type.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                                  BindingFlags.DeclaredOnly)
    .Any(method => method.GetCustomAttributesData()
        .Any(attribute => attribute.AttributeType.Name is "FactAttribute" or "TheoryAttribute"));
