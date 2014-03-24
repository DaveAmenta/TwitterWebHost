
/* TODO determine why // comments break this... yeah I know. */
setInterval(function () {
    try
    {
        if (document.activeElement &&
            !document.activeElement.classList.contains('rich-editor') && /* textarea */
            document.documentElement &&
            document.documentElement.scrollTop == 0)
        {
            var homeBtn = document.getElementById('global-nav-home');
            var connectBtn = $("li[data-global-action='connect']")[0];

            if (homeBtn && homeBtn.classList.contains("active"))
            {
                homeBtn.children[0].click(); /* may be IE specific */
                console.log("Click home");
            }
            else if (connectBtn && connectBtn.classList.contains("active"))
            {
                var connectLink = $("li[data-nav='connect']")[0];
                if (connectLink)
                {
                    connectLink.children[0].click();
                    console.log("Click connect");
                }
            }
            else
            {
                console.log("No button selected");
            }
        }
        else
        {
            console.log("Not scrolling");
        }
    }
    catch (e)
    {
        /* TODO instrument */
        console.log(e);
    }
}, 20 * 1000); /* 20s */

/* Attach logging inside managed code */
console.log = function (msg) { window.external.log(msg); }


