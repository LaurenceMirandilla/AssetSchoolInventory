# Frontend design — School Inventory Management

**Stack: unchanged.** ASP.NET Core MVC + Razor views, Bootstrap 5.1, jQuery 3 and
jQuery-validation-unobtrusive, all already vendored in `wwwroot/lib`. No React, no build
step, no npm, no CDN, no icon font. Everything below is CSS classes, Razor markup and one
small jQuery file.

## The three pieces

| File | What it is |
|---|---|
| `wwwroot/css/design-system.css` | The component layer. Additive: every selector is a new `.sab-` class, so no page that works today changes. |
| `Views/Shared/_Icons.cshtml` | Inline SVG sprite, 34 icons. Rendered once per page by `_Layout`. |
| `wwwroot/js/ui.js` | Progressive enhancement — confirm dialogs, auto-applying filters, clickable rows, the mobile drawer. Every one is optional on top of markup that already works. |
| `docs/design/styleguide.html` | Living style guide. Open it in a browser; it loads the app's real CSS. |
| `docs/design/screens.html` | Six screen blueprints — the target for the existing views. |

`_Layout.cshtml` gained four lines: the stylesheet, the sprite partial, `ui.js`, and an
`id="main-content"` for the skip link. Nothing else in the app was touched.

---

## 1. Where the frontend stands today

Worth writing down, because the design is a response to it rather than a fresh start. The
good news first: there is already a real visual identity here — the green `sab-` theme, the
app shell, status pills and report tiles are a design system in everything but name.

What is missing is the layer between "theme" and "view". Because it does not exist, views
improvise:

- **Inline styles are the de-facto component layer.** `Assets/Index.cshtml` has 6 `style="`
  attributes, `Home/Index.cshtml` has 7 — gaps, widths, bar charts and avatar resizes that
  should be classes. The selected-tile state on the asset list is an inline
  `border border-2` plus an inline colour.
- **Every page header is rebuilt by hand** out of `d-flex justify-content-between` with a
  different inline gap each time.
- **Empty states are one sentence, reused for three different situations.** "No assets
  found." is printed whether nothing exists, the filter hid everything, or the user is not
  allowed to see it. Only the first deserves a "Register asset" button.
- **Icons are emoji.** `&#128269;` for search and `&#128065;` for the password toggle.
  They render differently on Windows, Android and iOS, and there is no way to colour them.
- **Below 900px the sidebar becomes a horizontally scrolling strip of links**, and the wide
  asset table just overflows. An Asset Officer doing a physical count is holding a phone.
- **Two controls on the login page promise things that do not exist.** `LoginDTO` carries
  only `Email` and `Password`, so the "Keep me signed in" checkbox posts nothing; "Forgot
  password?" is a `<span>`, not a link. The page already explains that resets go through an
  Administrator — the two decorative controls should go.
- **`Views/Shared/_Layout.cshtml.css` still holds the default template's blue.** It sets
  `a { color: #0077cc }` and a blue `.btn-primary`. Scoped CSS compiles to
  `a[b-xxxxx]`, which has higher specificity than site.css's `.sab-nav-link`, so it wins
  inside `_Layout` — the sidebar links and the topbar Login button are being pulled back
  toward Bootstrap blue. That file is leftover scaffolding and should be emptied.

None of these are bugs in the BLL sense. They are the difference between a system that
works and one that a teacher trusts.

---

## 2. Principles

1. **The UI mirrors the permission rules, and explains them.** `PermissionHelper` already
   decides who may do what. The UI should not merely hide what you cannot do — it should
   say why, when saying so teaches the rule. A disabled **Dispose** button with
   *"Return it from Maria first"* under it is worth more than a button that vanishes.
2. **The URL is the state.** Filters, sorting and paging stay in the query string, as GET
   forms. A filtered list is then bookmarkable, shareable and correct on the back button —
   and works with JavaScript off.
3. **Every screen answers one question.** Dashboard: *what needs me?* Register: *where is
   unit X?* Approvals: *what am I holding up?* My requests: *where is mine?*
4. **Nothing is decoration.** Green means actionable. An icon always has a text label beside
   it. A control that does nothing gets removed, not styled.
5. **No new dependencies.** Anything that needs a package is out of scope; there is always a
   Bootstrap or vanilla answer, and this app has no build step to hide the cost in.

---

## 3. Information architecture

Six roles exist (`RoleNames`), but only four permission groups do, and the navigation should
show exactly those. This is what `_Layout` already computes — the design keeps it and adds
the counts that make the sidebar useful:

