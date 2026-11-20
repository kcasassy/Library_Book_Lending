const authSection = document.getElementById("authSection");
const applicationSection = document.getElementById("applicationSection");
const loginForm = document.getElementById("loginForm");
const registerForm = document.getElementById("registerForm");
const authMessage = document.getElementById("authMessage");
const logoutButton = document.getElementById("logoutButton");
const userInfo = document.getElementById("userInfo");
const roleBadge = document.getElementById("roleBadge");
const viewContainer = document.getElementById("viewContainer");
const pageTitle = document.getElementById("pageTitle");
const pageMessage = document.getElementById("pageMessage");
const loginPanel = document.getElementById("loginPanel");
const registerPanel = document.getElementById("registerPanel");
const showLoginTab = document.getElementById("showLoginTab");
const showRegisterTab = document.getElementById("showRegisterTab");

function normalizeRole(role) {
    return String(role || "").trim().toLowerCase();
}

function showMessage(element, message, type = "") {
    if (!element) return;

    element.textContent = message;
    element.className = `message ${type}`;
}

function showAuthTab(tab) {
    if (!loginPanel || !registerPanel) return;

    const isLogin = tab === "login";

    loginPanel.classList.toggle("hidden", !isLogin);
    registerPanel.classList.toggle("hidden", isLogin);

    showLoginTab?.classList.toggle("active", isLogin);
    showRegisterTab?.classList.toggle("active", !isLogin);

    showMessage(authMessage, "");
}

showLoginTab?.addEventListener("click", () => {
    showAuthTab("login");
});

showRegisterTab?.addEventListener("click", () => {
    showAuthTab("register");
});

function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, character => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        '"': "&quot;",
        "'": "&#39;"
    })[character]);
}

function showApplication(user) {
    authSection.classList.add("hidden");
    applicationSection.classList.remove("hidden");

    logoutButton.classList.remove("hidden");
    userInfo.classList.remove("hidden");

    const role = normalizeRole(user.role);

    userInfo.textContent = user.fullName || user.email || "Signed in";
    roleBadge.textContent = user.role || "User";

    document.querySelectorAll(".staff-only").forEach(element => {
        element.classList.toggle("hidden", role !== "librarian");
    });

    openView("dashboard");
}

function showLogin() {
    applicationSection.classList.add("hidden");
    authSection.classList.remove("hidden");

    logoutButton.classList.add("hidden");
    userInfo.classList.add("hidden");

    showAuthTab("login");
}

function isViewAllowed(viewName, role) {
    const commonViews = [
        "dashboard",
        "books",
        "bookReservations",
        "borrowings",
        "seats",
        "seatReservations",
        "readingSpaces",
        "readingSpaceReservations"
    ];

    if (commonViews.includes(viewName)) {
        return ["librarian", "member"].includes(role);
    }

    if (["qcpl", "reports", "users"].includes(viewName)) {
        return role === "librarian";
    }

    return false;
}

