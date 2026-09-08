using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PotyInternosAPI.Data;
using PotyInternosAPI.DTOs.Auth;
using PotyInternosAPI.Exceptions;
using PotyInternosAPI.Models;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(
        AppDbContext context,
        IPasswordHasher<Usuario> passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.NomeUsuario == dto.Usuario);

        if (usuario is null)
        {
            throw new UnauthorizedException("Usuário ou senha inválidos.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(usuario, usuario.Senha, dto.Senha);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Usuário ou senha inválidos.");
        }

        if (!usuario.Status)
        {
            throw new UnauthorizedException("Usuário inativo.");
        }

        if (!usuario.IsAdmin)
        {
            throw new UnauthorizedException("Usuário não possui permissão de administrador.");
        }

        var (token, expiresAt) = GenerateToken(usuario);

        return new LoginResponseDto
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            UsuarioId = usuario.UsuarioId,
            Nome = usuario.Nome,
            Usuario = usuario.NomeUsuario,
            IsAdmin = usuario.IsAdmin
        };
    }

    private (string token, DateTime expiresAt) GenerateToken(Usuario usuario)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection["Key"]
            ?? throw new InvalidOperationException("A chave JWT (Jwt:Key) não está configurada.");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var expiresMinutes = int.TryParse(jwtSection["ExpiresMinutes"], out var minutes) ? minutes : 120;

        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.UsuarioId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.UniqueName, usuario.NomeUsuario),
            new(ClaimTypes.Name, usuario.NomeUsuario),
            new(ClaimTypes.NameIdentifier, usuario.UsuarioId),
            new("isAdmin", usuario.IsAdmin.ToString().ToLowerInvariant())
        };

        if (usuario.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, expiresAt);
    }
}
