public class Question 
{
    public int Id { get; set; }
    public string Text { get; set; }
    public List<string> Options { get; set; } 
    public string CorrectAnswer { get; set; }
    public int TestId { get; set; }
    public Test Test { get; set; }
}