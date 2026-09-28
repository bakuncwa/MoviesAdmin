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
    const catalog = document.getElementById('movie-catalog');
    const hero = document.getElementById('movie-hero');
    const searchInput = document.getElementById('movie-search');

    // SweetAlert2 follows the site's light/dark mode (data-bs-theme on <html>, set by site.js).
    function swal(options) {
        const theme = document.documentElement.getAttribute('data-bs-theme') === 'light' ? 'light' : 'dark';
        return Swal.fire({ theme, ...options });
    }

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
                await swal({ icon: 'error', title: 'Could not load that movie.', text: `Server responded with ${response.status}.` });
                return;
            }
            modalContent.innerHTML = await response.text();
            bindModalContent();
            movieModal.show();
        } catch (err) {
            await swal({ icon: 'error', title: 'Something went wrong', text: 'Could not reach the server. Please try again.' });
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
        button.innerHTML = '<i class="fa-solid fa-spinner fa-spin me-1" aria-hidden="true"></i>Loading...';

        try {
            const response = await fetch(trailerUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok) {
                throw new Error(`Server responded with ${response.status}`);
            }
            slot.innerHTML = await response.text();
        } catch (err) {
            await swal({ icon: 'error', title: 'Could not load the trailer.' });
            button.disabled = false;
            button.innerHTML = '<i class="fa-solid fa-play me-1" aria-hidden="true"></i>Watch Trailer';
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
                await refreshAll();
                await swal({ toast: true, position: 'top-end', icon: 'success', title: 'Movie saved', showConfirmButton: false, timer: 2000, timerProgressBar: true });
                return;
            }

            // 422 Unprocessable Entity: the server re-rendered the form with validation messages.
            modalContent.innerHTML = await response.text();
            bindModalContent();
        } catch (err) {
            await swal({ icon: 'error', title: 'Something went wrong', text: 'Could not save the movie. Please try again.' });
        } finally {
            submitBtn?.removeAttribute('disabled');
        }
    }

    // --- Async search + refreshing the hero/catalog ------------------------------------------

    async function refreshCatalog() {
        const term = searchInput ? searchInput.value : '';
        const response = await fetch(`/Movies/Search?q=${encodeURIComponent(term)}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (response.ok) {
            catalog.innerHTML = await response.text();
        }
        // A non-ok response means the regex validation in MoviesController.Search rejected the
        // term; leave the catalog showing whatever it last showed rather than clearing it.
    }

    async function refreshHero() {
        const response = await fetch('/Movies/Hero', { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (response.ok) {
            hero.innerHTML = await response.text();
        }
    }

    // After any create/edit/delete: the featured movie and the catalog may both have changed.
    function refreshAll() {
        return Promise.all([refreshCatalog(), refreshHero()]);
    }

    let searchDebounce = null;
    searchInput?.addEventListener('input', () => {
        clearTimeout(searchDebounce);
        searchDebounce = setTimeout(refreshCatalog, 300);
    });

    // --- Grid/list toggle (remembered per browser; storage may be unavailable) -----------------

    const viewButtons = document.querySelectorAll('[data-catalog-view]');

    function setCatalogView(view) {
        catalog.classList.toggle('is-grid', view === 'grid');
        catalog.classList.toggle('is-list', view === 'list');
        viewButtons.forEach(btn => btn.setAttribute('aria-pressed', String(btn.dataset.catalogView === view)));
    }

    try {
        const savedView = localStorage.getItem('movies-catalog-view');
        if (savedView === 'grid' || savedView === 'list') {
            setCatalogView(savedView);
        }
    } catch { /* keep the default grid view */ }

    viewButtons.forEach(btn => btn.addEventListener('click', () => {
        const view = btn.dataset.catalogView;
        setCatalogView(view);
        try { localStorage.setItem('movies-catalog-view', view); } catch { /* not persisted */ }
    }));

    // --- Create/Edit/View delegated clicks -----------------------------------------------------

    document.addEventListener('click', (event) => {
        // Delegated (like the row/card actions below) so it keeps working if the toolbar is re-rendered.
        if (event.target.closest('.movie-create-btn')) {
            loadModal('/Movies/CreateModal');
            return;
        }

        const editBtn = event.target.closest('.movie-edit-btn');
        if (editBtn) {
            // From inside the View modal this swaps the modal's content in place.
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

        const result = await swal({
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
            confirmButtonColor: '#e11d48',
            cancelButtonText: 'Cancel',
            focusCancel: true
        });

        if (!result.isConfirmed) {
            return;
        }

        beginPendingDelete(id, title);
    }

    // Both the poster card and the list row for this movie (only one is visible at a time).
    function movieElements(id) {
        return document.querySelectorAll(`[data-movie-id="${CSS.escape(String(id))}"]`);
    }

    function setPendingDelete(id, pending) {
        movieElements(id).forEach(el => el.classList.toggle('movie-pending-delete', pending));
    }

    function beginPendingDelete(id, title) {
        setPendingDelete(id, true);

        swal({
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
                await commitDelete(id);
            } else {
                setPendingDelete(id, false);
            }
        });
    }

    async function commitDelete(id) {
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
                movieElements(id).forEach(el => el.remove());
                await refreshAll();
            } else {
                setPendingDelete(id, false);
                await swal({ icon: 'error', title: 'Could not delete the movie.' });
            }
        } catch (err) {
            setPendingDelete(id, false);
            await swal({ icon: 'error', title: 'Could not delete the movie.' });
        }
    }
})();
