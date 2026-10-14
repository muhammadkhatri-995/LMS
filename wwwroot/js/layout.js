const Layout = (() => {
    const NAV = {
        Student: [
            { label: "Dashboard", href: "/student/dashboard.html" },
            { label: "Courses", href: "/student/courses.html" },
            { label: "Profile", href: "/profile.html" }
        ],
        Instructor: [
            { label: "Dashboard", href: "/instructor/dashboard.html" },
            { label: "My courses", href: "/instructor/courses.html" },
            { label: "Profile", href: "/profile.html" }
        ],
        Admin: [
            { label: "Dashboard", href: "/admin/dashboard.html" },
            { label: "Users", href: "/admin/users.html" },
            { label: "Courses", href: "/admin/courses.html" },
            { label: "Categories", href: "/admin/categories.html" },
            { label: "Profile", href: "/profile.html" }
        ]
    };

    // Builds the top bar for the logged-in user and puts it inside `container`.
    function renderHeader(container) {
        const user = Auth.getUser();

        if (!user) {
            return;
        }

        const currentPath = window.location.pathname.toLowerCase();
        const links = NAV[user.role] || [];

        const header = Ui.el("header", "bg-white border-b border-slate-200");
        const bar = Ui.el("div", "max-w-5xl mx-auto px-4 py-3 flex flex-wrap items-center justify-between gap-3");

        const left = Ui.el("div", "flex items-center gap-6");
        left.appendChild(Ui.el("span", "font-semibold text-slate-800", "LearnHub"));

        const nav = Ui.el("nav", "flex flex-wrap items-center gap-4 text-sm");
        links.forEach(link => {
            const isActive = currentPath === link.href.toLowerCase();
            const cssClass = isActive
                ? "text-blue-600 font-medium"
                : "text-slate-600 hover:text-slate-900";

            const anchor = Ui.el("a", cssClass, link.label);
            anchor.href = link.href;
            nav.appendChild(anchor);
        });
        left.appendChild(nav);

        const right = Ui.el("div", "flex items-center gap-3 text-sm");
        right.appendChild(Ui.el("span", "text-slate-600", user.fullName));
        right.appendChild(Ui.el("span", "rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600", user.role));

        const logoutButton = Ui.el(
            "button",
            "rounded-lg border border-slate-300 px-3 py-1.5 hover:bg-slate-100",
            "Log out");
        logoutButton.type = "button";
        logoutButton.addEventListener("click", Auth.logout);
        right.appendChild(logoutButton);

        bar.append(left, right);
        header.appendChild(bar);
        container.replaceChildren(header);
    }

    return { renderHeader };
})();