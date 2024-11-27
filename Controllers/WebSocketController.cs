using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;

[ApiController]
[Route("ws")]
public class WebSocketController : ControllerBase
{
    private readonly ITestService _testService;
    private readonly IUserService _userService;
    private readonly ApplicationDbContext _context;

    public WebSocketController(ITestService testService, IUserService userService, ApplicationDbContext context)
    {
        _testService = testService;
        _userService = userService;
        _context = context;
    }

    [HttpGet]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await HandleWebSocket(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private async Task HandleWebSocket(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var response = await ProcessMessage(message);
            var responseBytes = Encoding.UTF8.GetBytes(response);

            await webSocket.SendAsync(
                new ArraySegment<byte>(responseBytes),
                result.MessageType,
                result.EndOfMessage,
                CancellationToken.None);

            result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);
        }
    }

    private async Task<string> ProcessMessage(string message)
    {
        var json = JsonDocument.Parse(message).RootElement;
        var action = json.GetProperty("action").GetString();

        return action switch
        {
            "get_law_branches" => await GetLawBranches(),
            "get_test_collections" => await GetTestCollections(json),
            "get_test_questions" => await GetTestQuestions(json),
            "submit_test_answer" => await ProcessTestAnswer(json),
            "create_test" => await CreateTest(json),
            "create_question" => await CreateQuestion(json),
            "login" => await LoginUser(json),
            "register" => await RegisterUser(json),
            _ => JsonSerializer.Serialize(new { status = "error", message = "Unknown action" })
        };
    }

    private async Task<string> GetLawBranches()
    {
        var lawBranches = await _context.LawBranches.ToListAsync();
        return JsonSerializer.Serialize(new { status = "success", branches = lawBranches });
    }

    private async Task<string> GetTestCollections(JsonElement json)
    {
        var lawBranchId = json.GetProperty("lawBranchId").GetInt32();
        var testCollections = await _context.TestCollections
            .Where(tc => tc.LawBranchId == lawBranchId)
            .ToListAsync();
        return JsonSerializer.Serialize(new { status = "success", collections = testCollections });
    }

    private async Task<string> GetTestQuestions(JsonElement json)
    {
        var testCollectionId = json.GetProperty("testCollectionId").GetInt32();
        var testQuestions = await _context.TestQuestions
            .Where(tq => tq.TestCollectionId == testCollectionId)
            .ToListAsync();
        return JsonSerializer.Serialize(new { status = "success", questions = testQuestions });
    }

    private async Task<string> ProcessTestAnswer(JsonElement json)
    {
        var testId = json.GetProperty("testId").GetInt32();
        var userAnswers = json.GetProperty("answers").EnumerateArray().Select(a => a.GetString()).ToList();
        var result = await _testService.CheckAnswersAsync(testId, userAnswers);
        return JsonSerializer.Serialize(new { status = "success", result });
    }

    private async Task<string> CreateTest(JsonElement json)
    {
        var newTest = JsonSerializer.Deserialize<Test>(json.GetRawText());
        var createdTest = await _testService.CreateTestAsync(newTest);
        return JsonSerializer.Serialize(new { status = "success", test = createdTest });
    }

    private async Task<string> CreateQuestion(JsonElement json)
    {
        var newQuestion = JsonSerializer.Deserialize<Question>(json.GetRawText());
        var createdQuestion = await _testService.CreateQuestionAsync(newQuestion);
        return JsonSerializer.Serialize(new { status = "success", question = createdQuestion });
    }

    private async Task<string> RegisterUser(JsonElement json)
    {
        var username = json.GetProperty("username").GetString();
        var password = json.GetProperty("password").GetString();

        try
        {
            var user = await _userService.RegisterAsync(username, password);
            return JsonSerializer.Serialize(new { status = "success", user });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { status = "error", message = ex.Message });
        }
    }

    private async Task<string> LoginUser(JsonElement json)
    {
        var username = json.GetProperty("username").GetString();
        var password = json.GetProperty("password").GetString();

        try
        {
            var user = await _userService.AuthenticateAsync(username, password);
            return JsonSerializer.Serialize(new { status = "success", user });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { status = "error", message = ex.Message });
        }
    }
}