// Registers the minimal service worker for installability only.
// No offline caching is performed.

if ("serviceWorker" in navigator) {
    window.addEventListener("load", () => {
        navigator.serviceWorker
            .register("/service-worker.js")
            .catch(err => console.error("ServiceWorker registration failed:", err));
    });
}