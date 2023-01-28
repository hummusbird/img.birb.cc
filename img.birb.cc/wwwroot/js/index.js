async function stats() {
    let res = await fetch("/api/stats")
    let parsed = await res.json()
    console.log(parsed)
    document.getElementById("users").innerHTML = numberWithCommas(parsed.users) + " registered users"
    document.getElementById("files").innerHTML = numberWithCommas(parsed.files) + " files uploaded"
    document.getElementById("gb").innerHTML = changeToSize(parsed.bytes) + " used"
    document.getElementById("time").innerHTML = changeToTime(Math.round((new Date().getTime() - Date.parse(parsed.newest)) / 1000 / 60)) + " since last upload"
}

async function dashmsg() {
    let res = await fetch("/api/dashmsg")
    let parsed = await res.json()
    console.log(parsed)
    document.getElementById("username").innerHTML = "- " + parsed.username
    document.getElementById("message").innerText = parsed.dashMsg
}

function numberWithCommas(x) {
    return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

function setTitle() {
    document.getElementById("title").innerHTML = "welcome to " + (window.location.host || "img.birb.cc")
    document.getElementById("toptitle").innerHTML = (window.location.host || "img.birb.cc")
}

function changeToSize(x) {
    if (x < 1024){
        return (numberWithCommas(x) + " bytes")
    }
    if (x >= 1024 && x < 1048576) {
        return (numberWithCommas(Math.round(x/1024)) + " kilobytes")
    }
    if (x >= 1048576 && x < 1073741824) {
        return (numberWithCommas(Math.round(x/1024/10.24)/100) + " megabytes")
    }
    if (x >= 1073741824) {
        return (numberWithCommas(Math.round(x/1024/1024/10.24)/100) + " gigabytes")
    }
}

function changeToTime(x) {
    if (x < 60) {
        return (x == 1 ? (x + " minute") : x + " minutes")
    }
    else if (x >= 60 && x < 1440 ) {
        x = Math.round(x / 6) / 10
        return ( x == 1 ? (x + " hour") : x + " hours")
    }
    else if (x >= 1440) {
        x = Math.round(x / 1440)
        return ( x == 1 ? (x + " day") : x + " days")
    }
}