/*
    Progressive enhancement for the School Inventory UI.

    Ground rule: every feature here is an improvement on top of markup that
    already works without it. Filters submit via their button, destructive
    forms post normally, rows are reachable through the link in the first
    cell. If this file fails to load, the app is slightly more tedious and
    entirely functional.

    Depends only on jQuery and bootstrap.bundle, both already loaded by
    _Layout. No build step, no new packages.
*/
(function ($) {
    "use strict";

    $(function () {

        /* ------------------------------------------------------------------
           Confirm before a destructive or irreversible POST.

               <form ... data-confirm="Dispose PRJ-014? ...">
               <button data-confirm-title="Dispose asset"
                       data-confirm-ok="Dispose"
                       data-confirm-variant="danger">

           Anonymize, dispose, deactivate and delete all need this. A native
           confirm() would do, but it cannot name the consequence in a second
           line, and irreversible actions deserve the sentence.
        ------------------------------------------------------------------ */
        var $modal = null;

        function confirmModal(opts, onConfirm) {
            if (!$modal) {
                $modal = $(
                    '<div class="modal fade" tabindex="-1" aria-hidden="true">' +
                      '<div class="modal-dialog modal-dialog-centered">' +
                        '<div class="modal-content">' +
                          '<div class="modal-header">' +
                            '<h5 class="modal-title"></h5>' +
                            '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>' +
                          '</div>' +
                          '<div class="modal-body"><p class="mb-0 js-confirm-text"></p></div>' +
                          '<div class="modal-footer">' +
                            '<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>' +
                            '<button type="button" class="btn js-confirm-ok"></button>' +
                          '</div>' +
                        '</div>' +
                      '</div>' +
                    '</div>'
                ).appendTo(document.body);
            }

            $modal.find(".modal-title").text(opts.title || "Please confirm");
            $modal.find(".js-confirm-text").text(opts.message);
            $modal.find(".js-confirm-ok")
                .text(opts.okLabel || "Confirm")
                .removeClass("btn-danger btn-primary")
                .addClass(opts.variant === "danger" ? "btn-danger" : "btn-primary")
                .off("click")
                .on("click", function () {
                    bsModal.hide();
                    onConfirm();
                });

            var bsModal = bootstrap.Modal.getOrCreateInstance($modal[0]);
            bsModal.show();
        }

        $(document).on("click", "[data-confirm]", function (e) {
            var $el = $(this);
            if ($el.data("confirmed")) { return; }

            e.preventDefault();
            confirmModal({
                message: $el.data("confirm"),
                title: $el.data("confirm-title"),
                okLabel: $el.data("confirm-ok"),
                variant: $el.data("confirm-variant")
            }, function () {
                $el.data("confirmed", true);
                if ($el.is("form")) { $el.trigger("submit"); }
                else if ($el.is("button, input[type=submit]")) { $el.closest("form").trigger("submit"); }
                else { window.location.href = $el.attr("href"); }
            });
        });

        $(document).on("submit", "form[data-confirm]", function (e) {
            var $form = $(this);
            if ($form.data("confirmed")) { return; }
            e.preventDefault();
            confirmModal({
                message: $form.data("confirm"),
                title: $form.data("confirm-title"),
                okLabel: $form.data("confirm-ok"),
                variant: $form.data("confirm-variant")
            }, function () {
                $form.data("confirmed", true).trigger("submit");
            });
        });

        /* ------------------------------------------------------------------
           Filter bars: changing a dropdown applies the filter.

               <form method="get" data-auto-filter>

           The Apply button stays in the markup for keyboard users and for
           the text input, where auto-submitting on every keystroke would
           reload the page mid-word.
        ------------------------------------------------------------------ */
        $(document).on("change", "form[data-auto-filter] select", function () {
            $(this).closest("form").trigger("submit");
        });

        /* ------------------------------------------------------------------
           Clickable table rows. The row is a shortcut; the link in the first
           cell remains the real navigation, so Tab order and "open in new
           tab" still work. Ignores clicks on anything interactive inside.
        ------------------------------------------------------------------ */
        $(document).on("click", "tr.sab-row-link", function (e) {
            if ($(e.target).closest("a, button, input, select, label, .dropdown").length) { return; }
            var href = $(this).data("href");
            if (href) { window.location.href = href; }
        });

        $(document).on("keydown", "tr.sab-row-link", function (e) {
            if (e.key !== "Enter") { return; }
            var href = $(this).data("href");
            if (href) { window.location.href = href; }
        });

        /* ------------------------------------------------------------------
           Flash messages fade after 6s — success only. A warning or an error
           stays until dismissed; auto-hiding the reason something failed is
           how a user ends up repeating it.
        ------------------------------------------------------------------ */
        window.setTimeout(function () {
            $(".sab-flash[data-auto-dismiss]").fadeOut(400);
        }, 6000);

        /* ------------------------------------------------------------------
           Mobile drawer. Sidebar needs class .sab-drawer; the toggle button
           needs data-sab-drawer-toggle.
        ------------------------------------------------------------------ */
        $(document).on("click", "[data-sab-drawer-toggle]", function (e) {
            e.preventDefault();
            var $sidebar = $(".sab-sidebar.sab-drawer");
            if (!$sidebar.length) { return; }

            var opening = !$sidebar.hasClass("is-open");
            $sidebar.toggleClass("is-open", opening);
            $(this).attr("aria-expanded", opening ? "true" : "false");

            if (opening) {
                $('<div class="sab-drawer-backdrop"></div>')
                    .appendTo(document.body)
                    .on("click", function () {
                        $sidebar.removeClass("is-open");
                        $(this).remove();
                    });
            } else {
                $(".sab-drawer-backdrop").remove();
            }
        });

        $(document).on("keydown", function (e) {
            if (e.key === "Escape") {
                $(".sab-sidebar.sab-drawer").removeClass("is-open");
                $(".sab-drawer-backdrop").remove();
            }
        });

        /* ------------------------------------------------------------------
           Print the current report. <button data-print>Print</button>
        ------------------------------------------------------------------ */
        $(document).on("click", "[data-print]", function (e) {
            e.preventDefault();
            window.print();
        });

        /* ------------------------------------------------------------------
           Guard against a double-posted approval. Disables the submit button
           after the first click and lets the form through.

           Only on forms that opt in with data-submit-once, because a form
           that fails server-side validation and re-renders must not come
           back with a dead button.
        ------------------------------------------------------------------ */
        $(document).on("submit", "form[data-submit-once]", function () {
            var $btn = $(this).find('button[type="submit"]');
            window.setTimeout(function () {
                $btn.prop("disabled", true).attr("aria-busy", "true");
            }, 0);
        });
    });
})(jQuery);
