let booksList = [];
let selectedBook = null;
let editingBook = null;

function escapeBookHtml(value) {
    return String(value ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function getBookValue(book, property) {
    if (!book) return undefined;

    return book[property] ?? book[
        property.charAt(0).toLowerCase() + property.slice(1)
    ];
}

function getBookRole() {
    return String(getCurrentUser()?.role || "").toLowerCase();
}

function canManageBooks() {
    const role = getBookRole();
    return role === "librarian";
}

function isMember() {
    return getBookRole() === "member";
}

function getBookId(book) {
    return getBookValue(book, "Id");
}

function normalizeBooksResponse(result) {
    if (Array.isArray(result)) return result;
    if (Array.isArray(result?.items)) return result.items;
    if (Array.isArray(result?.data)) return result.data;
    if (Array.isArray(result?.$values)) return result.$values;

    return [];
}

function renderBooksView() {
    selectedBook = null;
    editingBook = null;

    viewContainer.innerHTML = `
        <div class="books-header">
            <div>
                <h2>Books Management</h2>
                <p class="muted">View and manage the library book collection.</p>
            </div>

            ${canManageBooks() ? `
                <button class="button button-primary" id="addBookButton" type="button">
                    Add New Book
                </button>
            ` : ""}
        </div>

        <div class="books-search">
            <label for="bookSearch">Search books</label>
            <input
                type="search"
                id="bookSearch"
                placeholder="Search by ISBN, title, author, or category..."
            >
        </div>

        <div id="booksMessage" class="message" role="status"></div>
        <div id="booksCount" class="books-count"></div>
        <div id="booksList"></div>
        <div id="bookDetails"></div>
        <div id="bookFormContainer"></div>
    `;

    document.getElementById("addBookButton")?.addEventListener("click", () => {
        showBookForm();
    });

    document.getElementById("bookSearch").addEventListener("input", event => {
        displayBooks(event.target.value);
    });

    document.getElementById("booksList").addEventListener("click", event => {
        const button = event.target.closest("[data-action]");

        if (!button) return;

        const bookId = Number(button.dataset.id);
        const book = booksList.find(item => Number(getBookId(item)) === bookId);

        if (!book) return;

        if (button.dataset.action === "view") {
            showBookDetails(book);
        }

        if (button.dataset.action === "reserve" && isMember()) {
            reserveBook(book);
        }

        if (button.dataset.action === "edit" && canManageBooks()) {
            showBookForm(book);
        }

        if (button.dataset.action === "deactivate" && canManageBooks()) {
            deactivateBook(book);
        }
    });

    loadBooks();
}

async function loadBooks() {
    const message = document.getElementById("booksMessage");

    if (!message) return;

    message.className = "message";
    message.textContent = "Loading books...";

    try {
        const result = await apiRequest("/Books");

        booksList = normalizeBooksResponse(result).filter(book =>
            getBookValue(book, "IsActive") === true
        );

        displayBooks(document.getElementById("bookSearch")?.value || "");

        message.textContent = "";
    } catch (error) {
        message.textContent = error.message || "Unable to load books.";
        message.className = "message error";
    }
}

function displayBooks(searchTerm = "") {
    const container = document.getElementById("booksList");
    const count = document.getElementById("booksCount");

    if (!container || !count) return;

    const search = searchTerm.trim().toLowerCase();

    const filteredBooks = booksList.filter(book => {
        const searchableValues = [
            getBookValue(book, "ISBN"),
            getBookValue(book, "Title"),
            getBookValue(book, "Author"),
            getBookValue(book, "Category")
        ];

        return searchableValues.some(value =>
            String(value ?? "").toLowerCase().includes(search)
        );
    });

    count.innerHTML = `<p class="muted">${filteredBooks.length} book(s) found</p>`;

    if (filteredBooks.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <h3>No books found</h3>
                <p class="muted">There are no books matching your search.</p>
            </div>
        `;
        return;
    }

    container.innerHTML = `
        <div class="table-wrapper">
            <table class="data-table">
                <thead>
                    <tr>
                        <th>ISBN</th>
                        <th>Title</th>
                        <th>Author</th>
                        <th>Category</th>
                        <th>Available</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    ${filteredBooks.map(book => {
        const availableCopies = Number(
            getBookValue(book, "AvailableCopies") ?? 0
        );

        return `
                            <tr>
                                <td>${escapeBookHtml(getBookValue(book, "ISBN"))}</td>
                                <td>${escapeBookHtml(getBookValue(book, "Title"))}</td>
                                <td>${escapeBookHtml(getBookValue(book, "Author"))}</td>
                                <td>${escapeBookHtml(getBookValue(book, "Category") || "Uncategorized")}</td>
                                <td>
                                    ${escapeBookHtml(availableCopies)}
                                    /
                                    ${escapeBookHtml(getBookValue(book, "TotalCopies"))}
                                </td>
                                <td>
                                    <button
                                        class="button button-outline"
                                        type="button"
                                        data-action="view"
                                        data-id="${Number(getBookId(book))}">
                                        View
                                    </button>

                                    ${isMember() ? `
                                        <button
                                            class="button button-primary"
                                            type="button"
                                            data-action="reserve"
                                            data-id="${Number(getBookId(book))}"
                                            ${availableCopies <= 0 ? "disabled" : ""}>
                                            Reserve Book
                                        </button>
                                    ` : ""}

                                    ${canManageBooks() ? `
                                        <button
                                            class="button button-outline"
                                            type="button"
                                            data-action="edit"
                                            data-id="${Number(getBookId(book))}">
                                            Edit
                                        </button>

                                        <button
                                            class="button button-outline"
                                            type="button"
                                            data-action="deactivate"
                                            data-id="${Number(getBookId(book))}">
                                            Deactivate
                                        </button>
                                    ` : ""}
                                </td>
                            </tr>
                        `;
    }).join("")}
                </tbody>
            </table>
        </div>
    `;
}

async function reserveBook(book) {
    if (!isMember()) return;

    const bookId = Number(getBookId(book));
    const title = getBookValue(book, "Title");
    const availableCopies = Number(getBookValue(book, "AvailableCopies") ?? 0);

    if (!Number.isInteger(bookId) || bookId <= 0) {
        showBooksMessage("Invalid book ID.", "error");
        return;
    }

    if (availableCopies <= 0) {
        showBooksMessage("This book currently has no available copies.", "error");
        return;
    }

    const confirmed = confirm(
        `Do you want to reserve "${title}"?\n\nYour reservation will be submitted for staff approval.`
    );

    if (!confirmed) return;

    const buttons = document.querySelectorAll(
        `[data-action="reserve"][data-id="${bookId}"]`
    );

    buttons.forEach(button => {
        button.disabled = true;
        button.textContent = "Submitting...";
    });

    try {
        const result = await apiRequest("/BookReservations", {
            method: "POST",
            body: JSON.stringify({
                bookId: bookId
            })
        });

        showBooksMessage(
            result?.message || "Reservation submitted successfully. Please wait for staff approval.",
            "success"
        );

        await loadBooks();
    } catch (error) {
        showBooksMessage(
            error.message || "Unable to submit your reservation.",
            "error"
        );

        buttons.forEach(button => {
            button.disabled = false;
            button.textContent = "Reserve Book";
        });
    }
}

function showBooksMessage(message, type = "") {
    const container = document.getElementById("booksMessage");

    if (!container) return;

    container.textContent = message;
    container.className = type ? `message ${type}` : "message";
}

function showBookDetails(book) {
    selectedBook = book;

    const container = document.getElementById("bookDetails");
    if (!container) return;

    const publishedYear = getBookValue(book, "PublishedYear");
    const availableCopies = Number(getBookValue(book, "AvailableCopies") ?? 0);

    container.innerHTML = `
        <div class="panel book-details-panel">
            <div class="books-header">
                <h3>Book Details</h3>
                <button class="button button-outline" id="closeBookDetails" type="button">
                    Close
                </button>
            </div>

            <p><strong>ISBN:</strong> ${escapeBookHtml(getBookValue(book, "ISBN"))}</p>
            <p><strong>Title:</strong> ${escapeBookHtml(getBookValue(book, "Title"))}</p>
            <p><strong>Author:</strong> ${escapeBookHtml(getBookValue(book, "Author"))}</p>
            <p><strong>Category:</strong> ${escapeBookHtml(getBookValue(book, "Category") || "Uncategorized")}</p>
            <p><strong>Published Year:</strong> ${escapeBookHtml(publishedYear ?? "Not specified")}</p>
            <p><strong>Total Copies:</strong> ${escapeBookHtml(getBookValue(book, "TotalCopies"))}</p>
            <p><strong>Available Copies:</strong> ${escapeBookHtml(availableCopies)}</p>

            ${isMember() ? `
                <button
                    class="button button-primary"
                    id="reserveBookDetailsButton"
                    type="button"
                    ${availableCopies <= 0 ? "disabled" : ""}>
                    Reserve Book
                </button>
            ` : ""}
        </div>
    `;

    document.getElementById("closeBookDetails").addEventListener("click", () => {
        container.innerHTML = "";
        selectedBook = null;
    });

    document.getElementById("reserveBookDetailsButton")?.addEventListener("click", () => {
        reserveBook(book);
    });
}

function showBookForm(book = null) {
    if (!canManageBooks()) return;

    editingBook = book;

    const container = document.getElementById("bookFormContainer");
    if (!container) return;

    const isEditing = Boolean(book);

    container.innerHTML = `
        <div class="panel book-form-panel">
            <h3>${isEditing ? "Edit Book" : "Add New Book"}</h3>

            <form id="bookForm">
                <label for="bookISBN">ISBN</label>
                <input
                    id="bookISBN"
                    type="text"
                    maxlength="30"
                    required
                    value="${escapeBookHtml(getBookValue(book, "ISBN") || "")}"
                >

                <label for="bookTitle">Title</label>
                <input
                    id="bookTitle"
                    type="text"
                    maxlength="200"
                    required
                    value="${escapeBookHtml(getBookValue(book, "Title") || "")}"
                >

                <label for="bookAuthor">Author</label>
                <input
                    id="bookAuthor"
                    type="text"
                    maxlength="150"
                    required
                    value="${escapeBookHtml(getBookValue(book, "Author") || "")}"
                >

                <label for="bookCategory">Category</label>
                <input
                    id="bookCategory"
                    type="text"
                    maxlength="100"
                    value="${escapeBookHtml(getBookValue(book, "Category") || "")}"
                >

                <label for="bookPublishedYear">Published Year</label>
                <input
                    id="bookPublishedYear"
                    type="number"
                    min="0"
                    max="2100"
                    value="${escapeBookHtml(getBookValue(book, "PublishedYear") ?? "")}"
                >

                <label for="bookTotalCopies">Total Copies</label>
                <input
                    id="bookTotalCopies"
                    type="number"
                    min="0"
                    max="10000"
                    required
                    value="${escapeBookHtml(getBookValue(book, "TotalCopies") ?? 0)}"
                >

                <div id="bookFormMessage" class="message" role="alert"></div>

                <div class="form-actions">
                    <button class="button button-primary" type="submit">
                        ${isEditing ? "Save Changes" : "Add Book"}
                    </button>

                    <button class="button button-outline" id="cancelBookForm" type="button">
                        Cancel
                    </button>
                </div>
            </form>
        </div>
    `;

    document.getElementById("bookForm").addEventListener("submit", saveBook);

    document.getElementById("cancelBookForm").addEventListener("click", () => {
        container.innerHTML = "";
        editingBook = null;
    });

    container.scrollIntoView({ behavior: "smooth", block: "start" });
}

async function saveBook(event) {
    event.preventDefault();

    if (!canManageBooks()) return;

    const message = document.getElementById("bookFormMessage");
    const submitButton = event.currentTarget.querySelector("button[type='submit']");

    const isbn = document.getElementById("bookISBN").value.trim();
    const title = document.getElementById("bookTitle").value.trim();
    const author = document.getElementById("bookAuthor").value.trim();
    const category = document.getElementById("bookCategory").value.trim();
    const yearInput = document.getElementById("bookPublishedYear").value;
    const totalCopies = Number(document.getElementById("bookTotalCopies").value);

    const publishedYear = yearInput === "" ? null : Number(yearInput);

    if (!isbn || !title || !author) {
        message.textContent = "ISBN, title, and author are required.";
        message.className = "message error";
        return;
    }

    if (isbn.length > 30 || title.length > 200 || author.length > 150 || category.length > 100) {
        message.textContent = "One or more fields exceed the allowed character limit.";
        message.className = "message error";
        return;
    }

    if (
        !Number.isInteger(totalCopies) ||
        totalCopies < 0 ||
        totalCopies > 10000
    ) {
        message.textContent = "Total copies must be between 0 and 10000.";
        message.className = "message error";
        return;
    }

    if (
        publishedYear !== null &&
        (!Number.isInteger(publishedYear) || publishedYear < 0 || publishedYear > 2100)
    ) {
        message.textContent = "Published year must be between 0 and 2100.";
        message.className = "message error";
        return;
    }

    const oldTotal = Number(getBookValue(editingBook, "TotalCopies") ?? 0);
    const oldAvailable = Number(getBookValue(editingBook, "AvailableCopies") ?? 0);
    const unavailableCopies = editingBook ? oldTotal - oldAvailable : 0;

    if (totalCopies < unavailableCopies) {
        message.textContent = `Total copies cannot be less than ${unavailableCopies}, because that many copies are currently unavailable.`;
        message.className = "message error";
        return;
    }

    const payload = {
        isbn,
        title,
        author,
        category,
        publishedYear,
        totalCopies,
        availableCopies: totalCopies - unavailableCopies
    };

    submitButton.disabled = true;
    message.textContent = "Saving book...";
    message.className = "message";

    try {
        if (editingBook) {
            await apiRequest(`/Books/${getBookId(editingBook)}`, {
                method: "PUT",
                body: JSON.stringify(payload)
            });
        } else {
            await apiRequest("/Books", {
                method: "POST",
                body: JSON.stringify(payload)
            });
        }

        editingBook = null;
        document.getElementById("bookFormContainer").innerHTML = "";
        document.getElementById("bookDetails").innerHTML = "";

        showBooksMessage("Book saved successfully.", "success");

        await loadBooks();
    } catch (error) {
        message.textContent = error.message || "Unable to save book.";
        message.className = "message error";
        submitButton.disabled = false;
    }
}

async function deactivateBook(book) {
    if (!canManageBooks()) return;

    const title = getBookValue(book, "Title");

    const confirmed = confirm(
        `Are you sure you want to deactivate "${title}"?`
    );

    if (!confirmed) return;

    try {
        await apiRequest(`/Books/${getBookId(book)}`, {
            method: "DELETE"
        });

        document.getElementById("bookDetails").innerHTML = "";
        document.getElementById("bookFormContainer").innerHTML = "";

        showBooksMessage("Book deactivated successfully.", "success");

        await loadBooks();
    } catch (error) {
        showBooksMessage(
            error.message || "Unable to deactivate book.",
            "error"
        );
    }
}

window.renderBooksView = renderBooksView;
