const API_BASE_URL = `${window.location.protocol === "https:" ? "https://localhost:7273" : "http://localhost:5169"}/api`;

const screens = ["loginScreen", "forgotPasswordScreen", "aboutScreen", "dashboardScreen"];
const contentSections = ["dashboardContent", "studentsContent", "coursesContent", "enrollmentsContent", "transactionsContent", "accountsContent", "reportsContent"];

const state = {
    authToken: null,
    userName: null,
    students: [],
    courses: [],
    enrollments: [],
    transactions: [],
    accounts: [],
    filteredAccounts: [],
    currentReport: { columns: [], rows: [] },
    previousScreen: "loginScreen",
    previousContent: "dashboardContent"
};

document.addEventListener("DOMContentLoaded", () => {
    ["reportType", "reportSemester", "reportYear"].forEach(id => {
        document.getElementById(id)?.addEventListener("change", generateReport);
    });

    document.getElementById("password")?.addEventListener("keydown", event => {
        if (event.key === "Enter") {
            login();
        }
    });

    document.getElementById("accountSearch")?.addEventListener("keydown", event => {
        if (event.key === "Enter") {
            searchAccounts();
        }
    });

    document.getElementById("accountSearch")?.addEventListener("input", () => {
        if (state.accounts.length) {
            searchAccounts(false);
        }
    });
});

function setVisibleScreen(screenId) {
    screens.forEach(id => {
        document.getElementById(id)?.classList.toggle("d-none", id !== screenId);
    });
}

function setActiveContent(contentId) {
    contentSections.forEach(id => {
        document.getElementById(id)?.classList.toggle("d-none", id !== contentId);
    });
}

function setActiveSidebar(clickedLabel) {
    document.querySelectorAll(".list-group-item").forEach(button => {
        button.classList.toggle("active", button.textContent.trim().includes(clickedLabel));
    });
}

async function login() {
    const username = document.getElementById("username")?.value.trim();
    const password = document.getElementById("password")?.value.trim();
    const errorBox = document.getElementById("loginError");

    if (!username || !password) {
        showLoginError("Please enter both username and password.");
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password })
        });

        if (!response.ok) {
            showLoginError("Invalid username or password.");
            return;
        }

        const data = await response.json();
        state.authToken = data.token;
        state.userName = data.userName;

        errorBox.classList.add("d-none");
        document.getElementById("userName").textContent = data.userName;
        setVisibleScreen("dashboardScreen");
        await loadInitialData();
        showDashboard();
    } catch (error) {
        showLoginError(`Unable to reach the API. ${error.message}`);
    }
}

function showLoginError(message) {
    const errorBox = document.getElementById("loginError");
    errorBox.textContent = message;
    errorBox.classList.remove("d-none");
}

function logout() {
    state.authToken = null;
    state.userName = null;
    document.getElementById("username").value = "";
    document.getElementById("password").value = "";
    setVisibleScreen("loginScreen");
}

function showLogin() {
    setVisibleScreen("loginScreen");
}

function showForgotPassword() {
    state.previousScreen = "loginScreen";
    setVisibleScreen("forgotPasswordScreen");
}

function showAbout() {
    const dashboardVisible = !document.getElementById("dashboardScreen")?.classList.contains("d-none");
    state.previousScreen = dashboardVisible ? "dashboardScreen" : "loginScreen";
    const activeContent = contentSections.find(id => !document.getElementById(id)?.classList.contains("d-none"));
    state.previousContent = activeContent ?? "dashboardContent";
    setVisibleScreen("aboutScreen");
}

function closeAbout() {
    if (state.previousScreen === "dashboardScreen") {
        setVisibleScreen("dashboardScreen");
        setActiveContent(state.previousContent || "dashboardContent");
        syncSidebarToContent(state.previousContent || "dashboardContent");
        return;
    }

    setVisibleScreen("loginScreen");
}

function sendRecoveryEmail() {
    const usernameOrEmail = document.getElementById("recoveryUsernameOrEmail")?.value.trim();
    const email = document.getElementById("recoveryEmail")?.value.trim();
    const newPassword = document.getElementById("recoveryNewPassword")?.value.trim();

    if (!usernameOrEmail || !email || !newPassword) {
        alert("Username/email, email, and new password are required.");
        return;
    }

    if (!isValidEmail(email)) {
        alert("Please enter a valid email address.");
        return;
    }

    if (newPassword.length < 6) {
        alert("New password must be at least 6 characters.");
        return;
    }

    apiFetch("/auth/recover", {
        method: "POST",
        body: JSON.stringify({ usernameOrEmail, email, newPassword })
    })
        .then(() => {
            alert("Password changed successfully.");
            showLogin();
        })
        .catch(error => {
            alert(`Password change failed: ${error.message}`);
        });
}

