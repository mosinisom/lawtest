public class TestQuestion
{
    public int Id { get; set; }
    public string Question { get; set; }
    public string Answer { get; set; }
    public int TestCollectionId { get; set; }
    public TestCollection TestCollection { get; set; }
}