using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolInventoryManagement.BLL.Interfaces;
using SchoolInventoryManagement.DAL.Constants;
using SchoolInventoryManagement.Web.ViewModels;

namespace SchoolInventoryManagement.Web.Controllers
{
    [Authorize] // any logged-in user, regardless of role
    public class HomeController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IAssetRequestService _requestService;

        public HomeController(IReportService reportService, IAssetRequestService requestService)
        {
            _reportService = reportService;
            _requestService = requestService;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Index()
        {
            var isReportsViewer =
                User.IsInRole(RoleNames.Administrator) ||
                User.IsInRole(RoleNames.Principal) ||
                User.IsInRole(RoleNames.AssetOfficer);

            var model = new HomeDashboardViewModel
            {
                IsReportsViewer = isReportsViewer,
                DisplayName = User.Identity?.Name?.Split(' ').FirstOrDefault() ?? "there"
            };

            if (isReportsViewer)
            {
                var summary = await _reportService.GetInventorySummaryAsync(CurrentUserId);

                model.TotalAssets = summary.TotalAssets;
                model.CurrentlyIssued = summary.AssignedCount;
                model.UnderMaintenanceCount = summary.UnderMaintenanceCount;
                model.ValueInService = summary.TotalValue;
                model.ByCategory = summary.ByCategory;

                var pending = await _requestService.GetPendingRequestsAsync(CurrentUserId);
                model.PendingRequestCount = pending.Count;

                model.RecentActivity = await _reportService.GetAuditTrailAsync(
                    fromDate: null, toDate: null, userId: null, actionContains: null,
                    maxRows: 6, actingUserId: CurrentUserId);
            }

            return View(model);
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult Privacy() { return View(); }
    }
}