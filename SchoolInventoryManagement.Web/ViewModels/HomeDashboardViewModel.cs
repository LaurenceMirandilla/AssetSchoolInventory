using System.Collections.Generic;
using SchoolInventoryManagement.BLL.DTOs;

namespace SchoolInventoryManagement.Web.ViewModels
{
    public class HomeDashboardViewModel
    {
        // False for roles that can't view reports (Teacher, Staff, Department
        // Head) — those get a simple fallback instead of this dashboard.
        public bool IsReportsViewer { get; set; }

        public string DisplayName { get; set; } = "";

        public int TotalAssets { get; set; }
        public int CurrentlyIssued { get; set; }
        public int UnderMaintenanceCount { get; set; }
        public decimal ValueInService { get; set; }

        public List<CountByLabelDTO> ByCategory { get; set; } = new();

        public int PendingRequestCount { get; set; }

        // Capped, most-recent-first. Reuses IReportService.GetAuditTrailAsync
        // rather than a new query.
        public List<AuditReportRowDTO> RecentActivity { get; set; } = new();
    }
}