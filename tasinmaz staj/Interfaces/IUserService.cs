using System.Threading.Tasks;

public interface IUserService
{
    Task<object> GetUsersAsync(UserFilterDto filter);
    Task<bool> CreateUserAsync(UserCreateDto dto);
    Task<bool> UpdateUserAsync(int id, UserUpdateDto dto);
    Task<bool> DeleteUserAsync(int id);
}