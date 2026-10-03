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
    const queryForm = document.getElementById('catalog-query');

    // SweetAlert2 follows the site's light/dark mode (data-bs-theme on <html>, set by site.js).
    function swal(options) {
        const theme = document.documentElement.getAttribute('data-bs-theme') === 'light' ? 'light' : 'dark';
        return Swal.fire({ theme, ...options });
    }

    // Reset the modal body once it's fully hidden, so the next open never briefly shows stale content.
    movieModalEl.addEventListener('hidden.bs.modal', () => {
        setModalContent('');
    });

    // Every swap of the modal's content goes through here: Tom Select pickers keep their dropdown
    // on <body>, so they're destroyed first or those dropdowns would be left behind.
    function setModalContent(html) {
        modalContent.querySelectorAll('select').forEach(select => select.tomselect?.destroy());
        modalContent.innerHTML = html;
    }

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
            setModalContent(await response.text());
            bindModalContent();
            movieModal.show();
        } catch (err) {
            await swal({ icon: 'error', title: 'Something went wrong', text: 'Could not reach the server. Please try again.' });
        }
    }

    function bindModalContent() {
        const form = modalContent.querySelector('#movie-form');
        if (form) {
            form.addEventListener('submit', onFormSubmit);
            // The form arrives via fetch(), after jquery-validation-unobtrusive scanned the page, so
            // its data-val-* rules have to be registered by hand to get in-browser warnings.
            if (window.jQuery?.validator?.unobtrusive) {
                jQuery.validator.unobtrusive.parse(form);
                // The lookup panel's search boxes aren't part of the movie, so never let them
                // block saving.
                const validator = jQuery(form).data('validator');
                if (validator) {
                    validator.settings.ignore = ':hidden, .lookup-panel :input';
                }
            }

            // Pressing Save would otherwise blur the field being edited first, and its validation
            // message can shift the (vertically centered) dialog so the button moves out from
            // under the pointer before mouseup and the click never lands. Keeping focus where it
            // is avoids the shift; the submit handler then validates every field anyway.
            form.querySelector('button[type="submit"]')?.addEventListener('mousedown', (event) => event.preventDefault());
            bindLookupPanel(form);
            form.querySelectorAll('select[data-credit-picker]').forEach(initCreditPicker);
        }

        const fileInput = modalContent.querySelector('input[type="file"]');
        const preview = document.getElementById('movie-form-preview');
        fileInput?.addEventListener('change', () => {
            const file = fileInput.files && fileInput.files[0];
            if (file && preview) {
                preview.src = URL.createObjectURL(file);
            }
        });

        // An external poster URL previews too, unless an uploaded file is chosen or already saved
        // (uploads take display priority, see MoviePosterResolver).
        const posterUrlInput = form?.querySelector('[name="PosterUrl"]');
        posterUrlInput?.addEventListener('change', () => updatePosterPreview(form));

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

    function hasUploadedPoster(form) {
        const fileInput = form.querySelector('input[type="file"]');
        return !!(fileInput?.files?.length || form.querySelector('[name="ExistingPosterImagePath"]')?.value);
    }

    function updatePosterPreview(form) {
        const preview = document.getElementById('movie-form-preview');
        const url = form.querySelector('[name="PosterUrl"]')?.value.trim();
        if (preview && url && /^https?:\/\//i.test(url) && !hasUploadedPoster(form)) {
            preview.src = url;
        }
    }

    // Client-side check before posting; the server re-validates everything regardless (422).
    function validateForm(form) {
        const $ = window.jQuery;
        if (!$?.validator || $(form).valid()) {
            return true;
        }

        const warning = form.querySelector('#movie-form-warning');
        if (warning) {
            warning.hidden = false;
        }
        form.querySelector('.input-validation-error')?.focus();
        return false;
    }

    async function onFormSubmit(event) {
        event.preventDefault();
        const form = event.target;
        if (!validateForm(form)) {
            return;
        }
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
            setModalContent(await response.text());
            bindModalContent();
        } catch (err) {
            await swal({ icon: 'error', title: 'Something went wrong', text: 'Could not save the movie. Please try again.' });
        } finally {
            submitBtn?.removeAttribute('disabled');
        }
    }

    // --- Director / Studio pickers: searchable, with "Add ..." (MovieOptionsController) ----------

    const normalizeName = (name) => name.trim().replace(/\s+/g, ' ');

    // Turns a plain <select data-credit-picker="director|studio"> into a Tom Select combobox: type to
    // filter, or pick "Add <name>" to create a new entry on the spot.
    function initCreditPicker(select) {
        if (!window.TomSelect || select.tomselect) {
            return;
        }

        new TomSelect(select, {
            allowEmptyOption: true,
            placeholder: select.dataset.placeholder,
            // While the picker is focused, site.css hides the current choice so the box shows only
            // what's being typed; keeping the placeholder lets the empty box still say what to do.
            hidePlaceholder: false,
            maxOptions: 500,
            // The modal clips overflow, and these pickers sit near its bottom edge.
            dropdownParent: 'body',
            create: (input, callback) => {
                addCredit(select, input).then(option => callback(option || undefined));
            },
            render: {
                option_create: (data, escape) =>
                    `<div class="create"><i class="fa-solid fa-plus me-2" aria-hidden="true"></i>Add <strong>${escape(normalizeName(data.input))}</strong></div>`,
                no_results: () => '<div class="no-results">No matches. Keep typing to add a new one.</div>'
            }
        });
    }

    // Creates a director/studio, or explains that it already exists and selects the existing one.
    // Resolves to the new { value, text } option for Tom Select to add, or null when nothing new
    // should be added.
    async function addCredit(select, rawName) {
        const kind = select.dataset.creditPicker;
        const name = normalizeName(rawName);
        const picker = select.tomselect;

        // Instant check against what's already in the list (ignoring case and extra spaces).
        const existing = Object.values(picker.options)
            .find(o => o.value !== '' && normalizeName(o.text).toLowerCase() === name.toLowerCase());
        if (existing) {
            await showDuplicateAlert(kind, existing.text);
            picker.setValue(existing.value);
            return null;
        }

        try {
            const token = select.form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const response = await fetch(select.dataset.addUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
                body: new URLSearchParams({ Name: name, __RequestVerificationToken: token })
            });
            const body = await response.json().catch(() => ({}));

            if (response.status === 201) {
                swal({ toast: true, position: 'top-end', icon: 'success', title: `${kind === 'director' ? 'Director' : 'Studio'} "${body.name}" added`, showConfirmButton: false, timer: 2000 });
                return { value: String(body.id), text: body.name };
            }

            // 409: the server found it (someone may have added it since this form loaded).
            if (response.status === 409) {
                await showDuplicateAlert(kind, body.name);
                if (!picker.options[String(body.id)]) {
                    picker.addOption({ value: String(body.id), text: body.name });
                }
                picker.setValue(String(body.id));
                return null;
            }

            await swal({ icon: 'error', title: `Couldn't add that ${kind}`, text: body.error || `Server responded with ${response.status}.` });
        } catch {
            await swal({ icon: 'error', title: `Couldn't add that ${kind}`, text: 'Could not reach the server. Please try again.' });
        }
        return null;
    }

    function showDuplicateAlert(kind, name) {
        return swal({
            icon: 'warning',
            title: `${kind === 'director' ? 'Director' : 'Studio'} already exists`,
            text: `"${name}" is already in the list, so it's been selected instead of adding a duplicate.`,
            confirmButtonText: 'OK'
        });
    }

    // --- Lookup: pre-fill the form from Wikidata / TMDB / OMDb (MovieLookupController) ----------

    function bindLookupPanel(form) {
        const searchBtn = form.querySelector('#lookup-search-btn');
        const queryInput = form.querySelector('#lookup-query');
        const yearInput = form.querySelector('#lookup-year');
        if (!searchBtn || !queryInput) {
            return;
        }

        searchBtn.addEventListener('click', () => runLookupSearch(form));
        // Enter in the lookup boxes searches instead of submitting the movie form.
        [queryInput, yearInput].forEach(input => input?.addEventListener('keydown', (event) => {
            if (event.key === 'Enter') {
                event.preventDefault();
                runLookupSearch(form);
            }
        }));
    }

    function setLookupStatus(form, html, isError = false) {
        const status = form.querySelector('#lookup-status');
        status.classList.toggle('text-danger', isError);
        status.innerHTML = html;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text ?? '';
        return div.innerHTML;
    }

    async function lookupFetch(url) {
        const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        const body = await response.json().catch(() => ({}));
        if (!response.ok) {
            throw new Error(body.error || `Lookup failed (${response.status}).`);
        }
        return body;
    }

    async function runLookupSearch(form) {
        const source = form.querySelector('#lookup-source').value;
        const q = form.querySelector('#lookup-query').value.trim();
        const year = form.querySelector('#lookup-year').value.trim();
        const list = form.querySelector('#lookup-results');

        if (!q) {
            setLookupStatus(form, 'Enter a title to search for.', true);
            return;
        }

        list.hidden = true;
        setLookupStatus(form, '<i class="fa-solid fa-spinner fa-spin me-1" aria-hidden="true"></i>Searching...');

        try {
            const params = new URLSearchParams({ source, q });
            if (year) params.set('year', year);
            const results = await lookupFetch(`/MovieLookup/Search?${params}`);

            if (results.length === 0) {
                setLookupStatus(form, 'No matches. Try another spelling or remove the year.');
                return;
            }

            list.innerHTML = results.map(r => `
                <li>
                    <button type="button" class="lookup-result" data-id="${escapeHtml(r.externalId)}">
                        <img src="${escapeHtml(r.posterUrl || '/images/movies/placeholder.svg')}" alt="" loading="lazy" />
                        <span><strong>${escapeHtml(r.title)}</strong>${r.year ? ` <span class="text-body-secondary">(${r.year})</span>` : ''}</span>
                    </button>
                </li>`).join('');
            list.hidden = false;
            list.querySelectorAll('.lookup-result').forEach(btn =>
                btn.addEventListener('click', () => applyLookupResult(form, source, btn.dataset.id)));
            setLookupStatus(form, `${results.length} match${results.length === 1 ? '' : 'es'}. Pick one to fill the form.`);
        } catch (err) {
            setLookupStatus(form, escapeHtml(err.message), true);
        }
    }

    async function applyLookupResult(form, source, id) {
        setLookupStatus(form, '<i class="fa-solid fa-spinner fa-spin me-1" aria-hidden="true"></i>Loading details...');

        let movie;
        try {
            movie = await lookupFetch(`/MovieLookup/Details?${new URLSearchParams({ source, id })}`);
        } catch (err) {
            setLookupStatus(form, escapeHtml(err.message), true);
            return;
        }

        const onlyEmpty = form.querySelector('#lookup-only-empty')?.checked;
        const filled = [];

        // Copies one value into the named field unless it's blank, or "only empty" is on and the
        // field already has something. Re-validates the field so warnings update immediately.
        const fill = (name, value, label) => {
            const field = form.querySelector(`[name="${name}"]`);
            if (!field || value === null || value === undefined || value === '') return;
            if (onlyEmpty && field.value.trim() !== '') return;
            if (field.tomselect) {
                field.tomselect.setValue(String(value));
            } else {
                field.value = String(value);
                field.dispatchEvent(new Event('change', { bubbles: true }));
            }
            if (window.jQuery?.validator) jQuery(field).valid();
            if (label && !filled.includes(label)) filled.push(label);
        };

        fill('Title', movie.title, 'title');
        fill('Synopsis', movie.synopsis, 'synopsis');
        fill('ReleaseDate', movie.releaseDate, 'release date');
        if (movie.runtimeHours !== null || movie.runtimeMinutes !== null) {
            fill('RuntimeHours', movie.runtimeHours, 'runtime');
            fill('RuntimeMinutesPart', movie.runtimeMinutes, 'runtime');
        }
        fill('ContentRating', movie.contentRating, 'rating');
        fill('PosterUrl', movie.posterUrl, 'poster');
        fill('TrailerUrl', movie.trailerUrl, 'trailer');
        fill('DirectorId', movie.directorId, 'director');
        fill('StudioId', movie.studioId, 'studio');

        const genreBoxes = form.querySelectorAll('input[name="SelectedGenreIds"]');
        const anyGenreChecked = Array.from(genreBoxes).some(box => box.checked);
        if (movie.genreIds.length > 0 && !(onlyEmpty && anyGenreChecked)) {
            genreBoxes.forEach(box => { box.checked = movie.genreIds.includes(Number(box.value)); });
            filled.push('genres');
        }

        updatePosterPreview(form);

        // Summary: what was filled, review scores from the source, and anything that didn't map.
        const notes = [];
        if (movie.certification && !movie.contentRating) {
            notes.push(`Rated "${escapeHtml(movie.certification)}" there, which isn't one of G/PG/PG-13/R/NC-17.`);
        }
        const addButton = (kind, name) =>
            ` <button type="button" class="btn btn-link btn-sm p-0 align-baseline lookup-add-credit" data-kind="${kind}" data-name="${escapeHtml(name)}">Add it</button>`;
        if (movie.unmatched.director && !(onlyEmpty && form.querySelector('[name="DirectorId"]').value)) {
            notes.push(`Director "${escapeHtml(movie.unmatched.director)}" isn't in the list yet.${addButton('director', movie.unmatched.director)}`);
        }
        if (movie.unmatched.studio && !(onlyEmpty && form.querySelector('[name="StudioId"]').value)) {
            notes.push(`Studio "${escapeHtml(movie.unmatched.studio)}" isn't in the list yet.${addButton('studio', movie.unmatched.studio)}`);
        }
        if (movie.unmatched.genres.length) notes.push(`No matching genre for ${movie.unmatched.genres.map(g => `"${escapeHtml(g)}"`).join(', ')}.`);
        if (movie.posterUrl && hasUploadedPoster(form)) notes.push('The uploaded poster still takes priority over the poster URL.');

        const ratings = movie.ratings.map(r => `<span class="lookup-rating">${escapeHtml(r.source)} <strong>${escapeHtml(r.value)}</strong></span>`).join('');
        const sourceLink = movie.sourceUrl
            ? ` <a href="${escapeHtml(movie.sourceUrl)}" target="_blank" rel="noopener noreferrer">View on source <i class="fa-solid fa-arrow-up-right-from-square" aria-hidden="true"></i></a>`
            : '';

        setLookupStatus(form, `
            <div><i class="fa-solid fa-circle-check text-success me-1" aria-hidden="true"></i>
                ${filled.length ? `Filled ${filled.join(', ')} from ${escapeHtml(movie.source)}.` : 'Nothing to fill: every field already has a value.'}
                Everything stays editable, so review it before saving.${sourceLink}</div>
            ${ratings ? `<div class="lookup-ratings">${ratings}</div>` : ''}
            ${notes.map(n => `<div class="text-body-secondary"><i class="fa-solid fa-circle-info me-1" aria-hidden="true"></i>${n}</div>`).join('')}
        `);
        form.querySelector('#lookup-results').hidden = true;

        // "Add it" next to an unmatched director/studio: same add-or-alert flow as the picker.
        form.querySelectorAll('.lookup-add-credit').forEach(btn => btn.addEventListener('click', async () => {
            const select = form.querySelector(`select[data-credit-picker="${btn.dataset.kind}"]`);
            if (!select?.tomselect) return;
            btn.disabled = true;
            const option = await addCredit(select, btn.dataset.name);
            if (option) {
                select.tomselect.addOption(option);
                select.tomselect.setValue(option.value);
            }
            btn.closest('div').remove();
        }));
    }

    // --- Async search/filter/sort + refreshing the hero/catalog ---------------------------------

    // Non-empty toolbar values as query parameters (q, genreId, year, rating, sort), matching
    // MovieCatalogQuery on the server.
    function catalogParams() {
        const params = new URLSearchParams();
        if (queryForm) {
            for (const [key, value] of new FormData(queryForm)) {
                if (String(value).trim() !== '') params.set(key, String(value).trim());
            }
        }
        return params;
    }

    async function refreshCatalog() {
        const params = catalogParams();
        const response = await fetch(`/Movies/Search?${params}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });

        if (response.ok) {
            searchInput?.setCustomValidity('');
            catalog.innerHTML = await response.text();
            // Keep the URL in step with the toolbar so a reload or shared link shows the same view.
            history.replaceState(null, '', params.size ? `/Movies?${params}` : '/Movies');
            return;
        }

        // 400: MovieCatalogQuery's validation rejected a value (in practice, the search text).
        // Keep the catalog as it was and show the reason on the search box.
        if (response.status === 400 && searchInput) {
            const errors = await response.json().catch(() => ({}));
            const message = Object.values(errors).flat()[0] || 'That search isn\'t valid.';
            searchInput.setCustomValidity(message);
            searchInput.reportValidity();
        }
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
        searchInput.setCustomValidity('');
        clearTimeout(searchDebounce);
        searchDebounce = setTimeout(refreshCatalog, 300);
    });

    // Filters and sort apply immediately; the form's normal GET submit (Enter in the search box)
    // is replaced by the same async refresh.
    queryForm?.querySelectorAll('select').forEach(select => select.addEventListener('change', refreshCatalog));
    queryForm?.addEventListener('submit', (event) => {
        event.preventDefault();
        clearTimeout(searchDebounce);
        refreshCatalog();
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
        const { id, title, image, release, runtime, rating, genres } = data;

        const result = await swal({
            title: 'Delete this movie?',
            html: `
                <div class="d-flex gap-3 text-start align-items-start">
                    <img src="${image}" alt="${title} poster" class="rounded border flex-shrink-0" style="width: 96px; height: 144px; object-fit: cover;" />
                    <div>
                        <div class="fw-semibold mb-1">${title}</div>
                        <div class="small text-body-secondary">${rating} &middot; ${release} &middot; ${runtime}</div>
                        <div class="small text-body-secondary">${genres}</div>
                        <p class="mt-2 mb-0 small">You'll have 10 seconds to undo this before it's permanent.</p>
                    </div>
                </div>
            `,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Delete',
            // The theme's terracotta "danger" color, so the button matches light and dark mode.
            confirmButtonColor: getComputedStyle(document.documentElement).getPropertyValue('--ma-danger').trim() || '#b5442c',
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
