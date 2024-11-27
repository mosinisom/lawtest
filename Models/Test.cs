// Test.cs
public class Test
{
  public int Id { get; set; }
  public string Name { get; set; }
  public string TestType { get; set; }
  public List<Question> Questions { get; set; }
  public int LawBranchId { get; set; }
  public LawBranch LawBranch { get; set; }
}