using GameServerAdmin.Domain.Posts;

namespace GameServerAdmin.Models.Posts.PublicApi;

public sealed class PublicPostResponse
{
    public long PostId { get; init; }
    public string Title { get; init; } = null!;
    public string Content { get; init; } = null!;
    public DateTime CreatedAt { get; init; }

    public static PublicPostResponse From(Post post)
    {
        return new PublicPostResponse
        {
            PostId = post.PostId,
            Title = post.Title,
            Content = post.Content,
            CreatedAt = post.CreatedAt
        };
    }
}
