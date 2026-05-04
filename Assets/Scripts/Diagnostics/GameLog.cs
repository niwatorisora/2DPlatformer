using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Centralizes gameplay log formatting so Unity Console output stays easy to filter.
/// </summary>
public static class GameLog
{
    const string UnknownCategory = "Unknown";

    public static void Debug(Object context, string message)
        => UnityEngine.Debug.Log(Format("Debug", GetCategory(context), message), context);

    public static void Warning(Object context, string message)
        => UnityEngine.Debug.LogWarning(Format("Warning", GetCategory(context), message), context);

    public static void Error(Object context, string message)
        => UnityEngine.Debug.LogError(Format("Error", GetCategory(context), message), context);

    public static void Debug(string category, string message)
        => UnityEngine.Debug.Log(Format("Debug", category, message));

    public static void Warning(string category, string message)
        => UnityEngine.Debug.LogWarning(Format("Warning", category, message));

    public static void Error(string category, string message)
        => UnityEngine.Debug.LogError(Format("Error", category, message));

    static string GetCategory(Object context)
        => context != null ? context.GetType().Name : UnknownCategory;

    static string Format(string level, string category, string message)
    {
        if (string.IsNullOrWhiteSpace(category))
            category = UnknownCategory;

        return $"[{level}:{category}] {message}";
    }
}
