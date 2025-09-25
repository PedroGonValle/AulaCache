using Dapper;
using Domain;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Repository
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly string _connectionString;

        public VehicleRepository(IConfiguration configuration)
        {
            _connectionString = configuration["DefaultConnection"];
        }
        public async Task<IEnumerable<Vehicles>> GetAllVehicles()
        {
            using var connection = new MySqlConnector.MySqlConnection(_connectionString);
            await connection.OpenAsync();
            var sql = "SELECT id, brand, model, year, plate FROM vehicles;";
            return await connection.QueryAsync<Vehicles>(sql);
        }

    }
}
