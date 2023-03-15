const url = window.location.host
const delay = ms => new Promise(res => setTimeout(res, ms));

let usersout;

document.title = url + " // dashboard"

async function login() {
    let formData = new FormData();
    formData.append("api_key", document.getElementById("keybox").value)

    fetch(`https://${url}/api/users`,
        {
            body: formData,
            method: "post"
        }).then(res => res.json())
        .then(
            data => {
                usersout = data
                document.getElementById("loginstat").innerHTML = "api key:"
                document.getElementById("loginarea").style.display = "none"

                console.log(usersout)
            })
        .catch(e => document.getElementById("loginstat").innerHTML = "failed to login")
}