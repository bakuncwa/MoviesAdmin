// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Light/dark mode toggle. The initial theme (from localStorage, otherwise light — the earth-tone
// theme is light-first) is already applied by the inline script in _Layout.cshtml's <head>
// before this file loads, so here we only wire up the button and keep it in sync with that state.
(() => {
    'use strict'

    const getStoredTheme = () => localStorage.getItem('theme')
    const setStoredTheme = theme => localStorage.setItem('theme', theme)

    const getPreferredTheme = () => {
        const storedTheme = getStoredTheme()
        if (storedTheme === 'light' || storedTheme === 'dark') {
            return storedTheme
        }
        return 'light'
    }

    const setTheme = theme => {
        document.documentElement.setAttribute('data-bs-theme', theme)
    }

    const toggleBtn = document.getElementById('theme-toggle')
    const sunIcon = document.getElementById('theme-icon-light')
    const moonIcon = document.getElementById('theme-icon-dark')

    const updateToggleUI = theme => {
        const isDark = theme === 'dark'
        sunIcon?.classList.toggle('d-none', isDark)
        moonIcon?.classList.toggle('d-none', !isDark)
        toggleBtn?.setAttribute('aria-pressed', String(isDark))
        toggleBtn?.setAttribute('title', isDark ? 'Switch to light mode' : 'Switch to dark mode')
    }

    updateToggleUI(getPreferredTheme())

    toggleBtn?.addEventListener('click', () => {
        const current = document.documentElement.getAttribute('data-bs-theme') || getPreferredTheme()
        const next = current === 'dark' ? 'light' : 'dark'
        setStoredTheme(next)
        setTheme(next)
        updateToggleUI(next)
    })

})()
