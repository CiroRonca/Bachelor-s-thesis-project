// preview.js
(() => {
    const previewEl = document.getElementById("previewImage");
    const resultContainerEl = document.getElementById("resultContainer");
    const fileInputEl = document.getElementById("image");
    const toggleBtnEl = document.getElementById("toggleMessages");
    const messagesBoxEl = document.getElementById("messagesBox");
    const resultBoxEl = document.getElementById("result");

    if (!previewEl || !resultContainerEl || !fileInputEl) return;

    fileInputEl.addEventListener("change", (event) => {
        const file = event.target.files?.[0];
        if (!file) return;

        // Anteprima immediata
        const url = URL.createObjectURL(file);
        previewEl.src = url;
        resultContainerEl.classList.remove("hidden");

        // Reset descrizione e dettagli
        if (resultBoxEl) resultBoxEl.innerText = "";
        if (messagesBoxEl) {
            messagesBoxEl.innerText = "";
            messagesBoxEl.classList.add("hidden");
        }
        if (toggleBtnEl) {
            toggleBtnEl.textContent = "Mostra dettagli"; // reset testo
            toggleBtnEl.classList.add("hidden");         
        }
    });
})();
