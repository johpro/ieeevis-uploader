using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using BunnyCDN.Net.Storage;
using Flurl;
using IeeeVisUploaderWebApp.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubtitlesConverterApp;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace IeeeVisUploaderWebApp.Tests.Helpers
{
    /// <summary>
    /// No unit tests but helper functions to export upload urls
    /// </summary>
    [TestClass]
    public class ExportHelpers
    {


        private static DateTimeOffset Expiry = new(new DateTime(2024, 1, 30));
        public static UrlSigner Signer = new("", File.ReadAllText("sign-key.txt"));

        public static async Task<ItemKeyValuePair[]> GetItems()
        {
            var url = await File.ReadAllTextAsync("bunny-items.txt");
            var json = await new HttpClient().GetStringAsync(url);
            return JsonSerializer.Deserialize<ItemKeyValuePair[]>(json);
        }

        public static BunnyCDNStorage GetBunnyCdnStorage()
        {
            var auth = File.ReadAllLines("bunny-storage.txt");
            return new BunnyCDNStorage(auth[0], auth[1]);
        }

        public static string GetBunnyUrl(Item item)
        {
            var url = item.url;
            if (string.IsNullOrEmpty(url))
                return "";
            url = url[..url.IndexOf('?')];
            if (item.name.Contains("Subtitles"))
            {
                url = url[..^3] + "vtt";
            }
            return Signer.SignBunnyUrl(url, Expiry);
        }

        [TestMethod]
        public async Task RetrieveBunnyContentUrls()
        {
            var items = await GetItems();
            foreach (var kv in items)
            {
                var uid = kv.uid;
                if (uid.StartsWith("v-test"))
                    continue;
                var dct = kv.items.ToDictionary(k => k.name, v => v);
                //UID	FF Video Bunny URL	FF Video Subtitles Bunny URL	Video Bunny URL	Video Subtitles Bunny URL
                Trace.WriteLine($"{uid}\t{GetBunnyUrl(dct["Video Preview"])}\t{GetBunnyUrl(dct["Video Preview Subtitles"])}\t{GetBunnyUrl(dct["Presentation Video"])}\t{GetBunnyUrl(dct["Presentation Video Subtitles"])}");
            }
        }

        public static IEnumerable<string> ReadLines(Stream stream)
        {
            using var r = new StreamReader(stream);
            while (r.ReadLine() is { } line)
            {
                yield return line;
            }

        }

        public static Stream GetStreamFromLines(IEnumerable<string> lines)
        {
            var stream = new MemoryStream();
            using var w = new StreamWriter(stream, null, -1, true);
            foreach (var l in lines)
            {
                w.WriteLine(l);
            }
            w.Flush();

            stream.Position = 0;
            return stream;
        }

        [TestMethod]
        public async Task ConvertSubtitles()
        {
            var storage = GetBunnyCdnStorage();
            var items = await GetItems();
            var prefixLength = "https://ieeevis-uploads.b-cdn.net".Length;
            const bool forceUpdate = false;
            foreach (var kv in items)
            {
                var firstUrl = kv.items.FirstOrDefault(it => !string.IsNullOrEmpty(it.url))?.url;
                if (firstUrl == null)
                    continue;
                firstUrl = firstUrl[..firstUrl.LastIndexOf('/')];
                var folderPath = "/ieeevis-uploads" + firstUrl[prefixLength..];
                var files = (await storage.GetStorageObjectsAsync(folderPath)) ?? throw new Exception("cannot find " + folderPath);
                foreach (var item in kv.items.Where(it => it.name.Contains("Subtitles", StringComparison.Ordinal)))
                {
                    var url = item.url;
                    if (string.IsNullOrEmpty(url))
                        continue;
                    url = url[..url.IndexOf('?')];
                    var path = "/ieeevis-uploads" + url[prefixLength..];
                    Trace.WriteLine("processing " + path);
                    var baseFile = files.SingleOrDefault(f => f.FullPath == path) ?? throw new Exception("cannot find " + path);
                    var targetPath = path[..^3] + "vtt";
                    var targetFile = files.SingleOrDefault(f => f.FullPath == targetPath);
                    if (!forceUpdate && targetFile != null && targetFile.LastChanged < baseFile.LastChanged)
                    {
                        Trace.WriteLine("    already up to date.");
                        continue;
                    }
                    await using var stream = await storage.DownloadObjectAsStreamAsync(path);
                    var lines = ReadLines(stream);
                    var targetLines = path.EndsWith(".srt", StringComparison.OrdinalIgnoreCase)
                        ? SubtitlesConverter.ConvertSrtToVtt(lines)
                        : SubtitlesConverter.ConvertSbvToVtt(lines);
                    using var uploadStream = GetStreamFromLines(targetLines);
                    await storage.UploadAsync(uploadStream, targetPath);
                    Trace.WriteLine("    uploaded.");
                }
            }
        }




    }



    public class ItemKeyValuePair
    {
        public string uid { get; set; }
        public Item[] items { get; set; }
    }

    public class Item
    {
        public string name { get; set; }
        public string fileName { get; set; }
        public bool isPresent { get; set; }
        public int fileSize { get; set; }
        public string checksum { get; set; }
        public string lastUploaded { get; set; }
        public string lastChecked { get; set; }
        public string url { get; set; }
        public int numErrors { get; set; }
        public int numWarnings { get; set; }
        public string[] errors { get; set; }
        public string[] warnings { get; set; }
    }

}
