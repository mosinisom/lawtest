public class TestCollection 
{
    public int Id { get; set; }
    public string Title { get; set; }
    public TestType Type { get; set; }
    public int LawBranchId { get; set; }
    public List<TestQuestion> Questions { get; set; }
}