| Group | Roles (per `PermissionHelper`) | Sees |
|---|---|---|
| Requester | Teacher, Staff, Department Head | Home, Assets (read), My Requests, Notifications |
| Asset manager | Asset Officer, Administrator | + Categories, Models, Locations, asset write actions |
| Approver | Asset Officer, Administrator, Principal | + Approvals **with a pending count**, Reports |
| User manager | Administrator, Principal | + Users, Branches, Departments |

Two notes for you to decide on, not for me to decide:

- **Department Head and Staff currently get the plain requester view.** A Department Head
  who cannot see their own department's outstanding assignments will ask for it. The seam is
  already in the code — `PermissionHelper.ReportViewerRoles` is deliberately a separate list
  from `ApproverRoles` for exactly this. The design's report screens work unchanged if you
  add the role there.
- **The sidebar badge on Approvals** needs a pending count in the layout. `NotificationBellViewComponent`
  is the pattern to copy — a second view component, not a controller change.

### Navigation depth

Two levels everywhere (sidebar → page). The third level only exists for an object's own
sub-pages — asset history, request approval — and those get `.sab-crumbs`. If a screen needs
a fourth level, it is the wrong screen.

---

## 4. Page templates

Five templates cover all 40-odd views. Each existing view should end up being one of them.

### 4.1 List / register — `Assets`, `Users`, `Categories`, `Models`, `Locations`, `Branches`, `Departments`

```
page header            title · count subtitle · [export] [primary create]
stat tiles             optional; each one links to a filtered view of this same list
filter bar             GET form; auto-applies on select change
filter chips           what is narrowing the list right now, each removable
table card             count header · rows · pager footer, all in one card
empty state            one of three, never the same sentence
```

```cshtml
<div class="sab-page-header">
    <div class="sab-page-header-text">
        <h1 class="sab-page-title">Assets</h1>
        <p class="sab-page-subtitle">@Model.TotalCount registered unit@(Model.TotalCount == 1 ? "" : "s")</p>
    </div>
    <div class="sab-page-actions">
        <a asp-action="ExportAssets"
           asp-route-keyword="@Model.Keyword" asp-route-categoryId="@Model.CategoryId"
           asp-route-status="@Model.Status" class="btn btn-outline-secondary">
            <svg class="sab-icon" aria-hidden="true"><use href="#i-download"></use></svg> Export CSV
        </a>
        @if (canManage)
        {
            <a asp-action="Create" class="btn btn-primary">
                <svg class="sab-icon" aria-hidden="true"><use href="#i-plus"></use></svg> Register asset
            </a>
        }
    </div>
</div>
```

Tiles become links with a real selected state, replacing the inline border:

```cshtml
<div class="sab-stats">
    <a asp-action="Index" class="sab-stat @(Model.Status == null ? "is-active" : "")">
        <span class="sab-stat-value">@Model.TotalCount</span>
        <span class="sab-stat-label">All assets</span>
    </a>
    <a asp-action="Index" asp-route-status="Available"
       class="sab-stat @(Model.Status == AssetStatus.Available ? "is-active" : "")">
        <span class="sab-stat-value">@Model.AvailableCount</span>
        <span class="sab-stat-label">Available</span>
    </a>
    @* … *@
</div>
```

Filters keep `Html.DropDownList` exactly as they are; only the wrapper changes, plus
`data-auto-filter` so changing a dropdown applies it:

```cshtml
<form method="get" class="sab-filters" data-auto-filter>
    <div class="sab-filter sab-filter-wide">
        <label class="sab-filter-label" for="keyword">Search</label>
        <div class="sab-search-field">
            <svg class="sab-icon" aria-hidden="true"><use href="#i-search"></use></svg>
            <input id="keyword" type="text" name="keyword" class="form-control"
                   placeholder="Name, code, serial…" value="@Model.Keyword" />
        </div>
    </div>
    <div class="sab-filter">
        <label class="sab-filter-label" for="categoryId">Category</label>
        @Html.DropDownList("categoryId", (IEnumerable<SelectListItem>)ViewBag.CategoryFilter,
             "All categories", new { @class = "form-select", id = "categoryId" })
    </div>
    <div class="sab-filter-actions">
        <button type="submit" class="btn btn-secondary">Apply</button>
        <a asp-action="Index" class="btn btn-outline-secondary">Reset</a>
    </div>
</form>
```

Table cells carry `data-label` so the table restacks as cards on a phone:

```cshtml
<div class="sab-section sab-table-card">
    <div class="sab-section-head">
        <h2 class="sab-section-title">Showing @Model.FirstRowNumber–@Model.LastRowNumber of @Model.TotalFilteredCount</h2>
    </div>
    <div class="sab-section-body-flush">
        <table class="table table-striped sab-table-stack mb-0">
            <thead>…</thead>
            <tbody>
            @foreach (var asset in Model.Assets)
            {
                <tr class="sab-row-link" data-href="@Url.Action("Details", new { id = asset.AssetID })" tabindex="0">
                    <td data-label="Code"><span class="sab-cell-code">@asset.AssetCode</span></td>
                    <td data-label="Asset">
                        <div class="sab-cell-primary">@asset.AssetName</div>
                        <div class="sab-cell-secondary">@asset.ModelName</div>
                    </td>
                    <td data-label="Status">
                        <span class="status-pill status-@asset.Status.ToString().ToLowerInvariant()">@asset.Status</span>
                    </td>
                    <td class="sab-cell-actions">
                        <a asp-action="Details" asp-route-id="@asset.AssetID">View</a>
                    </td>
                </tr>
            }
            </tbody>
        </table>
    </div>
    <div class="sab-section-foot">…pager…</div>
</div>
```

### 4.2 Detail — `Assets/Details`, request details, user details

`.sab-detail` is a two-column grid: facts and history left, actions right. The right rail is
where the design earns its keep — it is the only place in the app that answers *"what can I
do with this thing right now, and why not the rest"*.

```cshtml
<div class="sab-actions-stack">
    @if (asset.Status == AssetStatus.Assigned)
    {
        <a asp-controller="AssetAssignments" asp-action="Return" asp-route-id="@activeAssignmentId"
           class="btn btn-primary">Return from @asset.AssignedUserName</a>
    }
    <button class="btn btn-danger" disabled="@(asset.Status == AssetStatus.Assigned)">Dispose</button>
    @if (asset.Status == AssetStatus.Assigned)
    {
        <p class="sab-action-note">Return it from @asset.AssignedUserName first — an assigned unit cannot be disposed.</p>
    }
</div>
```

History uses `.sab-timeline`, merging assignments, movements and disposal records newest
first. The dot colour is a hint; the sentence carries the meaning.

### 4.3 Form — create, edit, assign, transfer, dispose, approve, reject

One column, max 720px, fieldsets, actions on a top border at the bottom. Any form that acts
on an existing thing opens with `.sab-context` — a read-only block naming what is being acted
on. Deciding a request with the asset's details one page back is how the wrong unit gets
issued.

Validation stays exactly as it is: `asp-validation-for`, `asp-validation-summary` and the
unobtrusive scripts in `_ValidationScriptsPartial`.

Destructive submits get a confirm:

```cshtml
<form asp-action="Dispose" method="post"
      data-confirm="Dispose @Model.AssetCode? It stops appearing in the register and only an Administrator can restore it."
      data-confirm-title="Dispose asset"
      data-confirm-ok="Dispose"
      data-confirm-variant="danger"
      data-submit-once>
```

### 4.4 Queue — `AssetRequests/Pending`, `MyRequests`

A list, ordered oldest first, where each row ends in the decision. Requests the signed-in
officer may not decide — the same-role rule the BLL enforces — stay visible and explain
themselves in the row, instead of throwing a 403 after the click.

### 4.5 Dashboard — `Home/Index`

Tiles (what exists) then an attention list (what needs me) with a verb on every row. The
existing view has an honest comment where three of the attention rows should be, saying the
queries do not exist in `IReportService` yet rather than showing fake counts. The blueprint
shows all four rows; three of them need those queries before the row is real. Keep the
current discipline — an empty attention list is fine, an invented one is not.

---

## 5. States

Four states, four treatments. Getting this right is most of the difference in how the app
feels.

| State | Class | Rule |
|---|---|---|
| Nothing exists yet | `.sab-empty` | Offer the action that creates the first one — **only if this user may** |
| Filter hid everything | `.sab-empty-filtered` | Say how many exist unfiltered; offer *Clear filters*. Never offer "create" |
| Not permitted | `.sab-empty-denied` | Name the roles that may, and where this user should go instead |
| Action failed | `.sab-flash-error` | The server's sentence, not a status code |

**Concurrency deserves its own sentence.** `ConcurrencyConflictException` already travels
from the BLL to `BaseController.HandleServiceException` as a ModelState error. In the UI that
must read as *"Someone changed this asset while you were editing. Reload to see their change,
then re-apply yours."* — not "Error 409" and not a silent overwrite. The `RowVersion` plumbing
in `RowVersionHelper` is what makes this recoverable; the copy is what makes it survivable.

**Flash messages.** `TempData["StatusMessage"]` renders as `.sab-flash`. Success auto-hides
after six seconds (`data-auto-dismiss`); warnings and errors stay until dismissed, because
auto-hiding the reason something failed is how a user repeats it.

