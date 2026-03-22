#include "pch.h"
#include "ObjectUtils.h"
#include "Player.h"
#include "Minion.h"
#include "Turret.h"
#include "Nexus.h"
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

MinionRef ObjectUtils::CreateMinion()
{
    const int64 newId = s_idGenerator.fetch_add(1);

    MinionRef minion = MakeShared<Minion>();
    minion->SetMinionId(newId);

    return minion;
}

TurretRef ObjectUtils::CreateTurret()
{
    const int64 newId = s_idGenerator.fetch_add(1);

    TurretRef turret = MakeShared<Turret>();
    turret->SetTurretId(newId);

    return turret;
}

NexusRef ObjectUtils::CreateNexus()
{
    const int64 newId = s_idGenerator.fetch_add(1);
    NexusRef nexus = MakeShared<Nexus>();
    nexus->SetNexusId(newId);
    return nexus;
}
