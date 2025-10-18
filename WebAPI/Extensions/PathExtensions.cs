namespace WebAPI.Extensions;

public static class PathExtensions {
	public static string NormalizePath(this string? path) {
		if (string.IsNullOrEmpty(path)) return "/";
		if (!path!.StartsWith("/")) path = "/" + path;
		if (path.Length > 1 && path.EndsWith("/")) path = path.TrimEnd('/');
		return path;
	}

	public static string? GetRelativePath(this string requestPath, string basePath) {
		if (basePath == "/") return requestPath;
		if (!requestPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) return null;

		var relative = requestPath.Substring(basePath.Length);
		if (string.IsNullOrEmpty(relative) || relative == "/") return "/";
		return relative.NormalizePath();
	}
}