using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json;
using performance_cache.Model;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace performance_cache.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {

        public const string key = "get-users";
        private const string redisConnection = "localhost:6379";
        private const string connectionString = "Server=localhost;database=fiap;User=root;Password=123";

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            //Implementando o cache
            var redis = ConnectionMultiplexer.Connect(redisConnection);
            IDatabase db = redis.GetDatabase();
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(20));
            string userValue = await db.StringGetAsync(key);

            if (!string.IsNullOrEmpty(userValue))
            {
                return Ok(userValue);
            }

            //Buscando no banco
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string sql = "SELECT id, name, email FROM users;";
            var users = await connection.QueryAsync<Users>(sql);
            var usersJson = JsonConvert.SerializeObject(users);
            await db.StringSetAsync(key, usersJson); //Configura o cache

            Thread.Sleep(3000); //Forçando uma espera
            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Users user)
        {
            if(user == null)
                return BadRequest("Dados inválidos");

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO users (name, email)
                VALUES (@Name, @Email);
                SELECT LAST_INSERT_ID();
            ";

            var newId = await connection.QuerySingleAsync<int>(sql, user);
            user.Id = newId;

            //Invalidar o cache
            //await InvalidateCache();

            return CreatedAtAction(nameof(Get), new { id = newId }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Users user)
        {
            if (user == null)
                return BadRequest("Usuário inválido!");

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE users SET
                name = @Name,
                email = @Email
                WHERE id = @Id;
            ";

            user.Id = id;
            var rowsAffected = await connection.ExecuteAsync(sql, user);

            if(rowsAffected == 0)
                return NotFound("Usuário não encontrado!");

            await InvalidateCache();

            return NoContent();
        }

        [HttpDelete ("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id == 0)
                return BadRequest("Identificador não informado!");

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string sql = @"
               DELETE FROM users 
               WHERE id = @Id;
            ";

            var rowsAffected = await connection.ExecuteAsync(sql, new { id });

            if (rowsAffected == 0)
                return NotFound("Usuário não encontrado!");

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
