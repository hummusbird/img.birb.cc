const url = window.location.host
const delay = ms => new Promise(res => setTimeout(res, ms));

let usersout;

document.title = url + " // dashboard"

async function login() {
    let formData = new FormData();
    formData.append("api_key", document.getElementById("keybox").value)

    fetch(`/api/users`,
        {
            body: formData,
            method: "post"
        }).then(res => res.json())
        .then(
            data => {
                usersout = data;
                document.getElementById("loginstat").innerHTML = "api key:"
                document.getElementById("loginarea").style.display = "none"

                document.getElementById("newuserblock").style.display = "block"

                table.style.display = "table"
            })
        .catch(e => document.getElementById("loginstat").innerHTML = "failed to login")
        .then(e => { if (usersout) { buildTable() } })
}

async function getimgs(uid) {
    let formData = new FormData();
    formData.append("api_key", document.getElementById("keybox").value)
    formData.append("uid", uid)

    let imgs;

    await fetch(`/api/img`,
        {
            body: formData,
            method: "post"
        }).then(res => res.json())
        .then(
            data => {
                imgs = data
            })
        .catch(e => console.log(e))

    return imgs;
}

async function buildTable() {

    // todo:
    // nuke imgs per user
    // invite gen
    // lock / unlock user
    // create new user
    // reset API keys

    let table = document.getElementById("table")
    table.innerHTML = ""

    usersout.forEach(async user => {
        var row = table.insertRow(table.rows.length);
        var UID = row.insertCell(0);

        var last = row.insertCell(1);
        var admin = row.insertCell(1);

        var uploadsize = row.insertCell(1)
        var uploadcount = row.insertCell(1);

        var username = row.insertCell(1);

        UID.innerHTML = user["uid"]
        username.innerHTML = user["username"]

        uploadcount.innerHTML = user["uploadCount"]
        uploadsize.innerHTML = bytes(user["uploadedBytes"])

        admin.innerHTML = user["isAdmin"]

        data = await getimgs(user["uid"])
        last.innerHTML = user["uploadCount"] != 0 ? changeToTime(Math.round((new Date().getTime() - Date.parse(data[data.length - 1]["timestamp"])) / 1000 / 60)) + " ago" : "never"
    });

    var title = table.insertRow(0)
    title.style["font-weight"] = "bolder"
    title.style["background"] = "white"
    title.style["color"] = "black"

    var uid_title = title.insertCell(0)
    var last_title = title.insertCell(1)
    var admin_title = title.insertCell(1)
    var uploadsize_title = title.insertCell(1)
    var uploadcount_title = title.insertCell(1)
    var username_title = title.insertCell(1)

    uid_title.innerHTML = "UID"
    last_title.innerHTML = "last upload"
    admin_title.innerHTML = "admin"
    uploadsize_title.innerHTML = "size"
    uploadcount_title.innerHTML = "uploads"
    username_title.innerHTML = "username"
}

async function submitnewuser() {
    let name = document.getElementById("username").value
    let uidbox = document.getElementById("UID")
    let uid = null;
    if (uidbox != null) { uid = uidbox.value }

    document.getElementById("newuseroutput").value = await newuser(name, uid)
}

async function newuser(name, uid) {
    let formData = new FormData();
    formData.append("api_key", document.getElementById("keybox").value)
    formData.append("username", name)
    if (uid != null) { formData.append("uid", uid) }

    let token;

    await fetch(`/api/usr/new`,
        {
            body: formData,
            method: "post"
        })
        .then(res => res.json())
        .then(
            data => {
                token = data
            })
        .catch(e => { console.log(e) })

    return token;
}

function numberWithCommas(x) {
    return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

function bytes(x) {
    if (x < 1024) {
        return (numberWithCommas(x) + " b")
    }
    if (x >= 1024 && x < 1048576) {
        return (numberWithCommas(Math.round(x / 1024)) + " kb")
    }
    if (x >= 1048576 && x < 1073741824) {
        return (numberWithCommas(Math.round(x / 1024 / 10.24) / 100) + " mb")
    }
    if (x >= 1073741824) {
        return (numberWithCommas(Math.round(x / 1024 / 1024 / 10.24) / 100) + " gb")
    }
}

function changeToTime(x) {
    if (x < 60) {
        return (x == 1 ? (x + " minute") : x + " mins")
    }
    else if (x >= 60 && x < 1440) {
        x = Math.round(x / 6) / 10
        return (x == 1 ? (x + " hour") : x + " hrs")
    }
    else if (x >= 1440) {
        x = Math.round(x / 1440)
        return (x == 1 ? (x + " day") : x + " days")
    }
}