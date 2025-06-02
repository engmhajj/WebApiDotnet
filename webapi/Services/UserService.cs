using webapi.Models;
using webapi.Models.Dtos;
using webapi.Security;

public class UserService
{
    private readonly UserRepository _repo;

    public UserService(UserRepository repo)
    {
        _repo = repo;
    }

    public async Task<LoginResult> AuthenticateUserAsync(string username, string password)
    {
        var user = await _repo.GetByUsernameAsync(username); // ✅ Use repo method
        if (user == null)
        {
            return new LoginResult { Success = false, ErrorMessage = "User not found." };
        }

        bool isValid = SecretHasher.VerifySecret(password, user.PasswordSalt!, user.PasswordHash!);
        if (!isValid)
        {
            return new LoginResult { Success = false, ErrorMessage = "Invalid credentials." };
        }
        return new LoginResult { Success = true, User = user };
    }

    public async Task<User?> RegisterUserAsync(RegisterUserDto dto)
    {
        if (await _repo.ExistsByUsernameAsync(dto.Username))
            return null;

        if (!string.IsNullOrEmpty(dto.Email) && await _repo.ExistsByEmailAsync(dto.Email))
            return null; // Or handle this differently

        var (salt, hash) = SecretHasher.HashSecret(dto.Password);

        var user = new User
        {
            Username = dto.Username,
            PasswordSalt = salt,
            PasswordHash = hash,
            Email = dto.Email,
            Roles = dto.Roles,
        };

        await _repo.AddAsync(user);

        return user;
    }

    public async Task<RegistrationResult> RegisterUserDetailedAsync(RegisterUserDto dto)
    {
        if (await _repo.ExistsByUsernameAsync(dto.Username))
            return new RegistrationResult
            {
                Success = false,
                ErrorMessage = "Username already exists.",
            };

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _repo.ExistsByEmailAsync(dto.Email))
            return new RegistrationResult
            {
                Success = false,
                ErrorMessage = "Email is already in use.",
            };

        var (salt, hash) = SecretHasher.HashSecret(dto.Password);

        var user = new User
        {
            Username = dto.Username,
            PasswordSalt = salt,
            PasswordHash = hash,
            Email = dto.Email,
            Roles = dto.Roles,
        };

        await _repo.AddAsync(user);

        return new RegistrationResult { Success = true, User = user };
    }
}
