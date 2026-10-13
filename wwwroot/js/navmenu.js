// Cierra el menú lateral en pantallas pequeñas al hacer clic en una opción.
// Reemplaza el atributo onclick en línea de NavMenu.razor, que la CSP bloquea.
document.addEventListener("click", function (event) {
    if (event.target.closest(".nav-scrollable")) {
        const toggler = document.querySelector(".navbar-toggler");
        if (toggler) {
            toggler.click();
        }
    }
});

