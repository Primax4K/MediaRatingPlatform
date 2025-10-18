namespace WebAPI.Extensions;

public static class SegmentExtensions {
	public static bool IsParamSegment(this string segment) =>
		segment.Length >= 2 && segment[0] == '{' && segment[^1] == '}';
}