function showDashboard() {
    setVisibleScreen("dashboardScreen");
    setActiveContent("dashboardContent");
    setActiveSidebar("Dashboard");
}

async function showStudents() {
    setActiveContent("studentsContent");
    setActiveSidebar("Students");
    await loadStudents();
}

function syncSidebarToContent(contentId) {
    const labelMap = {
        dashboardContent: "Dashboard",
        studentsContent: "Students",
        coursesContent: "Courses",
        enrollmentsContent: "Enrollments",
        transactionsContent: "Transactions",
        accountsContent: "Accounts",
        reportsContent: "Reports"
    };

    setActiveSidebar(labelMap[contentId] ?? "Dashboard");
}

async function showCourses() {
    setActiveContent("coursesContent");
    setActiveSidebar("Courses");
    await loadCourses();
}

async function showEnrollments() {
    setActiveContent("enrollmentsContent");
    setActiveSidebar("Enrollments");
    await loadEnrollments();
}

async function showTransactions() {
    setActiveContent("transactionsContent");
    setActiveSidebar("Transactions");
    if (!state.students.length || !state.courses.length) {
        await Promise.all([loadStudents(), loadCourses()]);
    }
    await loadTransactions();
}

async function showReports() {
    setActiveContent("reportsContent");
    setActiveSidebar("Reports");
    if (!state.students.length || !state.courses.length || !state.enrollments.length) {
        await Promise.all([loadStudents(), loadCourses(), loadEnrollments()]);
    }
    await generateReport();
}

async function showAccounts() {
    setActiveContent("accountsContent");
    setActiveSidebar("Accounts");
    await loadAccounts();
}

async function loadInitialData() {
    await Promise.all([
        loadDashboard(),
        loadStudents(),
        loadCourses(),
        loadEnrollments(),
        loadAccounts()
    ]);
    populateEnrollmentOptions();
}

async function apiFetch(path, options = {}) {
    const response = await fetch(`${API_BASE_URL}${path}`, {
        ...options,
        headers: {
            "Content-Type": "application/json",
            ...(options.headers || {})
        }
    });

    if (!response.ok) {
        let message = `Request failed with status ${response.status}`;
        try {
            const problem = await response.json();
            message = problem.detail || problem.title || message;
        } catch {
        }
        throw new Error(message);
    }

    if (response.status === 204) {
        return null;
    }

    const text = await response.text();
    return text ? JSON.parse(text) : null;
}

async function loadDashboard() {
    try {
        const data = await apiFetch("/dashboard");
        document.getElementById("totalStudents").textContent = data.totalStudents;
        document.getElementById("totalCourses").textContent = data.totalCourses;
        document.getElementById("avgGPA").textContent = data.averageGpa == null ? "N/A" : Number(data.averageGpa).toFixed(2);

        const tbody = document.querySelector("#recentEnrollmentsTable tbody");
        tbody.innerHTML = data.recentEnrollments.map(item => `
            <tr>
                <td>${item.studentName}</td>
                <td>${item.courseName}</td>
                <td>${item.semester}</td>
                <td>${item.year}</td>
                <td>${item.grade ?? "N/A"}</td>
            </tr>
        `).join("");
    } catch (error) {
        console.error(error);
        renderDashboardFromLocalState();
    }
}

function renderDashboardFromLocalState() {
    document.getElementById("totalStudents").textContent = state.students.length;
    document.getElementById("totalCourses").textContent = state.courses.length;

    const grades = state.enrollments
        .map(item => Number(item.grade))
        .filter(Number.isFinite);

    document.getElementById("avgGPA").textContent = grades.length
        ? (grades.reduce((sum, value) => sum + value, 0) / grades.length).toFixed(2)
        : "N/A";

    renderRecentEnrollmentPreview();
}

async function loadStudents() {
    try {
        state.students = await apiFetch("/students");
        renderStudents();
    } catch (error) {
        console.error(error);
        alert(`Unable to load students: ${error.message}`);
    }
}

function renderStudents() {
    const tbody = document.querySelector("#studentsTable tbody");
    tbody.innerHTML = state.students.map(student => `
        <tr>
            <td>${student.studentId}</td>
            <td>${student.firstName}</td>
            <td>${student.lastName}</td>
            <td>${student.gender}</td>
            <td>${student.email}</td>
            <td>
                <button class="btn btn-sm btn-outline-primary" onclick="editStudent(${student.studentId})">Edit</button>
            </td>
        </tr>
    `).join("");
}

async function loadCourses() {
    try {
        state.courses = await apiFetch("/courses");
        renderCourses();
    } catch (error) {
        console.error(error);
        alert(`Unable to load courses: ${error.message}`);
    }
}

