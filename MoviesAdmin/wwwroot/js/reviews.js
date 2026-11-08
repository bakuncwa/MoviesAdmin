// Review form (Views/Reviews/_ReviewForm.cshtml): the half-star rating picker and the Quill
// rich-text editor. Each writes into a hidden input (Rating, Text), which is what the form posts;
// ReviewsController sanitizes the HTML and re-checks both limits on the server.
(() => {
    'use strict';

    const form = document.querySelector('form:has(.star-picker)');
    if (!form) {
        return;
    }

    // --- Star picker: 0 to 5 in half-star steps -------------------------------------------------

    const picker = form.querySelector('.star-picker');
    const stars = [...picker.querySelectorAll('[data-star]')];
    const ratingInput = form.querySelector('input[name="Rating"]');
    const valueLabel = form.querySelector('.star-picker-value');
    let rating = ratingInput.value === '' ? null : Number(ratingInput.value);

    // Paints the stars for a value (full, half or empty) without changing the saved rating.
    const paint = (value) => {
        stars.forEach((star) => {
            const n = Number(star.dataset.star);
            const full = value !== null && value >= n;
            const half = value !== null && !full && value >= n - 0.5;
            star.classList.toggle('fa-star', !half);
            star.classList.toggle('fa-star-half-stroke', half);
            star.classList.toggle('star-empty', !full && !half);
        });
    };

    const setRating = (value) => {
        rating = Math.min(5, Math.max(0, Math.round(value * 2) / 2));
        ratingInput.value = rating;
        picker.setAttribute('aria-valuenow', rating);
        picker.setAttribute('aria-valuetext', `${rating} out of 5 stars`);
        valueLabel.textContent = `${rating} / 5`;
        paint(rating);
    };

    // Left half of a star = half a star, right half = the whole star.
    const valueAt = (event) => {
        const star = event.target.closest('[data-star]');
        if (!star) {
            return null;
        }
        const box = star.getBoundingClientRect();
        const n = Number(star.dataset.star);
        return event.clientX - box.left < box.width / 2 ? n - 0.5 : n;
    };

    picker.addEventListener('mousemove', (event) => {
        const value = valueAt(event);
        if (value !== null) {
            paint(value);
        }
    });
    picker.addEventListener('mouseleave', () => paint(rating));
    picker.addEventListener('click', (event) => {
        const value = valueAt(event);
        if (value !== null) {
            setRating(value);
        }
    });
    picker.addEventListener('keydown', (event) => {
        const steps = { ArrowRight: 0.5, ArrowUp: 0.5, ArrowLeft: -0.5, ArrowDown: -0.5 };
        if (event.key in steps) {
            setRating((rating ?? 0) + steps[event.key]);
        } else if (event.key === 'Home') {
            setRating(0);
        } else if (event.key === 'End') {
            setRating(5);
        } else {
            return;
        }
        event.preventDefault();
    });
    form.querySelector('.star-picker-zero').addEventListener('click', () => setRating(0));

    if (rating === null) {
        valueLabel.textContent = 'Not rated yet';
        paint(null);
    } else {
        setRating(rating);
    }

    // --- Rich-text editor -------------------------------------------------------------------------

    const maxLength = 1000;
    const textInput = form.querySelector('input[name="Text"]');
    const count = document.getElementById('review-text-count');
    const textError = form.querySelector('[data-valmsg-for="Text"]');

    // Only formatting the server's sanitizer (ReviewHtml) keeps is offered, and pasted content is
    // limited to the same formats, so what the critic sees is what gets saved.
    const quill = new Quill('#review-editor', {
        theme: 'snow',
        placeholder: 'What did you think of it?',
        formats: ['header', 'bold', 'italic', 'underline', 'strike', 'list', 'indent', 'align', 'blockquote'],
        modules: {
            toolbar: [
                [{ header: [2, 3, false] }],
                ['bold', 'italic', 'underline', 'strike'],
                [{ list: 'ordered' }, { list: 'bullet' }],
                [{ indent: '-1' }, { indent: '+1' }],
                [{ align: [] }],
                ['blockquote', 'clean'],
            ],
        },
    });

    if (textInput.value) {
        quill.clipboard.dangerouslyPasteHTML(textInput.value); // already sanitized by the server
    }

    // Visible characters, counted the way the server does (line breaks between blocks don't count).
    const visibleLength = () => quill.getText().replace(/\n/g, '').trim().length;

    const updateCount = () => {
        const length = visibleLength();
        count.textContent = length;
        count.parentElement.classList.toggle('text-danger', length > maxLength);
    };
    quill.on('text-change', updateCount);
    updateCount();

    form.addEventListener('submit', (event) => {
        const length = visibleLength();
        let message = '';
        if (length === 0) {
            message = 'Review text is required.';
        } else if (length > maxLength) {
            message = `Review text can be at most ${maxLength} characters (this one has ${length}).`;
        }
        if (message) {
            event.preventDefault();
            textError.textContent = message;
            textError.classList.add('field-validation-error');
            quill.focus();
            return;
        }
        // Quill 2.0.3's getSemanticHTML() writes every space as &nbsp;, which would stop the review
        // from wrapping when it's displayed; turn them back into ordinary spaces.
        textInput.value = quill.getSemanticHTML().replaceAll('&nbsp;', ' ');
    });
})();
