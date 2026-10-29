using System.Collections.Concurrent;
using System.Reflection;
using Fida.Logic;

namespace Fida.Agent;

/// <summary>复制当前规则对象图，保留状态栈、步骤结果、技能次数与枚举器位置；剥离外部事件。</summary>
internal static class SimulationSnapshot
{
    private static readonly MethodInfo Memberwise = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> Fields = new();

    public static GameManager Copy(GameManager game)
    {
        var seen = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        object? Clone(object? value)
        {
            if (value == null) return null;
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string or decimal or Delegate or Type)
                return value;
            if (seen.TryGetValue(value, out var previous)) return previous;
            if (value is Array array)
            {
                var copy = (Array)array.Clone();
                seen[value] = copy;
                if (!type.GetElementType()!.IsPrimitive && !type.GetElementType()!.IsEnum)
                    for (int i = 0; i < copy.Length; i++) copy.SetValue(Clone(array.GetValue(i)), i);
                return copy;
            }
            // 规则实体、集合和 LINQ 枚举器属于状态；其余框架服务是不可变共享对象。
            string ns = type.Namespace ?? "";
            if (!ns.StartsWith("Fida.Logic", StringComparison.Ordinal) &&
                !ns.StartsWith("System.Collections", StringComparison.Ordinal) &&
                !ns.StartsWith("System.Linq", StringComparison.Ordinal)) return value;
            var result = Memberwise.Invoke(value, null)!;
            seen[value] = result;
            foreach (var field in Fields.GetOrAdd(type, AllFields))
            {
                bool observer = value is GameManager or Player && typeof(Delegate).IsAssignableFrom(field.FieldType);
                field.SetValue(result, observer ? null : Clone(field.GetValue(value)));
            }
            return result;
        }
        var snapshot = (GameManager)Clone(game)!;
        // UI 的动画延迟不属于模拟规则，模拟过程中立即检查胜负。
        typeof(GameManager).GetField("_gameEndDeferralDepth", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(snapshot, 0);
        return snapshot;
    }

    private static FieldInfo[] AllFields(Type type)
    {
        var result = new List<FieldInfo>();
        for (Type? current = type; current != null; current = current.BaseType)
            result.AddRange(current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        return result.ToArray();
    }
}