function renderCourses() {
    const tbody = document.querySelector("#coursesTable tbody");
    tbody.innerHTML = state.courses.map(course => `
        <tr>
            <td>${course.courseId}</td>
            <td>${course.courseName}</td>
            <td>${course.courseCode}</td>
            <td>${course.units}</td>
            <td>
                <button class="btn btn-sm btn-outline-primary" onclick="editCourse(${course.courseId})">Edit</button>
            </td>
        </tr>
    `).join("");
}

async function loadEnrollments() {
    try {
        state.enrollments = await apiFetch("/enrollments");
        renderEnrollments();
    } catch (error) {
        console.error(error);
        alert(`Unable to load enrollments: ${error.message}`);
    }
}

async function loadAccounts() {
    try {
        state.accounts = await apiFetch("/accounts");
        searchAccounts(false);
    } catch (error) {
        console.error(error);
        alert(`Unable to load accounts: ${error.message}`);
    }
}

function renderAccounts() {
    const tbody = document.querySelector("#accountsTable tbody");
    if (!tbody) {
        return;
    }

    const accountsToRender = state.filteredAccounts.length || !document.getElementById("accountSearch")?.value.trim()
        ? state.filteredAccounts
        : [];

    if (!accountsToRender.length) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted">No accounts found.</td></tr>`;
        return;
    }

    tbody.innerHTML = accountsToRender.map(account => `
        <tr>
            <td>${account.accountId}</td>
            <td>${account.username}</td>
            <td>${account.email}</td>
            <td>${account.role}</td>
            <td>${account.status}</td>
            <td>${account.studentId ?? account.instructorId ?? "N/A"}</td>
            <td>
                <button class="btn btn-sm btn-outline-primary" onclick="editAccount(${account.accountId})">Edit</button>
                <button class="btn btn-sm btn-outline-secondary" onclick="toggleAccountStatus(${account.accountId}, '${account.status}')">
                    ${account.status === "Active" ? "Deactivate" : "Activate"}
                </button>
            </td>
        </tr>
    `).join("");
}

function searchAccounts(showEmptyMessage = true) {
    const search = document.getElementById("accountSearch")?.value.trim().toLowerCase() ?? "";

    state.filteredAccounts = state.accounts.filter(account => {
        if (!search) {
            return true;
        }

        const linkedId = account.studentId ?? account.instructorId ?? "";
        return [
            account.username,
            account.email,
            account.role,
            account.status,
            String(linkedId)
        ].some(value => String(value).toLowerCase().includes(search));
    });

    renderAccounts();

    if (showEmptyMessage && search && !state.filteredAccounts.length) {
        alert("No matching accounts found.");
    }
}

function renderEnrollments() {
    const tbody = document.querySelector("#enrollmentsTable tbody");
    tbody.innerHTML = state.enrollments.map(item => `
        <tr>
            <td>${item.enrollmentId}</td>
            <td>${item.studentName}</td>
            <td>${item.courseName}</td>
            <td>${item.semester}</td>
            <td>${item.year}</td>
            <td>${item.grade ?? "N/A"}</td>
            <td>
                <button class="btn btn-sm btn-outline-primary" onclick="editEnrollment(${item.enrollmentId})">Edit</button>
            </td>
        </tr>
    `).join("");
}

async function loadTransactions() {
    try {
        const allTransactions = await apiFetch("/transactions");
        const filterType = document.getElementById("transactionFilterType")?.value ?? "All";
        state.transactions = filterType === "All"
            ? allTransactions
            : allTransactions.filter(item => item.transactionType === filterType);
        renderTransactions();
        setTransactionStatus(`${state.transactions.length} transaction records loaded.`, "info");
    } catch (error) {
        console.error(error);
        setTransactionStatus(`Unable to load transactions: ${error.message}`, "warning");
    }
}

function renderTransactions() {
    const tbody = document.querySelector("#transactionsTable tbody");
    if (!tbody) {
        return;
    }

    if (!state.transactions.length) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted">No transaction records found.</td></tr>`;
        return;
    }

    tbody.innerHTML = state.transactions.map(item => `
        <tr>
            <td>${item.transactionId}</td>
            <td>${item.studentName}</td>
            <td>${item.transactionType}</td>
            <td>${item.description || "N/A"}</td>
            <td>${formatDate(item.transactionDate)}</td>
            <td>${item.amount == null ? "N/A" : Number(item.amount).toFixed(2)}</td>
            <td>${item.status}</td>
        </tr>
    `).join("");
}

function populateEnrollmentOptions() {
    const studentSelect = document.getElementById("enrollmentStudentId");
    const courseSelect = document.getElementById("enrollmentCourseId");

    studentSelect.innerHTML = state.students
        .map(student => `<option value="${student.studentId}">${student.firstName} ${student.lastName}</option>`)
        .join("");

    courseSelect.innerHTML = state.courses
        .map(course => `<option value="${course.courseId}">${course.courseName}</option>`)
        .join("");
}

