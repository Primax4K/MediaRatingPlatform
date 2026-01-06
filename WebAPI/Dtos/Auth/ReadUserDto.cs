namespace WebAPI.Dtos.Auth;

public class ReadUserDto {
	public Guid Id { get; init; }
	public string Username { get; set; } = null!;
	public DateTimeOffset CreatedAt { get; init; } 
}