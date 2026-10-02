using Triso.Domain.Entities;
namespace Triso.Application.Ports.Security;
public interface IUserPasswordHasher { string Hash(User user, string password); }