---

## 6. Role-aware UI — the rule

Hiding a button is a courtesy, never a control. Every action in this app is authorised twice
already — `[Authorize]` on the controller and an `Ensure*` call inside the service — and the
UI is a third, weakest layer. So:

- Hide what the user can never do (a Teacher never sees *Register asset*).
- Disable, with a reason, what they could do if the object were in a different state
  (*Dispose* on an assigned unit).
- Explain, don't hide, what another person of the same rank must do (the same-role approval
  rule).

---

## 7. Responsive

| Width | Behaviour |
|---|---|
| ≥ 1200px | Sidebar + two-column detail, tiles 4–5 across |
| 900–1200px | Sidebar + single-column detail |
| < 900px | Sidebar becomes a drawer (`.sab-drawer` + the ☰ toggle); topbar search moves to its own row |
| < 760px | Tables restack as cards via `data-label`; tiles go 2-up then 1-up |

The drawer is opt-in per the extra class, so you can adopt it one layout change at a time.

## 8. Print

Reports and the asset detail sheet are printed — a department head wants the outstanding
assignments on paper at handover. `@media print` in `design-system.css` drops the sidebar,
topbar, filters and buttons, un-shadows the cards, repeats table headers across pages and
avoids breaking rows. Add a `.sab-print-header` block (hidden on screen) to any report so the
paper says what it is and when it was run.

## 9. Accessibility

Not a separate pass — these are cheap if done in the markup from the start:

- Every icon is `aria-hidden` and sits beside text; icon-only buttons carry `aria-label`.
- One visible focus ring (`--sab-focus-ring`), `:focus-visible` so mouse clicks don't flash it.
- Skip link to `#main-content`, already in `_Layout`.
- Status is never colour alone — every pill spells the status out.
- Labels are real `<label for>`; the required marker is a `*` **and** the word where it matters.
- Clickable rows keep a real link in the first cell, so Tab order and "open in new tab" work.
- Contrast: measured, not assumed. Every pill pair and every text colour used in this design
  clears WCAG AA for normal text (4.5:1). Two things had to change to make that true:

  | Pair | Before | After |
  |---|---|---|
  | Amber pill text on amber pill background | 4.29:1 ✗ | **5.17:1 ✓** (`--sab-amber-text` darkened to `#8a5e14`) |
  | Field labels, metadata, mobile card labels | 2.54:1 ✗ (`--sab-text-faint`) | **4.83:1 ✓** (`--sab-text-muted`) |

  Pill text is 12px bold, which does **not** qualify as WCAG "large text" (that needs
  18.66px bold), so 4.29:1 was a genuine failure rather than a technicality. The amber token
  is the one existing value this design changes — the resulting shade is slightly deeper and
  is used by the Pending pill, the maintenance pill and warning alerts.

  `--sab-text-faint` (#9ca3af, 2.54:1) survives for decoration only: breadcrumb separators,
  the search glyph, empty-state and placeholder icons, the grey timeline dot. Note that
  `site.css` still uses it for `.table thead th` and `.sab-brand-sub` — column headers are
  arguably readable text, and moving them to `--sab-text-muted` is a one-line follow-up I
  left alone because it changes every existing table.

---

## 10. Adoption order

Nothing here needs a big-bang rewrite; each step is independently shippable.

1. **Empty `_Layout.cshtml.css`.** It is scaffolding fighting the theme. (5 minutes)
2. **Delete the two dead login controls**, keep the help text that tells people how resets
   actually work.
3. **`Assets/Index`** — the most-used screen and the one with the most inline styles. It
   becomes the reference implementation for every other list.
4. **`Assets/Details`** — the right rail with reasons is the biggest single usability win.
5. **`AssetRequests/Pending` and `MyRequests`** — queue template, same-role explanation.
6. **`Home/Index`** — attention rows get verbs; add the three missing `IReportService`
   queries if you want the other rows.
7. **The rest of the CRUD lists** (Categories, Models, Locations, Branches, Departments,
   Users) — mechanical once step 3 exists.
8. **Mobile drawer** on `_Layout`, once at least the asset list restacks.

## 11. Deliberately not in this design

- **Dark mode.** No token work here supports it, and a school inventory on shared desktops
  does not need it. Adding it later means a second `:root` block, nothing more.
- **Client-side sorting/paging.** Server-side stays; it keeps the URL honest and the DOM small.
- **Charts.** The category bar list is CSS. A charting library would be the first new
  dependency, for one dashboard panel.
- **A component library or SASS build.** No build step exists. Plain CSS with custom
  properties does everything this app needs.