function populateTransactionOptions() {
    const studentSelect = document.getElementById("transactionStudentId");
    const courseSelect = document.getElementById("transactionCourseId");

    if (studentSelect) {
        studentSelect.innerHTML = state.students
            .map(student => `<option value="${student.studentId}">${student.firstName} ${student.lastName}</option>`)
            .join("");
    }

    if (courseSelect) {
        courseSelect.innerHTML = state.courses
            .map(course => `<option value="${course.courseId}">${course.courseName}</option>`)
            .join("");
    }
}

function clearStudentForm() {
    document.getElementById("studentId").value = "";
    document.getElementById("firstName").value = "";
    document.getElementById("lastName").value = "";
    document.getElementById("gender").value = "Male";
    document.getElementById("birthDate").value = "";
    document.getElementById("email").value = "";
}

function clearCourseForm() {
    document.getElementById("courseId").value = "";
    document.getElementById("courseName").value = "";
    document.getElementById("courseCode").value = "";
    document.getElementById("units").value = "";
}

function clearEnrollmentForm() {
    document.getElementById("enrollmentId").value = "";
    document.getElementById("enrollmentSemester").value = "1st";
    document.getElementById("enrollmentYear").value = new Date().getFullYear();
    document.getElementById("enrollmentGrade").value = "";
}

function clearAccountForm() {
    document.getElementById("accountId").value = "";
    document.getElementById("accountUsername").value = "";
    document.getElementById("accountEmail").value = "";
    document.getElementById("accountPassword").value = "";
    document.getElementById("accountRole").value = "Admin";
    document.getElementById("accountStatus").value = "Active";
    document.getElementById("accountStudentId").value = "";
    document.getElementById("accountInstructorId").value = "";
}

async function prepareEnrollmentForm() {
    await Promise.all([loadStudents(), loadCourses()]);
    populateEnrollmentOptions();
    clearEnrollmentForm();

    if (!state.students.length) {
        alert("Add at least one student before creating an enrollment.");
        return;
    }

    if (!state.courses.length) {
        alert("Add at least one course before creating an enrollment.");
    }
}

function editStudent(id) {
    const student = state.students.find(item => item.studentId === id);
    if (!student) {
        return;
    }

    document.getElementById("studentId").value = student.studentId;
    document.getElementById("firstName").value = student.firstName;
    document.getElementById("lastName").value = student.lastName;
    document.getElementById("gender").value = student.gender;
    document.getElementById("birthDate").value = student.birthDate ?? "";
    document.getElementById("email").value = student.email ?? "";
    bootstrap.Modal.getOrCreateInstance(document.getElementById("studentModal")).show();
}

function editCourse(id) {
    const course = state.courses.find(item => item.courseId === id);
    if (!course) {
        return;
    }

    document.getElementById("courseId").value = course.courseId;
    document.getElementById("courseName").value = course.courseName;
    document.getElementById("courseCode").value = course.courseCode;
    document.getElementById("units").value = course.units;
    bootstrap.Modal.getOrCreateInstance(document.getElementById("courseModal")).show();
}

function editEnrollment(id) {
    const enrollment = state.enrollments.find(item => item.enrollmentId === id);
    if (!enrollment) {
        return;
    }

    populateEnrollmentOptions();
    document.getElementById("enrollmentId").value = enrollment.enrollmentId;
    document.getElementById("enrollmentStudentId").value = enrollment.studentId;
    document.getElementById("enrollmentCourseId").value = enrollment.courseId;
    document.getElementById("enrollmentSemester").value = enrollment.semester;
    document.getElementById("enrollmentYear").value = enrollment.year;
    document.getElementById("enrollmentGrade").value = enrollment.grade ?? "";
    bootstrap.Modal.getOrCreateInstance(document.getElementById("enrollmentModal")).show();
}

function editAccount(id) {
    const account = state.accounts.find(item => item.accountId === id);
    if (!account) {
        return;
    }

    document.getElementById("accountId").value = account.accountId;
    document.getElementById("accountUsername").value = account.username;
    document.getElementById("accountEmail").value = account.email;
    document.getElementById("accountPassword").value = "";
    document.getElementById("accountRole").value = account.role;
    document.getElementById("accountStatus").value = account.status;
    document.getElementById("accountStudentId").value = account.studentId ?? "";
    document.getElementById("accountInstructorId").value = account.instructorId ?? "";
    bootstrap.Modal.getOrCreateInstance(document.getElementById("accountModal")).show();
}

async function prepareTransactionForm() {
    await Promise.all([loadStudents(), loadCourses()]);
    populateTransactionOptions();
    document.getElementById("transactionType").value = "Enrollment";
    document.getElementById("transactionSemester").value = "1st";
    document.getElementById("transactionYear").value = new Date().getFullYear();
    document.getElementById("transactionAmount").value = "";
    document.getElementById("transactionPaymentMethod").value = "Cash";
    document.getElementById("transactionNotes").value = "";
    syncTransactionFields();
}

