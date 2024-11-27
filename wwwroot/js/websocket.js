const socket = new WebSocket("ws://localhost:5180/ws");

socket.onopen = function() {
    console.log("WebSocket соединение установлено");
};

socket.onmessage = function(event) {
    const data = JSON.parse(event.data);
    handleResponse(data);
};

function handleResponse(data) {
    switch(data.action) {
        case "get_law_branches":
            renderLawBranches(data.branches);
            break;
        case "get_test_collections":
            renderTestCollections(data.collections);
            break;
        case "get_test_questions":
            renderTestQuestions(data.questions);
            break;
        case "login":
            handleLogin(data);
            break;
        case "register":
            handleRegister(data);
            break;
        case "submit_test_answer":
            handleSubmitTest(data);
            break;
        case "create_test":
            handleCreateTest(data);
            break;
        case "create_question":
            handleCreateQuestion(data);
            break;
        case "create_law_branch":
            handleCreateLawBranch(data);
            break;
        default:
            console.error("Unknown action:", data.action);
    }
}

function login() {
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;
    socket.send(JSON.stringify({ action: "login", username, password }));
}

function register() {
    const username = document.getElementById("regUsername").value;
    const password = document.getElementById("regPassword").value;
    socket.send(JSON.stringify({ action: "register", username, password }));
}

function showLogin() {
    document.getElementById("login").style.display = "block";
    document.getElementById("register").style.display = "none";
}

function showRegister() {
    document.getElementById("login").style.display = "none";
    document.getElementById("register").style.display = "block";
}

function handleLogin(data) {
    if (data.status === "success") {
        document.getElementById("login").style.display = "none";
        document.getElementById("main").style.display = "block";
        document.getElementById("createLawBranch").style.display = "block";
        document.getElementById("createTest").style.display = "block";
        getLawBranches();
    } else {
        alert(data.message);
    }
}

function handleRegister(data) {
    if (data.status === "success") {
        showLogin();
    } else {
        alert(data.message);
    }
}

function getLawBranches() {
    socket.send(JSON.stringify({ action: "get_law_branches" }));
}

function renderLawBranches(branches) {
    const container = document.getElementById("lawBranches");
    container.innerHTML = "";
    const select = document.getElementById("lawBranchSelect");
    select.innerHTML = "";
    branches.forEach(branch => {
        const div = document.createElement("div");
        div.textContent = branch.name;
        div.onclick = () => getTestCollections(branch.id);
        container.appendChild(div);

        const option = document.createElement("option");
        option.value = branch.id;
        option.textContent = branch.name;
        select.appendChild(option);
    });
}

function getTestCollections(lawBranchId) {
    socket.send(JSON.stringify({ action: "get_test_collections", lawBranchId }));
}

function renderTestCollections(collections) {
    const container = document.getElementById("testCollections");
    container.innerHTML = "";
    collections.forEach(collection => {
        const div = document.createElement("div");
        div.textContent = collection.title;
        div.onclick = () => getTestQuestions(collection.id);
        container.appendChild(div);
    });
}

function getTestQuestions(testCollectionId) {
    socket.send(JSON.stringify({ action: "get_test_questions", testCollectionId }));
}

function renderTestQuestions(questions) {
    const container = document.getElementById("testQuestions");
    container.innerHTML = "";
    questions.forEach(question => {
        const div = document.createElement("div");
        div.textContent = question.question;
        container.appendChild(div);
    });
}

function submitTest() {
    const testId = 1; // Example test ID
    const answers = Array.from(document.getElementById("testQuestionsContainer").children).map(div => {
        const input = div.getElementsByTagName("input")[0];
        return input.value;
    });
    socket.send(JSON.stringify({ action: "submit_test_answer", testId, answers }));
}

function handleSubmitTest(data) {
    if (data.status === "success") {
        alert("Test submitted successfully!");
    } else {
        alert(data.message);
    }
}

function createLawBranch() {
    const name = document.getElementById("lawBranchName").value;
    socket.send(JSON.stringify({ action: "create_law_branch", name }));
}

function handleCreateLawBranch(data) {
    if (data.status === "success") {
        alert("Law branch created successfully!");
        getLawBranches();
    } else {
        alert(data.message);
    }
}

function addQuestion() {
    const container = document.getElementById("questionsContainer");
    const questionDiv = document.createElement("div");
    questionDiv.innerHTML = `
        <input type="text" placeholder="Question Text">
        <input type="text" placeholder="Correct Answer">
    `;
    container.appendChild(questionDiv);
}

function createTest() {
    const lawBranchId = document.getElementById("lawBranchSelect").value;
    const category = document.getElementById("testCategory").value;
    const subCategory = document.getElementById("testSubCategory").value;
    const testType = document.getElementById("testType").value;
    const questions = Array.from(document.getElementById("questionsContainer").children).map(div => {
        const inputs = div.getElementsByTagName("input");
        return {
            text: inputs[0].value,
            correctAnswer: inputs[1].value
        };
    });
    const newTest = { lawBranchId, category, subCategory, testType, questions };
    socket.send(JSON.stringify({ action: "create_test", ...newTest }));
}

function handleCreateTest(data) {
    if (data.status === "success") {
        alert("Test created successfully!");
    } else {
        alert(data.message);
    }
}