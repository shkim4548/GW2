using GameServerAdmin.Models.Comments.PublicApi;

namespace GameServerAdmin.Models.Posts.PublicUi
{
    public sealed class PublicBoardDetailViewModel
    {
        public PublicPostDetailViewModel Post { get; init; } = null!;
        public CommentListResponse Comments { get; init; } = new();
        public long? CurrentUserId { get; init; }   // 본인 댓글 삭제 버튼 표시용
        public bool IsAuthenticated { get; init; }
    }
}