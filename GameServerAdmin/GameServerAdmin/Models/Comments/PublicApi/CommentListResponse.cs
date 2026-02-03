namespace GameServerAdmin.Models.Comments.PublicApi
{
    public class CommentListResponse
    {
        public long PostId { get; set; }
        public int TotalCount { get; set; }
        public int CommentCount { get; set; }  // 원댓글 수
        public int ReplyCount { get; set; }    // 대댓글 수
        public List<CommentResponse> Comments { get; set; } = new();
    }
}