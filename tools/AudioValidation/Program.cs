using System.Reflection;
using Xunit;

string temp = Path.GetFullPath("artifacts/test-temp");
Directory.CreateDirectory(temp);
Environment.SetEnvironmentVariable("TMP", temp);
Environment.SetEnvironmentVariable("TEMP", temp);
int passed = 0, failed = 0;
foreach (var type in typeof(Fida.Tests.AudioTests).Assembly.GetTypes().Where(t => t.IsPublic && !t.IsAbstract))
foreach (var method in type.GetMethods().Where(m => m.GetCustomAttribute<FactAttribute>() != null))
{
    var fact = method.GetCustomAttribute<FactAttribute>()!;
    if (fact.Skip != null) continue;
    var rows = method.GetCustomAttributes<InlineDataAttribute>().SelectMany(a => a.GetData(method)).ToArray();
    if (rows.Length == 0) rows = [Array.Empty<object>()];
    foreach (var row in rows)
    {
        object? test = null;
        try
        {
            test = Activator.CreateInstance(type);
            var result = method.Invoke(test, row);
            if (result is Task task) await task;
            passed++;
        }
        catch (Exception ex)
        {
            failed++;
            Console.WriteLine($"FAIL {type.Name}.{method.Name}({string.Join(",", row)}): {(ex.InnerException ?? ex)}");
        }
        finally { (test as IDisposable)?.Dispose(); }
    }
}
Console.WriteLine($"Direct sequential execution: {passed} passed, {failed} failed.");
return failed == 0 ? 0 : 1;
