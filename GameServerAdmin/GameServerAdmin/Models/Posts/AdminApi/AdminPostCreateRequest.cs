namespace GameServerAdmin.Models.Posts.AdminApi;

public sealed class AdminPostCreateRequest
{
    public string PostType   { get; set; } = null!;
    public string Title      { get; set; } = null!;
    public string Content    { get; set; } = null!;
    public string AuthorType { get; set; } = "ADMIN";
    public long   AuthorId   { get; set; }
    public string AuthorName { get; set; } = null!;
}
