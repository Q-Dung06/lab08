using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Lab08.DAL;

public sealed class SqlScriptRunner
{
    private static readonly Regex GoSeparator = new(
        @"^\s*GO(?:\s+\d+)?\s*(?:--.*)?$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public async Task<int> ExecuteAsync(string connectionString, string scriptPath, CancellationToken ct = default)
    {
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("Không tìm thấy file SQL.", scriptPath);
        var script = await File.ReadAllTextAsync(scriptPath, ct);
        var batches = GoSeparator.Split(script).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        var count = 0;
        foreach (var batch in batches)
        {
            await using var command = new SqlCommand(batch, connection) { CommandTimeout = 90 };
            await command.ExecuteNonQueryAsync(ct);
            count++;
        }
        return count;
    }
}
