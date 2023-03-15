const url = window.location.host
const delay = ms => new Promise(res => setTimeout(res, ms));

document.title = url + " // dashboard"