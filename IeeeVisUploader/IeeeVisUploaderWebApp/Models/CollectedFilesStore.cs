/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace IeeeVisUploaderWebApp.Models
{
    public class CollectedFilesStore
    {

        private readonly object _lck = new();
        private readonly object _saveLck = new();
        private readonly string _fileName;

        private readonly Dictionary<string, List<CollectedFile>> _filesPerPaper = new();
        private long _version;
        private bool _savingFailed;


        public CollectedFilesStore(string fileName)
        {
            _fileName = fileName;
            if (File.Exists(fileName))
            {
                foreach (var l in File.ReadLines(fileName))
                {
                    var f = JsonSerializer.Deserialize<CollectedFile>(l);
                    if (f == null)
                        continue;
                    ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_filesPerPaper, f.ParentUid, out _);
                    if (list == null)
                        list = new List<CollectedFile> { f };
                    else
                        list.Add(f);
                }
            }
        }

        public Dictionary<string, List<CollectedFile>> GetDictionaryCopy()
        {
            lock (_lck)
            {
                return _filesPerPaper.ToDictionary(k => k.Key, k => k.Value.Select(it => it.Clone()).ToList());
            }
        }

        public List<CollectedFile> GetCollectedFilesCopy(string uid)
        {
            lock (_lck)
            {
                _filesPerPaper.TryGetValue(uid, out var l);
                if (l == null)
                    return new();
                return l.Select(it => it.Clone()).ToList();
            }
        }

        public CollectedFile? GetCollectedFileCopy(string uid, string itemId)
        {
            lock (_lck)
            {
                _filesPerPaper.TryGetValue(uid, out var l);
                return l?.FirstOrDefault(it => it.FileTypeId == itemId).Clone();
            }
        }

        public List<(string uid, List<CollectedFile> files)> GetEventCollectedFilesCopy(string eventId)
        {
            var res = new List<(string uid, List<CollectedFile> files)>();
            lock (_lck)
            {
                foreach (var (uid, l) in _filesPerPaper)
                {
                    if(!uid.StartsWith(eventId))
                        continue;
                    var pos = eventId.Length;
                    if(uid.Length <= pos)
                        continue;
                    var ch = uid[pos];
                    if (ch != '-' && ch != '_')
                        continue;
                    var files = l.Select(it => it.Clone()).ToList();
                    res.Add((uid, files));
                }
            }

            return res;
        }

        public List<CollectedFile> GetAllCollectedFilesCopy()
        {
            lock (_lck)
            {
                var res = new List<CollectedFile>();
                foreach (var l in _filesPerPaper.Values)
                {
                    foreach (var f in l)
                    {
                        res.Add(f.Clone());
                    }
                }

                return res;
            }
        }

        public void InsertOrUpdate(CollectedFile file)
        {
            var f = file.Clone();
            lock (_lck)
            {
                ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_filesPerPaper, f.ParentUid, out _);
                if (list == null)
                {
                    list = new List<CollectedFile> { f };
                    return;
                }

                var updated = false;
                for (int i = 0; i < list.Count; i++)
                {
                    var ef = list[i];
                    if (ef.Name == f.Name)
                    {
                        list[i] = f;
                        updated = true;
                        break;
                    }
                }
                if (!updated)
                    list.Add(f);
            }
        }

        public bool DeleteUid(string uid, bool onlyIfNoUploads = true)
        {
            lock (_lck)
            {
                ref var list = ref CollectionsMarshal.GetValueRefOrNullRef(_filesPerPaper, uid);
                if (Unsafe.IsNullRef(ref list))
                {
                    return false;
                }

                if (onlyIfNoUploads && list is { Count: > 0 })
                {
                    foreach (var f in list)
                    {
                        if (!string.IsNullOrEmpty(f.RawDownloadUrl))
                            return false;
                    }
                }

                _filesPerPaper.Remove(uid);
                return true;
            }
        }

        public void SetFiles(string uid, List<CollectedFile> files)
        {
            files = files.Select(f => f.Clone()).ToList();
            lock (_lck)
            {
                _filesPerPaper[uid] = files;
            }
        }

        public void EnsureStoreIsOnDisk()
        {
            lock (_lck)
            {
                if (!_savingFailed)
                    return;
            }

            Save();
        }


        public void Save()
        {

            var tmpFn = _fileName + Guid.NewGuid().ToString("N");
            long version;
            List<CollectedFile> files;
            lock (_lck)
            {
                files = GetAllCollectedFilesCopy();
                version = ++_version;
            }

            try
            {
                File.WriteAllLines(tmpFn,
                    files.Select(f => JsonSerializer.Serialize(f, JsonSerializerOptions.Default)));


                lock (_lck)
                {
                    if (_version == version)
                    {
                        File.Move(tmpFn, _fileName, true);
                        _savingFailed = false;
                    }
                }
            }
            catch (Exception)
            {
                lock (_lck)
                {
                    _savingFailed = true;
                }

                throw;
            }
            finally
            {
                try
                {
                    if (File.Exists(tmpFn))
                        File.Delete(tmpFn);
                }
                catch (Exception)
                {
                }
            }

        }
    }
}
