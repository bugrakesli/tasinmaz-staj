using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // REQ-8: Yalnýzca Admin eriþebilir[cite: 1]
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
            return BadRequest(new { message = "Failed to add user. Please check all required fields." }); // REQ-6[cite: 1]

        var success = await _userService.CreateUserAsync(dto);
        if (success)
            return Ok(new { message = "User added successfully." }); // REQ-6[cite: 1]

        return BadRequest(new { message = "Failed to add user. Please check all required fields." });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Failed to update user. Please check all required fields." }); // REQ-6[cite: 1]

        var success = await _userService.UpdateUserAsync(id, dto);
        if (success)
            return Ok(new { message = "User updated successfully." }); // REQ-6[cite: 1]

        return BadRequest(new { message = "Failed to update user. Please check all required fields." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (success)
            return Ok(new { message = "User and associated properties deleted successfully." }); // REQ-6[cite: 1]

        return NotFound(new { message = "User not found." });
    }
}