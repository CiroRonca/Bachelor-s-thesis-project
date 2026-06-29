document.addEventListener("DOMContentLoaded", () => {
    const uploadForm = document.getElementById("uploadForm");
    const imageInput = document.getElementById("image");
    const userMessageInput = document.getElementById("text");
    const resultContainer = document.getElementById("resultContainer");
    const previewImage = document.getElementById("previewImage");
    const resultBox = document.getElementById("result");
    const toggleMessagesBtn = document.getElementById("toggleMessages");
    const messagesBox = document.getElementById("messagesBox");
    const galleryEl = document.getElementById("gallery");
    const submitBtn = document.getElementById("submitBtn");

    const loadingBar = document.createElement("div");
    loadingBar.className = "loading-bar";
    loadingBar.innerHTML = '<div class="loading-progress"></div>';
    resultContainer.appendChild(loadingBar);

    const modal = document.getElementById("imageModal");
    const closeModalBtn = document.getElementById("closeModal");
    const modalGroq = document.getElementById("modalGroqDescription");
    const modalUser = document.getElementById("modalUserMessage");
    const modalOther = document.getElementById("modalOtherInfo");

    let selectedImageId = null; // id immagine selezionata dalla galleria

    // 🔹 funzione per gestire abilitazione pulsante submit
    function updateSubmitButtonState() {
        const hasFile = imageInput && imageInput.files && imageInput.files.length > 0;
        const hasGalleryImage = !!selectedImageId;

        if (submitBtn) {
            submitBtn.disabled = !(hasFile || hasGalleryImage);
        }
    }

    // Disabilita bottone all'avvio
    updateSubmitButtonState();

    // Nascondi bottone dettagli all’avvio
    if (toggleMessagesBtn) {
        toggleMessagesBtn.classList.add("hidden");
        toggleMessagesBtn.textContent = "Mostra dettagli";
    }

    // Preview nuovo file e reset
    imageInput.addEventListener("change", (event) => {
        const file = event.target.files?.[0];
        if (!file) return;

        selectedImageId = null; // nuovo upload, reset id

        if (resultBox) resultBox.innerText = "";
        if (messagesBox) {
            messagesBox.innerText = "";
            messagesBox.classList.add("hidden");
        }
        if (toggleMessagesBtn) toggleMessagesBtn.classList.add("hidden");

        previewImage.src = URL.createObjectURL(file);
        resultContainer.classList.remove("hidden");

        updateSubmitButtonState(); // 🔹 aggiorna bottone
    });

    // Upload o update
    uploadForm.addEventListener("submit", async (e) => {
        e.preventDefault();
        const file = imageInput.files?.[0];
        const userMessage = userMessageInput.value;

        loadingBar.classList.add("active");

        try {
            let response, data;

            if (selectedImageId) {
                // 🔹 Aggiornamento messaggio + rigenerazione descrizioni
                response = await fetch(`http://localhost:5031/api/ImagesDb/${selectedImageId}/updateMessage`, {
                    method: "PUT",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ userMessage })
                });

                const contentType = response.headers.get("content-type") || "";
                if (contentType.includes("application/json")) {
                    data = await response.json();
                } else {
                    data = { message: await response.text() };
                }

                loadingBar.classList.remove("active");

                if (!response.ok) {
                    showPopup(data.message || "Errore durante aggiornamento.");
                    return;
                }

                // 🔹 Mostra subito i dati aggiornati
                if (resultBox) resultBox.innerText = data.groqDescription || "";

                if (toggleMessagesBtn && messagesBox) {
                    messagesBox.innerText =
                        `Azure: ${data.azureDescription || "N/A"}\nTags: ${data.clarifaiTags || "N/A"}\nColors: ${data.clarifaiColors || "N/A"}\nMessaggio utente: ${data.userMessage || "Nessuno"}`;
                    messagesBox.classList.add("hidden");
                    toggleMessagesBtn.textContent = "Mostra dettagli";
                    toggleMessagesBtn.classList.remove("hidden");
                }

                if (modalGroq) modalGroq.innerText = data.groqDescription || "";
                if (modalUser) modalUser.innerText = data.userMessage || "";
                if (modalOther) modalOther.innerText =
                    `Azure: ${data.azureDescription || "N/A"}\nTags: ${data.clarifaiTags || "N/A"}\nColors: ${data.clarifaiColors || "N/A"}`;

                loadGallery();
                updateSubmitButtonState();
                return; // evita di proseguire con l’upload
            } else {
                // 🔹 Nuovo upload
                if (!file) {
                    showPopup("Errore: seleziona un'immagine o scegli dalla galleria.");
                    loadingBar.classList.remove("active");
                    return;
                }

                const formData = new FormData();
                formData.append("image", file);
                formData.append("userMessage", userMessage);

                response = await fetch("http://localhost:5031/api/ImagesDb/upload", {
                    method: "POST",
                    body: formData
                });

                const contentType = response.headers.get("content-type") || "";
                if (contentType.includes("application/json")) {
                    data = await response.json();
                } else {
                    data = { message: await response.text() };
                }

                loadingBar.classList.remove("active");

                if (!response.ok) {
                    showPopup(data.message || "Errore durante l'upload.");
                    return;
                }

                // Mostra descrizione Groq
                if (resultBox) resultBox.innerText = data.groqDescription || "";

                // Dettagli extra
                if (toggleMessagesBtn && messagesBox) {
                    messagesBox.innerText =
                        `Azure: ${data.azureDescription || "N/A"}\nTags: ${data.clarifaiTags || "N/A"}\nColors: ${data.clarifaiColors || "N/A"}\nMessaggio utente: ${data.userMessage || "Nessuno"}`;
                    messagesBox.classList.add("hidden");
                    toggleMessagesBtn.textContent = "Mostra dettagli";
                    toggleMessagesBtn.classList.remove("hidden");
                }

                // Aggiorna modal
                if (modalGroq) modalGroq.innerText = data.groqDescription || "";
                if (modalUser) modalUser.innerText = data.userMessage || "";
                if (modalOther) modalOther.innerText =
                    `Azure: ${data.azureDescription || "N/A"}\nTags: ${data.clarifaiTags || "N/A"}\nColors: ${data.clarifaiColors || "N/A"}`;

                // Ricarica galleria
                loadGallery();

                updateSubmitButtonState(); // 🔹 disabilita se serve
            }
        } catch (error) {
            loadingBar.classList.remove("active");
            showPopup("Errore JS: " + error.message);
        }
    });

    // Toggle dettagli
    if (toggleMessagesBtn && messagesBox) {
        toggleMessagesBtn.addEventListener("click", () => {
            const isHidden = messagesBox.classList.contains("hidden");
            messagesBox.classList.toggle("hidden");
            toggleMessagesBtn.textContent = isHidden ? "Nascondi dettagli" : "Mostra dettagli";
        });
    }

    // Carica galleria
    async function loadGallery() {
        try {
            const response = await fetch("http://localhost:5031/api/ImagesDb");
            const images = await response.json();

            galleryEl.innerHTML = "";
            images.forEach(img => {
                const card = document.createElement("div");
                card.className = "card";

                const imageElement = document.createElement("img");
                imageElement.src = `http://localhost:5031/api/ImagesDb/${img.id}/file`;
                imageElement.alt = img.fileName;

                imageElement.addEventListener("click", () => {
                    selectedImageId = img.id;

                    if (resultBox) resultBox.innerText = "";
                    if (messagesBox) {
                        messagesBox.innerText = "";
                        messagesBox.classList.add("hidden");
                    }
                    if (toggleMessagesBtn) toggleMessagesBtn.classList.remove("hidden");

                    previewImage.src = `http://localhost:5031/api/ImagesDb/${img.id}/file`;
                    resultContainer.classList.remove("hidden");

                    if (resultBox) resultBox.innerText = img.groqDescription || "";
                    if (messagesBox) {
                        messagesBox.innerText =
                            `Azure: ${img.azureDescription || "N/A"}\nTags: ${img.clarifaiTags || "N/A"}\nColors: ${img.clarifaiColors || "N/A"}\nMessaggio utente: ${img.userMessage || "Nessuno"}`;
                        messagesBox.classList.add("hidden");
                    }

                    if (modalGroq) modalGroq.innerText = img.groqDescription || "";
                    if (modalUser) modalUser.innerText = img.userMessage || "";
                    if (modalOther) modalOther.innerText =
                        `Azure: ${img.azureDescription || "N/A"}\nTags: ${img.clarifaiTags || "N/A"}\nColors: ${img.clarifaiColors || "N/A"}`;

                    updateSubmitButtonState(); // 🔹 abilita submit
                });

                card.appendChild(imageElement);
                galleryEl.appendChild(card);
            });
        } catch (error) {
            console.error("Errore caricamento galleria:", error);
        }
    }

    // Modal close
    if (closeModalBtn) {
        closeModalBtn.addEventListener("click", () => modal.classList.add("hidden"));
    }

    // Popup errori
    function showPopup(message) {
        const popup = document.createElement("div");
        popup.className = "popup";
        popup.innerHTML = `
            <div class="popup-content">
                <span class="close-btn">&times;</span>
                <p>${message}</p>
            </div>
        `;
        document.body.appendChild(popup);
        popup.querySelector(".close-btn")?.addEventListener("click", () => popup.remove());
    }

    // Carica galleria all’avvio
    loadGallery();
});
