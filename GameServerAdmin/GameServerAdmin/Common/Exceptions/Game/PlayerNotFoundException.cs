using GameServerAdmin.Common;

namespace GameServerAdmin.Common.Exceptions
{
    /// <summary>
    /// 게임 플레이어(유저)를 찾을 수 없을 때 사용하는 예외.
    /// ErrorCode.GAME_PLAYER_NOT_FOUND 사용.
    /// </summary>
    public sealed class PlayerNotFoundException : NotFoundException
    {
        public PlayerNotFoundException(long userId)
            : base(
                ErrorCode.GAME_PLAYER_NOT_FOUND,
                $"플레이어를 찾을 수 없습니다. userId={userId}"
            )
        {
        }
    }
}