const url = "localhost:7247"
const delay = ms => new Promise(res => setTimeout(res, ms));

let usrout;
let imgout;
let pageCount = 0;

async function login() {

    pageCount = 0;

    let formData = new FormData();
    formData.append("api_key", document.getElementById("keybox").value)

    fetch(`https://${url}/api/usr`,
        {
            body: formData,
            method: "post"
        }).then(res => res.json())
        .then(
            data => {
                usrout = data
                document.getElementById("loginstat").innerHTML = "api key:"
                document.getElementById("loginarea").style.display = "none"
                document.getElementById("stats").style.display = "initial"

                document.getElementById("wide").style.display = "flex"
                document.getElementById("nuke").style.display = "initial"
                document.getElementById("next").style.display = "initial"

                document.getElementById("dashboard").innerHTML = "dashboard - " + usrout["username"]
                document.getElementById("uid").innerHTML = usrout["uid"]
                document.getElementById("showURL").checked = usrout["showURL"]
                document.getElementById("domain").value = usrout["domain"]
            })
        .catch(e => document.getElementById("loginstat").innerHTML = "invalid key")

    displayPage(1)
}

async function nuke() {
    document.getElementById("nuke").innerHTML = "wait"
    await delay(5000);
    document.getElementById("nuke").innerHTML = "sure?"
    document.getElementById("nuke").setAttribute("onclick", `definitelyNuke()`)
}

async function definitelyNuke() {
    let formData = new FormData();
    formData.append("api_key", document.getElementById("keybox").value)

    fetch(`https://${url}/api/nuke`,
        {
            body: formData,
            method: "DELETE"
        }).then(res => res.json())
        .catch(e => document.getElementById("loginstat").innerHTML = "invalid key")

    document.getElementById("images").innerHTML = null
    document.getElementById("nuke").innerHTML = "Nuking"
    await delay(1000);

    location.reload();
}

function displayPage(x) {

    if (pageCount == 0) {
        pageCount++
        loadImages()
    }
    else {
        if (imgout.length < 50) {
            for (var i = imgout.length - 1; i > 0; i--) {
                var item = document.createElement("img");
                item.src = `https://${usrout["domain"]}/` + imgout[i]["filename"]
                item.setAttribute("onclick", `display("${imgout[i]["filename"]}","${usrout["domain"]}");`)
                document.getElementById("images").appendChild(item)
            }
            document.getElementById("next").style.display = "none"

        }
        else {
            pageCount += x

            for (var i = imgout.length - 50 * (pageCount - 1) - 1; i > imgout.length - 50 * (pageCount); i--) {
                var item = document.createElement("img");
                item.src = `https://${usrout["domain"]}/` + imgout[i]["filename"]
                item.setAttribute("onclick", `display("${imgout[i]["filename"]}","${usrout["domain"]}");`)
                document.getElementById("images").appendChild(item)
            }
        }
    }
}

function loadImages() {
    let formData = new FormData();
    formData.append("api_key", document.getElementById("keybox").value)

    fetch(`https://${url}/api/img`,
        {
            body: formData,
            method: "post"
        }).then(res => res.json())
        .then(
            data => {
                imgout = data
                console.log(imgout)
                document.getElementById("files").innerHTML = usrout["uploadCount"] + " files uploaded"
                document.getElementById("gb").innerHTML = bytes(usrout["uploadedBytes"]) + " uploaded"
                document.getElementById("time").innerHTML = changeToTime(Math.round((new Date().getTime() - Date.parse(data[data.length - 1]["timestamp"])) / 1000 / 60)) + " since last upload"

                displayPage(0)
            })
        .catch()
}

function submitDomain() {

    if (document.getElementById("domain").value != null) {
        let formData = new FormData();
        formData.append("api_key", document.getElementById("keybox").value)
        formData.append("domain", document.getElementById("domain").value)
        formData.append("showURL", document.getElementById("showURL").checked)

        fetch(`https://${url}/api/usr/domain`,
            {
                body: formData,
                method: "POST"
            }).then(res => res.json())
            .catch(e => document.getElementById("loginstat").innerHTML = "invalid key")

        document.getElementById("images").innerHTML = null

        login()
    }
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text);
    document.getElementById("copyurl").innerHTML = "copied!"
}

function delimg(hash) {
    let formData = new FormData();
    formData.append("api_key", document.getElementById("keybox").value)

    fetch(`https://${url}/api/delete/${hash}`,
        {
            body: formData,
            method: "DELETE"
        }).then(res => res.json())
        .catch()

    document.getElementById("images").innerHTML = null

    login()
}

function closePreview() {
    document.getElementById("nuke").style.display = "initial"
    document.getElementById("next").style.display = "initial"
    document.getElementById("preview").style.display = "none"
}

function display(filename, domain) {
    document.getElementById("nuke").style.display = "none"
    document.getElementById("next").style.display = "none"
    document.getElementById("preview").style.display = "initial"
    document.getElementById("copyurl").innerHTML = "copy URL"
    document.getElementById("copyurl").setAttribute("onclick", "copyToClipboard(" + `"https://${domain}/${filename}")`)
    document.getElementById("preview_img").src = `https://${domain}/` + filename
    document.getElementById("delete").setAttribute("onclick", `delimg("${filename.substr(0, 8)}");`)
}

function bytes(x) {
    if (x < 1024) {
        return (numberWithCommas(x) + " bytes")
    }
    if (x >= 1024 && x < 1048576) {
        return (numberWithCommas(Math.round(x / 1024)) + " kilobytes")
    }
    if (x >= 1048576 && x < 1073741824) {
        return (numberWithCommas(Math.round(x / 1024 / 10.24) / 100) + " megabytes")
    }
    if (x >= 1073741824) {
        return (numberWithCommas(Math.round(x / 1024 / 1024 / 10.24) / 100) + " gigabytes")
    }
}

function numberWithCommas(x) {
    return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

function changeToTime(x) {
    if (x < 60) {
        return (x == 1 ? (x + " minute") : x + " minutes")
    }
    else if (x >= 60 && x < 1440) {
        x = Math.round(x / 6) / 10
        return (x == 1 ? (x + " hour") : x + " hours")
    }
    else if (x >= 1440) {
        x = Math.round(x / 1440)
        return (x == 1 ? (x + " day") : x + " days")
    }
}