function syncTransactionFields() {
    const type = document.getElementById("transactionType")?.value;
    const isEnrollment = type === "Enrollment";
    const isRegistration = type === "Course Registration";
    const isPayment = type === "Payment";

    document.getElementById("transactionCourseGroup")?.classList.toggle("d-none", isPayment);
    document.getElementById("transactionSemesterGroup")?.classList.toggle("d-none", !isRegistration);
    document.getElementById("transactionYearGroup")?.classList.toggle("d-none", !isRegistration);
    document.getElementById("transactionAmountGroup")?.classList.toggle("d-none", isEnrollment);
    document.getElementById("transactionPaymentMethodGroup")?.classList.toggle("d-none", !isPayment);
}

async function saveTransaction() {
    const type = document.getElementById("transactionType").value;
    const studentId = Number(document.getElementById("transactionStudentId").value);
    const courseId = Number(document.getElementById("transactionCourseId").value);
    const amount = Number(document.getElementById("transactionAmount").value);
    const notes = document.getElementById("transactionNotes").value.trim();

    if (!studentId) {
        alert("Please select a student.");
        return;
    }

    let path = "/transactions/enrollment";
    let payload = { studentId, courseId, notes };

    if (type === "Enrollment" && !courseId) {
        alert("Please select a course.");
        return;
    }

    if (type === "Course Registration") {
        const year = Number(document.getElementById("transactionYear").value);
        if (!courseId || !Number.isInteger(year) || year < 2000 || year > 2100 || !amount || amount <= 0) {
            alert("Course registration requires course, valid year, and fee.");
            return;
        }

        path = "/transactions/course-registration";
        payload = {
            studentId,
            courseId,
            semester: document.getElementById("transactionSemester").value,
            year,
            registrationFee: amount
        };
    }

    if (type === "Payment") {
        if (!amount || amount <= 0) {
            alert("Payment amount must be greater than 0.");
            return;
        }

        path = "/transactions/payment";
        payload = {
            studentId,
            amount,
            paymentMethod: document.getElementById("transactionPaymentMethod").value,
            paymentDescription: notes
        };
    }

    try {
        await apiFetch(path, { method: "POST", body: JSON.stringify(payload) });
        bootstrap.Modal.getOrCreateInstance(document.getElementById("transactionModal")).hide();
        await loadTransactions();
        setTransactionStatus(`${type} transaction saved successfully.`, "success");
    } catch (error) {
        alert(`Unable to save transaction: ${error.message}`);
    }
}

async function saveStudent() {
    const previousCount = state.students.length;
    const id = document.getElementById("studentId").value;
    const payload = {
        firstName: document.getElementById("firstName").value.trim(),
        lastName: document.getElementById("lastName").value.trim(),
        gender: document.getElementById("gender").value,
        birthDate: document.getElementById("birthDate").value || null,
        email: document.getElementById("email").value.trim()
    };

    if (!payload.firstName || !payload.lastName) {
        alert("First name and last name are required.");
        return;
    }

    if (payload.email && !isValidEmail(payload.email)) {
        alert("Please enter a valid email address.");
        return;
    }

    try {
        if (id) {
            await apiFetch(`/students/${id}`, { method: "PUT", body: JSON.stringify(payload) });
        } else {
            await apiFetch("/students", { method: "POST", body: JSON.stringify(payload) });
        }

        bootstrap.Modal.getOrCreateInstance(document.getElementById("studentModal")).hide();
        await loadStudents();
        await loadDashboard();
        if (!id && state.students.length <= previousCount) {
            document.getElementById("totalStudents").textContent = previousCount + 1;
        } else {
            document.getElementById("totalStudents").textContent = state.students.length;
        }
        populateEnrollmentOptions();
        alert("Student saved. To assign a course, open Enrollment Management and click New Enrollment.");
    } catch (error) {
        alert(`Unable to save student: ${error.message}`);
    }
}

async function saveCourse() {
    const id = document.getElementById("courseId").value;
    const payload = {
        courseName: document.getElementById("courseName").value.trim(),
        courseCode: document.getElementById("courseCode").value.trim(),
        units: Number(document.getElementById("units").value)
    };

    if (!payload.courseName || !payload.courseCode) {
        alert("Course name and course code are required.");
        return;
    }

    if (!Number.isInteger(payload.units) || payload.units <= 0) {
        alert("Units must be a whole number greater than 0.");
        return;
    }

    try {
        if (id) {
            await apiFetch(`/courses/${id}`, { method: "PUT", body: JSON.stringify(payload) });
        } else {
            await apiFetch("/courses", { method: "POST", body: JSON.stringify(payload) });
        }

        bootstrap.Modal.getOrCreateInstance(document.getElementById("courseModal")).hide();
        await loadCourses();
        await loadDashboard();
        populateEnrollmentOptions();
    } catch (error) {
        alert(`Unable to save course: ${error.message}`);
    }
}

