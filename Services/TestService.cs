using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TestService : ITestService
{
    private readonly ApplicationDbContext _context;

    public TestService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Test>> GetAllTestsAsync()
    {
        return await _context.Tests.Include(t => t.Questions).ToListAsync();
    }

    public async Task<Test> GetTestByIdAsync(int id)
    {
        return await _context.Tests.Include(t => t.Questions)
                    .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Test>> GetTestsByLawBranchIdAsync(int lawBranchId)
    {
        return await _context.Tests.Include(t => t.Questions)
                                   .Where(t => t.LawBranchId == lawBranchId)
                                   .ToListAsync();
    }

    public async Task<TestResult> CheckAnswersAsync(int testId, List<string> userAnswers)
    {
        var test = await _context.Tests.Include(t => t.Questions)
                                       .FirstOrDefaultAsync(t => t.Id == testId);

        if (test == null)
        {
            return null;
        }

        int correctAnswers = 0;
        for (int i = 0; i < test.Questions.Count; i++)
        {
            if (test.Questions[i].CorrectAnswer == userAnswers[i])
            {
                correctAnswers++;
            }
        }

        return new TestResult
        {
            TestId = testId,
            CorrectAnswers = correctAnswers,
            TotalQuestions = test.Questions.Count
        };
    }

    public async Task<Test> CreateTestAsync(Test newTest)
    {
        _context.Tests.Add(newTest);
        await _context.SaveChangesAsync();
        return newTest;
    }

    public async Task<Question> CreateQuestionAsync(Question newQuestion)
    {
        _context.Questions.Add(newQuestion);
        await _context.SaveChangesAsync();
        return newQuestion;
    }
}