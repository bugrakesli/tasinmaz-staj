using System.ComponentModel.DataAnnotations;

public class UserFilterDto
{
    // REQ-9: Sayfalama zorunluluðu
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class UserCreateDto
{
    [Required]
    public string Email { get; set; }

    // REQ-10: Þifre kurallarý validasyonu
    [Required]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[\W_]).{8,12}$",
        ErrorMessage = "Þifre 8-12 karakter arasý olmalý; en az bir harf, bir rakam ve bir özel karakter içermelidir.")]
    public string Password { get; set; }

    [Required]
    public string Role { get; set; } // "Admin" veya "User"
}

public class UserUpdateDto
{
    [Required]
    public string Email { get; set; }

    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[\W_]).{8,12}$",
        ErrorMessage = "Þifre 8-12 karakter arasý olmalý; en az bir harf, bir rakam ve bir özel karakter içermelidir.")]
    public string Password { get; set; } // Güncellemede þifre boþ geçilebilir, doluysa kurala uymak zorundadýr

    [Required]
    public string Role { get; set; }
}