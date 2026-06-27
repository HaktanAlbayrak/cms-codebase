/** @type {import('tailwindcss').Config} */
module.exports = {
  // Hem genel site hem admin panelindeki sınıfları tara.
  content: [
    "./Views/**/*.cshtml",
    "./Areas/**/*.cshtml",
    "./wwwroot/js/**/*.js",
  ],
  theme: {
    extend: {
      colors: {
        // Marka rengi DB'den gelir; layout, hex'i RGB kanal biçimine çevirip
        // --brand CSS değişkenine enjekte eder. Kanal biçimi sayesinde
        // bg-brand/90 gibi opacity modifier'ları çalışır.
        brand: "rgb(var(--brand) / <alpha-value>)",
        "brand-dark": "rgb(var(--brand-dark) / <alpha-value>)",
        ink: "rgb(var(--ink) / <alpha-value>)",
      },
      fontFamily: {
        sans: ["Inter", "system-ui", "sans-serif"],
        display: ["Montserrat", "Inter", "system-ui", "sans-serif"],
      },
    },
  },
  plugins: [],
};
