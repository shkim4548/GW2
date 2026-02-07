using GameServerAdmin.Common.Models;
using GameServerAdmin.Domain.Comments;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Comments.AdminApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Comments.Admin
{
    public interface IAdminCommentService
    {
        /*--------------------------
            Admin Comment Service
        ----------------------------*/

        Task<PagedResponse<AdminCommentListItemDto>> GetAllCommentsAsync(AdminCommentListQuery query);
        Task<AdminCommentResponse> GetCommentDetailAsync(int commentId);
        Task DeleteCommentAsync(int commentId);
        Task RestoreCommentAsync(int commentId);
        Task HardDeleteCommentAsync(int commentId);
    }

    public class AdminCommentService : IAdminCommentService
    {
        private readonly AppDbContext _db;

        public AdminCommentService(AppDbContext db)
        {
            _db = db;
        }

        public Task DeleteCommentAsync(int commentId)
        {
            throw new NotImplementedException();
        }

        public async Task<PagedResponse<AdminCommentListItemDto>> GetAllCommentsAsync(AdminCommentListQuery query)
        {
            var queryable = _db.Comments.AsQueryable();

            if(query.PostId.HasValue)
            {
                queryable = queryable.Where(c => c.PostId == query.PostId);
            }

            if(query.AuthorId.HasValue)
            {
                queryable = queryable.Where(c => c.AuthorId == query.AuthorId.Value);
            }

            if(query.Status.HasValue)
            {
                queryable = queryable.Where(c => c.Status == query.Status.Value);
            }

            if(!query.IncludeDeleted)
            {
                queryable = queryable.Where(c => c.Status == CommentStatus.Active);
            }
            // 모든 행의 수
            
            var totalCount = await queryable.CountAsync();
            // 정렬
            queryable = query.SortBy.ToLower() switch
            {
                "updatedat" => query.SortOrder.ToLower() == "asc"
                    ? queryable.OrderBy(c => c.UpdatedAt)
                    : queryable.OrderByDescending(c => c.UpdatedAt),
                "deletedat" => query.SortOrder.ToLower() == "asc"
                    ? queryable.OrderBy(c => c.DeletedAt)
                    : queryable.OrderByDescending(c => c.DeletedAt),
                _ => query.SortOrder.ToLower() == "asc"
                    ? queryable.OrderBy(c => c.CreatedAt)
                    : queryable.OrderByDescending(c => c.CreatedAt)
            };

            // 페이징
            var items = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            // 각 댓글의 대댓글 수 계산
            var commentIds = items.Select(c => c.Id).ToList();
            var replyCounts = await _db.Comments
                .Where(c => c.ParentCommentId.HasValue && commentIds.Contains(c.ParentCommentId.Value))
                .GroupBy(c => c.ParentCommentId!.Value)
                .Select(g => new { CommentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CommentId, x => x.Count);

            var itemDtos = items.Select(c => new AdminCommentListItemDto
            {
                Id = c.Id,
                PostId = c.PostId,
                ParentCommentId = c.ParentCommentId,
                AuthorId = c.AuthorId,
                Content = c.Content,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                DeletedAt = c.DeletedAt,
                ReplyCount = replyCounts.GetValueOrDefault(c.Id, 0)
            }).ToList();

            return new PagedResponse<AdminCommentListItemDto>
            {
                Items = itemDtos,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<AdminCommentResponse> GetCommentDetailAsync(int commentId)
        {
            var comment = await _db.Comments.FindAsync(commentId);
            if(comment == null)
            {
                throw new CommentNotFoundException(commentId);
            }

            if(comment.IsReply())
            {
                return MapToAdminCommentResponse(comment);
            }

            var replies = await _db.Comments
                .Where(c => c.ParentCommentId == commentId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return MapToAdminCommentResponse(comment, replies);
        }

        public async Task HardDeleteCommentAsync(int commentId)
        {
            var comment = await _db.Comments.FindAsync(commentId);
            if(comment == null)
            {
                throw new CommentNotFoundException(commentId);
            }

            if (comment.IsReply() == false)
            {
                var replies = await _db.Comments
                    .Where(c => c.ParentCommentId == commentId)
                    .ToListAsync();

                _db.Comments.RemoveRange(comment);
            }

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
        }

        public async Task RestoreCommentAsync(int commentId)
        {
            var comment = await _db.Comments.FindAsync(commentId);
            if (comment == null)
            {
                throw new CommentNotFoundException(commentId);
            }

            comment.Restore();
            await _db.SaveChangesAsync();
        }

        // Helper Method
        private AdminCommentResponse MapToAdminCommentResponse(Domain.Comments.Comment comment, List<Domain.Comments.Comment>? replies = null)
        {
            return new AdminCommentResponse
            {
                Id = comment.Id,
                PostId = comment.PostId,
                AuthorId = comment.AuthorId,
                Content = comment.Content,
                Status = comment.Status,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                DeletedAt = comment.DeletedAt,
                Replies = replies?.Select(MapToAdminReplyResponse).ToList() ?? new List<AdminReplyResponse>()
            };
        }

        private AdminReplyResponse MapToAdminReplyResponse(Domain.Comments.Comment reply)
        {
            return new AdminReplyResponse
            {
                Id = reply.Id,
                ParentCommentId = reply.ParentCommentId!.Value,
                AuthorId = reply.AuthorId,
                Content = reply.Content,
                Status = reply.Status,
                CreatedAt = reply.CreatedAt,
                UpdatedAt = reply.UpdatedAt,
                DeletedAt = reply.DeletedAt
            };
        }
    }
}
