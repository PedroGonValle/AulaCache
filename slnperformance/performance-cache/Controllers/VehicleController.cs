using Dapper;
using Domain;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json;
using Repository;
using StackExchange.Redis;

namespace performance_cache.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleController : ControllerBase
    {
        public const string key = "get-vehicles";
        private const string redisConnection = "localhost:6379";
        private readonly IVehicleRepository vehicleRepository;
        private const string connectionString = "";

        public VehicleController(IVehicleRepository vehicleRepository)
        {
            //Recebe a injeção de dependência
            this.vehicleRepository = vehicleRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            //Implementando o cache
            var redis = ConnectionMultiplexer.Connect(redisConnection);
            IDatabase db = redis.GetDatabase();
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(20));
            string vehicleValue = await db.StringGetAsync(key);

            if (!string.IsNullOrEmpty(vehicleValue))
            {
                return Ok(vehicleValue);
            }

            //Buscando no banco
            var vehicles = await vehicleRepository.GetAllVehicles();
            var vehiclesJson = JsonConvert.SerializeObject(vehicles);
            await db.StringSetAsync(key, vehiclesJson); //Configura o cache

            Thread.Sleep(3000); //Forçando uma espera
            return Ok(vehicles);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Vehicles vehicle)
        {
            if (vehicle == null)
                return BadRequest("Dados inválidos");

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO vehicles (brand, model, year, plate)
                VALUES (@Brand, @Model, @Year, @Plate);
                SELECT LAST_INSERT_ID();
            ";
            try {
                var newId = await connection.QuerySingleAsync<int>(sql, vehicle);
                vehicle.Id = newId;

                //Invalidar o cache
                await InvalidateCache();

                return CreatedAtAction(nameof(Get), new { id = newId }, vehicle);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return BadRequest("A placa informada já está cadastrada!");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Vehicles vehicle)
        {
            if (vehicle == null)
                return BadRequest("Veículo inválido!");

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE vehicles SET
                brand = @Brand,
                model = @Model,
                year = @Year,
                plate = @Plate
                WHERE id = @Id;
            ";

            vehicle.Id = id;
            try
            {
                var rowsAffected = await connection.ExecuteAsync(sql, vehicle);

                if (rowsAffected == 0)
                    return NotFound("Veículo não encontrado!");

                await InvalidateCache();

                return NoContent();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return BadRequest("A placa informada já está cadastrada para outro veículo!");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id == 0)
                return BadRequest("Identificador não informado!");

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string sql = @"
               DELETE FROM vehicles 
               WHERE id = @Id;
            ";

            var rowsAffected = await connection.ExecuteAsync(sql, new { id });

            if (rowsAffected == 0)
                return NotFound("Veículo não encontrado!");

            await InvalidateCache();

            return NoContent();
        }

        private async Task InvalidateCache()
        {
            var redis = ConnectionMultiplexer.Connect(redisConnection);
            IDatabase db = redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
    }
}
