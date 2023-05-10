export function StoreCookie(user) {
    var url = "/account/storeCookie";
    var xhr = new XMLHttpRequest();

    // Initialization
    xhr.open("POST", url);
    xhr.setRequestHeader("Accept", "application/json");
    xhr.setRequestHeader("Content-Type", "application/json");

    // Call API
    xhr.send(JSON.stringify(user));
}

export function ClearCookie() {
    var url = "/account/clearCookie";
    var xhr = new XMLHttpRequest();

    // Initialization
    xhr.open("POST", url);
    xhr.setRequestHeader("Accept", "application/json");
    xhr.setRequestHeader("Content-Type", "application/json");

    // Call API
    xhr.send();
}