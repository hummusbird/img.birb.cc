async function stats() {
    let res = await fetch("/api/stats")
    let parsed = await res.json()
    console.log(parsed)
    document.getElementById("users").innerHTML = parsed.users + " registered users"
    document.getElementById("files").innerHTML = parsed.files + " files uploaded"
    document.getElementById("gb").innerHTML = Math.round(parsed.bytes/1024/1024) + "mb used"
}