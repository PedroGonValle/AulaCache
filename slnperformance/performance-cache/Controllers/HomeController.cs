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
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            //Implementando o cache
            string key = "get-users";
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            IDatabase db = redis.GetDatabase();
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(20));
            string userValue = await db.StringGetAsync(key);

            if (!string.IsNullOrEmpty(userValue))
            {
                return Ok(userValue);
            }

            //Buscando no banco
            using var connection = new MySqlConnection("Server=localhost;database=fiap;User=root;Password=123");
            await connection.OpenAsync();
            string sql = "SELECT id, name, email FROM users;";
            var users = await connection.QueryAsync<Users>(sql);
            var usersJson = JsonConvert.SerializeObject(users);
            await db.StringSetAsync(key, usersJson); //Configura o cache

            Thread.Sleep(3000); //Forçando uma espera
            return Ok(users);
        }
    }
}
