// Client-side counterparts for this app's validation attributes, loaded after
// jquery.validate.unobtrusive by Views/Shared/_ValidationScriptsPartial.cshtml.
(($) => {
    'use strict';

    if (!$ || !$.validator || !$.validator.unobtrusive) {
        return;
    }

    // [RegularExpression] patterns here use Unicode classes such as \p{L} (any letter), which a
    // JavaScript RegExp only understands with the "u" flag. The stock unobtrusive "regex" method
    // omits it, so a title like "Amélie" would be rejected in the browser but accepted by the
    // server. Same whole-value match semantics as the original, plus the flag.
    $.validator.addMethod('regex', function (value, element, pattern) {
        if (this.optional(element)) {
            return true;
        }
        let regex;
        try {
            regex = new RegExp(pattern, 'u');
        } catch {
            regex = new RegExp(pattern);
        }
        const match = regex.exec(value);
        return !!match && match.index === 0 && match[0].length === value.length;
    });

    // [PosterFile] (Validation/PosterFileAttribute.cs): allowed extensions + maximum size.
    $.validator.addMethod('posterfile', function (value, element, params) {
        const file = element.files && element.files[0];
        if (!file) {
            return true;
        }
        const dot = file.name.lastIndexOf('.');
        const extension = dot >= 0 ? file.name.slice(dot).toLowerCase() : '';
        return params.extensions.includes(extension) && file.size > 0 && file.size <= params.maxBytes;
    });
    $.validator.unobtrusive.adapters.add('posterfile', ['extensions', 'maxbytes'], (options) => {
        options.rules.posterfile = {
            extensions: options.params.extensions.split(','),
            maxBytes: Number(options.params.maxbytes)
        };
        options.messages.posterfile = options.message;
    });

    // [ReleaseDate] (Validation/ReleaseDateAttribute.cs): <input type="date"> values are
    // yyyy-MM-dd, which compare correctly as plain strings.
    $.validator.addMethod('releasedate', function (value, element, params) {
        return this.optional(element) || (value >= params.min && value <= params.max);
    });
    $.validator.unobtrusive.adapters.add('releasedate', ['min', 'max'], (options) => {
        options.rules.releasedate = { min: options.params.min, max: options.params.max };
        options.messages.releasedate = options.message;
    });
})(window.jQuery);
