using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManagement.BLL.DTOs;
using SchoolInventoryManagement.BLL.Interfaces;
using SchoolInventoryManagement.DAL.Constants;
using SchoolInventoryManagement.DAL.Context;
using SchoolInventoryManagement.DAL.Entities;
using SchoolInventoryManagement.DAL.Entities.Enums;
using SchoolInventoryManagement.Web.Helpers;
using SchoolInventoryManagement.Web.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using QRCoder;

namespace SchoolInventoryManagement.Web.Controllers
{
    [Authorize] // any logged-in user can view; specific actions below tighten further
    public class AssetsController : BaseController
    {
        private readonly IAssetAssignmentService _assignmentService;
        private readonly IAssetMovementService _movementService;
        private readonly IAssetService _assetService;
        private readonly ApplicationDbContext _context; // dropdown lookups only
        private readonly IDisposalService _disposalService;

        public AssetsController(
            IAssetService assetService,
            IDisposalService disposalService,
            IAssetAssignmentService assignmentService,
            IAssetMovementService movementService,
            ApplicationDbContext context)
        {
            _assetService = assetService;
            _disposalService = disposalService;
            _assignmentService = assignmentService;
            _movementService = movementService;
            _context = context;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task PopulateDropdownsAsync()
        {
            ViewBag.Models = new SelectList(
                await _context.Models.OrderBy(m => m.ModelName).ToListAsync(),
                "ModelID", "ModelName");

            ViewBag.Locations = new SelectList(
                await _context.Locations.OrderBy(l => l.LocationName).ToListAsync(),
                "LocationID", "LocationName");

            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync(),
                "DepartmentID", "DepartmentName");

            ViewBag.Branches = new SelectList(
                await _context.Branches.OrderBy(b => b.BranchName).ToListAsync(),
                "BranchID", "BranchName");

            ViewBag.LocationsByBranch = await _context.Locations
                .OrderBy(l => l.LocationName)
                .Select(l => new LocationDTO
                {
                    LocationID = l.LocationID,
                    LocationName = l.LocationName,
                    Description = l.Description,
                    BranchID = l.BranchID,
                    BranchName = l.Branch.BranchName
                })
                .ToListAsync();

            ViewBag.DepartmentsByBranch = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .Select(d => new DepartmentDTO
                {
                    DepartmentID = d.DepartmentID,
                    DepartmentName = d.DepartmentName,
                    Description = d.Description,
                    BranchID = d.BranchID,
                    BranchName = d.Branch.BranchName
                })
                .ToListAsync();
        }

        // GET /Assets
        public async Task<IActionResult> Index(string? keyword, int? categoryId, int? modelId, AssetStatus? status)
        {
            var results = await _assetService.SearchAssetsAsync(
                keyword, categoryId, modelId, null, null, status, null);

            return View(results);
        }

        // GET /Assets/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset is null)
                return NotFound();

            return View(asset);
        }

        // GET /Assets/Create
        [Authorize(Roles = RoleNames.AssetOfficer + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            return View(new AssetCreateViewModel());
        }

        // POST /Assets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.AssetOfficer + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Create(AssetCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(model);
            }

