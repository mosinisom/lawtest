const socket = new WebSocket("ws://localhost:5180/ws");

socket.onopen = function () {
  console.log("WebSocket соединение установлено");
  if (currentAuthToken) {
    showMainPage();
  }
};

socket.onmessage = function (event) {
  const data = JSON.parse(event.data);
  handleResponse(data);
};

let currentAuthToken = localStorage.getItem('authToken');

function handleResponse(data) {
  switch (data.action) {
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
    currentAuthToken = data.token;
    localStorage.setItem('authToken', currentAuthToken);
    showMainPage();
  } else {
    alert(data.message);
  }
}

function handleRegister(data) {
  if (data.status === "success") {
    currentAuthToken = data.token;
    localStorage.setItem('authToken', currentAuthToken);
    showLogin();
  } else {
    alert(data.message);
  }
}

function showMainPage() {
  document.getElementById("login").style.display = "none";
  document.getElementById("register").style.display = "none";
  document.getElementById("main").style.display = "block";
  document.getElementById("createLawBranch").style.display = "none";
  document.getElementById("createTest").style.display = "none";
  getLawBranches();
}

function getLawBranches() {
  socket.send(JSON.stringify({ action: "get_law_branches" }));
}

function renderLawBranches(branches) {
  console.log("Rendering branches:", branches);
  const container = document.getElementById("lawBranches");
  const select = document.getElementById("lawBranchSelect");

  container.innerHTML = "";
  select.innerHTML = "";

  if (!branches || branches.length === 0) {
    console.log("No branches to render");
    return;
  }

  branches.forEach(branch => {
    console.log("Processing branch:", branch);

    const div = document.createElement("div");
    div.textContent = branch.Name;
    div.onclick = () => getTestCollections(branch.Id);
    container.appendChild(div);

    const option = document.createElement("option");
    option.value = branch.Id;
    option.textContent = branch.Name;
    select.appendChild(option);
  });
}
function getTestCollections(lawBranchId) {
    socket.send(JSON.stringify({ 
        action: "get_test_collections", 
        lawBranchId: lawBranchId.toString() 
    }));
}

function renderTestCollections(collections) {
    const container = document.getElementById("lawBranches");
    const currentLawBranchId = document.getElementById("lawBranchSelect").value;
    
    console.log("Rendering collections for law branch:", currentLawBranchId);
    
    let lawBranchContainer = document.querySelector(`[data-law-branch-id="${currentLawBranchId}"]`);
    
    if (!lawBranchContainer) {
        lawBranchContainer = document.createElement('div');
        lawBranchContainer.dataset.lawBranchId = currentLawBranchId;
        lawBranchContainer.className = 'law-branch-container';
        container.appendChild(lawBranchContainer);
    }
    
    let testsContainer = lawBranchContainer.querySelector('.tests');
    if (!testsContainer) {
        testsContainer = document.createElement('div');
        testsContainer.className = 'tests';
        lawBranchContainer.appendChild(testsContainer);
    }
    
    testsContainer.innerHTML = '';
    
    if (!collections || collections.length === 0) {
        testsContainer.innerHTML = '<p>Нет доступных тестов</p>';
        return;
    }

    collections.forEach(test => {
        const testElement = document.createElement('div');
        testElement.className = 'test-item';
        testElement.innerHTML = `
            <h3>${test.Name}</h3>
            <p>Тип теста: ${test.TestType}</p>
            <p>Количество вопросов: ${test.Questions ? test.Questions.length : 0}</p>
            <button onclick="getTestQuestions(${test.Id})" class="test-button">Пройти тест</button>
        `;
        testsContainer.appendChild(testElement);
    });

    lawBranchContainer.style.display = 'block';
}

function getTestQuestions(testId) {
    if (!testId) {
        console.error("Invalid test ID:", testId);
        return;
    }
    
    console.log("Getting questions for test:", testId);
    socket.send(JSON.stringify({ 
        action: "get_test_questions", 
        testCollectionId: testId 
    }));
}

