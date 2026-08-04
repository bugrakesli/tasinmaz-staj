using System.ComponentModel.DataAnnotations;

public class UserFilterDto
{
    // REQ-9: Sayfalama zorunluluğu
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class UserCreateDto
{
    [Required]
    public string Email { get; set; }

    // REQ-10: Şifre kuralları validasyonu
    [Required]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[\W_]).{8,12}$",
        ErrorMessage = "Şifre 8-12 karakter arası olmalı; en az bir harf, bir rakam ve bir özel karakter içermelidir.")]
    public string Password { get; set; }

    [Required]
    public string Role { get; set; } // "Admin" veya "User"
}

public class UserUpdateDto
{
    [Required]
    public string Email { get; set; }

    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[\W_]).{8,12}$",
        ErrorMessage = "Şifre 8-12 karakter arası olmalı; en az bir harf, bir rakam ve bir özel karakter içermelidir.")]
    public string Password { get; set; } // Güncellemede şifre boş geçilebilir, doluysa kurala uymak zorundadır

    [Required]
    public string Role { get; set; }
}