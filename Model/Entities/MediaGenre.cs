namespace Model.Entities;

public class MediaGenre {
	public Guid MediaId { get; set; }
	public Guid GenreId { get; set; }

	public Media? Media { get; set; }
	public Genre? Genre { get; set; }
}