using Dapper;
using Domain;

namespace Repository
{
    public interface IVehicleRepository
    {
        Task<IEnumerable<Vehicles>> GetAllVehicles();
    }
}
