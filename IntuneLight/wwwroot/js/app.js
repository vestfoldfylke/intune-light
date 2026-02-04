function setDarkMode(value) {
    document.cookie =
        "darkMode=" + (value ? "true" : "false") +
        "; path=/; max-age=31536000; SameSite=Lax";
}