// wwwroot/js/vmConsole.js
window.vmConsole = (function () {
    function buildConsolePath(sessionId) {
        // Path used by noVNC to connect to our backend WebSocket
        return `ws/console/${sessionId}`;
    }

    function openConsoleWithNoVnc(iframeId, sessionId, vncPassword) {
        if (!sessionId) {
            console.error("vmConsole.openConsoleWithNoVnc: sessionId is required.");
            return;
        }

        const iframe = document.getElementById(iframeId);
        if (!iframe) {
            console.error("vmConsole.openConsoleWithNoVnc: iframe not found", iframeId);
            return;
        }

        const base = window.location.origin;
        const path = `ws/console/${sessionId}`;

        const url =
            `${base}/novnc/vnc_lite.html?` +
            `path=${encodeURIComponent(path)}` +
            `&password=${encodeURIComponent(vncPassword)}` +
            `&scale=true`;

        console.log("vmConsole: loading noVNC URL", url);

        // Optional: clear previous status in parent
        if (window.parent && window.parent.vmConsoleStatus) {
            window.parent.vmConsoleStatus("connecting");
        }
    }

    return {
        openConsoleWithNoVnc: openConsoleWithNoVnc
    };
})();
