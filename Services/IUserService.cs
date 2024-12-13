public interface IUserService
{
    Task<User> RegisterAsync(string username, string password);
    Task<AuthResponse> AuthenticateAsync(string username, string password);
}