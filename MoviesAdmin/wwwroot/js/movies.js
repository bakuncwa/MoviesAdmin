// Movies admin CRUD page (Views/Movies/Index.cshtml). Only runs there — bails out immediately if
// the page doesn't have the modal shell this file depends on.
(() => {
    'use strict';

    const movieModalEl = document.getElementById('movie-modal');
    if (!movieModalEl) {
        return;
    }

    const movieModal = new bootstrap.Modal(movieModalEl);
    const modalContent = document.getElementById('movie-modal-content');
    const tableBody = document.getElementById('movie-table-body');
    const searchInput = document.getElementById('movie-search');
    const createBtn = document.getElementById('create-movie-btn');

    // Reset the modal body once it's fully hidden, so the next open never briefly shows stale content.
    movieModalEl.addEventListener('hidden.bs.modal', () => {
        modalContent.innerHTML = '';
    });

    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('#antiforgery-form input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    // --- Loading Create/Edit/View content into the shared modal -----------------------------

    async function loadModal(url) {
        try {
            const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok) {
                await Swal.fire({ icon: 'error', title: 'Could not load that movie.', text: `Server responded with ${response.status}.` });
                return;
            }
            modalContent.innerHTML = await response.text();
            bindModalContent();
            movieModal.show();
        } catch (err) {
            await Swal.fire({ icon: 'error', title: 'Something went wrong', text: 'Could not reach the server. Please try again.' });
        }
    }

    function bindModalContent() {
        const form = modalContent.querySelector('#movie-form');
        form?.addEventListener('submit', onFormSubmit);

        const fileInput = modalContent.querySelector('input[type="file"]');
        const preview = document.getElementById('movie-form-preview');
        fileInput?.addEventListener('change', () => {
            const file = fileInput.files && fileInput.files[0];
            if (file && preview) {
                preview.src = URL.createObjectURL(file);
            }
        });

        // Details (View) modal: lazily fetch the trailer <iframe> only once "Watch Trailer" is clicked.
        const trailerBtn = modalContent.querySelector('.movie-trailer-btn');
        trailerBtn?.addEventListener('click', onTrailerButtonClick);
    }

    async function onTrailerButtonClick(event) {
        const button = event.currentTarget;
        const slot = document.getElementById('movie-trailer-slot');
        const trailerUrl = slot?.dataset.trailerUrl;
        if (!slot || !trailerUrl) {
            return;
        }

        button.disabled = true;
        button.textContent = 'Loading...';

        try {
            const response = await fetch(trailerUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok) {
                throw new Error(`Server responded with ${response.status}`);
            }
            slot.innerHTML = await response.text();
        } catch (err) {
            await Swal.fire({ icon: 'error', title: 'Could not load the trailer.' });
            button.disabled = false;
            button.innerHTML = '&#9654; Watch Trailer';
        }
    }

    async function onFormSubmit(event) {
        event.preventDefault();
        const form = event.target;
        const submitBtn = form.querySelector('button[type="submit"]');
        submitBtn?.setAttribute('disabled', 'disabled');

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            const contentType = response.headers.get('content-type') || '';

            if (response.ok && contentType.includes('application/json')) {
                movieModal.hide();
                await refreshTable();
                await Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: 'Movie saved', showConfirmButton: false, timer: 2000, timerProgressBar: true });
                return;
            }

            // 422 Unprocessable Entity: the server re-rendered the form with validation messages.
            modalContent.innerHTML = await response.text();
            bindModalContent();
        } catch (err) {
            await Swal.fire({ icon: 'error', title: 'Something went wrong', text: 'Could not save the movie. Please try again.' });
        } finally {
            submitBtn?.removeAttribute('disabled');
        }
    }

    // --- Async search -------------------------------------------------------------------------

    async function refreshTable() {
        const term = searchInput ? searchInput.value : '';
        const response = await fetch(`/Movies/Search?q=${encodeURIComponent(term)}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (response.ok) {
            tableBody.innerHTML = await response.text();
        }
        // A non-ok response means the regex validation in MoviesController.Search rejected the
        // term; leave the table showing whatever it last showed rather than clearing it.
    }

    let searchDebounce = null;
    searchInput?.addEventListener('input', () => {
        clearTimeout(searchDebounce);
        searchDebounce = setTimeout(refreshTable, 300);
    });

    // --- Create/Edit/View delegated clicks -----------------------------------------------------

    createBtn?.addEventListener('click', () => loadModal('/Movies/CreateModal'));

    document.addEventListener('click', (event) => {
        const editBtn = event.target.closest('.movie-edit-btn');
        if (editBtn) {
            // If Edit was clicked from inside the View modal's footer, close that one first.
            if (movieModalEl.classList.contains('show') && modalContent.querySelector('.movie-trailer-btn, #movie-trailer-slot')) {
                movieModal.hide();
            }
            loadModal(`/Movies/EditModal/${editBtn.dataset.id}`);
            return;
        }

        const viewBtn = event.target.closest('.movie-view-btn');
        if (viewBtn) {
            loadModal(`/Movies/DetailsModal/${viewBtn.dataset.id}`);
            return;
        }

        const deleteBtn = event.target.closest('.movie-delete-btn');
        if (deleteBtn) {
            confirmDelete(deleteBtn.dataset);
        }
    });

    // --- Delete: SweetAlert2 confirm, then a 10-second "Undo" window before anything is removed ---

    async function confirmDelete(data) {
        const { id, title, image, release, runtime, genres } = data;

        const result = await Swal.fire({
            title: 'Delete this movie?',
            html: `
                <div class="d-flex gap-3 text-start align-items-start">
                    <img src="${image}" alt="${title} poster" class="rounded border flex-shrink-0" style="width: 96px; height: 144px; object-fit: cover;" />
                    <div>
                        <div class="fw-semibold mb-1">${title}</div>
                        <div class="small text-body-secondary">${release} &middot; ${runtime}</div>
                        <div class="small text-body-secondary">${genres}</div>
                        <p class="mt-2 mb-0 small">You'll have 10 seconds to undo this before it's permanent.</p>
                    </div>
                </div>
            `,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Delete',
            confirmButtonColor: '#dc3545',
            cancelButtonText: 'Cancel',
            focusCancel: true
        });

        if (!result.isConfirmed) {
            return;
        }

        beginPendingDelete(id, title);
    }

    function beginPendingDelete(id, title) {
        const row = document.getElementById(`movie-row-${id}`);
        row?.classList.add('movie-row-pending-delete');

        Swal.fire({
            toast: true,
            position: 'bottom-end',
            icon: 'info',
            title: `"${title}" will be deleted`,
            showConfirmButton: true,
            confirmButtonText: 'Undo',
            showCloseButton: true,
            timer: 10000,
            timerProgressBar: true,
            didOpen: (toastEl) => {
                toastEl.addEventListener('mouseenter', Swal.stopTimer);
                toastEl.addEventListener('mouseleave', Swal.resumeTimer);
            }
        }).then(async (result) => {
            // Only a fully-elapsed timer counts as "didn't undo in time" — clicking Undo, the
            // close button, or clicking away are all treated as Undo (safer default).
            if (result.dismiss === Swal.DismissReason.timer) {
                await commitDelete(id, row);
            } else {
                row?.classList.remove('movie-row-pending-delete');
            }
        });
    }

    async function commitDelete(id, row) {
        try {
            const response = await fetch(`/Movies/Delete/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: `__RequestVerificationToken=${encodeURIComponent(getAntiForgeryToken())}`
            });

            if (response.ok) {
                row?.remove();
            } else {
                row?.classList.remove('movie-row-pending-delete');
                await Swal.fire({ icon: 'error', title: 'Could not delete the movie.' });
            }
        } catch (err) {
            row?.classList.remove('movie-row-pending-delete');
            await Swal.fire({ icon: 'error', title: 'Could not delete the movie.' });
        }
    }
})();
