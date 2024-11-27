public interface ITestService
{
    Task<List<Test>> GetAllTestsAsync();
    Task<Test> GetTestByIdAsync(int id);
    Task<List<Test>> GetTestsByCategoryAsync(string category);
    Task<TestResult> CheckAnswersAsync(int testId, List<string> userAnswers);
    Task<Test> CreateTestAsync(Test newTest);
    Task<Question> CreateQuestionAsync(Question newQuestion);
}