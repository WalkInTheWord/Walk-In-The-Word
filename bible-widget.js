document.addEventListener("DOMContentLoaded", () => {
  const container = document.getElementById("verse-container");
  const verses = [
    "Psalm 119:105 – Your word is a lamp to my feet and a light to my path.",
    "Romans 10:17 – So faith comes from hearing, and hearing through the word of Christ.",
    "Matthew 4:4 – Man shall not live by bread alone, but by every word that comes from the mouth of God."
  ];
  const random = verses[Math.floor(Math.random() * verses.length)];
  container.innerText = random;
});