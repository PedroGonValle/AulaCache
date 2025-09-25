using Domain;

namespace Repository
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Categories>> GetAllCategories();
    }
}
