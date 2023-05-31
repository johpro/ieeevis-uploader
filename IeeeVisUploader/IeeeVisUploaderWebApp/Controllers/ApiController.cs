/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
using System.Diagnostics;
using System.Net;
using BunnyCDN.Net.Storage;
using Flurl;
using IeeeVisUploaderWebApp.Helpers;
using IeeeVisUploaderWebApp.Models;
using IeeeVisUploaderWebApp.Pages;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace IeeeVisUploaderWebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : Controller
    {
        public IActionResult Index()
        {
            return new EmptyResult();
        }

        private readonly UrlSigner _signer = new();
        private readonly FileChecker _fileChecker = new();

        private readonly BunnyCDNStorage _storage = new(DataProvider.Settings.BunnyStorageZoneName,
            DataProvider.Settings.BunnyAccessKey);

        private readonly HttpClient _generalBunnyClient;
        private readonly HttpClient _httpClient;

        private static readonly HashSet<string> CurrentlyProcessing = new();
        private readonly ILogger<IndexModel> _logger;
        private readonly string _downloadFolderUrl;

        public ApiController(ILogger<IndexModel> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.Timeout = new TimeSpan(0, 0, 120);
            
            _generalBunnyClient = new HttpClient();
            _generalBunnyClient.Timeout = new TimeSpan(0, 0, 120);
            _generalBunnyClient.DefaultRequestHeaders.Add("AccessKey", DataProvider.Settings.BunnyUserApiKey ?? "");

            _downloadFolderUrl = $"https://storage.bunnycdn.com/{DataProvider.Settings.BunnyStorageZoneName}/?AccessKey={DataProvider.Settings.BunnyAccessKey}";
        }

        public async Task<HttpResponseMessage> PurgeCache(string url)
        {
            var uri = new Url("https://api.bunny.net/purge");
            uri.SetQueryParam("url", url);
            var postUrl = uri.ToString();
            return await _generalBunnyClient.PostAsync(postUrl, null);
        }


        [HttpGet]
        [Route("download/{expiry}/{auth}/{uid}")]
        public async Task DownloadFolder(string uid, long expiry, string auth)
        {

            if (DateTimeOffset.FromUnixTimeSeconds(expiry) < DateTimeOffset.UtcNow)
            {
                HttpContext.Response.StatusCode = 400;
                HttpContext.Abort();
                return;
            }
            var authCorrect = _signer.GetUrlAuth(uid, ":download-folder:", expiry);
            if (!UrlSigner.SafeCompareEquality(authCorrect, auth))
            {
                HttpContext.Response.StatusCode = 400;
                HttpContext.Abort();
                return;
            }

            var rootPath =
                $"/{DataProvider.Settings.BunnyStorageZoneName}/{DataProvider.Settings.BunnyBasePath.Trim('/')}/";
            string path;
            var isEvent = uid.IndexOf('_') == -1;
            if (isEvent)
            {
                path = $"{rootPath}{uid}/";
            }
            else
            {
                rootPath += HelperMethods.GetEventFromUid(uid) + "/";
                path = $"{rootPath}{uid}/";
            }

            var reqBody = new { RootPath = rootPath, Paths = new[] { path } };
            var resp = await _httpClient.PostAsJsonAsync(_downloadFolderUrl, reqBody, HttpContext.RequestAborted);
            if (HttpContext.RequestAborted.IsCancellationRequested)
                return;
            await HttpContext.ProxyForwardHttpResponse(resp, new []{ ("content-disposition", $"attachment; filename={uid}.zip") });
        }


        private async Task<bool> DeleteFile(CollectedFile collF)
        {
            var baseUrl = DataProvider.Settings.BunnyCdnRootUrl;
            var path = collF.RawDownloadUrl[baseUrl.Length..];
            var targetPath = $"/{DataProvider.Settings.BunnyStorageZoneName}/{path.TrimStart('/')}";
            var success = await _storage.DeleteObjectAsync(targetPath);
            if (!success)
                return false;
            collF.Errors?.Clear();
            collF.Warnings?.Clear();
            collF.FileSize = 0;
            collF.IsPresent = false;
            collF.Checksum = null;
            collF.DownloadUrl = null;
            collF.RawDownloadUrl = null;
            collF.LastUploaded = null;
            collF.LastChecked = null;

            DataProvider.CollectedFiles.InsertOrUpdate(collF);
            DataProvider.CollectedFiles.Save();
            return true;
        }


        [HttpGet]
        [Route("urls/{auth}/{uid}")]
        public IActionResult GetUrls(string auth, string uid)
        {
            var authCorrect = _signer.GetUrlAuth("urls", "");
            if (!UrlSigner.SafeCompareEquality(authCorrect, auth))
            {
                return new BadRequestResult();
            }

            if (char.IsDigit(uid[^1]))
                HelperMethods.EnsureCollectedFiles(uid);

            var previewItemTypes = new[] { "video-ff", "video-ff-subs" };
            var previewItemTypesS = string.Join('|', previewItemTypes);
            var uploadAuth = _signer.GetUrlAuth("upload", uid);
            var getAuth = _signer.GetUrlAuth("get", uid);
            var itemsAuth = _signer.GetUrlAuth("api-items", uid);
            var previewItemsAuth = _signer.GetUrlAuth("api-items" + previewItemTypesS, uid);
            var reqUrl = HttpContext.Request.GetDisplayUrl();
            var idx = reqUrl?.IndexOf("Api/urls", StringComparison.InvariantCultureIgnoreCase) ?? -1;
            var baseUrl = "/";
            if (idx != -1)
            {
                baseUrl = reqUrl[..idx];
            }

            return Json(new
            {
                uploadUrl = $"{baseUrl}{uploadAuth}/upload/{uid}",
                retrieveUrl = $"{baseUrl}{getAuth}/get/{uid}",
                itemsUrl = $"{baseUrl}Api/items/{itemsAuth}/{uid}",
                previewItemsUrl = $"{baseUrl}Api/items/{previewItemsAuth}/{uid}?{string.Join('&', previewItemTypes.Select(s => "ft=" + s))}"
            });
        }

        [HttpGet]
        [Route("items/{auth}/{uid}")]
        public IActionResult GetItems(string auth, string uid, [FromQuery] string[]? ft = null)
        {
            var ftS = string.Empty;
            if (ft != null)
            {
                ftS = string.Join('|', ft);
            }
            if (!UrlSigner.SafeCompareEquality(auth, _signer.GetUrlAuth("api-items" + ftS, uid)))
            {
                return BadRequest();
            }

            var items = HelperMethods.RetrieveCollectedFiles(_signer, uid, 24);
            var res = items.Select(t => new
            {
                uid = t.uid,
                items = t.files
                    .Where(it => ft == null || 
                                 ft.Any(s => s.Equals(it.FileTypeId, StringComparison.Ordinal)))
                    .Select(it => new
                {
                    name = it.Name,
                    fileName = it.FileName,
                    isPresent = it.IsPresent,
                    fileSize = it.FileSize,
                    checksum = it.Checksum,
                    lastUploaded = it.LastUploaded?.ToString("u"),
                    lastChecked = it.LastChecked?.ToString("u"),
                    url = it.DownloadUrl,
                    numErrors = it.Errors?.Count ?? 0,
                    numWarnings = it.Warnings?.Count ?? 0,
                    errors = it.Errors ?? new(),
                    warnings = it.Warnings ?? new(),
                })
            }).ToArray();
            return Json(res);
        }


        [HttpPost]
        [Route("delete/{uid}/{itemId}/{expiry}/{auth}")]
        public async Task<IActionResult> DeleteFile(string uid, string itemId, long expiry, string auth)
        {

            if (DateTimeOffset.FromUnixTimeSeconds(expiry) < DateTimeOffset.UtcNow)
                return new BadRequestResult();
            var authCorrect = _signer.GetUrlAuth(uid, itemId, expiry);
            if (!UrlSigner.SafeCompareEquality(authCorrect, auth))
            {
                return new BadRequestResult();
            }
            
            var lckKey = uid + itemId;
            lock (CurrentlyProcessing)
            {
                if (!CurrentlyProcessing.Add(lckKey))
                    return Fail("concurrent action already in progress");
            }

            try
            {
                var collF = DataProvider.CollectedFiles.GetCollectedFileCopy(uid, itemId);
                if (collF == null)
                    return Fail("requested file could not be found");

                var baseUrl = DataProvider.Settings.BunnyCdnRootUrl;
                if (collF.RawDownloadUrl == null || collF.RawDownloadUrl.Length <= baseUrl.Length)
                {
                    return Fail("internal error");
                }
                if(!await DeleteFile(collF))
                    return Fail("delete was not successful");
                return Json(new { statusCode = 200 });
            }
            finally
            {
                lock (CurrentlyProcessing)
                {
                    CurrentlyProcessing.Remove(lckKey);
                }
            }
        }



        [HttpPost]
        [DisableFormValueModelBinding]
        [Route("upload/{uid}/{itemId}/{expiry}/{auth}")]
        public async Task<IActionResult> UploadFile(string uid, string itemId, long expiry, string auth)
        {

            var request = HttpContext.Request;

            if (!request.HasFormContentType ||
                !MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaTypeHeader) ||
                string.IsNullOrEmpty(mediaTypeHeader.Boundary.Value))
            {
                return new UnsupportedMediaTypeResult();
            }

            if (DateTimeOffset.FromUnixTimeSeconds(expiry) < DateTimeOffset.UtcNow)
                return new BadRequestResult();
            var authCorrect = _signer.GetUrlAuth(uid, itemId, expiry);
            if (!UrlSigner.SafeCompareEquality(authCorrect, auth))
            {
                return new BadRequestResult();
            }

            if (!DataProvider.FileTypes.TryGetValue(itemId, out var fileTypeDesc))
                return new BadRequestResult();


            var lckKey = uid + itemId;
            lock (CurrentlyProcessing)
            {
                if (!CurrentlyProcessing.Add(lckKey))
                    return Fail("concurrent upload already in progress");
            }

            try
            {
                var reader = new MultipartReader(mediaTypeHeader.Boundary.Value, request.Body);
                var section = await reader.ReadNextSectionAsync();
                
                while (section != null)
                {
                    var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition,
                        out var contentDisposition);

                    if (hasContentDispositionHeader && contentDisposition.DispositionType.Equals("form-data") &&
                        !string.IsNullOrEmpty(contentDisposition.FileName.Value))
                    {
                        var fn = contentDisposition.FileName.Value;
                        if (string.IsNullOrWhiteSpace(fn))
                            return Fail("no file name provided");
                        var dotIdx = fn.LastIndexOf('.');
                        if (dotIdx == -1)
                            return Fail("missing file extension");
                        var extension = fn.Substring(dotIdx + 1).ToLowerInvariant();
                        if (fileTypeDesc.FileExtensions != null && fileTypeDesc.FileExtensions.All(s => s != extension))
                            return Fail("invalid file extension");
                        var fileName = Path.GetRandomFileName();
                        var saveToPath = Path.Combine(Path.GetTempPath(), fileName);
                        var targetFn = $"{uid}_{fileTypeDesc.FileName}.{extension}";
                        var eventId = HelperMethods.GetEventFromUid(uid);
                        var targetUrlPath =
                            $"/{DataProvider.Settings.BunnyBasePath?.Trim('/', '\\')}/{eventId}/{uid}/{targetFn}";
                        var targetPath = $"/{DataProvider.Settings.BunnyStorageZoneName}{targetUrlPath}";

                        try
                        {
                            using (var targetStream = System.IO.File.Create(saveToPath))
                            {
                                await section.Body.CopyToAsync(targetStream);
                            }
                            var fileSize = new FileInfo(saveToPath).Length;
                            _logger.LogInformation($"{uid} file upload for {itemId} with size {fileSize}");
                            if (fileTypeDesc.CheckInfo != null)
                            {
                                if (fileSize > fileTypeDesc.CheckInfo.MaxFileSize)
                                    return Fail("the file is too big");
                                if (fileSize < fileTypeDesc.CheckInfo.MinFileSize)
                                    return Fail("the file is too small");
                            }
                            var checksum = _signer.GetFileSha256Checksum(saveToPath);

                            var collF = DataProvider.CollectedFiles.GetCollectedFileCopy(uid, fileTypeDesc.Id);
                            if (collF != null && !string.IsNullOrWhiteSpace(collF.RawDownloadUrl))
                            {
                                try
                                {
                                    if (!await DeleteFile(collF))
                                        throw new Exception("delete operation did not return successfully");
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(e, "error while deleting old version of file");
                                }
                            }


                            collF = new CollectedFile(uid, fileTypeDesc.Id, fileTypeDesc.Name ?? "")
                            {
                                FileName = targetFn,
                                IsPresent = true,
                                Checksum = checksum,
                                LastUploaded = DateTime.UtcNow,
                                FileSize = fileSize,
                                Errors = new(),
                                Warnings = new(),
                                RawDownloadUrl = $"{DataProvider.Settings.BunnyCdnRootUrl?.TrimEnd('/')}{targetUrlPath}"
                            };

                            try
                            {
                                await _storage.UploadAsync(saveToPath, targetPath, true, checksum);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "error while transferring file");
                                return Fail("An internal error occurred while transferring the received file.");
                            }

                            if (!string.IsNullOrEmpty(DataProvider.Settings.BunnyUserApiKey))
                            {
                                try
                                {
                                    var resp = await PurgeCache(collF.RawDownloadUrl);
                                    resp.EnsureSuccessStatusCode();
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(e, "error while purging cache of file");
                                }
                            }


                            if (fileTypeDesc.PerformChecks)
                            {
                                try
                                {
                                    _fileChecker.PerformChecks(saveToPath, collF, fileTypeDesc);
                                }
                                catch (Exception e)
                                {
                                    collF.Errors ??= new();
                                    collF.Errors.Add($"something went wrong while checking the file");
                                    _logger.LogError(e, "error while performing checks");
                                }
                                collF.LastChecked = DateTime.UtcNow;
                            }

                            DataProvider.CollectedFiles.InsertOrUpdate(collF);
                            DataProvider.CollectedFiles.Save();
                            collF.DownloadUrl =
                                _signer.SignBunnyUrl(collF.RawDownloadUrl, DateTimeOffset.UtcNow.AddHours(1));
                        }
                        finally
                        {
                            try
                            {
                                if (System.IO.File.Exists(saveToPath))
                                    System.IO.File.Delete(saveToPath);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        return Json(new { statusCode = 200 });
                    }

                    section = await reader.ReadNextSectionAsync();
                }

                return Fail("No files data in the request.");
            }
            finally
            {
                lock (CurrentlyProcessing)
                {
                    CurrentlyProcessing.Remove(lckKey);
                }
            }

        }

        public IActionResult Fail(string message, int statusCode = 400)
        {
            HttpContext.Response.StatusCode = statusCode;
            return Json(new { statusCode = statusCode, errorMessage = message });
        }
    }
}
