using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;

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
      "create_law_branch" => await CreateLawBranch(json),
      "login" => await LoginUser(json),
      "register" => await RegisterUser(json),
      _ => JsonSerializer.Serialize(new { status = "error", message = "Unknown action" })
    };
  }

  private string GenerateAuthToken()
  {
    return Guid.NewGuid().ToString();
  }

  private async Task<string> GetLawBranches()
  {
    try
    {
      var lawBranches = await _context.LawBranches.ToListAsync();
      var response = new
      {
        action = "get_law_branches",
        status = "success",
        branches = lawBranches.Select(lb => new
        {
          Id = lb.Id,
          Name = lb.Name,
          Description = lb.Description,
          TestCollections = lb.TestCollections
        })
      };
      var json = JsonSerializer.Serialize(response);
      Console.WriteLine($"Sending law branches: {json}");
      return json;
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new
      {
        action = "get_law_branches",
        status = "error",
        message = ex.Message
      });
    }
  }

  private async Task<string> GetTestCollections(JsonElement json)
  {
    try
    {
        var lawBranchIdStr = json.GetProperty("lawBranchId").GetString();
        var lawBranchId = int.Parse(lawBranchIdStr);

        Console.WriteLine($"Getting tests for law branch ID: {lawBranchId}");

        var testCollections = await _context.Tests
            .Include(t => t.Questions)
            .Where(t => t.LawBranchId == lawBranchId)
            .ToListAsync();

        Console.WriteLine($"Found {testCollections.Count} tests");

        var response = new
        {
            action = "get_test_collections",
            status = "success",
            collections = testCollections.Select(tc => new
            {
                tc.Id,
                tc.Name,
                tc.TestType,
                Questions = tc.Questions.Select(q => new
                {
                    q.Id,
                    q.Text,
                    q.Options,
                    q.CorrectAnswer
                }).ToList()
            }).ToList()
        };

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var serialized = JsonSerializer.Serialize(response, options);
        Console.WriteLine($"Sending response: {serialized}");
        return serialized;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error getting test collections: {ex}");
        return JsonSerializer.Serialize(new
        {
            action = "get_test_collections",
            status = "error",
            message = ex.Message
        });
    }
  }

  private async Task<string> GetTestQuestions(JsonElement json)
  {
    try
    {
      var testCollectionId = json.GetProperty("testCollectionId").GetInt32();
      var testQuestions = await _context.TestQuestions
          .Where(tq => tq.TestCollectionId == testCollectionId)
          .ToListAsync();
      return JsonSerializer.Serialize(new
      {
        action = "get_test_questions",
        status = "success",
        questions = testQuestions
      });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new
      {
        action = "get_test_questions",
        status = "error",
        message = ex.Message
      });
    }
  }

  private async Task<string> ProcessTestAnswer(JsonElement json)
  {
    try
    {
      var testId = json.GetProperty("testId").GetInt32();
      var userAnswers = json.GetProperty("answers").EnumerateArray()
          .Select(a => a.GetString()).ToList();
      var result = await _testService.CheckAnswersAsync(testId, userAnswers);
      return JsonSerializer.Serialize(new
      {
        action = "submit_test_answer",
        status = "success",
        result
      });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new
      {
        action = "submit_test_answer",
        status = "error",
        message = ex.Message
      });
    }
  }

  private async Task<string> CreateTest(JsonElement json)
  {
    try
    {
        Console.WriteLine($"Received test data: {json}");
        var testData = json.GetProperty("test");
        var test = new Test
        {
            Name = testData.GetProperty("name").GetString(),
            TestType = testData.GetProperty("testType").GetString(),
            LawBranchId = testData.GetProperty("lawBranchId").GetInt32(),
            Questions = new List<Question>()
        };

        Console.WriteLine($"Creating test: Name={test.Name}, Type={test.TestType}, LawBranchId={test.LawBranchId}");

        if (string.IsNullOrEmpty(test.Name))
        {
            throw new ArgumentException("Test name cannot be empty");
        }

        var questions = testData.GetProperty("questions").EnumerateArray();
        foreach (var q in questions)
        {
            var question = new Question
            {
                Text = q.GetProperty("text").GetString(),
                Options = q.GetProperty("options").EnumerateArray()
                    .Select(o => o.GetString())
                    .ToList(),
                CorrectAnswer = q.GetProperty("correctAnswer").GetString()
            };
            test.Questions.Add(question);
        }

        var createdTest = await _testService.CreateTestAsync(test);

        var response = new
        {
            action = "create_test",
            status = "success",
            test = new
            {
                createdTest.Id,
                createdTest.Name,
                createdTest.TestType,
                createdTest.LawBranchId,
                Questions = createdTest.Questions.Select(q => new
                {
                    q.Id,
                    q.Text,
                    q.Options,
                    q.CorrectAnswer
                })
            }
        };

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        return JsonSerializer.Serialize(response, options);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating test: {ex}");
        return JsonSerializer.Serialize(new
        {
            action = "create_test",
            status = "error",
            message = ex.Message
        });
    }
  }

  private async Task<string> CreateQuestion(JsonElement json)
  {
    try
    {
      var newQuestion = JsonSerializer.Deserialize<Question>(json.GetRawText());
      var createdQuestion = await _testService.CreateQuestionAsync(newQuestion);
      return JsonSerializer.Serialize(new
      {
        action = "create_question",
        status = "success",
        question = createdQuestion
      });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new
      {
        action = "create_question",
        status = "error",
        message = ex.Message
      });
    }
  }

  private async Task<string> RegisterUser(JsonElement json)
  {
    try
    {
      var username = json.GetProperty("username").GetString();
      var password = json.GetProperty("password").GetString();

      var user = await _userService.RegisterAsync(username, password);
      user.AuthToken = GenerateAuthToken();
      await _context.SaveChangesAsync();

      return JsonSerializer.Serialize(new
      {
        action = "register",
        status = "success",
        user = user,
        token = user.AuthToken
      });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new
      {
        action = "register",
        status = "error",
        message = ex.Message
      });
    }
  }

  private async Task<string> LoginUser(JsonElement json)
  {
    var username = json.GetProperty("username").GetString();
    var password = json.GetProperty("password").GetString();

    try
    {
      var user = await _userService.AuthenticateAsync(username, password);
      user.AuthToken = GenerateAuthToken(); 
      await _context.SaveChangesAsync(); 

      return JsonSerializer.Serialize(new
      {
        action = "login",
        status = "success",
        user = user,
        token = user.AuthToken
      });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new
      {
        action = "login",
        status = "error",
        message = ex.Message
      });
    }
  }

  private async Task<string> CreateLawBranch(JsonElement json)
  {
    try
    {
      var name = json.GetProperty("name").GetString();
      var lawBranch = new LawBranch
      {
        Name = name,
        Description = ""
      };

      _context.LawBranches.Add(lawBranch);
      await _context.SaveChangesAsync();

      return JsonSerializer.Serialize(new
      {
        action = "create_law_branch",
        status = "success",
        lawBranch = lawBranch
      });
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new
      {
        action = "create_law_branch",
        status = "error",
        message = ex.Message
      });
    }
  }
}