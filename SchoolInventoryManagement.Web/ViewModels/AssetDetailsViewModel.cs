using System;
using System.Collections.Generic;
using SchoolInventoryManagement.BLL.DTOs;

namespace SchoolInventoryManagement.Web.ViewModels
{
    // One row of the History card on the asset page. The card shows what
    // physically happened to the unit -- issued, returned, moved, disposed,
    // restored -- which is assembled here from the three history services
    // rather than read from a table of its own. The full audit trail, which
    // also records edits, stays on the History page.
    public class AssetTimelineEntry
    {
        public DateTime When { get; set; }
        public string Title { get; set; } = "";
        public string Meta { get; set; } = "";

        // Matches an AssetStatus name so the view can build the pill class
        // the same way every other list does.
        public string StatusLabel { get; set; } = "";
    }

    public class AssetDetailsViewModel
    {
        public AssetResponseDTO Asset { get; set; } = null!;

        // Null unless the asset is currently out with someone. Carries the
        // issue date and the condition it was handed over in, which the
        // asset row itself does not record.
        public AssetAssignmentResponseDTO? ActiveAssignment { get; set; }

        // Newest first, capped by the controller.
        public List<AssetTimelineEntry> Timeline { get; set; } = new();

        public int? DaysOut =>
            ActiveAssignment is null
                ? null
                : (int)(DateTime.Now.Date - ActiveAssignment.AssignmentDate.Date).TotalDays;
    }
}
