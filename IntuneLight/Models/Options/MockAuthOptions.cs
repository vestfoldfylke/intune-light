namespace IntuneLight.Models.Options;

public class MockAuthOptions
{
    public string Username { get; set; } = "dev.user@fylke.no";
    public string Name { get; set; } = "Dev User";
    public string ObjectId { get; set; } = "dev-oid-1234";
    public List<string> Roles { get; set; } = ["User"];
}