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
    // Turns whatever the server sent into a list of readable messages.
    function collectMessages(data, status) {
        const messages = [];

        if (data && typeof data === "object") {
            // Our own errors: { "error": "..." }
            if (typeof data.error === "string") {
                messages.push(data.error);
            }

            // ASP.NET validation errors: { "errors": { "Email": ["..."] } }
            if (data.errors && typeof data.errors === "object") {
                Object.values(data.errors).forEach(list => {
                    (Array.isArray(list) ? list : [list]).forEach(m => messages.push(String(m)));
                });
            }
        }

        if (messages.length > 0) {
            return messages;
        }

        switch (status) {
            case 400: return ["The request was not valid."];
            case 401: return ["You are not logged in."];
            case 403: return ["You do not have permission to do this."];
            case 404: return ["The requested item was not found."];
            case 413: return ["The file is too large."];
            default: return ["Something went wrong on the server. Please try again."];
        }
    }

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

    function sessionEnded() {
        Auth.clearSession();
        window.location.href = "/login.html?expired=1";
        return new ApiError(401, ["Your session has ended. Please log in again."]);
    }

    async function request(method, url, body) {
        const headers = {};
        const token = Auth.getToken();

        if (token) {
            headers["Authorization"] = "Bearer " + token;
        }

        let payload;

        if (body instanceof FormData) {
            // The browser sets the Content-Type for file uploads.
            payload = body;
        } else if (body !== undefined && body !== null) {
            headers["Content-Type"] = "application/json";
            payload = JSON.stringify(body);
        }

        let response;

        try {
            response = await fetch(url, { method, headers, body: payload });
        } catch {
            throw new ApiError(0, ["Cannot reach the server. Check your connection and try again."]);
        }

        // We had a token, but the server rejected it: expired, deactivated, or changed role.
        if (response.status === 401 && token) {
            throw sessionEnded();
        }

        const data = await readBody(response);

        if (!response.ok) {
            throw new ApiError(response.status, collectMessages(data, response.status), data);
        }

        return data;
    }

    // File upload with a progress callback (fetch cannot report upload progress, XMLHttpRequest can).
    function upload(url, formData, onProgress, method = "PUT") {
        return new Promise((resolve, reject) => {
            const token = Auth.getToken();
            const xhr = new XMLHttpRequest();

            xhr.open(method, url);

            if (token) {
                xhr.setRequestHeader("Authorization", "Bearer " + token);
            }

            xhr.upload.addEventListener("progress", (event) => {
                if (event.lengthComputable && onProgress) {
                    onProgress(Math.round((event.loaded * 100) / event.total));
                }
            });

            xhr.addEventListener("load", () => {
                let data = null;

                if (xhr.responseText) {
                    try {
                        data = JSON.parse(xhr.responseText);
                    } catch {
                        data = null;
                    }
                }

                if (xhr.status === 401 && token) {
                    reject(sessionEnded());
                    return;
                }

                if (xhr.status >= 200 && xhr.status < 300) {
                    resolve(data);
                    return;
                }

                reject(new ApiError(xhr.status, collectMessages(data, xhr.status), data));
            });

            xhr.addEventListener("error", () => {
                reject(new ApiError(0, ["Cannot reach the server. Check your connection and try again."]));
            });

            xhr.addEventListener("abort", () => {
                reject(new ApiError(0, ["The upload was cancelled."]));
            });

            xhr.send(formData);
        });
    }

    return {
        get: (url) => request("GET", url),
        post: (url, body) => request("POST", url, body),
        put: (url, body) => request("PUT", url, body),
        del: (url) => request("DELETE", url),
        upload
    };
})();