async function saveEnrollment() {
    const id = document.getElementById("enrollmentId").value;
    const payload = {
        studentId: Number(document.getElementById("enrollmentStudentId").value),
        courseId: Number(document.getElementById("enrollmentCourseId").value),
        semester: document.getElementById("enrollmentSemester").value,
        year: Number(document.getElementById("enrollmentYear").value),
        grade: document.getElementById("enrollmentGrade").value ? Number(document.getElementById("enrollmentGrade").value) : null
    };

    if (!payload.studentId || !payload.courseId) {
        alert("Please select both a student and a course.");
        return;
    }

    if (!payload.semester) {
        alert("Please select a semester.");
        return;
    }

    if (!Number.isInteger(payload.year) || payload.year < 2000 || payload.year > 2100) {
        alert("Please enter a valid year.");
        return;
    }

    if (payload.grade !== null && (Number.isNaN(payload.grade) || payload.grade < 1 || payload.grade > 5)) {
        alert("Grade must be between 1.00 and 5.00.");
        return;
    }

    try {
        if (id) {
            await apiFetch(`/enrollments/${id}`, { method: "PUT", body: JSON.stringify(payload) });
        } else {
            await apiFetch("/enrollments", { method: "POST", body: JSON.stringify(payload) });
        }

        bootstrap.Modal.getOrCreateInstance(document.getElementById("enrollmentModal")).hide();
        await loadEnrollments();
        await loadDashboard();
        if (!document.getElementById("dashboardScreen")?.classList.contains("d-none")) {
            renderRecentEnrollmentPreview();
        }
    } catch (error) {
        alert(`Unable to save enrollment: ${error.message}`);
    }
}

async function saveAccount() {
    const id = document.getElementById("accountId").value;
    const payload = {
        username: document.getElementById("accountUsername").value.trim(),
        email: document.getElementById("accountEmail").value.trim(),
        password: document.getElementById("accountPassword").value.trim() || null,
        role: document.getElementById("accountRole").value,
        status: document.getElementById("accountStatus").value,
        studentId: document.getElementById("accountStudentId").value ? Number(document.getElementById("accountStudentId").value) : null,
        instructorId: document.getElementById("accountInstructorId").value ? Number(document.getElementById("accountInstructorId").value) : null
    };

    if (!payload.username || !payload.email || !payload.role) {
        alert("Username, email, and role are required.");
        return;
    }

    if (!isValidEmail(payload.email)) {
        alert("Please enter a valid email address.");
        return;
    }

    if (!id && !payload.password) {
        alert("Password is required for new accounts.");
        return;
    }

    if (payload.password && payload.password.length < 6) {
        alert("Password must be at least 6 characters.");
        return;
    }

    if (payload.role === "Student" && !payload.studentId) {
        alert("Student role requires a student ID.");
        return;
    }

    if (payload.role === "Instructor" && !payload.instructorId) {
        alert("Instructor role requires an instructor ID.");
        return;
    }

    try {
        if (id) {
            await apiFetch(`/accounts/${id}`, { method: "PUT", body: JSON.stringify(payload) });
        } else {
            await apiFetch("/accounts", { method: "POST", body: JSON.stringify(payload) });
        }

        bootstrap.Modal.getOrCreateInstance(document.getElementById("accountModal")).hide();
        await loadAccounts();
    } catch (error) {
        alert(`Unable to save account: ${error.message}`);
    }
}

async function toggleAccountStatus(id, currentStatus) {
    const nextStatus = currentStatus === "Active" ? "Inactive" : "Active";

    try {
        await apiFetch(`/accounts/${id}/status`, {
            method: "PUT",
            body: JSON.stringify({ status: nextStatus })
        });
        await loadAccounts();
    } catch (error) {
        alert(`Unable to update account status: ${error.message}`);
    }
}

function renderRecentEnrollmentPreview() {
    const tbody = document.querySelector("#recentEnrollmentsTable tbody");
    const latestItems = [...state.enrollments]
        .sort((left, right) => right.enrollmentId - left.enrollmentId)
        .slice(0, 10);

    tbody.innerHTML = latestItems.map(item => `
        <tr>
            <td>${item.studentName}</td>
            <td>${item.courseName}</td>
            <td>${item.semester}</td>
            <td>${item.year}</td>
            <td>${item.grade ?? "N/A"}</td>
        </tr>
    `).join("");
}

