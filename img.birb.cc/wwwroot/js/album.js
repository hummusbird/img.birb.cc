const url = window.location.host;
const hash = window.location.pathname.replace("/album/", "");

document.title = "album // " + hash; // todo: replace with album name

let jsonout;
let imgout;

login();

async function login() {
    let formData = new FormData();
    formData.append("api_key", document.getElementById("keybox").value)

    fetch(window.location.href + "/info",
        {
            body: formData,
            method: "post"
        }).then(res => res.json())
        .then(
            data => {
                jsonout = data;

                document.getElementById("bottombutton").style.display = "block";
                document.getElementById("topbuttons").style.display = "initial";
                document.getElementById("albumtitle").innerHTML = jsonout['name'];
                document.getElementById("loginarea").style.display = "none";
            }
        )
        .catch(e => {
            document.getElementById("loginstat").innerHTML = "private album"
            document.getElementById("loginarea").style.display = "flex";
        })
        .then(e => { if (jsonout) { loadImages() } })
}

function loadImages() {
    let formData = new FormData();
    formData.append("api_key", document.getElementById("keybox").value)

    fetch(window.location.href + "/images",
        {
            body: formData,
            method: "post"
        }).then(res => res.json())
        .then(
            data => {
                imgout = data
                displayPage()
            })
        .catch()
}

function display(filename, timestamp) {
    document.getElementById("next").style.display = "none"
    document.getElementById("preview").style.display = "initial"
    document.getElementById("copyurl").innerHTML = "copy URL"
    document.getElementById("copyurl").setAttribute("onclick", "copyToClipboard(" + `"https://${url}/${filename}")`)

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

function closePreview() {
    document.getElementById("next").style.display = "initial"
    document.getElementById("preview").style.display = "none"

    document.getElementById("preview_vid").src = ""
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text);
    document.getElementById("copyurl").innerHTML = "copied!"
}