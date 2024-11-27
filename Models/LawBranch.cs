public class LawBranch
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<TestCollection> TestCollections { get; set; }
}