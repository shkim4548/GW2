#include "pch.h"
#include "ObjectUtils.h"
#include "Player.h"
#include "GameSession.h"

atomic<int32> ObjectUtils::s_idGenerator = 1;

PlayerRef ObjectUtils::CreatePlayer(GameSessionRef session)
{
    const int64 newId = s_idGenerator.fetch_add(1);

    PlayerRef player = MakeShared<Player>();
    player->SetPlayerId(newId);
    player->SetSession(session);

    session->_currentPlayer.store(player);

    return player;
}