function renderTestQuestions(data) {
    console.log("Received test questions data:", data);
    
    const mainContainer = document.getElementById("main");
    const testForm = document.getElementById("testForm");
    const container = document.getElementById("testContent");
    
    mainContainer.style.display = "none";
    testForm.style.display = "block";
    container.style.display = "block";
    
    container.innerHTML = "";
    
    const questions = Array.isArray(data) ? data : [data];
    
    const form = document.createElement("form");
    form.id = "testQuestionsForm";
    
    questions.forEach((question, index) => {
        const questionDiv = document.createElement("div");
        questionDiv.className = "test-item";
        
        let optionsHtml = '';
        if (Array.isArray(question.Options)) {
            optionsHtml = question.Options.map((option, optIndex) => `
                <div class="option">
                    <input type="radio" 
                           id="q${question.Id}_opt${optIndex}"
                           name="question${question.Id}" 
                           value="${option}">
                    <label for="q${question.Id}_opt${optIndex}">${option}</label>
                </div>
            `).join('');
        }

        questionDiv.innerHTML = `
            <div class="question">
                <p class="question-text">
                    <strong>Вопрос ${index + 1}:</strong> ${question.Text}
                </p>
                <div class="options-container">
                    ${optionsHtml}
                </div>
            </div>
        `;
        
        form.appendChild(questionDiv);
    });

    const buttonsDiv = document.createElement("div");
    buttonsDiv.className = "test-buttons";
    buttonsDiv.innerHTML = `
        <button type="button" onclick="submitTest()">Отправить ответы</button>
        <button type="button" onclick="closeTestForm()">Закрыть</button>
    `;
    form.appendChild(buttonsDiv);

    container.appendChild(form);
}

function closeTestForm() {
    document.getElementById("main").style.display = "block";
    document.getElementById("testForm").style.display = "none";
    document.getElementById("testContent").style.display = "none";
}

function submitTest() {
    const questionsContainer = document.getElementById("testQuestionsContainer");
    if (!questionsContainer) {
        alert("Ошибка: контейнер с вопросами не найден");
        return;
    }

    const answers = Array.from(questionsContainer.querySelectorAll('.question')).map(questionDiv => {
        const selectedOption = questionDiv.querySelector('input[type="radio"]:checked');
        return selectedOption ? selectedOption.value : '';
    });

    if (answers.some(answer => answer === '')) {
        alert("Пожалуйста, ответьте на все вопросы");
        return;
    }

    socket.send(JSON.stringify({ 
        action: "submit_test_answer", 
        answers: answers 
    }));
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
    closeCreateLawBranch();
    getLawBranches();
  } else {
    alert(data.message);
  }
}

function addQuestion() {
    const container = document.getElementById("questionsContainer");
    const questionDiv = document.createElement("div");
    questionDiv.classList.add("question-container");
    
    const testType = document.getElementById("testType").value;
    let optionsHtml = '';
    
    switch(testType) {
        case "SingleChoice":
        case "MultipleChoice":
            optionsHtml = `
                <div class="options-container">
                    <input type="text" class="question-text" placeholder="Question Text">
                    <div class="options">
                        <div class="option">
                            <input type="text" placeholder="Option 1">
                            <input type="radio" name="correct-${container.children.length}" value="0">
                        </div>
                        <div class="option">
                            <input type="text" placeholder="Option 2">
                            <input type="radio" name="correct-${container.children.length}" value="1">
                        </div>
                        <div class="option">
                            <input type="text" placeholder="Option 3">
                            <input type="radio" name="correct-${container.children.length}" value="2">
                        </div>
                        <div class="option">
                            <input type="text" placeholder="Option 4">
                            <input type="radio" name="correct-${container.children.length}" value="3">
                        </div>
                    </div>
                </div>
                <button onclick="addOption(this)">Add Option</button>`;
            break;
            
        case "TrueFalse":
            optionsHtml = `
                <input type="text" class="question-text" placeholder="Question Text">
                <div class="options">
                    <label><input type="radio" name="correct-${container.children.length}" value="true"> True</label>
                    <label><input type="radio" name="correct-${container.children.length}" value="false"> False</label>
                </div>`;
            break;
            
        case "Matching":
            optionsHtml = `
                <input type="text" class="question-text" placeholder="Question Text">
                <div class="matching-pairs">
                    <div class="pair">
                        <input type="text" placeholder="Item">
                        <input type="text" placeholder="Match">
                    </div>
                </div>
                <button onclick="addMatchingPair(this)">Add Pair</button>`;
            break;
    }
    
    questionDiv.innerHTML = optionsHtml + '<button onclick="removeQuestion(this)">Remove Question</button>';
    container.appendChild(questionDiv);
}

function addOption(button) {
    const optionsContainer = button.previousElementSibling.querySelector('.options');
    const optionIndex = optionsContainer.children.length;
    const optionDiv = document.createElement('div');
    optionDiv.className = 'option';
    optionDiv.innerHTML = `
        <input type="text" placeholder="Option ${optionIndex + 1}">
        <input type="radio" name="correct-${button.closest('.question-container').dataset.questionIndex}" value="${optionIndex}">
    `;
    optionsContainer.appendChild(optionDiv);
}

