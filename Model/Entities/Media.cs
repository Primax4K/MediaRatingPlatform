namespace Model.Entities;

public class Media {
	public Guid Id { get; init; }  = Guid.NewGuid();
	public string Title { get; set; } = null!;
	public string Description { get; set; } = null!;
	public string Type { get; set; } = null!;
	public int ReleaseYear { get; set; }
	public short AgeRestriction { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset CreatedAt { get; init; }
	public DateTimeOffset UpdatedAt { get; init; } 
	public double AverageStars { get; set; }
	public int RatingsCount { get; set; }
}