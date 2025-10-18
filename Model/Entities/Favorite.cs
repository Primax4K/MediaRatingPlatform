namespace Model.Entities;

public class Favorite {
	public Guid UserId { get; set; }
	public Guid MediaId { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
}