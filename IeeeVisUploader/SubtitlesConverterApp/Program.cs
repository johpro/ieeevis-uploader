using SubtitlesConverterApp;

var path = args.FirstOrDefault();
if(string.IsNullOrEmpty(path))
{
    Console.WriteLine($"ERROR: no path to subtitles file provided.");
    return;
}

if (path.EndsWith(".sbv", StringComparison.OrdinalIgnoreCase))
{
    var targetPath = path[..^3] + "vtt";
    if (File.Exists(targetPath))
    {
        Console.WriteLine($"ERROR: target file {targetPath} exists already.");
    }

    File.WriteAllLines(targetPath, SubtitlesConverter.ConvertSbvToVtt(File.ReadLines(path)));
}
else if (path.EndsWith(".srt", StringComparison.OrdinalIgnoreCase))
{
    var targetPath = path[..^3] + "vtt";
    if (File.Exists(targetPath))
    {
        Console.WriteLine($"ERROR: target file {targetPath} exists already.");
    }

    File.WriteAllLines(targetPath, SubtitlesConverter.ConvertSrtToVtt(File.ReadLines(path)));
}
else
{
    Console.WriteLine($"ERROR: provided path has no known extension.");
}