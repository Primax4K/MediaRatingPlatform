namespace Model.Entities;

public class Rating {
	public Guid Id { get; init; } = Guid.NewGuid();
	public Guid MediaId { get; set; }
	public Guid UserId { get; set; }
	public short Stars { get; set; }
	public string? Comment { get; set; }
	public bool CommentConfirmed { get; set; }
	public DateTimeOffset CreatedAt { get; init; }
	public DateTimeOffset UpdatedAt { get; init; }
}