            try
            {
                var savedPath = await SaveAssetImageAsync(model.ImageFile);
                var dto = new CreateAssetDTO
                {
                    AssetCode = model.AssetCode,
                    ModelID = model.ModelID,
                    AssetName = model.AssetName,
                    Description = model.Description,
                    SerialNumber = model.SerialNumber,
                    AcquisitionDate = model.AcquisitionDate,
                    AcquisitionCost = model.AcquisitionCost,
                    WarrantyInformation = model.WarrantyInformation,
                    ImageURL = savedPath,
                    QRCodeData = model.QRCodeData,
                    Condition = model.Condition,
                    CurrentLocationID = model.CurrentLocationID,
                    BranchID = model.BranchID
                };

                var created = await _assetService.CreateAssetAsync(dto, CurrentUserId);
                return RedirectToAction(nameof(Details), new { id = created.AssetID });
            }
            catch (Exception ex)
            {
                HandleServiceException(ex);
                await PopulateDropdownsAsync();
                return View(model);
            }
        }

        // GET /Assets/Edit/5
        [Authorize(Roles = RoleNames.AssetOfficer + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Edit(int id)
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset is null)
                return NotFound();

            if (asset.Status == AssetStatus.Assigned)
            {
                TempData["ErrorMessage"] = "This asset is currently assigned and cannot be edited until it's returned.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await PopulateDropdownsAsync();

            var model = new AssetEditViewModel
            {
                AssetID = asset.AssetID,
                AssetName = asset.AssetName,
                Description = asset.Description,
                SerialNumber = asset.SerialNumber,
                WarrantyInformation = asset.WarrantyInformation,
                ImageURL = asset.ImageURL,
                CurrentLocationID = asset.CurrentLocationID,
                BranchID = asset.BranchID, // NEW
                RowVersionBase64 = RowVersionHelper.ToBase64(asset.RowVersion)
            };

            return View(model);
        }

        // POST /Assets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.AssetOfficer + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Edit(int id, AssetEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(model);
            }

            try
            {
                var newPath = await SaveAssetImageAsync(model.ImageFile);
                var dto = new UpdateAssetDTO
                {
                    AssetName = model.AssetName,
                    Description = model.Description,
                    SerialNumber = model.SerialNumber,
                    WarrantyInformation = model.WarrantyInformation,
                    ImageURL = newPath ?? model.ImageURL,
                    CurrentLocationID = model.CurrentLocationID,
                    BranchID = model.BranchID,
                    RowVersion = RowVersionHelper.FromBase64(model.RowVersionBase64)
                };

                await _assetService.UpdateAssetAsync(id, dto, CurrentUserId);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                HandleServiceException(ex);
                await PopulateDropdownsAsync();
                return View(model);
            }
        }

        // GET /Assets/Dispose/5
        [Authorize(Roles = RoleNames.AssetOfficer + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Dispose(int id)
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset is null)
                return NotFound();

            var model = new DisposeAssetViewModel
            {
                AssetID = asset.AssetID,
                AssetRowVersionBase64 = RowVersionHelper.ToBase64(asset.RowVersion)
            };

            ViewBag.AssetName = asset.AssetName;
            ViewBag.AssetCode = asset.AssetCode;
            return View(model);
        }

        // POST /Assets/Dispose/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.AssetOfficer + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Dispose(int id, DisposeAssetViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var dto = new DisposeAssetDTO
                {
                    ReasonForDisposal = model.ReasonForDisposal,
                    DisposalMethod = model.DisposalMethod,
                    SupportingDocumentationURL = model.SupportingDocumentationURL,
                    Notes = model.Notes,
                    AssetRowVersion = RowVersionHelper.FromBase64(model.AssetRowVersionBase64)
                };

                await _disposalService.DisposeAssetAsync(id, dto, CurrentUserId);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                HandleServiceException(ex);
                return View(model);
            }
        }

        // POST /Assets/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.AssetOfficer + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Restore(int id, string rowVersionBase64)
        {
            try
            {
                var rowVersion = RowVersionHelper.FromBase64(rowVersionBase64);
                await _disposalService.RestoreAssetAsync(id, rowVersion, CurrentUserId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> History(int id)
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset is null)
                return NotFound();

            var model = new AssetHistoryViewModel
            {
                Asset = asset,
                Assignments = await _assignmentService.GetAssignmentHistoryForAssetAsync(id),
                Movements = await _movementService.GetMovementHistoryForAssetAsync(id),
                Disposals = await _disposalService.GetDisposalHistoryForAssetAsync(id),
                AuditEntries = await _context.AuditLogs
                    .Where(a => a.TargetAssetID == id)
                    .OrderByDescending(a => a.LogDateTime)
                    .ToListAsync()
            };

            return View(model);
        }

        private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        private async Task<string?> SaveAssetImageAsync(IFormFile? file)
        {
            if (file is null || file.Length == 0)
                return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new ArgumentException("Only JPG, PNG, or WEBP images are allowed.");
            if (file.Length > MaxImageBytes)
                throw new ArgumentException("Image must be under 5 MB.");

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var folder = Path.Combine("wwwroot", "images", "assets");
            Directory.CreateDirectory(folder);
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            return $"/images/assets/{fileName}";
        }
        // GET /Assets/QrCode/5 -- generates the PNG on demand, nothing stored.
        // Works identically for an asset created five minutes ago or five years ago.
        public async Task<IActionResult> QrCode(int id)
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset is null)
                return NotFound();

            // Encode a full URL, not just the bare code -- so any generic phone
            // camera app (not just this site's own scanner) opens straight to the
            // asset on scan, no app install required.
            var scanUrl = Url.Action(nameof(Scan), "Assets",
                new { code = asset.AssetCode }, Request.Scheme)!;

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(scanUrl, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(data).GetGraphic(20); // 20px per module

            return File(png, "image/png");
        }

        // GET /Assets/Scan?code=AST-0001 -- what the QR actually encodes.
        // Looked up by AssetCode (business key), not AssetID, so the QR keeps
        // working even if internal IDs were ever renumbered.
        public async Task<IActionResult> Scan(string code)
        {
            var results = await _assetService.SearchAssetsAsync(
                code, null, null, null, null, null, null);
            var asset = results.FirstOrDefault(a => a.AssetCode == code);

            if (asset is null)
            {
                TempData["ErrorMessage"] = $"No asset found for code '{code}'.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Details), new { id = asset.AssetID });
        }
    }
}