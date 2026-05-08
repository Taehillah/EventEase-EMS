(function () {
  const canvas = document.getElementById("stars");
  if (!canvas) {
    return;
  }

  const context = canvas.getContext("2d");
  let width = 0;
  let height = 0;
  let stars = [];

  function resize() {
    width = canvas.width = window.innerWidth;
    height = canvas.height = window.innerHeight;
  }

  function seed() {
    stars = [];
    const total = Math.max(40, Math.floor((width * height) / 7000));
    for (let index = 0; index < total; index += 1) {
      stars.push({
        x: Math.random() * width,
        y: Math.random() * height * 0.75,
        radius: Math.random() * 1.2 + 0.2,
        alpha: Math.random(),
        drift: (Math.random() - 0.5) * 0.005
      });
    }
  }

  function draw() {
    context.clearRect(0, 0, width, height);

    for (const star of stars) {
      star.alpha += star.drift;
      if (star.alpha <= 0 || star.alpha >= 1) {
        star.drift *= -1;
      }

      context.beginPath();
      context.arc(star.x, star.y, star.radius, 0, Math.PI * 2);
      context.fillStyle = `rgba(220, 215, 255, ${star.alpha * 0.75})`;
      context.fill();
    }

    window.requestAnimationFrame(draw);
  }

  window.addEventListener("resize", function () {
    resize();
    seed();
  });

  resize();
  seed();
  draw();
})();

document.querySelectorAll("[data-toggle-password]").forEach(function (button) {
  button.addEventListener("click", function () {
    const targetSelector = button.getAttribute("data-toggle-password");
    if (!targetSelector) {
      return;
    }

    const input = document.querySelector(targetSelector);
    if (!(input instanceof HTMLInputElement)) {
      return;
    }

    input.type = input.type === "password" ? "text" : "password";
  });
});

document.querySelectorAll("[data-confirm]").forEach(function (element) {
  element.addEventListener("click", function (event) {
    const prompt = element.getAttribute("data-confirm") || "Are you sure?";
    if (!window.confirm(prompt)) {
      event.preventDefault();
    }
  });
});