function removeQuestion(button) {
    button.parentElement.remove();
}

function addMatchingPair(button) {
    const pairsContainer = button.previousElementSibling;
    const pairDiv = document.createElement('div');
    pairDiv.className = 'pair';
    pairDiv.innerHTML = `
        <input type="text" placeholder="Item">
        <input type="text" placeholder="Match">
    `;
    pairsContainer.appendChild(pairDiv);
}

function createTest() {
    const name = document.getElementById("testName").value;
    const testType = document.getElementById("testType").value;
    const lawBranchId = parseInt(document.getElementById("lawBranchSelect").value);
    
    console.log("Creating test with:", {
        name,
        testType,
        lawBranchId
    });

    if (!name || !testType || !lawBranchId) {
        alert("Пожалуйста, заполните все поля теста");
        return;
    }

    const questionContainers = document.querySelectorAll('.question-container');
    if (questionContainers.length === 0) {
        alert("Добавьте хотя бы один вопрос");
        return;
    }

    const testData = {
        name: name, // Убедимся что имя передается корректно
        testType: testType,
        lawBranchId: lawBranchId,
        questions: []
    };

    questionContainers.forEach(container => {
        const questionText = container.querySelector('.question-text').value;
        const options = [];
        let correctAnswer = '';

        if (!questionText) {
            alert("Заполните текст вопроса");
            return;
        }

        switch(testType) {
            case "SingleChoice":
            case "MultipleChoice":
                container.querySelectorAll('.option input[type="text"]').forEach(input => {
                    options.push(input.value);
                });
                const selectedRadio = container.querySelector('input[type="radio"]:checked');
                correctAnswer = selectedRadio ? options[parseInt(selectedRadio.value)] : '';
                break;
            case "TrueFalse":
                options.push("True", "False");
                correctAnswer = container.querySelector('input[type="radio"]:checked').value;
                break;
            case "Matching":
                const pairs = container.querySelectorAll('.pair');
                pairs.forEach(pair => {
                    const [item, match] = pair.querySelectorAll('input');
                    if (item.value && match.value) {
                        options.push(item.value);
                        correctAnswer += `${item.value}:${match.value};`;
                    }
                });
                break;
        }

        testData.questions.push({
            text: questionText,
            options: options,
            correctAnswer: correctAnswer
        });
    });

    console.log("Sending test data:", testData);

    socket.send(JSON.stringify({
        action: "create_test",
        test: testData 
    }));
}

function handleCreateTest(data) {
  if (data.status === "success") {
    alert("Test created successfully!");
    closeCreateTest();
    getTestCollections(document.getElementById("lawBranchSelect").value);
  } else {
    alert(data.message);
  }
}

function showCreateLawBranchForm() {
  document.getElementById("createLawBranch").style.display = "block";
}

function closeCreateLawBranch() {
  document.getElementById("createLawBranch").style.display = "none";
}

function showCreateTestForm() {
  document.getElementById("createTest").style.display = "block";
}

function closeCreateTest() {
  document.getElementById("createTest").style.display = "none";
}

function showTestForm(testData) {
  document.getElementById("main").style.display = "none";
  document.getElementById("testForm").style.display = "block";
  renderTestContent(testData);
}

function closeTestForm() {
  document.getElementById("testForm").style.display = "none";
  document.getElementById("main").style.display = "block";
}

function renderTestContent(questions) {
    const container = document.getElementById("testContent");
    container.innerHTML = '';
    
    const questionsContainer = document.createElement("div");
    questionsContainer.id = "testQuestionsContainer";
    
    questions.forEach((question, index) => {
        const questionDiv = document.createElement("div");
        questionDiv.className = "question";
        
        let optionsHtml = '';
        if (Array.isArray(question.Options)) {
            optionsHtml = question.Options.map((option, optIndex) => `
                <div class="option">
                    <input type="radio" name="question${index}" value="${option}">
                    <label>${option}</label>
                </div>
            `).join('');
        }
        
        questionDiv.innerHTML = `
            <p class="question-text"><strong>Вопрос ${index + 1}:</strong> ${question.Text}</p>
            <div class="options-container">
                ${optionsHtml}
            </div>
        `;
        
        questionsContainer.appendChild(questionDiv);
    });
    
    container.appendChild(questionsContainer);
}