/*
 * motion.js — Sayfa içi scroll-reveal animasyonları.
 * Performans notları:
 *  - Tek bir IntersectionObserver kullanılır (element başına dinleyici yok).
 *  - Görünür olan öğe gözlemden çıkarılır (unobserve) → CPU boşa çalışmaz.
 *  - Sadece opacity/transform animasyonu (CSS'te tanımlı) → compositor katmanı.
 *  - Grup içi öğelere kademeli (stagger) gecikme verilir.
 * Not: `.js-reveal` sınıfı <head>'deki satır-içi guard tarafından, hareket
 * izni + Save-Data kontrolünden sonra eklenir; bu dosya yalnızca o sınıf
 * varsa iş yapar.
 */
(function () {
  "use strict";

  var root = document.documentElement;
  if (!root.classList.contains("js-reveal")) return; // hareket kapalı → çık
  if (!("IntersectionObserver" in window)) {
    // Eski tarayıcı: her şeyi anında göster, gizli kalmasın.
    root.classList.remove("js-reveal");
    return;
  }

  var STAGGER_MS = 90; // grup içi öğeler arası gecikme
  var STAGGER_MAX = 6; // en çok bu kadar öğeye kademeli gecikme

  function reveal(el) {
    el.classList.add("reveal-in");
  }

  var observer = new IntersectionObserver(
    function (entries) {
      for (var i = 0; i < entries.length; i++) {
        var e = entries[i];
        if (e.isIntersecting) {
          reveal(e.target);
          observer.unobserve(e.target);
        }
      }
    },
    { rootMargin: "0px 0px -8% 0px", threshold: 0.08 }
  );

  function setup() {
    var items = document.querySelectorAll("[data-reveal]");
    for (var i = 0; i < items.length; i++) {
      var el = items[i];

      // Kademeli gecikme: aynı [data-reveal-group] içindeki kardeşler.
      var group = el.closest("[data-reveal-group]");
      if (group) {
        var siblings = group.querySelectorAll("[data-reveal]");
        var idx = Array.prototype.indexOf.call(siblings, el);
        if (idx > 0) {
          el.style.transitionDelay =
            Math.min(idx, STAGGER_MAX) * STAGGER_MS + "ms";
        }
      }

      // Zaten ekrandaysa (ör. hero) observer beklemeden hemen göster.
      observer.observe(el);
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", setup);
  } else {
    setup();
  }
})();
