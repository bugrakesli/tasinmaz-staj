using System.Collections.Generic;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; } // "Admin" veya "User"
    public ICollection<Property> Properties { get; set; }
    public string Salt { get; set; }
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; }
}