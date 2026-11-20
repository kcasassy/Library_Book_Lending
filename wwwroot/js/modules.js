


const moduleRoot = () => document.getElementById("viewContainer");
const currentRole = () => String(getCurrentUser()?.role || "").toLowerCase();
const isStaff = () => currentRole() === "librarian";
const esc = value => String(value ?? "").replace(/[&<>"']/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[char]));
const dateValue = value => value ? new Date(value).toLocaleString() : "—";
const inputDate = value => value ? new Date(value).toISOString().slice(0, 16) : "";

function statusBadge(status) {
    const colors = {
        Pending: "#f5c542",
        Approved: "#2563eb",
        Rejected: "#b4232f",
        Cancelled: "#64748b",
        Active: "#2563eb",
        Inactive: "#64748b"
    };
    const color = colors[status] || "#64748b";
    return `<span style="display:inline-block;padding:4px 9px;border-radius:20px;background:${color};color:#fff;font-size:12px;font-weight:600;">${esc(status)}</span>`;
}

function moduleShell(title, description, actions = "") {
    moduleRoot().innerHTML = `<div class="module-heading"><div><h2>${esc(title)}</h2><p class="muted">${esc(description)}</p></div>${actions}</div><p class="message" id="moduleMessage" role="status"></p><div id="moduleFormHost"></div><div id="moduleTableHost"><p class="muted">Loading records...</p></div>`;
}

function moduleMessage(message, type = "") {
    const el = document.getElementById("moduleMessage");
    if (el) {
        el.textContent = message;
        el.className = `message ${type}`;
    }
}

function tableMarkup(columns, rows, actions = () => "") {
    if (!rows.length) return `<p class="muted">No records found.</p>`;

    return `<div class="table-wrap"><table><thead><tr>${columns.map(c => `<th>${esc(c.label)}</th>`).join("")}<th>Actions</th></tr></thead><tbody>${rows.map(row => `<tr>${columns.map(c => `<td>${c.html ? (c.render ? c.render(row) : row[c.key]) : esc(c.render ? c.render(row) : row[c.key])}</td>`).join("")}<td class="table-actions">${actions(row)}</td></tr>`).join("")}</tbody></table></div>`;
}

function actionButton(label, action, id, style = "button-outline") {
    return `<button type="button" class="button ${style} button-small" data-action="${esc(action)}" data-id="${esc(id)}">${esc(label)}</button>`;
}

function bindTableActions(handler) {
    document.getElementById("moduleTableHost")?.addEventListener("click", async event => {
        const button = event.target.closest("[data-action]");
        if (!button) return;

        button.disabled = true;

        try {
            await handler(button.dataset.action, Number(button.dataset.id));
        } catch (error) {
            moduleMessage(error.message, "error");
        } finally {
            button.disabled = false;
        }
    });
}

function formMarkup(fields, submitLabel = "Save") {
    return `<form id="moduleForm" class="module-form">${fields.map(f => `<label>${esc(f.label)}${f.type === "select" ? `<select name="${esc(f.name)}" ${f.required === false ? "" : "required"}>${(f.options || []).map(o => `<option value="${esc(o.value)}">${esc(o.label)}</option>`).join("")}</select>` : f.type === "textarea" ? `<textarea name="${esc(f.name)}" rows="3" ${f.required === false ? "" : "required"}>${esc(f.value || "")}</textarea>` : `<input name="${esc(f.name)}" type="${esc(f.type || "text")}" value="${esc(f.value || "")}" ${f.min !== undefined ? `min="${f.min}"` : ""} ${f.max !== undefined ? `max="${f.max}"` : ""} ${f.required === false ? "" : "required"}>`}</label>`).join("")}<div class="form-actions"><button class="button button-primary" type="submit">${esc(submitLabel)}</button><button class="button button-outline" id="cancelModuleForm" type="button">Cancel</button></div></form>`;
}

function showModuleForm(fields, submitLabel, onSubmit) {
    const host = document.getElementById("moduleFormHost");
    host.innerHTML = formMarkup(fields, submitLabel);

    host.querySelector("#cancelModuleForm").addEventListener("click", () => host.innerHTML = "");

    host.querySelector("form").addEventListener("submit", async event => {
        event.preventDefault();

        const button = event.currentTarget.querySelector("button[type=submit]");
        button.disabled = true;

        const data = Object.fromEntries(new FormData(event.currentTarget).entries());

        try {
            await onSubmit(data);
            host.innerHTML = "";
        } catch (error) {
            moduleMessage(error.message, "error");
        } finally {
            button.disabled = false;
        }
    });
}

async function loadList(endpoint) {
    const data = await apiRequest(endpoint);
    return Array.isArray(data) ? data : [];
}

async function renderBookReservations() {
    const staff = isStaff();

    moduleShell(
        "Book Reservations",
        staff ? "Review member requests and update pending reservations." : "Reserve books and track your reservation requests.",
        staff ? "" : `<button class="button button-primary" id="newBookReservation">Reserve a book</button>`
    );

    const load = async () => {
        const rows = await loadList(staff ? "/BookReservations" : "/BookReservations/my");

        document.getElementById("moduleTableHost").innerHTML = tableMarkup([
            { label: "ID", key: "id" },
            { label: "Book", key: "bookTitle" },
            { label: "ISBN", key: "isbn" },
            ...(staff ? [{ label: "Member", key: "memberName" }] : []),
            { label: "Reserved", render: r => dateValue(r.reservedAt) },
            { label: "Expires", render: r => dateValue(r.expiresAt) },
            { label: "Status", key: "status" }
        ], rows, r =>
            staff && r.status === "Pending"
                ? actionButton("Approve", "approve", r.id, "button-primary") + actionButton("Reject", "reject", r.id)
                : !staff && r.status === "Pending"
                    ? actionButton("Cancel", "cancel", r.id)
                    : "—"
        );
    };

    await load();

    if (!staff) {
        document.getElementById("newBookReservation").addEventListener("click", async () => {
            const books = await loadList("/Books");

            showModuleForm([
                {
                    name: "bookId",
                    label: "Book",
                    type: "select",
                    options: books.filter(b => b.isActive && b.availableCopies > 0).map(b => ({
                        value: b.id,
                        label: `${b.title} (${b.availableCopies} available)`
                    }))
                }
            ], "Submit reservation", async d => {
                await apiRequest("/BookReservations", {
                    method: "POST",
                    body: JSON.stringify({ bookId: Number(d.bookId) })
                });

                moduleMessage("Reservation submitted.", "success");
                await load();
            });
        });
    }

    bindTableActions(async (action, id) => {
        if (action === "cancel") {
            await apiRequest(`/BookReservations/${id}/cancel`, { method: "PUT" });
        } else {
            await apiRequest(`/BookReservations/${id}/status`, {
                method: "PUT",
                body: JSON.stringify({ status: action === "approve" ? "Approved" : "Rejected" })
            });
        }

        await load();
        moduleMessage("Reservation updated.", "success");
    });
}

async function renderBorrowings() {
    const staff = isStaff();

    moduleShell(
        "Borrowings",
        "Record book loans, review borrowing history, and process returns.",
        staff ? `<button class="button button-primary" id="newBorrowing">Record borrowing</button><button class="button button-outline" id="overdueBorrowings">Show overdue</button>` : ""
    );

    const load = async endpoint => {
        const rows = await loadList(endpoint || (staff ? "/Borrowings" : "/Borrowings/my"));

        document.getElementById("moduleTableHost").innerHTML = tableMarkup([
            { label: "ID", key: "id" },
            { label: "Book", key: "bookTitle" },
            { label: "ISBN", key: "isbn" },
            ...(staff ? [{ label: "Member", key: "memberName" }] : []),
            { label: "Borrowed", render: r => dateValue(r.borrowedAt) },
            { label: "Due", render: r => dateValue(r.dueDate) },
            { label: "Returned", render: r => dateValue(r.returnedAt) },
            { label: "Status", key: "status" }
        ], rows, r => staff && !r.returnedAt
            ? actionButton("Mark returned", "return", r.id, "button-primary")
            : "—"
        );
    };

    await load();

    if (staff) {
        document.getElementById("newBorrowing").addEventListener("click", async () => {
            const [users, books] = await Promise.all([
                loadList("/Users"),
                loadList("/Books")
            ]);

            showModuleForm([
                {
                    name: "userId",
                    label: "Member",
                    type: "select",
                    options: users.filter(u => u.role === "Member" && u.isActive).map(u => ({
                        value: u.id,
                        label: `${u.fullName} — ${u.memberNumber || u.email}`
                    }))
                },
                {
                    name: "bookId",
                    label: "Book",
                    type: "select",
                    options: books.filter(b => b.isActive && b.availableCopies > 0).map(b => ({
                        value: b.id,
                        label: `${b.title} (${b.availableCopies} available)`
                    }))
                }
            ], "Record loan", async d => {
                await apiRequest("/Borrowings", {
                    method: "POST",
                    body: JSON.stringify({
                        userId: Number(d.userId),
                        bookId: Number(d.bookId)
                    })
                });

                await load();
                moduleMessage("Borrowing recorded.", "success");
            });
        });

        document.getElementById("overdueBorrowings").addEventListener("click", () => load("/Borrowings/overdue"));
    }

    bindTableActions(async (action, id) => {
        if (action === "return") {
            await apiRequest(`/Borrowings/${id}/return`, { method: "PUT" });
        }

        await load();
        moduleMessage("Borrowing updated.", "success");
    });
}

async function renderSeats() {
    const staff = isStaff();

    moduleShell(
        "Seats",
        "View library seats and check their reservation schedules.",
        staff ? `<button class="button button-primary" id="newSeat">Add seat</button>` : ""
    );

    const load = async () => {
        const rows = await loadList("/Seats");

        document.getElementById("moduleTableHost").innerHTML = `
            <div style="display:flex;align-items:center;flex-wrap:wrap;gap:8px;margin:10px 0 14px;">
                <strong style="font-size:12px;">Reservation status:</strong>
                ${statusBadge("Pending")}
                ${statusBadge("Approved")}
            </div>

            ${tableMarkup([
            { label: "ID", key: "id" },
            { label: "Seat", key: "seatNumber" },
            { label: "Location", key: "location" },
            {
                label: "Operational status",
                html: true,
                render: r => statusBadge(r.isActive ? "Active" : "Inactive")
            },
            {
                label: "Reservation schedule",
                html: true,
                render: r => {
                    const reservations = r.reservations || [];

                    if (!reservations.length) {
                        return `<span style="color:#64748b;font-size:12px;">No upcoming reservations</span>`;
                    }

                    return `
                            <div style="display:flex;flex-direction:column;gap:5px;min-width:210px;">
                                ${reservations.map(reservation => `
                                    <div style="display:flex;align-items:center;flex-wrap:wrap;gap:7px;font-size:12px;line-height:1.4;">
                                        ${statusBadge(reservation.status)}
                                        <span>
                                            <strong>${esc(new Date(reservation.startTime).toLocaleDateString())}</strong>
                                            ${esc(new Date(reservation.startTime).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }))}
                                            –
                                            ${esc(new Date(reservation.endTime).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }))}
                                        </span>
                                    </div>
                                `).join("")}
                            </div>
                        `;
                }
            }
        ], rows, r =>
            staff && r.isActive
                ? actionButton("Edit", "edit", r.id) + actionButton("Deactivate", "deactivate", r.id)
                : "—"
        )}
        `;
    };

    await load();

    if (staff) {
        document.getElementById("newSeat").addEventListener("click", () => {
            showModuleForm([
                { name: "seatNumber", label: "Seat number" },
                { name: "location", label: "Location" }
            ], "Create seat", async d => {
                await apiRequest("/Seats", {
                    method: "POST",
                    body: JSON.stringify(d)
                });

                await load();
                moduleMessage("Seat created.", "success");
            });
        });
    }

    bindTableActions(async (action, id) => {
        const rows = await loadList("/Seats");
        const row = rows.find(x => x.id === id);

        if (!row) {
            throw new Error("Seat not found.");
        }

        if (action === "edit") {
            showModuleForm([
                { name: "seatNumber", label: "Seat number", value: row.seatNumber },
                { name: "location", label: "Location", value: row.location }
            ], "Update seat", async d => {
                await apiRequest(`/Seats/${id}`, {
                    method: "PUT",
                    body: JSON.stringify(d)
                });

                await load();
                moduleMessage("Seat updated.", "success");
            });
        } else if (action === "deactivate") {
            await apiRequest(`/Seats/${id}`, { method: "DELETE" });

            await load();
            moduleMessage("Seat deactivated.", "success");
        }
    });
}

async function renderSeatReservations() {
    const staff = isStaff();

    moduleShell(
        "Seat Reservations",
        "Book a seat for a future time or review reservation requests.",
        staff ? "" : `<button class="button button-primary" id="newSeatReservation">Reserve a seat</button>`
    );

    const load = async () => {
        const rows = await loadList("/SeatReservations");

        document.getElementById("moduleTableHost").innerHTML = tableMarkup([
            { label: "ID", key: "id" },
            { label: "Seat", key: "seatNumber" },
            { label: "Location", key: "location" },
            ...(staff ? [{ label: "Member", key: "memberName" }] : []),
            { label: "Start", render: r => dateValue(r.startTime) },
            { label: "End", render: r => dateValue(r.endTime) },
            { label: "Status", key: "status" }
        ], rows, r => staff && r.status === "Pending"
            ? actionButton("Approve", "approve", r.id, "button-primary") + actionButton("Reject", "reject", r.id)
            : !staff && r.status === "Pending"
                ? actionButton("Cancel", "cancel", r.id)
                : "—"
        );
    };

    await load();

    if (!staff) {
        document.getElementById("newSeatReservation").addEventListener("click", () => {
            const host = document.getElementById("moduleFormHost");
            const start = new Date(Date.now() + 60 * 60 * 1000);
            start.setMinutes(0, 0, 0);
            const end = new Date(start.getTime() + 60 * 60 * 1000);

            const toLocalInput = value => {
                const pad = number => String(number).padStart(2, "0");
                return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}`;
            };

            host.innerHTML = `
                <form id="seatReservationForm" class="module-form">
                    <label>Start time<input name="startTime" type="datetime-local" value="${toLocalInput(start)}" required></label>
                    <label>End time<input name="endTime" type="datetime-local" value="${toLocalInput(end)}" required></label>
                    <label>Available seat<select name="seatId" required disabled><option value="">Choose a valid schedule first</option></select></label>
                    <p class="muted" id="seatAvailabilityMessage" role="status">Checking available seats...</p>
                    <div class="form-actions">
                        <button class="button button-primary" type="submit" disabled>Submit reservation</button>
                        <button class="button button-outline" id="cancelSeatReservationForm" type="button">Cancel</button>
                    </div>
                </form>`;

            const form = host.querySelector("#seatReservationForm");
            const startInput = form.elements.startTime;
            const endInput = form.elements.endTime;
            const seatSelect = form.elements.seatId;
            const submitButton = form.querySelector('button[type="submit"]');
            const availabilityMessage = form.querySelector("#seatAvailabilityMessage");
            let availabilityRequest = 0;

            const refreshAvailability = async () => {
                const requestNumber = ++availabilityRequest;

                seatSelect.disabled = true;
                submitButton.disabled = true;
                seatSelect.innerHTML = `<option value="">Loading available seats...</option>`;

                if (!startInput.value || !endInput.value) {
                    seatSelect.innerHTML = `<option value="">Select start and end times</option>`;
                    availabilityMessage.textContent = "Please select both start and end times.";
                    return;
                }

                const selectedStart = new Date(startInput.value);
                const selectedEnd = new Date(endInput.value);

                if (Number.isNaN(selectedStart.getTime()) || Number.isNaN(selectedEnd.getTime()) || selectedStart <= new Date() || selectedStart >= selectedEnd) {
                    seatSelect.innerHTML = `<option value="">No valid schedule selected</option>`;
                    availabilityMessage.textContent = "Choose a future start time and an end time later than the start time.";
                    return;
                }

                const query = new URLSearchParams({
                    startTime: selectedStart.toISOString(),
                    endTime: selectedEnd.toISOString()
                });

                try {
                    const seats = await apiRequest(`/SeatReservations/availability?${query.toString()}`, { cache: "no-store" });

                    if (requestNumber !== availabilityRequest) return;

                    if (!Array.isArray(seats) || seats.length === 0) {
                        seatSelect.innerHTML = `<option value="">No seats available for this schedule</option>`;
                        availabilityMessage.textContent = "No seats are available for the selected schedule. Please choose another time.";
                        return;
                    }

                    seatSelect.innerHTML = `<option value="">Select a seat</option>` + seats.map(seat =>
                        `<option value="${esc(seat.id)}">${esc(seat.seatNumber)} — ${esc(seat.location)}</option>`
                    ).join("");

                    seatSelect.disabled = false;
                    availabilityMessage.textContent = `${seats.length} seat(s) available for the selected schedule.`;
                    submitButton.disabled = false;
                } catch (error) {
                    if (requestNumber !== availabilityRequest) return;

                    seatSelect.innerHTML = `<option value="">Unable to load seats</option>`;
                    availabilityMessage.textContent = error.message;
                }
            };

            startInput.addEventListener("change", refreshAvailability);
            endInput.addEventListener("change", refreshAvailability);

            form.querySelector("#cancelSeatReservationForm").addEventListener("click", () => {
                host.innerHTML = "";
            });

            form.addEventListener("submit", async event => {
                event.preventDefault();

                if (!seatSelect.value || submitButton.disabled) return;

                submitButton.disabled = true;

                try {
                    await apiRequest("/SeatReservations", {
                        method: "POST",
                        body: JSON.stringify({
                            seatId: Number(seatSelect.value),
                            startTime: new Date(startInput.value).toISOString(),
                            endTime: new Date(endInput.value).toISOString()
                        })
                    });

                    host.innerHTML = "";
                    moduleMessage("Seat reservation submitted successfully.", "success");
                    await load();
                } catch (error) {
                    moduleMessage(error.message, "error");
                    await refreshAvailability();
                } finally {
                    if (host.contains(form)) {
                        submitButton.disabled = !seatSelect.value || seatSelect.disabled;
                    }
                }
            });

            refreshAvailability();
        });
    }

    bindTableActions(async (action, id) => {
        if (action === "cancel") {
            await apiRequest(`/SeatReservations/${id}/cancel`, { method: "PUT" });
        } else {
            await apiRequest(`/SeatReservations/${id}/status`, {
                method: "PUT",
                body: JSON.stringify({ status: action === "approve" ? "Approved" : "Rejected" })
            });
        }

        await load();
        moduleMessage("Seat reservation updated.", "success");
    });
}

async function renderReadingSpaces() {
    const staff = isStaff();

    moduleShell(
        "Reading Spaces",
        "View available reading spaces or manage their capacity.",
        staff ? `<button class="button button-primary" id="newSpace">Add reading space</button>` : ""
    );

    const load = async () => {
        const rows = await loadList("/ReadingSpaces");

        document.getElementById("moduleTableHost").innerHTML = tableMarkup([
            { label: "ID", key: "id" },
            { label: "Name", key: "name" },
            { label: "Location", key: "location" },
            { label: "Capacity", key: "capacity" },
            { label: "Status", render: r => r.isActive ? "Active" : "Inactive" }
        ], rows, r => staff
            ? actionButton("Edit", "edit", r.id) + actionButton("Deactivate", "deactivate", r.id)
            : "—"
        );
    };

    await load();

    if (staff) {
        document.getElementById("newSpace").addEventListener("click", () => {
            showModuleForm([
                { name: "name", label: "Name" },
                { name: "location", label: "Location" },
                { name: "capacity", label: "Capacity", type: "number", min: 1, max: 500 }
            ], "Create space", async d => {
                await apiRequest("/ReadingSpaces", {
                    method: "POST",
                    body: JSON.stringify({ ...d, capacity: Number(d.capacity) })
                });

                await load();
            });
        });
    }

    bindTableActions(async (action, id) => {
        const rows = await loadList("/ReadingSpaces");
        const r = rows.find(x => x.id === id);

        if (!r) throw new Error("Reading space not found.");

        if (action === "edit") {
            showModuleForm([
                { name: "name", label: "Name", value: r.name },
                { name: "location", label: "Location", value: r.location },
                { name: "capacity", label: "Capacity", type: "number", min: 1, max: 500, value: r.capacity }
            ], "Update space", async d => {
                await apiRequest(`/ReadingSpaces/${id}`, {
                    method: "PUT",
                    body: JSON.stringify({ ...d, capacity: Number(d.capacity) })
                });

                await load();
            });
        } else {
            await apiRequest(`/ReadingSpaces/${id}`, { method: "DELETE" });
            await load();
        }
    });
}

async function renderReadingSpaceReservations() {
    const staff = isStaff();

    moduleShell(
        "Reading Space Reservations",
        "Reserve a reading space and review its approval status.",
        staff ? "" : `<button class="button button-primary" id="newSpaceReservation">Reserve a space</button>`
    );

    const load = async () => {
        const rows = await loadList("/ReadingSpaceReservations");

        document.getElementById("moduleTableHost").innerHTML = tableMarkup([
            { label: "ID", key: "id" },
            { label: "Space", key: "readingSpaceName" },
            { label: "Location", key: "location" },
            ...(staff ? [{ label: "Member", key: "memberName" }] : []),
            { label: "People", key: "numberOfPeople" },
            { label: "Start", render: r => dateValue(r.startTime) },
            { label: "End", render: r => dateValue(r.endTime) },
            { label: "Status", key: "status" }
        ], rows, r =>
            staff && r.status === "Pending"
                ? actionButton("Approve", "approve", r.id, "button-primary") + actionButton("Reject", "reject", r.id)
                : !staff && r.status === "Pending"
                    ? actionButton("Cancel", "cancel", r.id)
                    : "—"
        );
    };

    await load();

    if (!staff) {
        document.getElementById("newSpaceReservation").addEventListener("click", async () => {
            const spaces = await loadList("/ReadingSpaces");

            showModuleForm([
                {
                    name: "readingSpaceId",
                    label: "Reading space",
                    type: "select",
                    options: spaces.map(s => ({
                        value: s.id,
                        label: `${s.name} — ${s.location} (capacity ${s.capacity})`
                    }))
                },
                { name: "startTime", label: "Start time", type: "datetime-local" },
                { name: "endTime", label: "End time", type: "datetime-local" },
                { name: "numberOfPeople", label: "Number of people", type: "number", min: 1, max: 500 }
            ], "Submit reservation", async d => {
                await apiRequest("/ReadingSpaceReservations", {
                    method: "POST",
                    body: JSON.stringify({
                        readingSpaceId: Number(d.readingSpaceId),
                        startTime: new Date(d.startTime).toISOString(),
                        endTime: new Date(d.endTime).toISOString(),
                        numberOfPeople: Number(d.numberOfPeople)
                    })
                });

                await load();
            });
        });
    }

    bindTableActions(async (action, id) => {
        if (action === "cancel") {
            await apiRequest(`/ReadingSpaceReservations/${id}/cancel`, { method: "PUT" });
        } else {
            await apiRequest(`/ReadingSpaceReservations/${id}/status`, {
                method: "PUT",
                body: JSON.stringify({ status: action === "approve" ? "Approved" : "Rejected" })
            });
        }

        await load();
    });
}

async function renderQcpl() {
    moduleShell(
        "QCPL External Reservation Monitoring",
        "Track external reservations and record synchronization activity.",
        `<button class="button button-primary" id="newQcpl">Add external record</button>`
    );

    const statuses = ["Pending", "Confirmed", "Cancelled", "Completed"];

    const load = async () => {
        const rows = await loadList("/QcplExternalReservations");

        document.getElementById("moduleTableHost").innerHTML = tableMarkup([
            { label: "ID", key: "id" },
            { label: "Reference", key: "externalReferenceNumber" },
            { label: "Member", key: "memberName" },
            { label: "Book", key: "bookTitle" },
            { label: "Date", render: r => dateValue(r.reservationDate) },
            { label: "Status", key: "reservationStatus" },
            { label: "Last sync", render: r => dateValue(r.lastSyncedAt) }
        ], rows, r =>
            actionButton("Edit", "edit", r.id) + actionButton("Sync now", "sync", r.id, "button-primary")
        );
    };

    await load();

    document.getElementById("newQcpl").addEventListener("click", () => {
        showModuleForm([
            { name: "externalReferenceNumber", label: "External reference number" },
            { name: "memberName", label: "Member name" },
            { name: "bookTitle", label: "Book title" },
            {
                name: "reservationStatus",
                label: "Status",
                type: "select",
                options: statuses.map(s => ({ value: s, label: s }))
            },
            { name: "reservationDate", label: "Reservation date", type: "datetime-local" },
            { name: "notes", label: "Notes", type: "textarea", required: false }
        ], "Create record", async d => {
            await apiRequest("/QcplExternalReservations", {
                method: "POST",
                body: JSON.stringify({
                    ...d,
                    reservationDate: new Date(d.reservationDate).toISOString()
                })
            });

            await load();
        });
    });

    bindTableActions(async (action, id) => {
        if (action === "sync") {
            await apiRequest(`/QcplExternalReservations/${id}/record-sync`, { method: "POST" });
        } else {
            const r = (await loadList("/QcplExternalReservations")).find(x => x.id === id);

            if (!r) throw new Error("External reservation record not found.");

            showModuleForm([
                { name: "externalReferenceNumber", label: "External reference", value: r.externalReferenceNumber },
                { name: "memberName", label: "Member name", value: r.memberName },
                { name: "bookTitle", label: "Book title", value: r.bookTitle },
                {
                    name: "reservationStatus",
                    label: "Status",
                    type: "select",
                    options: statuses.map(s => ({ value: s, label: s }))
                },
                { name: "reservationDate", label: "Reservation date", type: "datetime-local", value: inputDate(r.reservationDate) },
                { name: "notes", label: "Notes", type: "textarea", required: false, value: r.notes }
            ], "Update record", async d => {
                await apiRequest(`/QcplExternalReservations/${id}`, {
                    method: "PUT",
                    body: JSON.stringify({
                        ...d,
                        reservationDate: new Date(d.reservationDate).toISOString()
                    })
                });

                await load();
            });

            return;
        }

        await load();
    });
}

async function renderReports() {
    moduleShell("Library Reports", "Current summary counts from the library database.");

    const r = await apiRequest("/Reports/summary");

    const cards = [
        ["Total books", r.totalBooks],
        ["Total copies", r.totalBookCopies],
        ["Available copies", r.availableBookCopies],
        ["Members", r.totalMembers],
        ["Borrowings", r.totalBorrowings],
        ["Book reservations", r.totalBookReservations],
        ["Total seats", r.totalSeats],
        ["Active seats", r.activeSeats],
        ["Seat reservations", r.totalSeatReservations],
        ["Reading spaces", r.totalReadingSpaces],
        ["Active reading spaces", r.activeReadingSpaces],
        ["Space reservations", r.totalReadingSpaceReservations],
        ["QCPL records", r.totalQcplExternalReservations]
    ];

    document.getElementById("moduleTableHost").innerHTML = `
        <div class="report-grid">
            ${cards.map(([label, value]) => `<article class="report-card"><span>${esc(label)}</span><strong>${esc(value)}</strong></article>`).join("")}
        </div>
        <p class="muted">Generated: ${esc(dateValue(r.generatedAt))}</p>
    `;
}

async function renderUsers() {
    if (!isStaff()) {
        throw new Error("Only librarians can manage members.");
    }

    moduleShell("User Management", "View accounts and update member memberships.");

    const load = async () => {
        const rows = await loadList("/Users");

        document.getElementById("moduleTableHost").innerHTML = tableMarkup([
            { label: "ID", key: "id" },
            { label: "Full name", key: "fullName" },
            { label: "Email", key: "email" },
            { label: "Role", key: "role" },
            { label: "Member no.", key: "memberNumber" },
            { label: "Membership expiry", render: r => dateValue(r.membershipExpiry) },
            { label: "Status", render: r => r.isActive ? "Active" : "Inactive" },
            { label: "Created", render: r => dateValue(r.createdAt) }
        ], rows, r =>
            r.role === "Member"
                ? actionButton("Edit membership", "edit", r.id)
                : "—"
        );
    };

    await load();

    bindTableActions(async (action, id) => {
        if (action !== "edit") return;

        const rows = await loadList("/Users");
        const row = rows.find(x => x.id === id);

        if (!row) {
            throw new Error("Member not found.");
        }

        const statusOptions = row.isActive
            ? [{ value: "true", label: "Active" }, { value: "false", label: "Inactive" }]
            : [{ value: "false", label: "Inactive" }, { value: "true", label: "Active" }];

        showModuleForm([
            {
                name: "membershipExpiry",
                label: "Membership expiry",
                type: "date",
                value: row.membershipExpiry ? inputDate(row.membershipExpiry).slice(0, 10) : ""
            },
            {
                name: "isActive",
                label: "Account status",
                type: "select",
                options: statusOptions
            }
        ], "Update member", async d => {
            await apiRequest(`/Users/${id}`, {
                method: "PUT",
                body: JSON.stringify({
                    membershipExpiry: d.membershipExpiry,
                    isActive: d.isActive === "true"
                })
            });

            await load();
            moduleMessage("Member updated.", "success");
        });
    });
}

async function renderModule(view) {
    try {
        const renderers = {
            bookReservations: renderBookReservations,
            borrowings: renderBorrowings,
            seats: renderSeats,
            seatReservations: renderSeatReservations,
            readingSpaces: renderReadingSpaces,
            readingSpaceReservations: renderReadingSpaceReservations,
            qcpl: renderQcpl,
            reports: renderReports,
            users: renderUsers
        };

        if (!renderers[view]) return false;

        await renderers[view]();
        return true;
    } catch (error) {
        moduleShell("Unable to load section", "The request could not be completed.");
        moduleMessage(error.message, "error");
        return true;
    }
}
