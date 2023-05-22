export function StoreCookie(user) {
    let url = "/account/storeCookie";
    let xhr = new XMLHttpRequest();

    // Initialization
    xhr.open("POST", url);
    xhr.setRequestHeader("Accept", "application/json");
    xhr.setRequestHeader("Content-Type", "application/json");

    // Call API
    xhr.send(JSON.stringify(user));
}

export function ClearCookie() {
    let url = "/account/clearCookie";
    let xhr = new XMLHttpRequest();

    // Initialization
    xhr.open("POST", url);
    xhr.setRequestHeader("Accept", "application/json");
    xhr.setRequestHeader("Content-Type", "application/json");

    // Call API
    xhr.send();
}

export function RefreshToken() {
    let url = "/account/refreshToken";
    let xhr = new XMLHttpRequest();

    // Initialization
    xhr.open("POST", url);

    // Call API
    xhr.send();
}