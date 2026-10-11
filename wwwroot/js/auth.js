const Auth = (() => {
    const TOKEN_KEY = "lms_token";
    const USER_KEY = "lms_user";
    const EXPIRES_KEY = "lms_expires";

    const ROLE_HOME = {
        Student: "/student/dashboard.html",
        Instructor: "/instructor/dashboard.html",
        Admin: "/admin/dashboard.html"
    };

    // .NET sends 7 fractional digits. Some browsers only accept 3, so we trim.
    function parseExpiry(iso) {
        const trimmed = String(iso).replace(/(\.\d{3})\d+/, "$1");
        const time = Date.parse(trimmed);
        return Number.isNaN(time) ? Date.now() + 30 * 60 * 1000 : time;
    }

    function saveSession(loginData) {
        localStorage.setItem(TOKEN_KEY, loginData.token);
        localStorage.setItem(USER_KEY, JSON.stringify(loginData.user));
        localStorage.setItem(EXPIRES_KEY, String(parseExpiry(loginData.expiresAt)));
    }

    function clearSession() {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        localStorage.removeItem(EXPIRES_KEY);
    }

    // Returns the token, or null if there is none or it has expired.
    function getToken() {
        const token = localStorage.getItem(TOKEN_KEY);
        const expires = Number(localStorage.getItem(EXPIRES_KEY));

        if (!token || !expires) {
            return null;
        }

        if (expires <= Date.now()) {
            clearSession();
            return null;
        }

        return token;
    }

    function getUser() {
        try {
            return JSON.parse(localStorage.getItem(USER_KEY));
        } catch {
            return null;
        }
    }

    // Merges new values into the saved user, e.g. after the name changes.
    function updateUser(changes) {
        const current = getUser();

        if (!current) {
            return;
        }

        localStorage.setItem(USER_KEY, JSON.stringify({ ...current, ...changes }));
    }

    function homeFor(role) {
        return ROLE_HOME[role] || "/login.html";
    }

    function logout() {
        clearSession();
        window.location.href = "/login.html";
    }

    // For pages any logged-in user may open. Returns true if the user may stay.
    function requireLogin() {
        const user = getToken() ? getUser() : null;

        if (!user) {
            window.location.replace("/login.html");
            return false;
        }

        return true;
    }

    // For pages that belong to one role. Returns true if the user may stay.
    function requireRole(role) {
        const user = getToken() ? getUser() : null;

        if (!user) {
            window.location.replace("/login.html");
            return false;
        }

        if (user.role !== role) {
            window.location.replace(homeFor(user.role));
            return false;
        }

        return true;
    }

    return {
        saveSession, clearSession, getToken, getUser, updateUser,
        homeFor, logout, requireLogin, requireRole
    };
})();