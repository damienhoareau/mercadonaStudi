export async function Login(userModel) {
    const url = "/account/login";
    const headers = new Headers({
        "Accept": "application/json",
        "Content-Type": "application/json",
    });
    const response = await fetch(url, {
        method: 'POST',
        headers: headers,
        body: JSON.stringify(userModel)
    });
    if (response.ok) {
        const connectedUser = await response.json();
        return connectedUser;
    }
    else {
        const errorResponse = await response.json();
        throw errorResponse.text;
    }
}

export async function Logout(accessToken) {
    const url = "/account/logout";
    const headers = new Headers({
        "Accept": "application/json",
        "Content-Type": "application/json",
        "Authorization": `Bearer ${accessToken}`,
    });
    const response = await fetch(url, {
        method: 'POST',
        headers: headers
    });
    return response.ok;
}

export async function RefreshToken(accessToken) {
    const url = "/account/refreshToken";
    const headers = new Headers({
        "Accept": "application/json",
        "Content-Type": "application/json",
        "Authorization": `Bearer ${accessToken}`,
    });
    const response = await fetch(url, {
        method: 'POST',
        headers: headers,
    });
    if (response.ok) {
        const newAccessTokenResponse = await response.json();
        return newAccessTokenResponse.text;
    }
    else {
        const errorResponse = await response.json();
        throw errorResponse.message;
    }
}