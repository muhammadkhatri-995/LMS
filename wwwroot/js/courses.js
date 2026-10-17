const Courses = (() => {
    const STATUS_STYLES = {
        Draft: "bg-amber-50 text-amber-700",
        Published: "bg-green-50 text-green-700"
    };

    const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

    // The same rules the server applies. The server checks again, so these are only for quick feedback.
    const FILE_RULES = {
        image: {
            extensions: [".jpg", ".jpeg", ".png", ".webp"],
            maxBytes: 2 * 1024 * 1024,
            label: "a JPG, PNG, or WebP image up to 2 MB"
        },
        video: {
            extensions: [".mp4", ".webm"],
            maxBytes: 500 * 1024 * 1024,
            label: "an MP4 or WebM video up to 500 MB"
        }
    };

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

    // The course picture: the uploaded thumbnail, or the first letter of the title.
    function thumbnail(course, heightClass) {
        const box = Ui.el("div", heightClass + " bg-slate-100 overflow-hidden flex items-center justify-center");
        const url = course.thumbnailUrl;

        // Only addresses we created ourselves are used as an image source.
        if (typeof url === "string" && url.startsWith("/uploads/thumbnails/")) {
            const image = document.createElement("img");
            image.src = url;
            image.alt = "";
            image.loading = "lazy";
            image.className = "w-full h-full object-cover";
            box.appendChild(image);
        } else {
            box.appendChild(Ui.el("span", "text-4xl font-semibold text-slate-400", course.title.charAt(0).toUpperCase()));
        }

        return box;
    }

    // Returns a problem message, or null if the file looks acceptable. kind is "image" or "video".
    function checkFile(file, kind) {
        const rule = FILE_RULES[kind];

        if (!file) {
            return "Please choose a file.";
        }

        const name = file.name.toLowerCase();

        if (!rule.extensions.some(extension => name.endsWith(extension))) {
            return "Please choose " + rule.label + ".";
        }

        if (file.size === 0) {
            return "The file is empty.";
        }

        if (file.size > rule.maxBytes) {
            return "The file is too large. Please choose " + rule.label + ".";
        }

        return null;
    }

    function formatBytes(bytes) {
        if (bytes < 1024 * 1024) {
            return Math.max(1, Math.round(bytes / 1024)) + " KB";
        }

        return (bytes / (1024 * 1024)).toFixed(1) + " MB";
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

    // 45 -> "45 min", 125 -> "2h 05m", 0 -> "" (nothing to show)
    function formatDuration(minutes) {
        const total = Number(minutes) || 0;

        if (total <= 0) {
            return "";
        }

        const hours = Math.floor(total / 60);
        const mins = total % 60;

        if (hours === 0) {
            return mins + " min";
        }

        return hours + "h " + String(mins).padStart(2, "0") + "m";
    }

    return {
        isGuid, detailsUrl, statusBadge, thumbnail, checkFile, formatBytes,
        fillCategorySelect, pagerText, formatDuration
    };
})();