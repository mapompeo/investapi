(function () {
    const THEME_KEY = "swagger_theme";
    const TOKEN_KEY = "swagger_auth_token";
    const BANNER_KEY = "swagger_hint_dismissed";

    function getInitialTheme() {
        const saved = localStorage.getItem(THEME_KEY);
        if (saved === "dark" || saved === "light") return saved;
        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
            ? "dark"
            : "light";
    }

    function applyTheme(theme) {
        document.body.classList.toggle("swagger-dark", theme === "dark");
        localStorage.setItem(THEME_KEY, theme);
        const btn = document.getElementById("swagger-theme-toggle");
        if (btn) {
            btn.textContent = theme === "dark" ? "☀️ Light" : "🌙 Dark";
        }
    }

    function ensureThemeButton() {
        const topbar = document.querySelector(".swagger-ui .topbar .download-url-wrapper")
            || document.querySelector(".swagger-ui .topbar");
        if (!topbar || document.getElementById("swagger-theme-toggle")) return;

        const btn = document.createElement("button");
        btn.id = "swagger-theme-toggle";
        btn.type = "button";
        btn.setAttribute("aria-label", "Toggle dark mode");
        btn.addEventListener("click", function () {
            const isDark = document.body.classList.contains("swagger-dark");
            applyTheme(isDark ? "light" : "dark");
        });

        topbar.appendChild(btn);
        applyTheme(getInitialTheme());
    }

    function withSwaggerUI(callback) {
        const started = Date.now();
        const timer = setInterval(() => {
            if (window.ui) {
                clearInterval(timer);
                callback(window.ui);
            } else if (Date.now() - started > 5000) {
                clearInterval(timer);
            }
        }, 200);
    }

    function preauthorizeFromStorage() {
        const token = (localStorage.getItem(TOKEN_KEY) || "").trim();
        if (!token) return;

        withSwaggerUI((ui) => {
            if (typeof ui.preauthorizeApiKey === "function") {
                ui.preauthorizeApiKey("Bearer", "Bearer " + token);
            }
        });
    }

    function saveTokenFromAuthorizeDialog() {
        document.addEventListener("click", function (event) {
            const target = event.target;
            if (!(target instanceof HTMLElement)) return;

            const button = target.closest("button");
            if (!button) return;

            const text = (button.textContent || "").trim().toLowerCase();
            if (text !== "authorize") return;

            const modal = document.querySelector(".swagger-ui .auth-container, .swagger-ui .dialog-ux");
            if (!modal) return;

            const input = modal.querySelector("input[type='text'], input[type='password']");
            if (!(input instanceof HTMLInputElement)) return;

            let value = (input.value || "").trim();
            if (!value) return;

            if (value.toLowerCase().startsWith("bearer ")) {
                value = value.slice(7).trim();
            }

            if (value) {
                localStorage.setItem(TOKEN_KEY, value);
            }
        }, true);
    }

    function showHintBannerIfNeeded() {
        const token = (localStorage.getItem(TOKEN_KEY) || "").trim();
        if (token) return;
        if (sessionStorage.getItem(BANNER_KEY) === "1") return;

        const root = document.querySelector("#swagger-ui");
        if (!root || document.querySelector(".swagger-hint-banner")) return;

        const banner = document.createElement("div");
        banner.className = "swagger-hint-banner";
        banner.innerHTML = "<span>💡 Tip: log in, copy the token from the response, and click Authorize to use protected routes.</span>";

        const close = document.createElement("button");
        close.type = "button";
        close.textContent = "×";
        close.addEventListener("click", () => {
            sessionStorage.setItem(BANNER_KEY, "1");
            banner.remove();
        });

        banner.appendChild(close);

        const topbar = document.querySelector(".swagger-ui .topbar");
        if (topbar && topbar.parentElement) {
            topbar.parentElement.insertBefore(banner, topbar.nextSibling);
        } else {
            root.prepend(banner);
        }
    }

    function attachDurationBadges() {
        const root = document.querySelector("#swagger-ui");
        if (!root) return;

        const paint = () => {
            const responses = root.querySelectorAll(".responses-wrapper");
            responses.forEach((wrapper) => {
                const durationEl = wrapper.querySelector(".request-duration");
                const headersTitle = wrapper.querySelector(".responses-header");
                if (!durationEl || !headersTitle) return;

                const msMatch = (durationEl.textContent || "").match(/(\d+(?:\.\d+)?)\s*ms/i);
                if (!msMatch) return;

                const ms = Number(msMatch[1]);
                if (!Number.isFinite(ms)) return;

                let level = "fast";
                if (ms >= 1500) level = "slow";
                else if (ms >= 500) level = "medium";

                const existing = headersTitle.querySelector(".swagger-duration-badge");
                if (existing) {
                    existing.textContent = "⏱ " + Math.round(ms) + "ms";
                    existing.className = "swagger-duration-badge " + level;
                    return;
                }

                const badge = document.createElement("span");
                badge.className = "swagger-duration-badge " + level;
                badge.textContent = "⏱ " + Math.round(ms) + "ms";
                headersTitle.appendChild(badge);
            });
        };

        const obs = new MutationObserver(() => paint());
        obs.observe(root, { childList: true, subtree: true });
        paint();
    }

    function boot() {
        ensureThemeButton();
        preauthorizeFromStorage();
        saveTokenFromAuthorizeDialog();
        showHintBannerIfNeeded();
        attachDurationBadges();

        // Re-ensure when Swagger rerenders parts of the UI.
        const root = document.querySelector("#swagger-ui");
        if (root) {
            const obs = new MutationObserver(() => {
                ensureThemeButton();
            });
            obs.observe(root, { childList: true, subtree: true });
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})();
