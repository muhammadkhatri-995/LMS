const Ui = (() => {
    const STYLES = {
        error: "border-red-200 bg-red-50 text-red-700",
        success: "border-green-200 bg-green-50 text-green-700",
        info: "border-blue-200 bg-blue-50 text-blue-700"
    };

    function clearAlert(container) {
        container.replaceChildren();
    }

    // Uses textContent, never innerHTML, so server or user text can't inject HTML.
    function showAlert(container, messages, type = "error") {
        clearAlert(container);

        const list = Array.isArray(messages) ? messages : [messages];
        const box = document.createElement("div");
        box.className = "rounded-lg border px-4 py-3 text-sm space-y-1 " + STYLES[type];
        box.setAttribute("role", "alert");

        list.forEach(text => {
            const line = document.createElement("p");
            line.textContent = text;
            box.appendChild(line);
        });

        container.appendChild(box);
    }

    function setBusy(button, busy, busyText) {
        if (busy) {
            button.dataset.label = button.textContent;
            button.textContent = busyText;
            button.disabled = true;
            button.classList.add("opacity-60", "cursor-not-allowed");
        } else {
            button.textContent = button.dataset.label || button.textContent;
            button.disabled = false;
            button.classList.remove("opacity-60", "cursor-not-allowed");
        }
    }

    // Creates an element with classes and text (text is always plain text).
    function el(tag, className, text) {
        const node = document.createElement(tag);

        if (className) {
            node.className = className;
        }

        if (text !== undefined) {
            node.textContent = text;
        }

        return node;
    }

    // Waits until the user stops typing before running the function.
    function debounce(fn, delay) {
        let timer;

        return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => fn(...args), delay);
        };
    }

    function formatDate(iso) {
        if (!iso) {
            return "";
        }

        const trimmed = String(iso).replace(/(\.\d{3})\d+/, "$1");
        const date = new Date(trimmed);

        if (Number.isNaN(date.getTime())) {
            return "";
        }

        return date.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
    }

    return { showAlert, clearAlert, setBusy, el, debounce, formatDate };
})();