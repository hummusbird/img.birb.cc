async function stats() {
    let res = await fetch("api/stats")
    let parsed = await res.json()
    console.log(parsed)
    document.getElementById("users").innerHTML = numberWithCommas(parsed.users) + " registered users"
    document.getElementById("files").innerHTML = numberWithCommas(parsed.files) + " files uploaded"
    document.getElementById("gb").innerHTML = numberWithCommas(Math.round(parsed.bytes/1024/1024)) + "mb used"
    document.getElementById("time").innerHTML = numberWithCommas(Math.round((new Date().getTime() - Date.parse(parsed.newest)) / 1000 / 60)) + " mins since last upload"
}

function numberWithCommas(x) {
    return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}