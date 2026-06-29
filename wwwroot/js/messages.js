// messages.js
(() => {
    const toggleBtnEl = document.getElementById("toggleMessages");
    const messagesBoxEl = document.getElementById("messagesBox");

    if (!toggleBtnEl || !messagesBoxEl) return;

    toggleBtnEl.addEventListener("click", () => {
        const isHidden = messagesBoxEl.classList.contains("hidden");
        messagesBoxEl.classList.toggle("hidden");
        toggleBtnEl.textContent = isHidden ? "Nascondi dettagli" : "Mostra dettagli";
    });

    // 👇 reset automatico del testo ogni volta che viene nascosto forzatamente
    const observer = new MutationObserver(() => {
        if (messagesBoxEl.classList.contains("hidden")) {
            toggleBtnEl.textContent = "Mostra dettagli";
        }
    });

    observer.observe(messagesBoxEl, { attributes: true, attributeFilter: ["class"] });
})();
