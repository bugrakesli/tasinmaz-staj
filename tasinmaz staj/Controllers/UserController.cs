using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // REQ-8: Yalnızca Admin erişebilir[cite: 1]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter)
    {
        var result = await _userService.GetUsersAsync(filter);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Kullanıcı eklenemedi. Lütfen tüm zorunlu alanları kontrol edin." }); // REQ-6[cite: 1]

        var success = await _userService.CreateUserAsync(dto);
        if (success)
            return Ok(new { message = "Kullanıcı başarıyla eklendi." }); // REQ-6[cite: 1]

        return BadRequest(new { message = "Kullanıcı eklenemedi. Lütfen tüm zorunlu alanları kontrol edin." });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Kullanıcı güncellenemedi. Lütfen tüm zorunlu alanları kontrol edin." }); // REQ-6[cite: 1]

        var success = await _userService.UpdateUserAsync(id, dto);
        if (success)
            return Ok(new { message = "Kullanıcı başarıyla güncellendi." }); // REQ-6[cite: 1]

        return BadRequest(new { message = "Kullanıcı güncellenemedi. Lütfen tüm zorunlu alanları kontrol edin." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (success)
            return Ok(new { message = "Kullanıcı ve ilişkili taşınmazlar başarıyla silindi." }); // REQ-6[cite: 1]

        return NotFound(new { message = "Kullanıcı bulunamadı." });
    }
}
