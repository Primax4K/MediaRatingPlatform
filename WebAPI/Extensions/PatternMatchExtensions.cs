namespace WebAPI.Extensions;

public static class PatternMatchExtensions {
	public static bool MatchesPattern(this string pattern, string path) {
		var p = pattern.Split('/');
		var s = path.Split('/');

		if (p.Length != s.Length) return false;

		for (int i = 0; i < p.Length; i++) {
			if (p[i].IsParamSegment()) continue;
			if (!string.Equals(p[i], s[i], StringComparison.OrdinalIgnoreCase)) return false;
		}

		return true;
	}

	public static Dictionary<string, string> ExtractParameters(this string pattern, string path) {
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var p = pattern.Split('/');
		var s = path.Split('/');

		if (p.Length != s.Length) return result;

		for (int i = 0; i < p.Length; i++) {
			if (!p[i].IsParamSegment()) continue;
			result[p[i].Trim('{', '}')] = s[i];
		}

		return result;
	}
}