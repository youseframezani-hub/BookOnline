    const form = document.getElementById("signupForm");
const successAlert = document.getElementById("successAlert");

const fullName = document.getElementById("fullName");
const email = document.getElementById("email");
const mobile = document.getElementById("mobile");
const role = document.getElementById("role");
const password = document.getElementById("password");
const terms = document.getElementById("terms");
const termsError = document.getElementById("termsError");

function isValidIranMobile(value) {
    return /^09\d{9}$/.test(value.trim());
}

form.addEventListener("submit", function (e) {
    e.preventDefault();

    let isValid = true;

    // reset
    [fullName, email, mobile, role, password].forEach(el => el.classList.remove("is-invalid"));
    termsError.style.display = "none";
    successAlert.classList.add("d-none");

    if (!fullName.value.trim()) {
        fullName.classList.add("is-invalid");
        isValid = false;
    }

    if (!email.value.trim() || !email.checkValidity()) {
        email.classList.add("is-invalid");
        isValid = false;
    }

    if (!isValidIranMobile(mobile.value)) {
        mobile.classList.add("is-invalid");
        isValid = false;
    }

    if (!role.value.trim()) {
        role.classList.add("is-invalid");
        isValid = false;
    }

    if (!password.value || password.value.length < 6) {
        password.classList.add("is-invalid");
        isValid = false;
    }

    if (!terms.checked) {
        termsError.style.display = "block";
        isValid = false;
    }

    if (!isValid) return;

    // اینجا می‌تونی API واقعی صدا بزنی
    // مثال:
    // fetch('/api/register', { method:'POST', body: JSON.stringify({...}) })

    successAlert.classList.remove("d-none");
    form.reset();
});
