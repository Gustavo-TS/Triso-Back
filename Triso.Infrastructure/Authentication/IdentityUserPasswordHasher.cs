using Microsoft.AspNetCore.Identity;
using Triso.Application.Ports.Security;
using Triso.Domain.Entities;
namespace Triso.Infrastructure.Authentication;
public sealed class IdentityUserPasswordHasher : IUserPasswordHasher
{
    public string Hash(User user, string password) => new PasswordHasher<User>().HashPassword(user, password);
}
