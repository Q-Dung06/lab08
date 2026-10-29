using System.Configuration;
using System.Data.SqlClient;

namespace Lab08.DAL;

public static class DbConnectionFactory
{
    public static string GetConnectionString()
    {
        var entry = ConfigurationManager.ConnectionStrings["QLSinhVien"];
        if (entry is null || string.IsNullOrWhiteSpace(entry.ConnectionString))
            throw new ConfigurationErrorsException("Thiếu connection string 'QLSinhVien' trong App.config.");
        return entry.ConnectionString;
    }

    public static SqlConnection Create() => new(GetConnectionString());
}
