// My Reviews (Views/Reviews/Index.cshtml): the View and Delete buttons open their content
// (Reviews/Details/5, Reviews/Delete/5) in a shared modal instead of navigating. Write and Edit
// stay full pages.
(() => {
    'use strict';

    const modalEl = document.getElementById('review-modal');
    if (!modalEl) {
        return;
    }

    const modal = new bootstrap.Modal(modalEl);
    const content = document.getElementById('review-modal-content');

    modalEl.addEventListener('hidden.bs.modal', () => {
        content.innerHTML = '';
    });

    async function openModal(url) {
        try {
            const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            // The server redirected elsewhere (e.g. a published review can't be deleted): follow it.
            if (response.redirected) {
                window.location.href = response.url;
                return;
            }
            if (!response.ok) {
                throw new Error(`Server responded with ${response.status}`);
            }
            content.innerHTML = await response.text();
            modal.show();
        } catch (err) {
            // Not a navigation fallback: the plain link comes back here with the modal set to open,
            // so a failing review would reload the page forever.
            alert('Could not load that review. Please try again.');
        }
    }

    // Delegated, so the Delete button inside the View modal works too (it swaps the content).
    document.addEventListener('click', (event) => {
        const button = event.target.closest('.review-view-btn, .review-delete-btn');
        if (!button) {
            return;
        }
        event.preventDefault();
        const action = button.classList.contains('review-delete-btn') ? 'Delete' : 'Details';
        openModal(`/Reviews/${action}/${button.dataset.id}`);
    });

    // After saving or publishing, the review the critic just worked on opens straight away.
    const openId = modalEl.dataset.openReview;
    if (openId) {
        openModal(`/Reviews/Details/${openId}`);
    }
})();
