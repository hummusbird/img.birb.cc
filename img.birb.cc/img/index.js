async function stats() {
    let res = await fetch("https://img.birb.cc/api/stats")
    let parsed = await res.json()
    console.log(parsed)
    document.getElementById("users").innerHTML = parsed.users + " registered users"
    document.getElementById("files").innerHTML = parsed.files + " files uploaded"
    document.getElementById("gb").innerHTML = Math.round(parsed.bytes/1024/1024) + "mb used"
    document.getElementById("time").innerHTML = Math.round((new Date().getTime() - Date.parse(parsed.newest)) / 1000 / 60) + " mins since last upload"
 }
