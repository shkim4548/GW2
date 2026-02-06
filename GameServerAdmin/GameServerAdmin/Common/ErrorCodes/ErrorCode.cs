namespace GameServerAdmin.Common
{
    public enum ErrorCode
    {
        // ===== Common =====
        VALIDATION_FAILED,
        NOT_FOUND,
        FORBIDDEN,
        UNAUTHORIZED,
        INVALID_STATE,
        DOMAIN_RULE_VIOLATION,
        DUPLICATED_RESOURCE,

        // ===== System =====
        INTERNAL_ERROR,
        SERVICE_UNAVAILABLE,

        // ===== Board =====
        BOARD_POST_NOT_FOUND,
        BOARD_POST_FORBIDDEN,
        BOARD_POST_ALREADY_DELETED,
        BOARD_COMMENT_LIMIT_EXCEEDED,

        // ===== Game =====
        GAME_PLAYER_NOT_FOUND,
        GAME_INSUFFICIENT_RESOURCE,
        GAME_INVALID_ACTION,
        GAME_COOLDOWN_NOT_EXPIRED,

        // ===== Admin =====
        ADMIN_PERMISSION_REQUIRED,

        // ===== Comment =====
        COMMENT_NOT_FOUND,
        PARENT_COMMENT_NOT_FOUND,
        INVALID_PARENT_COMMENT,
        COMMENT_ALREADY_DELETED,
        COMMENT_NOT_DELETED,
        COMMENT_UPDATE_FORBIDDEN,
        COMMENT_CONTENT_EMPTY,
        COMMENT_CONTENT_TOO_LONG,

        // ===== Notice =====
        NOTICE_NOT_FOUND,
        NOTICE_ALREADY_PUBLISHED,
        NOTICE_ALREADY_DELETED,
        NOTICE_NOT_PUBLISHED,
        NOTICE_NOT_DELETED,
    }
}