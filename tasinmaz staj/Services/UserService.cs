using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
public class UserService : IUserService
{
    private readonly RemsDbContext _context;

    public UserService(RemsDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetUsersAsync(UserFilterDto filter)
    {
        var query = _context.Users.AsQueryable();

        int totalRecords = await query.CountAsync();
        var users = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new { u.Id, u.Email, u.Role }) // Þifre hash'lerini listeye dahil etmiyoruz
            .ToListAsync();

        return new { TotalCount = totalRecords, Data = users };
    }

    public async Task<bool> CreateUserAsync(UserCreateDto dto)
    {
        // Þifre Hashleme iþlemi (Kendi yazdýðýn PasswordHelper'a göre uyarla)
        string salt = PasswordHelper.GenerateSalt();
        string hash = PasswordHelper.HashPassword(dto.Password, salt);

        var user = new User
        {
            Email = dto.Email,
            Role = dto.Role,
            PasswordHash = hash,
            Salt = salt
        };

        await _context.Users.AddAsync(user);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> UpdateUserAsync(int id, UserUpdateDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.Email = dto.Email;
        user.Role = dto.Role;

        if (!string.IsNullOrEmpty(dto.Password))
        {
            // Yeni þifre girildiyse onu da hashleyip güncelle
            user.Salt = PasswordHelper.GenerateSalt();
            user.PasswordHash = PasswordHelper.HashPassword(dto.Password, user.Salt);
        }

        _context.Users.Update(user);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        // REQ-5: Kullanýcý silinince ona ait taþýnmazlarýn da silinmesi (Cascade Delete)[cite: 1]
        var userProperties = _context.Properties.Where(p => p.UserId == id);
        _context.Properties.RemoveRange(userProperties);

        _context.Users.Remove(user);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }
}