async function openView(viewName) {
    const viewNames = {
        dashboard: "Dashboard",
        books: "Books",
        bookReservations: "Book Reservations",
        borrowings: "Borrowings",
        seats: "Seats",
        seatReservations: "Seat Reservations",
        readingSpaces: "Reading Spaces",
        readingSpaceReservations: "Reading Space Reservations",
        qcpl: "QCPL External Reservation Monitoring",
        reports: "Reports",
        users: "User Management"
    };

    const role = normalizeRole(getCurrentUser()?.role);

    if (!isViewAllowed(viewName, role)) {
        showMessage(
            pageMessage,
            "You are not authorized to access this section.",
            "error"
        );
        return;
    }

    pageTitle.textContent = viewNames[viewName] || "Dashboard";

    document.querySelectorAll(".nav-link").forEach(button => {
        button.classList.toggle(
            "active",
            button.dataset.view === viewName
        );
    });

    showMessage(pageMessage, "");

    if (viewName === "dashboard") {
        const user = getCurrentUser();

        viewContainer.innerHTML = `
            <div class="dashboard-welcome">
                <span class="eyebrow">QUEZON CITY PROJECT 4 BRANCH PUBLIC LIBRARY</span>
                <h2>Welcome, ${escapeHtml(user?.fullName || "Library User")}!</h2>
                <p class="muted">
                    Access library resources and manage your authorized activities.
                </p>
            </div>

            <div class="dashboard-cards">
                <article class="dashboard-card red-card">
                    <h3>Books</h3>
                    <p>Browse available library books.</p>
                </article>

                <article class="dashboard-card blue-card">
                    <h3>Reservations</h3>
                    <p>Review your reservation records.</p>
                </article>

                <article class="dashboard-card yellow-card">
                    <h3>Reading Spaces</h3>
                    <p>Explore available study areas.</p>
                </article>
            </div>
        `;

        return;
    }

    if (viewName === "books") {
        if (typeof renderBooksView === "function") {
            await renderBooksView();
        } else {
            viewContainer.innerHTML =
                `<p class="message error">Books module could not be loaded.</p>`;
        }

        return;
    }

    try {
        const loaded = await renderModule(viewName);

        if (!loaded) {
            viewContainer.innerHTML =
                `<p class="message error">This section is not available.</p>`;
        }
    } catch (error) {
        viewContainer.innerHTML =
            `<p class="message error">${escapeHtml(error.message)}</p>`;
    }
}

// SIGN IN
loginForm.addEventListener("submit", async event => {
    event.preventDefault();

    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value;
    const submitButton = loginForm.querySelector("button[type='submit']");

    showMessage(authMessage, "Signing in...");
    submitButton.disabled = true;

    try {
        const result = await apiRequest("/Auth/login", {
            method: "POST",
            body: JSON.stringify({ email, password })
        });

        if (!result?.token) {
            throw new Error(
                "The server did not return an authentication token."
            );
        }

        const user = {
            id: result.userId,
            fullName: result.fullName,
            email: result.email,
            role: result.role
        };

        if (!user.role) {
            throw new Error(
                "The server did not return the user's role."
            );
        }

        saveAuthSession(result.token, user);

        loginForm.reset();
        showMessage(authMessage, "");

        showApplication(user);
    } catch (error) {
        showMessage(authMessage, error.message, "error");
    } finally {
        submitButton.disabled = false;
    }
});

// SIGN UP
registerForm.addEventListener("submit", async event => {
    event.preventDefault();

    const formData = new FormData(registerForm);
    const submitButton = registerForm.querySelector("button[type='submit']");

    const registration = {
        fullName: String(formData.get("fullName") || "").trim(),
        memberNumber:
            String(formData.get("memberNumber") || "").trim() || null,
        email: String(formData.get("email") || "").trim(),
        password: String(formData.get("password") || "")
    };

    showMessage(authMessage, "Creating your account...");
    submitButton.disabled = true;

    try {
        const result = await apiRequest("/Auth/register", {
            method: "POST",
            body: JSON.stringify(registration)
        });

        registerForm.reset();

        showAuthTab("login");

        document.getElementById("email").value = registration.email;

        showMessage(
            authMessage,
            result?.message ||
            "Registration successful. You can now sign in.",
            "success"
        );
    } catch (error) {
        showMessage(authMessage, error.message, "error");
    } finally {
        submitButton.disabled = false;
    }
});

// LOG OUT
logoutButton.addEventListener("click", () => {
    clearAuthSession();
    showLogin();

    showMessage(
        authMessage,
        "You have been logged out.",
        "success"
    );
});

// NAVIGATION
document.getElementById("navigation").addEventListener("click", event => {
    const button = event.target.closest("[data-view]");

    if (!button || button.classList.contains("hidden")) {
        return;
    }

    openView(button.dataset.view);
});

// BRAND LINK
document.getElementById("brandLink").addEventListener("click", event => {
    event.preventDefault();

    if (getAuthToken()) {
        openView("dashboard");
    }
});

// INITIALIZE APPLICATION
function initializeApplication() {
    const token = getAuthToken();
    const user = getCurrentUser();

    if (token && user && user.role) {
        showApplication(user);
        return;
    }

    clearAuthSession();
    showLogin();
}

initializeApplication();
