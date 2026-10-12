const Courses = (() => {
    const STATUS_STYLES = {
        Draft: "bg-amber-50 text-amber-700",
        Published: "bg-green-50 text-green-700"
    };

    const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

    function isGuid(value) {
        return typeof value === "string" && GUID_PATTERN.test(value);
    }

    function detailsUrl(id) {
        return "/course-details.html?id=" + encodeURIComponent(id);
    }

    function statusBadge(status) {
        return Ui.el(
            "span",
            "inline-block rounded-full px-2 py-0.5 text-xs font-medium " + (STATUS_STYLES[status] || STATUS_STYLES.Draft),
            status);
    }

    // Fills a <select> with the categories. `firstOptionText` adds an empty first choice.
    async function fillCategorySelect(select, firstOptionText) {
        const categories = await Api.get("/api/categories");
        select.replaceChildren();

        if (firstOptionText !== undefined) {
            const first = Ui.el("option", "", firstOptionText);
            first.value = "";
            select.appendChild(first);
        }

        categories.forEach(category => {
            const option = Ui.el("option", "", category.name);
            option.value = category.id;
            select.appendChild(option);
        });

        return categories;
    }

    function pagerText(data) {
        const totalPages = Math.max(data.totalPages, 1);
        return "Page " + data.page + " of " + totalPages + " \u00B7 " + data.totalCount + " courses";
    }

    return { isGuid, detailsUrl, statusBadge, fillCategorySelect, pagerText };
})();