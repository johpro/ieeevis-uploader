/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
using System.Diagnostics;
using System.Net;
using IeeeVisUploaderWebApp.Helpers;
using IeeeVisUploaderWebApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IeeeVisUploaderWebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        [BindProperty(SupportsGet = true)]
        public string? Auth { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? Action { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? Uid { get; set; }

        public List<(string uid, List<CollectedFile> files)> Items { get; set; } = new();
        

        private readonly UrlSigner _signer = new();

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            if (Uid == null || Auth == null || !UrlSigner.SafeCompareEquality(Auth, _signer.GetUrlAuth(Action, Uid)))
            {
                return BadRequest();
            }

            Items = HelperMethods.RetrieveCollectedFiles(_signer, Uid);
            try
            {
                DataProvider.CollectedFiles.EnsureStoreIsOnDisk();
            }
            catch (Exception )
            {
                
            }

            return Page();
        }



    }
}