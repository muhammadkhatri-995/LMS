namespace LMS.API.Common;

public static class SqlLike
{
    // Builds a "contains" pattern and escapes % and _ so a search for "50%" is literal.
    public static string ContainsPattern(string term)
    {
        var escaped = term.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        return "%" + escaped + "%";
    }
}