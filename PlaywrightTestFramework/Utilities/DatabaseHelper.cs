using Dapper;
using Npgsql;
using PlaywrightTestFramework.Config;

namespace PlaywrightTestFramework.Utilities
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            _connectionString = ConfigReader.DbConnectionString;
        }

        public async Task<T?> ExecuteScalarAsync<T>(string query, object? parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<T>(query, parameters);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string query, object? parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<T>(query, parameters);
        }

        public async Task<int> ExecuteAsync(string query, object? parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteAsync(query, parameters);
        }
    }
}