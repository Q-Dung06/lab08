using System.Data;
using System.Data.SqlClient;

namespace Lab08.DAL;

/// <summary>Small ADO.NET helper. Each operation owns and disposes its connection, command and reader.</summary>
public static class SqlHelper
{
    public static async Task<object?> ExecuteScalarAsync(
        string sql,
        Action<SqlParameterCollection>? addParameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        await using var connection = DbConnectionFactory.Create();
        await using var command = new SqlCommand(sql, connection) { CommandType = commandType };
        addParameters?.Invoke(command.Parameters);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    public static async Task<int> ExecuteNonQueryAsync(
        string sql,
        Action<SqlParameterCollection>? addParameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        await using var connection = DbConnectionFactory.Create();
        await using var command = new SqlCommand(sql, connection) { CommandType = commandType };
        addParameters?.Invoke(command.Parameters);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task<List<T>> ExecuteReaderAsync<T>(
        string sql,
        Func<SqlDataReader, T> map,
        Action<SqlParameterCollection>? addParameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(map);
        var results = new List<T>();
        await using var connection = DbConnectionFactory.Create();
        await using var command = new SqlCommand(sql, connection) { CommandType = commandType };
        addParameters?.Invoke(command.Parameters);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(map(reader));
        return results;
    }
}
