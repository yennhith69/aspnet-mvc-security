// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.querySelectorAll("[data-print-report]").forEach((button) => {
    button.addEventListener("click", () => window.print());
});
