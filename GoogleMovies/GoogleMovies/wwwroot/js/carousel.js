document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll(".carousel-container").forEach(container => {
        const leftBtn = container.querySelector(".left-btn");
        const rightBtn = container.querySelector(".right-btn");
        const scrollContainer = container.querySelector(".carousel");

        leftBtn?.addEventListener("click", () => {
            scrollContainer.scrollBy({ left: -300, behavior: "smooth" });
        });

        rightBtn?.addEventListener("click", () => {
            scrollContainer.scrollBy({ left: 300, behavior: "smooth" });
        });
    });
});
