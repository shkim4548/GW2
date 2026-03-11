using GameServerAdmin.Application.Posts;
using GameServerAdmin.Common.Exceptions.Comment;
using GameServerAdmin.Common.Exceptions.Post;
using GameServerAdmin.Common.Models;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Domain.Comments;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Comments.AdminApi;
using GameServerAdmin.Models.Comments.PublicApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Comments.Public
{
    public interface IPublicCommentService
    {
        /*---------------------------
            Public Comment Service
        -----------------------------*/
        Task<CommentResponse> CreateCommentAsync(long postId, long authorId, CreateCommentRequest request);
        Task<ReplyResponse> CreateReplyAsync(long postId, long parentCommentId, long authorId, CreateReplyRequest request);
        Task<CommentResponse> UpdateCommentAsync(long postId, long commentId, long authorId, UpdateCommentRequest request);
        Task DeleteCommentAsync(long postId, long commentId, long authorId, ActorType actorType);
        Task<CommentListResponse> GetCommentsByPostAsync(int postId);
        Task<PagedResponse<AdminCommentListItemDto>> GetAllCommentsAsync(AdminCommentListQuery query);
        CommentResponse MapToCommentResponse(Domain.Comments.Comment comment, List<Domain.Comments.Comment> replies, Dictionary<long, string> authorNames);
        ReplyResponse MapToReplyResponse(Domain.Comments.Comment reply, Dictionary<long, string> authorName);
    }

    public class PublicCommentService : IPublicCommentService
    {
        private readonly AppDbContext _db;

        public PublicCommentService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CommentResponse> CreateCommentAsync(long postId, long authorId, CreateCommentRequest request)
        {
            var post = await _db.Posts.FindAsync(postId);
            if(post == null)
            {
                throw new CommentNotFoundException(postId);
            }

            if(post.IsDeleted)
            {
                throw new InvalidPostStateException("삭제된 게시글에는 댓글을 작성할 수 없습니다");
            }

            var comment = new Domain.Comments.Comment(postId, authorId, request.Comment);
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            return MapToCommentResponse(comment);
        }

        public async Task<ReplyResponse> CreateReplyAsync(long postId, long parentCommentId, long authorId, CreateReplyRequest request)
        {
            var post = await _db.Posts.FindAsync(postId);
            if (post == null)
            {
                throw new CommentNotFoundException(postId);
            }

            if(post.IsDeleted)
            {
                throw new InvalidPostStateException("삭제된 게시글에는 답글을 작성할 수 없습니다");
            }

            var parentComment = await _db.Comments.FindAsync(parentCommentId);
            if (parentComment == null)
            {
                throw new ParentCommentNotFoundException(parentCommentId);
            }

            if(parentComment.PostId != postId)
            {
                throw InvalidParentCommentException.NestedReplyNotAllowed(parentCommentId);
            }

            if(parentComment.Status == Domain.Comments.CommentStatus.Deleted)
            {
                throw InvalidParentCommentException.ParentDeleted(parentCommentId);
            }

            var reply = new Domain.Comments.Comment(postId, parentCommentId, authorId, request.Content);
            _db.Comments.Add(reply);

            await _db.SaveChangesAsync();
            return MapToReplyResponse(reply);
        }

        public async Task DeleteCommentAsync(long postId, long commentId, long authorId, ActorType actorType)
        {
            var comment = await _db.Comments.FindAsync(commentId);
            if (comment == null)
            {
                throw new CommentNotFoundException(commentId);
            }

            if (comment.PostId != postId)
            {
                throw new CommentNotFoundException(commentId);
            }

            if (!comment.IsAuthor(authorId))
            {
                throw new Common.Exceptions.ForbiddenException("본인이 작성한 댓글만 삭제할 수 있습니다.");
            }

            comment.SoftDelete();
            await _db.SaveChangesAsync();
        }

        public Task<PagedResponse<AdminCommentListItemDto>> GetAllCommentsAsync(AdminCommentListQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<CommentListResponse> GetCommentsByPostAsync(int postId)
        {
            // FIX: Posts 테이블에서 검증
            var post = await _db.Posts.FindAsync((long)postId);
            if (post == null)
                throw new Common.Exceptions.Comment.PostNotFoundException(postId);

            var allComments = await _db.Comments
                .Where(c => c.PostId == postId && c.Status == CommentStatus.Active)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            // 작성자 ID 목록으로 닉네임 한 번에 조회
            var authorIds = allComments.Select(c => c.AuthorId).Distinct().ToList();
            var authorNames = await _db.Users
                .Where(u => authorIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Nickname);

            var rootComments = allComments.Where(c => !c.ParentCommentId.HasValue).ToList();

            var commentResponses = rootComments.Select(root =>
            {
                var replies = allComments
                    .Where(c => c.ParentCommentId == root.Id)
                    .ToList();
                return MapToCommentResponse(root, replies, authorNames);
            }).ToList();

            return new CommentListResponse
            {
                PostId = postId,
                TotalCount = allComments.Count,
                CommentCount = rootComments.Count,
                ReplyCount = allComments.Count - rootComments.Count,
                Comments = commentResponses
            };
        }

        public CommentResponse MapToCommentResponse(Comment comment, List<Comment> replies, Dictionary<long, string> authorNames)
        {
            return new CommentResponse
            {
                Id = comment.Id,
                PostId = comment.PostId,
                AuthorId = comment.AuthorId,
                AuthorName = authorNames.GetValueOrDefault(comment.AuthorId, "(알 수 없음)"),
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                Replies = replies.Select(r => MapToReplyResponse(r, authorNames)).ToList()
            };
        }

        public ReplyResponse MapToReplyResponse(Comment reply, Dictionary<long, string> authorName)
        {
            return new ReplyResponse
            {
                Id = reply.Id,
                ParentCommentId = reply.ParentCommentId!.Value,
                AuthorId = reply.AuthorId,
                AuthorName = authorName.GetValueOrDefault(reply.AuthorId, "(알 수 없음)"),
                Content = reply.Content,
                CreatedAt = reply.CreatedAt,
                UpdatedAt = reply.UpdatedAt
            };
        }

        public async Task<CommentResponse> UpdateCommentAsync(long postId, long commentId, long authorId, UpdateCommentRequest request)
        {
            var comment = await _db.Comments.FindAsync(commentId);
            if (comment == null)
            {
                throw new CommentNotFoundException(commentId);
            }

            if (comment.PostId != postId)
            {
                throw new CommentNotFoundException(commentId);
            }

            if (!comment.IsAuthor(authorId))
            {
                throw new Common.Exceptions.ForbiddenException("본인이 작성한 댓글만 수정할 수 있습니다.");
            }

            comment.UpdateContent(request.Content);
            await _db.SaveChangesAsync();

            if (comment.IsReply())
            {
                return MapToCommentResponse(comment);
            }
            else
            {
                var replies = await _db.Comments
                    .Where(c => c.ParentCommentId == commentId && c.Status == CommentStatus.Active)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                return MapToCommentResponse(comment, replies);
            }
        }

        // Helper Methods
        private CommentResponse MapToCommentResponse(Domain.Comments.Comment comment, List<Domain.Comments.Comment> replies = null)
        {
            return new CommentResponse
            {
                Id = comment.Id,
                PostId = comment.PostId,
                AuthorId = comment.AuthorId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                Replies = replies?.Select(MapToReplyResponse).ToList() ?? new List<ReplyResponse>()
            };
        }

        private ReplyResponse MapToReplyResponse(Domain.Comments.Comment reply)
        {
            return new ReplyResponse
            {
                Id = reply.Id,
                ParentCommentId = reply.ParentCommentId!.Value,
                AuthorId = reply.AuthorId,
                Content = reply.Content,
                CreatedAt = reply.CreatedAt,
                UpdatedAt = reply.UpdatedAt
            };
        }


    }
}
