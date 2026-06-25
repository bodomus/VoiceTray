using System.Text;

namespace VoiceTray.Infrastructure.Speech;

internal static class CommandLineArgumentSplitter
{
    public static IReadOnlyList<string> Split(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return [];
        }

        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var character in commandLine)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                AddCurrent();
                continue;
            }

            current.Append(character);
        }

        AddCurrent();
        return result;

        void AddCurrent()
        {
            if (current.Length == 0)
            {
                return;
            }

            result.Add(current.ToString());
            current.Clear();
        }
    }
}
