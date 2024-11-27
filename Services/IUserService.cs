public interface IUserService
{
    Task<User> RegisterAsync(string username, string password);
    Task<User> AuthenticateAsync(string username, string password);
}