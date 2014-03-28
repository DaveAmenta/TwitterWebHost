/* TODO determine why // comments break this... yeah I know. */
/* Attach logging inside managed code */
console.log = function (msg) { window.external.log(msg); };

/* Clear timer instance (file reload) */
try {
    clearInterval(__g_injectedTimer);
    console.log('clear timer ' + __g_injectedTimer);
} catch(e) {
    console.log(e);
}

var __g_injectedTimer = setInterval(function () { __injectedrefresh(); }, 20 * 1000); /* 20s */

function __injectedrefresh() {
    try
    {
        var IsTextAreaFocused = document.activeElement && document.activeElement.classList.contains('rich-editor');
        var ScrolledDown = document.documentElement && document.documentElement.scrollTop;

        if (!IsTextAreaFocused && ScrolledDown <= 20) {

            console.log("Activating automatic refresh");
            /* Click home or connect */
            $("li[data-global-action='connect'].active").children(":first").click();
            $("#global-nav-home.active").children(":first").click();

            var oldElements = $("li[class~='js-stream-item']").slice(50);
            if (oldElements.length > 0)
            {
                console.log("Slice elements: " + oldElements.length);
                oldElements.remove();
            }
        }
        else
        {
            console.log("Not activating refresh. | txt=" + IsTextAreaFocused + " scroll=" + ScrolledDown);
        }
    }
    catch (e)
    {
        console.log(e);
    }
}

console.log("Page injection finished.");