using Dapper;
using Domain;
using Microsoft.Extensions.Configuration;

namespace Repository
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly string _connectionString;

        public CategoryRepository(IConfiguration configuration)
        {
            _connectionString = configuration["DefaultConnection"];
        }
        public async Task<IEnumerable<Categories>> GetAllCategories()
        {
            using var connection = new MySqlConnector.MySqlConnection(_connectionString);
            await connection.OpenAsync();
            var sql = "SELECT * FROM categories;";
            return await connection.QueryAsync<Categories>(sql);
        }
    }
}
