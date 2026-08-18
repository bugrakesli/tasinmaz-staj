using System;

// Logout sirasinda gecersiz kilinan JWT'lerin "jti" (JWT ID) kaydi.
// JWT stateless oldugu icin logout, token'in kendisini iptal edemez;
// bunun yerine token'in benzersiz kimligini (jti) kara listeye alip
// JwtBearerEvents.OnTokenValidated icinde kontrol ediyoruz (bkz. Startup.cs).
public class RevokedToken
{
    public int Id { get; set; }
    public string Jti { get; set; }
    public int UserId { get; set; }
    public DateTime RevokedAt { get; set; }

    // Token zaten dogal olarak bu tarihte sona erecekti; blacklist
    // kaydini bu tarihten sonra temizlemek/gormezden gelmek guvenlidir.
    public DateTime ExpiresAt { get; set; }
}
