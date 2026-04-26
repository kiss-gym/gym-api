namespace GymApi.Domain.UserManagement;

public sealed class User
{
    public Guid Id { get; private init; }
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private User() { }

    public static User Create(Guid id, string email, string name)
    {
        return new User
        {
            Id = id,
            Email = email,
            Name = name
        };
    }
}