async function generateReport() {
    const reportType = document.getElementById("reportType").value;
    const semester = document.getElementById("reportSemester").value;
    const year = document.getElementById("reportYear").value.trim();

    setReportStatus("Generating report...", "info");

    if (year && (!/^\d{4}$/.test(year) || Number(year) < 2000 || Number(year) > 2100)) {
        alert("Report year must be a valid 4-digit year.");
        return;
    }

    try {
        const params = new URLSearchParams({ type: reportType });
        if (semester) {
            params.set("semester", semester);
        }
        if (year) {
            params.set("year", year);
        }

        const data = await apiFetch(`/reports?${params.toString()}`);
        state.currentReport = normalizeReportData(data);
        setReportStatus(`Report generated successfully from the API.`, "info");
        renderReportTable();
    } catch (error) {
        state.currentReport = normalizeReportData(buildLocalReport(reportType, semester, year));
        setReportStatus(`API report failed, so a local preview was generated instead. ${error.message}`, "warning");
        renderReportTable();
    }
}

function renderReportTable() {
    const head = document.querySelector("#reportTable thead");
    const body = document.querySelector("#reportTable tbody");
    const columns = Array.isArray(state.currentReport.columns) ? state.currentReport.columns : [];
    const rows = Array.isArray(state.currentReport.rows) ? state.currentReport.rows : [];

    if (!columns.length) {
        head.innerHTML = "<tr><th>Report</th></tr>";
        body.innerHTML = `<tr><td class="text-center text-muted">No report columns were returned.</td></tr>`;
        return;
    }

    head.innerHTML = `<tr>${columns.map(column => `<th>${formatColumnName(column)}</th>`).join("")}</tr>`;

    if (!rows.length) {
        body.innerHTML = `<tr><td colspan="${Math.max(columns.length, 1)}" class="text-center text-muted">No data found for the selected filters.</td></tr>`;
        return;
    }

    body.innerHTML = rows.map(row => `
        <tr>
            ${normalizeRow(row, columns.length).map(value => `<td>${value ?? "N/A"}</td>`).join("")}
        </tr>
    `).join("");
}

function formatColumnName(name) {
    return name.replaceAll("_", " ").replace(/\b\w/g, char => char.toUpperCase());
}

async function exportToExcel() {
    const reportType = document.getElementById("reportType").value;
    const semester = document.getElementById("reportSemester").value;
    const year = document.getElementById("reportYear").value.trim();
    const signerName = document.getElementById("reportSignerName").value.trim() || "Authorized Signatory";

    try {
        const response = await fetch(`${API_BASE_URL}/reports/export`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                type: reportType,
                semester: semester || null,
                year: year ? Number(year) : null,
                signerName
            })
        });

        if (!response.ok) {
            throw new Error(await readErrorMessage(response));
        }

        await downloadBlob(response, `${reportType}.xlsx`);
    } catch (error) {
        alert(`Unable to export Excel report: ${error.message}`);
    }
}

function exportToCSV() {
    downloadCsv("report.csv");
}

function printReport() {
    window.print();
}

async function exportTransactionsToExcel() {
    const signerName = document.getElementById("transactionSignerName").value.trim() || "Authorized Signatory";
    const transactionType = document.getElementById("transactionFilterType").value;

    try {
        const response = await fetch(`${API_BASE_URL}/transactions/report/export`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ transactionType, signerName })
        });

        if (!response.ok) {
            throw new Error(await readErrorMessage(response));
        }

        await downloadBlob(response, "TransactionReport.xlsx");
    } catch (error) {
        alert(`Unable to export transactions: ${error.message}`);
    }
}

function downloadCsv(fileName) {
    const columns = state.currentReport.columns || [];
    const rows = state.currentReport.rows || [];

    if (!columns.length) {
        alert("Generate a report first.");
        return;
    }

    const lines = [
        columns.join(","),
        ...rows.map(row => row.map(escapeCsvValue).join(","))
    ];

    const blob = new Blob([lines.join("\n")], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
}

async function downloadBlob(response, fallbackFileName) {
    const blob = await response.blob();
    const disposition = response.headers.get("content-disposition") || "";
    const match = disposition.match(/filename="?([^"]+)"?/i);
    const fileName = match?.[1] || fallbackFileName;
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
}

async function readErrorMessage(response) {
    try {
        const problem = await response.json();
        return problem.detail || problem.title || `Request failed with status ${response.status}`;
    } catch {
        return `Request failed with status ${response.status}`;
    }
}

function escapeCsvValue(value) {
    const text = String(value ?? "");
    if (text.includes(",") || text.includes("\"") || text.includes("\n")) {
        return `"${text.replaceAll("\"", "\"\"")}"`;
    }
    return text;
}

function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function setReportStatus(message, type = "info") {
    const status = document.getElementById("reportStatus");
    status.textContent = message;
    status.className = `alert alert-${type} mb-3`;
    status.classList.remove("d-none");
}

