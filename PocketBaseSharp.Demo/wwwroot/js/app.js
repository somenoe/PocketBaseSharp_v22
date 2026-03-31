window.demoTheme = {
  toggle() {
    const documentElement = document.documentElement;
    const isDark = documentElement.classList.toggle("dark");
    localStorage.setItem("color-theme", isDark ? "dark" : "light");
  },
};
