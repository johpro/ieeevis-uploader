/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using IeeeVisUploaderWebApp.Models;

namespace IeeeVisUploaderWebApp.Helpers
{
    public static class HelperMethods
    {
        public static string GetEventFromUid(string? uid)
        {
            if (string.IsNullOrEmpty(uid))
                return "";
            var idx = uid.LastIndexOfAny(new []{'_', '-'});
            if (idx == -1)
                return "";
            return uid.Substring(0, idx);

        }

        public static string GetEventFromUid(string? uid, out string typePrefix)
        {
            typePrefix = "";
            if (string.IsNullOrEmpty(uid))
                return "";
            var res = GetEventFromUid(uid);
            var idx = uid.IndexOf('-');
            if (idx != -1)
                typePrefix = uid.Substring(0, idx);
            return res;
        }


        public static List<CollectedFile> EnsureCollectedFiles(string uid)
        {
            var files = DataProvider.CollectedFiles.GetCollectedFilesCopy(uid);
            if (files.Count != 0)
                return files;

            //check validity / format of uid
            if (string.IsNullOrEmpty(uid) || uid.Count(ch => ch == '-' || ch == '_') != 2)
                return files;
            var idx = uid.LastIndexOfAny(new[] { '_', '-' });
            var numberStr = idx == -1 ? "" : uid.Substring(idx+1);
            if (string.IsNullOrEmpty(numberStr) || numberStr.Any(ch => !char.IsDigit(ch)))
                return files;

            if (!DataProvider.Events.TryGetValue(GetEventFromUid(uid, out var typePrefix),
                    out var eventItem) &&
                !DataProvider.Events.TryGetValue(typePrefix, out eventItem))
            {
                return files;
            }

            foreach (var typeId in eventItem.FilesToCollect)
            {
                var ftd = DataProvider.FileTypes[typeId];
                files.Add(new CollectedFile(uid, typeId, ftd.Name ?? ""));
            }
            DataProvider.CollectedFiles.SetFiles(uid, files);
            DataProvider.CollectedFiles.Save();
            return files;

        }

        public static List<(string uid, List<CollectedFile> files)> RetrieveCollectedFiles(UrlSigner signer, string uid, int expiryInHours = 1)
        {
            var items = new List<(string uid, List<CollectedFile> files)>();
            if (uid == "_")
            {
                //get all items
                var dict = DataProvider.CollectedFiles.GetDictionaryCopy();
                foreach (var (id, lst) in dict)
                {
                    items.Add((id, lst));
                }
            }
            else if (DataProvider.Events.ContainsKey(uid))
            {
                items = DataProvider.CollectedFiles.GetEventCollectedFilesCopy(uid);
            }
            else
            {
                var files = EnsureCollectedFiles(uid);
                items.Add((uid, files));
            }

            foreach ((_, List<CollectedFile> files) in items)
            {
                foreach (var f in files)
                {
                    if (string.IsNullOrEmpty(f.RawDownloadUrl))
                    {
                        f.DownloadUrl = null;
                        continue;
                    }

                    f.DownloadUrl = signer.SignBunnyUrl(f.RawDownloadUrl, DateTimeOffset.UtcNow.AddHours(expiryInHours));
                }
            }

            return items;
        }
    }
}
