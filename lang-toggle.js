document.addEventListener("DOMContentLoaded", () => {
  const toggle = document.getElementById("langToggle");
  toggle.addEventListener("click", () => {
    const isEnglish = toggle.innerText === "Español";
    document.querySelectorAll("[data-en]").forEach(el => el.innerText = isEnglish ? el.dataset.es : el.dataset.en);
    toggle.innerText = isEnglish ? "English" : "Español";
  });
});