using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IeeeVisUploaderWebApp.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        public static string GetBunnyUrl(Item item)
        {
            var url = item.url;
            if (string.IsNullOrEmpty(url))
                return "";
            url = url[..url.IndexOf('?')];
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
