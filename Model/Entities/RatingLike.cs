namespace Model.Entities;

public class RatingLike {
	public Guid RatingId { get; set; }
	public Guid UserId { get; set; }
	public DateTimeOffset CreatedAt { get; init; }
}