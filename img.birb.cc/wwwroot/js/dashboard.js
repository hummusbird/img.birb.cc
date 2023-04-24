const url = window.location.host
//const url = "localhost:7247"
const delay = ms => new Promise(res => setTimeout(res, ms));

let usrout;
let imgout;

document.title = url + " // dashboard"

async function login() {
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
                document.getElementById("wide").style.display = "grid"

                document.getElementById("bottombutton").style.display = "block"

                document.getElementById("dashboard").innerHTML = "dashboard - " + usrout["username"]
                document.getElementById("topbuttons").style.display = "initial"

                document.getElementById("uid").innerHTML = usrout["uid"]
                document.getElementById("showURL").checked = usrout["showURL"]
                document.getElementById("domain").value = usrout["domain"]
                document.getElementById("feedback").value = usrout["dashMsg"]
                document.getElementById("stripEXIF").checked = usrout["stripEXIF"]

                document.getElementById("current").innerHTML = usrout["dashMsg"].length
            })
        .catch(e => document.getElementById("loginstat").innerHTML = "failed to login")
        .then(e => { if (usrout) { loadImages() } })
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
    document.getElementById("nuke").innerHTML = "nuking"
    await delay(1000);

    location.reload();
}

function displayPage(x) {
    var pagecount = parseInt(document.getElementById("pagenum").value)

    if (pagecount + x > 0 && pagecount + x <= Math.ceil(imgout.length / 50)) {
        pagecount += x
        document.getElementById("pagenum").value = pagecount
    }

    document.getElementById("images").innerHTML = null

    var pagelength = 50
    var length = pagecount * pagelength > imgout.length ? imgout.length : pagecount * pagelength

    for (var i = (pagecount - 1) * pagelength; i < length; i++) {
        var item = imgout[imgout.length - i - 1]["filename"].endsWith(".mp4") ? document.createElement("video") : document.createElement("img");
        item.src = `https://${url}/` + imgout[imgout.length - i - 1]["filename"]
        item.setAttribute("onclick", `display("${imgout[imgout.length - i - 1]["filename"]}","${imgout[imgout.length - i - 1]["timestamp"]}")`)
        document.getElementById("images").appendChild(item)
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
                document.getElementById("files").innerHTML = usrout["uploadCount"] + " files uploaded"
                document.getElementById("gb").innerHTML = bytes(usrout["uploadedBytes"]) + " uploaded"
                document.getElementById("time").innerHTML = changeToTime(Math.round((new Date().getTime() - Date.parse(data[data.length - 1]["timestamp"])) / 1000 / 60)) + " since last upload"

                displayPage()
            })
        .catch()
}

async function submitSettings() {
    if (document.getElementById("domain").value != null) {
        let formData = new FormData();
        formData.append("api_key", document.getElementById("keybox").value)
        formData.append("domain", document.getElementById("domain").value)
        formData.append("showURL", document.getElementById("showURL").checked)
        formData.append("stripEXIF", document.getElementById("stripEXIF").checked)
        formData.append("dashMsg", document.getElementById("feedback").value)

        fetch(`https://${url}/api/usr/settings`,
            {
                body: formData,
                method: "POST"
            }).then(res => res.json())
            .catch(e => document.getElementById("loginstat").innerHTML = "invalid key")

        document.getElementById("images").innerHTML = null
        await delay(100);
        login()
    }
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text);
    document.getElementById("copyurl").innerHTML = "copied!"
}

async function delimg(hash) {
    let formData = new FormData();
    formData.append("api_key", document.getElementById("keybox").value)

    fetch(`https://${url}/api/delete/${hash}`,
        {
            body: formData,
            method: "DELETE"
        }).then(
            async res => {
                document.getElementById("images").innerHTML = null
                closePreview()
                login()
            })
        .catch()
}

function closePreview() {
    document.getElementById("nuke").style.display = "initial"
    document.getElementById("next").style.display = "initial"
    document.getElementById("preview").style.display = "none"

    document.getElementById("preview_vid").src = ""
}

function display(filename, timestamp) {
    document.getElementById("nuke").style.display = "none"
    document.getElementById("next").style.display = "none"
    document.getElementById("preview").style.display = "initial"
    document.getElementById("copyurl").innerHTML = "copy URL"
    document.getElementById("copyurl").setAttribute("onclick", "copyToClipboard(" + `"${(document.getElementById("showURL").checked ? "​" : "")}https://${url}/${filename}")`)

    if (filename.endsWith(".mp4")) {
        document.getElementById("preview_vid").src = `https://${url}/` + filename
        document.getElementById("preview_vid").style.display = "initial"
        document.getElementById("preview_img").style.display = "none"
    }
    else {
        document.getElementById("preview_img").src = `https://${url}/` + filename
        document.getElementById("preview_img").style.display = "initial"
        document.getElementById("preview_vid").style.display = "none"
    }

    document.getElementById("preview_info").innerHTML = filename + " // " + timestamp.split("T")[0]

    document.getElementById("delete").setAttribute("onclick", `delimg("${filename.substr(0, 8)}");`)

    document.getElementById("preview").addEventListener("click", function (e) {
        e = window.event || e;
        if (this === e.target) {
            closePreview();
        }
    });
}

function downloadSH() {
    var SH = `#!/bin/bash\ncurl -X POST -F api_key="${document.getElementById("keybox").value}" -F img=@"\${1}" https://${window.location.host}/api/upload`

    var element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(SH));
    element.setAttribute('download', "uploader" + ".sh");

    element.style.display = 'none';
    document.body.appendChild(element);

    element.click();

    document.body.removeChild(element);
}

function downloadSXCU() {
    var SXCU = `{
    "Version": "13.7.0",
    "Name": "birb.cc",
    "DestinationType": "ImageUploader",
    "RequestMethod": "POST",
    "RequestURL": "https://${window.location.host}/api/upload",
    "Body": "MultipartFormData",
    "Arguments": {
        "api_key": "${document.getElementById("keybox").value}"
    },
    "FileFormName": "img"
}`

    var element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(SXCU));
    element.setAttribute('download', usrout["username"] + ".SXCU");

    element.style.display = 'none';
    document.body.appendChild(element);

    element.click();

    document.body.removeChild(element);
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

document.getElementById("upload").addEventListener('change', async function () {
    if (this.files[0].size > 104857600) {
        alert("Files must be smaller than 100mb")
    }
    else {
        let formData = new FormData();
        formData.append("api_key", document.getElementById("keybox").value)
        formData.append("img", this.files[0])

        fetch(`https://${url}/api/upload`,
            {
                body: formData,
                method: "POST"
            })
            .then(
                async res => {
                    document.getElementById("images").innerHTML = null
                    login()
                })
            .catch()
    }
})

document.getElementById("pagenum").addEventListener('change', async function () {
    num = document.getElementById("pagenum").value
    if (num <= 0) {
        document.getElementById("pagenum").value = 1
    }
    else if (num > Math.ceil(imgout.length / 50)) {
        document.getElementById("pagenum").value = Math.ceil(imgout.length / 50)
    }
    displayPage();
})

document.getElementById("feedback").addEventListener('input', function (e) {
    document.getElementById("current").innerHTML = e.target.value.length
})