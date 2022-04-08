let refreshPaused = false;

function installPartialRefresh(serviceURL, divContainerId, refreshRate, callBack = null) {
    // intallation of partial refresh
    setInterval(() => { DoPartialRefresh(serviceURL, divContainerId, callBack); }, refreshRate * 1000);
}
function PauseRefresh() {
    refreshPaused = true;
    console.log("partial refresh paused");
}

function StartRefresh() {
    refreshPaused = false;
    console.log("partial refresh started");
}

function DoPartialRefresh(serviceURL, divContainerId, callBack = null) {
    // posts partial refresh
    if (!refreshPaused) {
        $.ajax({
            url: serviceURL,
            dataType: "html",
            success: function (htmlContent) {
                if (htmlContent !== "") {
                    $("#" + divContainerId).html(htmlContent);
                    if (callBack != null) callBack();
                }
            }
        })
    }
}

function ajaxActionCall(actionLink, callback = null) {
    // Ajax Action Call to actionLink
    $.ajax({
        url: actionLink,
        method: 'GET',
        success: (data) => {
            if (callback != null) {
                callback(data);
            }
        }
    });
}

