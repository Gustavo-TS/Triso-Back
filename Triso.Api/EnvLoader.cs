public static class EnvLoader
{
    public static void Load() { var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); while (directory is not null) { var path = Path.Combine(directory.FullName, ".env"); if (File.Exists(path)) { foreach (var raw in File.ReadLines(path)) { var line = raw.Trim(); if (line.Length == 0 || line.StartsWith('#')) continue; var separator = line.IndexOf('='); if (separator <= 0) continue; Environment.SetEnvironmentVariable(line[..separator].Trim(), line[(separator + 1)..].Trim().Trim('"', '\'')); } return; } directory = directory.Parent; } }
}
