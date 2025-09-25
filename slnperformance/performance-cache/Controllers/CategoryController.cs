using Microsoft.AspNetCore.Mvc;
using Repository;

namespace performance_cache.Controllers
{
    public class CategoryController : ControllerBase
    {
        public const string key = "get-categories";
        private const string redisConnection = "localhost:6379";
        private readonly ICategoryRepository categoryRepository;
        private const string connectionString = "";

        public CategoryController(ICategoryRepository categoryRepository)
        {
            //Recebe a injeção de dependência
            this.categoryRepository = categoryRepository;
        }


    }
}