function setTransactionStatus(message, type = "info") {
    const status = document.getElementById("transactionStatus");
    if (!status) {
        return;
    }

    status.textContent = message;
    status.className = `alert alert-${type} mb-3`;
    status.classList.remove("d-none");
}

function formatDate(value) {
    if (!value) {
        return "N/A";
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString();
}

function normalizeReportData(data) {
    return {
        columns: Array.isArray(data?.columns) ? data.columns : [],
        rows: Array.isArray(data?.rows) ? data.rows : []
    };
}

function normalizeRow(row, expectedLength) {
    if (Array.isArray(row)) {
        return row;
    }

    if (row && typeof row === "object") {
        return Object.values(row).slice(0, expectedLength);
    }

    return [row];
}

function buildLocalReport(reportType, semester, year) {
    const filteredEnrollments = state.enrollments.filter(item => {
        const semesterMatch = !semester || item.semester === semester;
        const yearMatch = !year || String(item.year) === String(year);
        return semesterMatch && yearMatch;
    });

    if (reportType === "student-academic-performance") {
        return {
            columns: ["student_id", "student_name", "courses_taken", "average_gpa", "best_grade", "latest_semester", "latest_year"],
            rows: state.students.map(student => [
                student.studentId,
                `${student.firstName} ${student.lastName}`,
                filteredEnrollments.filter(item => item.studentId === student.studentId).length,
                calculateAverageGpa(filteredEnrollments.filter(item => item.studentId === student.studentId)),
                calculateBestGrade(filteredEnrollments.filter(item => item.studentId === student.studentId)),
                getLatestSemester(filteredEnrollments.filter(item => item.studentId === student.studentId)),
                getLatestYear(filteredEnrollments.filter(item => item.studentId === student.studentId))
            ])
        };
    }

    if (reportType === "course-popularity-performance") {
        return {
            columns: ["course_id", "course_name", "course_code", "units", "enrolled_students", "average_grade", "pass_rate"],
            rows: state.courses.map(course => [
                course.courseId,
                course.courseName,
                course.courseCode,
                course.units,
                filteredEnrollments.filter(item => item.courseId === course.courseId).length,
                calculateAverageGpa(filteredEnrollments.filter(item => item.courseId === course.courseId)),
                calculatePassRate(filteredEnrollments.filter(item => item.courseId === course.courseId))
            ])
        };
    }

    if (reportType === "semester-summary-trends") {
        const grouped = groupEnrollmentsBySemester(filteredEnrollments);
        return {
            columns: ["semester", "year", "total_enrollments", "unique_students", "average_grade", "passing_students"],
            rows: grouped.map(group => [
                group.semester,
                group.year,
                group.items.length,
                new Set(group.items.map(item => item.studentId)).size,
                calculateAverageGpa(group.items),
                group.items.filter(item => Number(item.grade) > 0 && Number(item.grade) <= 3).length
            ])
        };
    }

    return {
        columns: ["notice"],
        rows: [["Unsupported report type selected."]]
    };
}

function calculateAverageGpa(items) {
    const graded = items.map(item => Number(item.grade)).filter(Number.isFinite);
    if (!graded.length) {
        return null;
    }
    return (graded.reduce((sum, value) => sum + value, 0) / graded.length).toFixed(2);
}

function calculateBestGrade(items) {
    const graded = items.map(item => Number(item.grade)).filter(Number.isFinite);
    if (!graded.length) {
        return null;
    }
    return Math.min(...graded).toFixed(2);
}

function calculatePassRate(items) {
    const graded = items.map(item => Number(item.grade)).filter(Number.isFinite);
    if (!graded.length) {
        return null;
    }
    const passed = graded.filter(grade => grade <= 3).length;
    return `${((passed / graded.length) * 100).toFixed(0)}%`;
}

function getLatestSemester(items) {
    if (!items.length) {
        return null;
    }
    const latest = [...items].sort((a, b) => (b.year - a.year) || compareSemesterOrder(b.semester, a.semester))[0];
    return latest?.semester ?? null;
}

function getLatestYear(items) {
    if (!items.length) {
        return null;
    }
    return Math.max(...items.map(item => Number(item.year)).filter(Number.isFinite));
}

function compareSemesterOrder(left, right) {
    const order = { "1st": 1, "2nd": 2, "Summer": 3 };
    return (order[left] ?? 99) - (order[right] ?? 99);
}

function groupEnrollmentsBySemester(items) {
    const groups = new Map();

    items.forEach(item => {
        const key = `${item.year}-${item.semester}`;
        if (!groups.has(key)) {
            groups.set(key, { semester: item.semester, year: item.year, items: [] });
        }
        groups.get(key).items.push(item);
    });

    return [...groups.values()].sort((a, b) => (a.year - b.year) || compareSemesterOrder(a.semester, b.semester));
}
