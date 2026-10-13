
class ApiError extends Error {
    constructor(status, messages, data = null) {
        super(messages.join(" "));
        this.name = "ApiError";
        this.status = status;
        this.messages = messages;
        this.data = data;
    }
}

const Api = (() => {

    // Convert server responses into readable error messages.
    function collectMessages(data, status) {
        const messages = [];

        if (data && typeof data === "object") {

            // Custom API errors: { "error": "..." }
            if (typeof data.error === "string") {
                messages.push(data.error);
            }

            // ASP.NET validation errors:
            // { "errors": { "Email": ["Invalid email"] } }
            if (data.errors && typeof data.errors === "object") {
                Object.values(data.errors).forEach(list => {
                    (Array.isArray(list) ? list : [list])
                        .forEach(message => messages.push(String(message)));
                });
            }

            // ProblemDetails responses from ASP.NET Core.
            if (messages.length === 0 && typeof data.detail === "string") {
                messages.push(data.detail);
            }

            if (messages.length === 0 && typeof data.title === "string") {
                messages.push(data.title);
            }

            // Support APIs that return { "message": "..." }.
            if (messages.length === 0 && typeof data.message === "string") {
                messages.push(data.message);
            }
        }

        // Plain-text error responses.
        if (typeof data === "string" && data.trim()) {
            messages.push(data.trim());
        }

        if (messages.length > 0) {
            return messages;
        }

        switch (status) {
            case 400:
                return ["The request was not valid."];

            case 401:
                return ["You are not logged in or your session has expired."];

            case 403:
                return ["You do not have permission to do this."];

            case 404:
                return ["The requested item was not found."];

            case 409:
                return ["This operation conflicts with existing data."];

            case 500:
                return ["An internal server error occurred. Check the API logs."];

            default:
                return ["Something went wrong on the server. Please try again."];
        }
    }

    // Read the response safely, including empty responses such as HTTP 204.
    async function readBody(response) {
        const text = await response.text();

        if (!text) {
            return null;
        }

        try {
            return JSON.parse(text);
        } catch {
            return text;
        }
    }

    // Common HTTP request handler.
    async function request(method, url, body) {
        const headers = {};
        const token = Auth.getToken();

        if (token) {
            headers["Authorization"] = "Bearer " + token;
        }

        let payload;

        if (body instanceof FormData) {
            // The browser sets Content-Type automatically for FormData.
            payload = body;
        } else if (body !== undefined && body !== null) {
            headers["Content-Type"] = "application/json";
            payload = JSON.stringify(body);
        }

        let response;

        try {
            response = await fetch(url, {
                method,
                headers,
                body: payload
            });
        } catch {
            throw new ApiError(
                0,
                ["Cannot reach the server. Check your connection and try again."]
            );
        }

        // Clear an expired or rejected login session.
        if (response.status === 401 && token) {
            Auth.clearSession();
            window.location.href = "/login.html?expired=1";

            throw new ApiError(
                401,
                ["Your session has ended. Please log in again."]
            );
        }

        const data = await readBody(response);

        if (!response.ok) {
            throw new ApiError(
                response.status,
                collectMessages(data, response.status),
                data
            );
        }

        // Returns null for successful requests with no response body.
        return data;
    }

    // Public API methods.

    return {
        get: (url) => request("GET", url),
        post: (url, body) => request("POST", url, body),
        put: (url, body) => request("PUT", url, body),

        // Delete method
        delete: (url) => request("DELETE", url),

        // Backward compatibility with existing code
        del: (url) => request("DELETE", url)
    };

})();
