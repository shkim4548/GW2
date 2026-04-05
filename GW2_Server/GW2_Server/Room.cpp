#include "pch.h"
#include "Lobby.h"
#include "Room.h"
#include "Player.h"
#include "GameSession.h"
#include "NavigationSystem.h"
#include "NavmeshLoader.h"
#include "ClientPacketHandler.h"
#include "Minion.h"
#include "Turret.h"
#include "Nexus.h"
#include "Baron.h"
#include "ObjectUtils.h"
#include "LaneRouteLoader.h"

// 공용으로 사용할 전역 룸
//shared_ptr<Room> GRoom = make_shared<Room>();	//모든 클라를 여기에 접속시켜서 확인한다.

Room::Room()
{
	_navigationSystem = GLobby->GetNavigationSystem();
	_roomWalkableGrid = GLobby->GetWalkableGrid();
}

Room::~Room()
{
	_players.clear();

}

bool Room::Enter(PlayerRef gameObject)
{
	if (gameObject == nullptr) 
		return false;

	int32 objectId = gameObject->GetObjectId();

	// 1. 팀 자동 배정 (입장 순서 기준)
	Protocol::CampType assignedTeam;
	switch (gameObject->GetPlayerType())
	{
	case Protocol::PLAYER_TYPE_POLICE:
	case Protocol::PLAYER_TYPE_FIREFIGHTER:
		assignedTeam = Protocol::CAMP_HUMAN;
		break;
	case Protocol::PLAYER_TYPE_MONK:
	case Protocol::PLAYER_TYPE_LIGHTSABRE:
		assignedTeam = Protocol::CAMP_CYBORG;
		break;
	default:
		assignedTeam = Protocol::CAMP_HUMAN;
		break;
	}
	// 2. 스폰 위치 팀별 설정
	GameMath::Vector3 spawnPos =
		(assignedTeam == Protocol::CAMP_HUMAN)
		? GameMath::Vector3(-60.0f, 0.0f, 0.0f)
		: GameMath::Vector3(60.0f, 0.0f, 0.0f);
	gameObject->SetPosVector(spawnPos);
	gameObject->SetCampType(assignedTeam);

	// 3. Room 등록 및 초기화
	_objects.emplace(objectId, gameObject);
	_players.emplace(objectId, gameObject);
	shared_ptr<Room> roomSelf = static_pointer_cast<Room>(shared_from_this());
	gameObject->InitPlayer(roomSelf);
	_isRunning = true;

	Protocol::S_HAND_SYNC handPkt;
	handPkt.set_player_id(objectId);
	for (int32 cardId : gameObject->_hand)
	{
		handPkt.add_card_ids(cardId);
	}

	auto session = gameObject->GetSession().lock();
	if (session)
		session->Send(ClientPacketHandler::MakeSendBuffer(handPkt));

	// 4. 신규 플레이어에게 기존 오브젝트 동기화
	SyncObjectsToPlayer(gameObject);

	// 5. 신규 플레이어 정보를 기존 플레이어들에게 브로드캐스트
	Protocol::S_ENTER_GAME enterPkt;
	Protocol::ObjectInfo* objectInfo = new Protocol::ObjectInfo();
	Protocol::PosInfo* posInfo = new Protocol::PosInfo();
	objectInfo->set_object_type(Protocol::OBJECT_TYPE_PLAYER);
	objectInfo->set_object_id(objectId);
	objectInfo->set_team_flag(assignedTeam);
	posInfo->set_x(spawnPos._x);
	posInfo->set_y(spawnPos._y);
	posInfo->set_z(spawnPos._z);

	switch (gameObject->GetPlayerType())
	{
	case Protocol::PLAYER_TYPE_POLICE:       
		objectInfo->set_name("Police");
		break;
	case Protocol::PLAYER_TYPE_FIREFIGHTER:
		objectInfo->set_name("FireFighter"); 
		break;
	case Protocol::PLAYER_TYPE_MONK:         
		objectInfo->set_name("Monk");        
		break;
	case Protocol::PLAYER_TYPE_LIGHTSABRE:   
		objectInfo->set_name("LightSabre");
		break;
	default:
		objectInfo->set_name("Police");
		break;
	}
	
	objectInfo->set_allocated_pos_info(posInfo);
	enterPkt.set_allocated_player(objectInfo);
	//objectInfo->set_allocated_pos_info(posInfo);
	//enterPkt.set_allocated_player(objectInfo);
	Broadcast(ClientPacketHandler::MakeSendBuffer(enterPkt));

	// 6. 최초 입장 시 터렛/넥서스 스폰
	if (!_gameStarted)
	{
		_gameStarted = true;

		// HUMAN 터렛
		SpawnTurret(GameMath::Vector3(-15, 0.5, -25.5), Protocol::CAMP_HUMAN);
		SpawnTurret(GameMath::Vector3(-52.81, 0.5, -22.93), Protocol::CAMP_HUMAN);
		SpawnTurret(GameMath::Vector3(-65.26, 2, -4.28), Protocol::CAMP_HUMAN);
		SpawnTurret(GameMath::Vector3(-65.26, 2, 4.73), Protocol::CAMP_HUMAN);
		SpawnTurret(GameMath::Vector3(-51.2, 0.5, 22.31), Protocol::CAMP_HUMAN);
		SpawnTurret(GameMath::Vector3(-10.93, 0.5, 25.1), Protocol::CAMP_HUMAN);

		// CYBORG 터렛
		SpawnTurret(GameMath::Vector3(11.69, 0.5, -25.51), Protocol::CAMP_CYBORG);
		SpawnTurret(GameMath::Vector3(47.1, 0.5, -22.6), Protocol::CAMP_CYBORG);
		SpawnTurret(GameMath::Vector3(64.2, 2, -4.5), Protocol::CAMP_CYBORG);
		SpawnTurret(GameMath::Vector3(64.2, 2, 4.5), Protocol::CAMP_CYBORG);
		SpawnTurret(GameMath::Vector3(50.9, 0.5, 22.1), Protocol::CAMP_CYBORG);
		SpawnTurret(GameMath::Vector3(13, 0.5, 25.3), Protocol::CAMP_CYBORG);

		// 넥서스
		SpawnNexus(GameMath::Vector3(-70.5f, 2.3f, 0.0f), Protocol::CAMP_HUMAN);
		SpawnNexus(GameMath::Vector3(72.5f, 2.3f, 0.0f), Protocol::CAMP_CYBORG);

		// 바론 구현
		SpawnBaron();
	}

	return true;
}


void Room::Leave(int32 playerId)
{

}

void Room::Broadcast(SendBufferRef sendBuffer, int32 exceptId)
{
	for (auto& p : _players)
	{
		PlayerRef player = dynamic_pointer_cast<Player>(p.second);
		if (player == nullptr)
		{
			GConsoleLogger->WriteStdErr(Color::RED, L"[Room::Broadcast] player is nullptr\n");
			continue;
		}

		if (player->GetPlayerId() == exceptId)
		{
			continue;
		}

		if (GameSessionRef session = player->GetSession().lock())
		{
			//cout << "Broadcast" << '\n';
			session->Send(sendBuffer);
		}
	}
}

void Room::RoomInit(unordered_map<int32, shared_ptr<Navigation::LaneRoute>> route)
{
	_laneRoute = route;
	for (const auto& [laneId, route] : _laneRoute)
	{
		if (route == nullptr)
			continue;
		GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Room::RoomInit] laneId=%d, waypoints=%d\n",
			laneId, static_cast<int32>(route->waypoints.size()));
	}
}

bool Room::HandleEnterPlayer(PlayerRef player)
{
	return false;
}

bool Room::HandleSkill(ObjectRef attacker, Protocol::C_SKILL skillPkt)
{
	if (attacker == nullptr) 
		return false;

	int32 skillId = static_cast<int32>(skillPkt.command_id());

	// 논타겟 카드 (target_id=0): object lookup 전에 처리
	if (skillPkt.target_id() == 0)
	{
		if (skillId == 1)
			return false;

		auto player = dynamic_pointer_cast<Player>(attacker);
		if (player == nullptr) 
			return false;

		if (!player->_cardManager.HasCard(*player, skillId)) 
			return false;

		player->_cardManager.UseCard(*player, skillId);
		if (!player->_hand.empty())
		{
			Protocol::S_DRAW_CARD drawPkt;
			drawPkt.set_player_id(player->GetObjectId());
			drawPkt.set_card_id(player->_hand.back());
			auto session = player->GetSession().lock();
			if (session)
				session->Send(ClientPacketHandler::MakeSendBuffer(drawPkt));
		}

		// Heal 카드 처리
		CardStat cardStat = GLobby->GetCardStat(skillId);
		if (cardStat.heal > 0)
		{
			uint64 healed = attacker->Heal(cardStat.heal);

			Protocol::S_HP_CHANGE hpPkt;
			hpPkt.set_target_id(attacker->GetObjectId());
			hpPkt.set_current_hp(attacker->GetHp());
			hpPkt.set_max_hp(attacker->GetMaxHp());
			Broadcast(ClientPacketHandler::MakeSendBuffer(hpPkt));
		}
		// 버프 처리
		if (cardStat.buffType != 0 && cardStat.buffValue > 0.0f)
		{
			auto player = dynamic_pointer_cast<Player>(attacker);
			if (player)
			{
				switch (cardStat.buffType)
				{
				case 1: // BUFF_ATTACK
					player->_attackMult = cardStat.buffValue;
					player->_attackBuffTimer = cardStat.duration;
					break;
				case 2: // BUFF_DEFENSE
					player->_defenseReduct = cardStat.buffValue;
					player->_defenseBuffTimer = cardStat.duration;
					break;
				case 3: // BUFF_SPEED
					player->_speedMult = cardStat.buffValue;
					player->_speedBuffTimer = cardStat.duration;
					break;
				case 4: // BUFF_ATTACK_SPEED
					player->_attackSpeedMult = cardStat.buffValue;
					player->_attackSpeedBuffTimer = cardStat.duration;
					break;
				}

				Protocol::S_BUFF_APPLIED buffPkt;
				buffPkt.set_target_id(player->GetObjectId());
				buffPkt.set_buff_type(static_cast<Protocol::BuffType>(cardStat.buffType));
				buffPkt.set_value(cardStat.buffValue);
				buffPkt.set_duration(cardStat.duration);
				auto session = player->GetSession().lock();
				if (session)
					session->Send(ClientPacketHandler::MakeSendBuffer(buffPkt));
			}
		}

		// if (cardStat.aoeRadius > 0.0f && cardStat.damage > 0) 바로 위에 추가
		GConsoleLogger->WriteStdOut(Color::YELLOW,
			L"[HandleSkill] non-target skillId=%d radius=%.2f dmg=%d heal=%d buffType=%d\n",
			skillId, cardStat.aoeRadius, cardStat.damage, cardStat.heal, cardStat.buffType);


		// AOE 데미지 처리
		if (cardStat.aoeRadius > 0.0f && cardStat.damage > 0)
		{
			vector<shared_ptr<Object>> hits;
			GameMath::Vector3 attackerPos = attacker->GetPosVector();

			if (cardStat.aoeType == 1)  // 원형
			{
				GameMath::Vector3 center(skillPkt.pos_x(), 0.f, skillPkt.pos_z());
				for (auto& [id, obj] : _objects)
				{
					if (obj == nullptr || obj->IsDead()) 
						continue;
					if (obj->GetTeamFlag() == attacker->GetTeamFlag()) 
						continue;
					GameMath::Vector3 nowVector = obj->GetPosVector();
					float dist = GameMath::Vector3::GetDistTanceXZ(center, nowVector);
					if (dist <= cardStat.aoeRadius)
						hits.push_back(obj);
				}
			}
			else if (cardStat.aoeType == 2)  // 원뿔형
			{
				GameMath::Vector3 dir(skillPkt.dir_x(), 0.f, skillPkt.dir_z());
				if (dir.Length() < 0.001f)
					return false;
				dir = dir.Normalized();
				float halfAngleRad = (cardStat.angle * 0.5f) * (3.14159265f / 180.f);
				float cosHalf = cosf(halfAngleRad);

				for (auto& [id, obj] : _objects)
				{
					if (obj == nullptr || obj->IsDead()) continue;
					if (obj->GetTeamFlag() == attacker->GetTeamFlag()) continue;
					GameMath::Vector3 toTarget = obj->GetPosVector() - attackerPos;
					float dist = toTarget.Length();
					if (dist > cardStat.aoeRadius || dist < 0.001f) continue;
					float dot = dir.Dot(toTarget.Normalized());
					if (dot >= cosHalf)
						hits.push_back(obj);
				}
			}

			for (auto& target : hits)
			{
				target->ApplyDamage(cardStat.damage);

				Protocol::S_HP_CHANGE hpPkt;
				hpPkt.set_target_id(target->GetObjectId());
				hpPkt.set_current_hp(target->GetHp());
				hpPkt.set_max_hp(target->GetMaxHp());
				Broadcast(ClientPacketHandler::MakeSendBuffer(hpPkt));
			}
		}

		// S_SKILL 브로드캐스트 (이펙트용) — Mobility 분기 전에 추가
		{
			Protocol::S_SKILL skillResPkt;
			skillResPkt.set_skill_id(skillPkt.command_id());
			skillResPkt.set_attacker_id(attacker->GetObjectId());
			skillResPkt.set_target_id(0);
			skillResPkt.set_pos_x(skillPkt.pos_x());
			skillResPkt.set_pos_z(skillPkt.pos_z());
			skillResPkt.set_dir_x(skillPkt.dir_x());
			skillResPkt.set_dir_z(skillPkt.dir_z());
			Broadcast(ClientPacketHandler::MakeSendBuffer(skillResPkt));
		}

		// Mobility 카드: 목적지로 즉시 이동
		if (cardStat.buffType == 0 && cardStat.damage == 0 && cardStat.heal == 0)
		{
			GameMath::Vector3 dest;
			dest._x = skillPkt.pos_x();
			dest._z = skillPkt.pos_z();
			dest._y = 0.0f;

			player->_path.clear();
			player->_pathIndex = 0;
			player->SetPosVector(dest);
			player->SetMoveState(Protocol::MoveState::MOVE_STATE_IDLE);

			// 위치 브로드캐스트
			Protocol::S_MOVE_END movePkt;
			movePkt.set_object_id(player->GetObjectId());
			Protocol::PosInfo* pos = movePkt.mutable_server_pos_info();
			pos->set_x(dest._x);
			pos->set_y(dest._y);
			pos->set_z(dest._z);
			Broadcast(ClientPacketHandler::MakeSendBuffer(movePkt));
			return true;
		}
		return true;
	}

	// 기본 공격(CommandId=1) 쿨다운 검증
	if (skillId == 1)
	{
		auto player = dynamic_pointer_cast<Player>(attacker);
		if (player)
		{
			if (player->_attackCooldown > 0.0f)
				return false;
			float interval = player->_attackInterval / player->_attackSpeedMult;
			player->_attackCooldown = interval;
		}
	}

	auto targetIter = _objects.find(skillPkt.target_id());
	if (targetIter == _objects.end()) return false;

	shared_ptr<Object> target = targetIter->second;

	// 1. 팀 체크
	if (attacker->GetTeamFlag() == target->GetTeamFlag())
		return false;

	// 2. 이미 죽은 대상 체크
	if (target->IsDead())
		return false;

	// 3. 거리 체크
	CardStat cardStat = (skillId == 1) ? CardStat{} : GLobby->GetCardStat(skillId);
	const float attackRange = (skillId == 1)
		? attacker->GetStatInfo().attack_range()   // 평타: 캐릭터 사거리
		: cardStat.range;                          // 카드: CardStat.range
	GameMath::Vector3 attackerPos = attacker->GetPosVector();
	GameMath::Vector3 targetPos = target->GetPosVector();
	const float effectiveRange = (skillId == 1)
		? attackRange + 2.0f    // ← 위치 동기화 오차 보정 (이동속도 12 × 50ms RTT ≈ 0.6 + 안전마진)
		: attackRange;

	float dist = GameMath::Vector3::GetDistTanceXZ(attackerPos, targetPos);
	if (dist > effectiveRange)
	{
		GConsoleLogger->WriteStdErr(Color::YELLOW,
			L"[Room::HandleSkill] out of range dist=%.2f range=%.2f\n", dist, effectiveRange);
		return false;
	}

	//if (dist > attackRange)
	//{
	//	GConsoleLogger->WriteStdErr(Color::YELLOW,
	//		L"[Room::HandleSkill] out of range dist=%.2f\n", dist);
	//	return false;
	//}

	uint64_t damage = 0;
	//int32 skillId = static_cast<int32>(skillPkt.skill_id());

	// 논타겟 임시 처리
	if (skillPkt.target_id() == 0)
	{
		return true;
	}

	switch (skillPkt.command_id())
	{
	case 1:
	{
		// 평타: StatInfo.attack() 사용
		damage = attacker->GetStatInfo().attack();
		if (damage == 0) damage = 10; // 폴백
		break;
	}

	default:
	{
		auto player = dynamic_pointer_cast<Player>(attacker);
		if (player == nullptr) return false;

		if (!player->_cardManager.HasCard(*player, skillId))
		{
			GConsoleLogger->WriteStdErr(Color::YELLOW,
				L"[Room::HandleSkill] card not in hand playerId=%d cardId=%d\n",
				player->GetObjectId(), skillId);
			return false;
		}

		// 카드 소모 + 드로우
		player->_cardManager.UseCard(*player, skillId);

		if (!player->_hand.empty())
		{
			Protocol::S_DRAW_CARD drawPkt;
			drawPkt.set_player_id(player->GetObjectId());
			drawPkt.set_card_id(player->_hand.back());
			auto session = player->GetSession().lock();
			if (session)
				session->Send(ClientPacketHandler::MakeSendBuffer(drawPkt));
		}

		// 논타겟 카드 (힐, 버프 등): 소모/드로우만 하고 데미지 없이 종료
		if (skillPkt.target_id() == 0)
			return true;

		CardStat cardStat = GLobby->GetCardStat(skillId);
		uint64 baseAtk = attacker->GetStatInfo().attack();
		damage = cardStat.damage + static_cast<uint64>(cardStat.damageCoeff * baseAtk);
		break;
	}

	}

	// 공격 배율 적용
	auto attackerPlayer = dynamic_pointer_cast<Player>(attacker);
	if (attackerPlayer && attackerPlayer->_attackMult > 1.0f)
		damage = static_cast<uint64>(damage * attackerPlayer->_attackMult);

	// 방어 버프 적용
	auto targetPlayer = dynamic_pointer_cast<Player>(target);
	if (targetPlayer && targetPlayer->_defenseReduct > 0.0f)
		damage = static_cast<uint64>(damage * (1.0f - targetPlayer->_defenseReduct));

	// 4. 데미지 적용
	bool died = target->ApplyDamage(damage);
	// 바론 데미지 적용
	if (_baron != nullptr && target->GetObjectId() == _baron->GetObjectId())
	{
		_baron->OnHit(attacker->GetObjectId());
	}
	// Stun 적용 (포탑/넥서스 제외)
	if (skillId != 1)
	{
		CardStat cardStat = GLobby->GetCardStat(skillId);
		if (cardStat.duration > 0.0f && !target->IsTurret() && !target->IsNexus())
		{
			target->_isStunned = true;
			target->_stunTimer = cardStat.duration;

			Protocol::S_STUN stunPkt;
			stunPkt.set_target_id(target->GetObjectId());
			stunPkt.set_duration(cardStat.duration);
			Broadcast(ClientPacketHandler::MakeSendBuffer(stunPkt));
		}
	}

	GConsoleLogger->WriteStdOut(Color::GREEN,
		L"[Room::HandleSkill] attacker=%lld target=%d skillId=%d dmg=%llu hp=%llu died=%d\n",
		attacker->GetObjectId(), target->GetObjectId(),
		skillId, damage, target->GetHp(), died ? 1 : 0);

	// 5. S_SKILL 브로드캐스트
	Protocol::S_SKILL resPkt;
	resPkt.set_skill_id(skillPkt.command_id());
	resPkt.set_attacker_id(attacker->GetObjectId());
	resPkt.set_target_id(skillPkt.target_id());
	Broadcast(ClientPacketHandler::MakeSendBuffer(resPkt));

	// 6. S_HP_CHANGE 브로드캐스트
	Protocol::S_HP_CHANGE hpPkt;
	hpPkt.set_target_id(skillPkt.target_id());
	hpPkt.set_current_hp(target->GetHp());
	hpPkt.set_max_hp(target->GetMaxHp());
	Broadcast(ClientPacketHandler::MakeSendBuffer(hpPkt));

	// 7. 사망 처리
	if (died)
	{
		// 킬 골드: 공격자가 플레이어인 경우
		auto attackerPlayer = dynamic_pointer_cast<Player>(attacker);
		if (attackerPlayer)
			GiveGold(attackerPlayer, 300);

		HandleRemoveObject(target->GetObjectId(), attacker->GetObjectId());
	}

	return true;
}


void Room::HandleMovePlayer(Protocol::C_MOVE movePkt)
{
	Protocol::PosInfo startPos = movePkt.start_pos();
	Protocol::PosInfo endPos = movePkt.target_pos();

	GameMath::Vector3 startWorld(startPos.x(), startPos.y(), startPos.z());
	GameMath::Vector3 endWorld(endPos.x(), endPos.y(), endPos.z());

	//cout << "[Room::HandleMovePlayer] Before FindPath" << endl;
	// World -> Grid
	int32 sx, sz, tx, tz;
	shared_ptr<Navigation::NavigationSystem> navSystem = _navigationSystem.lock();
	if (navSystem == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMovePlayer] _navigationSystem is nullptr\n");
		return;
	}

	Navigation::WalkableGrid& grid = _navigationSystem.lock()->GetGridCells();
	if (!_navigationSystem.lock()->WorldToGrid(grid, startWorld, sx, sz))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMovePlayer] WorldToGrid Fail\n");
		return;
	}

	if (!_navigationSystem.lock()->WorldToGrid(grid, endWorld, tx, tz))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMovePlayer] WorldToGrid Fail\n");
		return;
	}

	// PathFinding
	vector<Navigation::GridCell*> gridPath;
	bool ok = _navigationSystem.lock()->FindPath(grid, sx, sz, tx, tz, gridPath, 0);

	if (!ok || gridPath.empty())
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMovePlayer] GridCell is nullptr\n");
		// TODO : 이동 실패 구현
		return;
	}

	int32 playerId = movePkt.object_id();
	weak_ptr<Player> player = _players[playerId];
	if (player.lock() == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMovePlayer] player is nullptr\n");
		// TODO : 이동 실패 구현
		return;
	}
	//cout << "End of HandleMovePlayer" << endl;
	HandleMovePlayerInternal(player.lock(), gridPath, startWorld, endWorld);
}

bool Room::HandleSpawnMinion(MinionRef minion)
{
	if (minion == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleSpawnMinion] minion is nullptr\n");
		return false;
	}

	Protocol::ObjectInfo* objectInfo = new Protocol::ObjectInfo();
	Protocol::PosInfo* posInfo = new Protocol::PosInfo();
	Protocol::S_ENTER_GAME enterPkt;

	//minion = ObjectUtils::CreateMinion();
	int32 objectId = minion->GetMinionId();

	GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::Enter] enterMinion\n");
	objectInfo->set_object_type(Protocol::OBJECT_TYPE_MINION);
	objectInfo->set_object_id(objectId);
	objectInfo->set_team_flag(minion->GetTeamFlag());
	posInfo->set_x(minion->GetPosInfo().x());
	posInfo->set_y(minion->GetPosInfo().y());
	posInfo->set_z(minion->GetPosInfo().z());
	posInfo->set_yaw(0);
	objectInfo->set_allocated_pos_info(posInfo);
	enterPkt.set_allocated_player(objectInfo);
	_objects.emplace(objectId, minion);
	_minionSpawnOrder.push_back(objectId);

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(enterPkt);
	Broadcast(sendBuffer);
	return true;
}

// Path를 player에 할당한다.
void Room::HandleMovePlayerInternal(PlayerRef player, std::vector<Navigation::GridCell*>& gridPath, const GameMath::Vector3& startWorld, const GameMath::Vector3& endWorld)
{
	vector<GameMath::Vector3> worldPath;
	worldPath.reserve(gridPath.size() + 2);
	worldPath.push_back(startWorld);

	for (Navigation::GridCell* cell : gridPath)
	{
		GameMath::Vector3 worldPos;
		if (!_navigationSystem.lock()->GridToWorld(_navigationSystem.lock()->GetGridCells(), cell->x, cell->z, worldPos))
		{
			continue;
		}

		if ((worldPos - startWorld).Length() < 0.01f)
		{
			continue;
		}

		worldPath.push_back(worldPos);
	}

	if (worldPath.empty() || (worldPath.back() - endWorld).Length() >= 0.01f)
	{
		worldPath.push_back(endWorld);
	}

	if (player == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[HandleMovePlayerInternal] player is nullptr");
		return;
	}
	// 이 부분이 빠져있었음
	//player->GetMoveState();
	player->SetMoveState(Protocol::MOVE_STATE_RUN);
	player->_path = move(worldPath);
	player->_pathIndex = 0;
	player->SetIsMoving(true);
}

void Room::UpdateRoom(float deltaTime)
{
	// 0. 미니언 스폰
	_minionSpawnAccumulate += deltaTime;
	if (_isRunning)
	{
		if (!_isSpawningWave)
		{
			if (_minionSpawnAccumulate >= _minionSpawnCoolDown)
			{
				_minionSpawnAccumulate -= _minionSpawnCoolDown;
				_isSpawningWave = true;
				_waveSpawnCount = 0;
				_waveSpawnAccumulate = 0.0f;
			}
		}

		if (_isSpawningWave)
		{
			_waveSpawnAccumulate += deltaTime;
			if (_waveSpawnAccumulate >= WAVE_SPAWN_INTERVAL && _waveSpawnCount < WAVE_MINION_COUNT)
			{
				_waveSpawnAccumulate -= WAVE_SPAWN_INTERVAL;
				while (_minionSpawnOrder.size() + 4 > MAX_MINION_COUNT
					&& !_minionSpawnOrder.empty())
				{
					int32 oldId = _minionSpawnOrder.front();
					_minionSpawnOrder.pop_front();
					_objects.erase(oldId);

					Protocol::S_DIE removePkt;
					removePkt.set_target_id(oldId);
					Broadcast(ClientPacketHandler::MakeSendBuffer(removePkt));
				}
				SpawnMinion(MINION_LANE_TOP, Protocol::CAMP_HUMAN);
				SpawnMinion(MINION_LANE_BOT, Protocol::CAMP_HUMAN);
				SpawnMinion(MINION_LANE_TOP, Protocol::CAMP_CYBORG);
				SpawnMinion(MINION_LANE_BOT, Protocol::CAMP_CYBORG);
				_waveSpawnCount++;
			}

			if (_waveSpawnCount >= WAVE_MINION_COUNT)
				_isSpawningWave = false;
		}
	}

	// 1) Controller Phase
	for (auto& [id, obj] : _objects)
	{
		if (obj == nullptr)
			continue;

		obj->UpdateController(deltaTime);
	}

	// 2) Movement + Broadcast Phase
	for (auto& [id, obj] : _objects)
	{
		if (obj == nullptr)
			continue;

		bool wasMoving = obj->GetIsMoving();

		obj->UpdateMovement(deltaTime);

		bool isMoving = obj->GetIsMoving();

		obj->AccumulateMoveTime(deltaTime);

		if (obj->ShouldBroadcastMove())
		{
			BroadcastMoving(obj);
			obj->ResetBroadcastTimer();
		}

		if (wasMoving && !isMoving)
			BroadcastMovingEnd(obj);

		obj->PostUpdate();
	}

	// 3) 리스폰 타이머
	for (auto it = _respawnTimers.begin(); it != _respawnTimers.end(); )
	{
		it->second -= deltaTime;
		if (it->second <= 0.0f)
		{
			HandleRespawnPlayer(it->first);
			it = _respawnTimers.erase(it);
		}
		else ++it;
	}

	// 4) 자동 골드 수입
	_goldIncomeTimer += deltaTime;
	if (_goldIncomeTimer >= GOLD_INCOME_INTERVAL)
	{
		_goldIncomeTimer = 0.0f;
		for (auto& [id, obj] : _players)
		{
			auto player = dynamic_pointer_cast<Player>(obj);
			if (player && !player->IsDead())
				GiveGold(player, GOLD_INCOME_AMOUNT);
		}
	}
	// Baron 업데이트
	if (_baron != nullptr && !_baron->IsDead())
	{
		_baron->UpdateController(deltaTime);
		_baron->UpdateMovement(deltaTime);

		// 탐지 범위 내 플레이어 → OnHit(aggro 등록)
		GameMath::Vector3 baronPos = _baron->GetPosVector();
		for (auto& [id, player] : _players)
		{
			if (player->IsDead()) 
				continue;

			GameMath::Vector3 nowPlayerPos = player->GetPosVector();
			float dist = GameMath::Vector3::GetDistTanceXZ(baronPos, nowPlayerPos);
			if (dist <= _baron->GetDetectionRange())
				_baron->OnHit(id);
		}
	}
}



shared_ptr<Minion> Room::SpawnMinion(int32 laneId, Protocol::CampType team)
{
	// route 확보
	shared_ptr<Navigation::LaneRoute> baseRoute = GetLaneRoute(laneId).lock();
	if (baseRoute == nullptr || baseRoute->waypoints.empty())
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::SpawnMinion] LaneRoute missing. laneId=%d\n", laneId);
		return nullptr;
	}

	// [CHANGED] 우측 진영은 waypoints 역방향 사용
	shared_ptr<Navigation::LaneRoute> route;
	if (team == Protocol::CAMP_CYBORG)
	{
		route = make_shared<Navigation::LaneRoute>();
		route->laneId = baseRoute->laneId;
		route->waypoints = vector<GameMath::Vector3>(
			baseRoute->waypoints.rbegin(),
			baseRoute->waypoints.rend());
	}
	else
	{
		route = baseRoute;
	}

	// 스폰 위치 초기화
	const GameMath::Vector3& spawnWorldPos = route->waypoints.front();

	// 미니언 생성
	shared_ptr<Minion> minion = ObjectUtils::CreateMinion();
	if (minion == nullptr)
		return nullptr;

	GConsoleLogger->WriteStdOut(
		Color::GREEN,
		L"[Room::SpawnMinion] objId=%d minionId=%d laneId=%d pos=(%.2f,%.2f)\n",
		minion->GetObjectId(),
		minion->GetMinionId(),
		minion->GetLaneId(),
		minion->GetPosVector()._x,
		minion->GetPosVector()._z);

	// 기본 파라미터 세팅
	minion->SetLaneRoute(route);
	//minion->_laneId = static_cast<uint8>(laneId);
	minion->SetMinionLaneId(laneId);

	// 위치 초기화
	Protocol::PosInfo posInfo;
	posInfo.set_x(spawnWorldPos._x);
	posInfo.set_y(spawnWorldPos._y);
	posInfo.set_z(spawnWorldPos._z);
	minion->SetPosInfo(posInfo);
	minion->SetRoomId(this->GetRoomId());
	minion->SetMinionTeam(team);

	shared_ptr<Room> room = static_pointer_cast<Room>(shared_from_this());
	minion->InitMinion(room);

	//GConsoleLogger->WriteStdOut(Color::GREEN, L"SpawnMinion\n");
	// Room에 등록한다
	HandleSpawnMinion(minion);
	return minion;
}

shared_ptr<Turret> Room::SpawnTurret(GameMath::Vector3 pos, Protocol::CampType team)
{
	shared_ptr<Turret> turret = make_shared<Turret>();
	turret = ObjectUtils::CreateTurret();
	int32 objectId = turret->GetObjectId();

	Protocol::PosInfo* posInfo = new Protocol::PosInfo();
	posInfo->set_x(pos._x);
	posInfo->set_y(pos._y);
	posInfo->set_z(pos._z);
	posInfo->set_yaw(0);
	GameMath::Vector3 tPos = GameMath::Vector3(pos._x, pos._y, pos._z);
	turret->SetPosInfo(*posInfo);

	Protocol::ObjectInfo* objectInfo = new Protocol::ObjectInfo();
	Protocol::S_ENTER_GAME enterPkt;

	GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::Enter] enter turret\n");
	objectInfo->set_object_type(Protocol::ObjectType::OBJECT_TYPE_TURRET);
	objectInfo->set_object_id(turret->GetObjectId());
	objectInfo->set_team_flag(team);
	objectInfo->set_allocated_pos_info(posInfo);
	enterPkt.set_allocated_player(objectInfo);
	_objects.emplace(objectId, turret);
	shared_ptr<Room> roomSelf = static_pointer_cast<Room>(shared_from_this());
	turret->InitTurret(roomSelf, team);

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(enterPkt);
	Broadcast(sendBuffer);
	return turret;
}

shared_ptr<Nexus> Room::SpawnNexus(GameMath::Vector3 pos, Protocol::CampType team)
{
	NexusRef nexus = ObjectUtils::CreateNexus();
	int32 objectId = nexus->GetObjectId();
	GConsoleLogger->WriteStdOut(Color::YELLOW,
		L"[SpawnNexus] objectId=%d team=%d pos=(%.2f,%.2f,%.2f)\n",
		objectId, (int)team, pos._x, pos._y, pos._z);
	Protocol::PosInfo* posInfo = new Protocol::PosInfo();
	posInfo->set_x(pos._x);
	posInfo->set_y(pos._y);
	posInfo->set_z(pos._z);
	nexus->SetPosInfo(*posInfo);

	Protocol::ObjectInfo* objectInfo = new Protocol::ObjectInfo();
	Protocol::S_ENTER_GAME enterPkt;
	objectInfo->set_object_type(Protocol::OBJECT_TYPE_NEXUS);
	objectInfo->set_object_id(objectId);
	objectInfo->set_team_flag(team);
	objectInfo->set_allocated_pos_info(posInfo);
	enterPkt.set_allocated_player(objectInfo);

	_objects.emplace(objectId, nexus);
	nexus->InitNexus(static_pointer_cast<Room>(shared_from_this()), team);

	Broadcast(ClientPacketHandler::MakeSendBuffer(enterPkt));

	GConsoleLogger->WriteStdOut(Color::YELLOW,
		L"[SpawnNexus] Broadcast done objectId=%d\n", objectId);
	return nexus;
}

shared_ptr<Baron> Room::SpawnBaron()
{
	GameMath::Vector3 spawnPos(0.0f, 2.3f, 0.0f);

	shared_ptr<Baron> baron = ObjectUtils::CreateBaron();
	baron->SetRoomId(GetRoomId());

	Protocol::PosInfo posInfo;
	posInfo.set_x(spawnPos._x);
	posInfo.set_y(spawnPos._y);
	posInfo.set_z(spawnPos._z);
	baron->SetPosInfo(posInfo);

	shared_ptr<Room> room = static_pointer_cast<Room>(shared_from_this());
	baron->InitBaron(room, spawnPos);
	_baron = baron;

	// 클라이언트에 스폰 패킷 전송 (기존과 동일)
	Protocol::ObjectInfo* info = new Protocol::ObjectInfo();
	Protocol::PosInfo* pos = new Protocol::PosInfo();
	Protocol::S_ENTER_GAME pkt;
	info->set_object_type(Protocol::OBJECT_TYPE_BARON);
	info->set_object_id(baron->GetBaronId());
	info->set_team_flag(Protocol::CAMP_NEUTURAL);
	pos->set_x(spawnPos._x); pos->set_y(spawnPos._y); pos->set_z(spawnPos._z);
	info->set_allocated_pos_info(pos);
	pkt.set_allocated_player(info);
	_objects.emplace(baron->GetBaronId(), baron);
	Broadcast(ClientPacketHandler::MakeSendBuffer(pkt));
	return baron;
}

void Room::CollectEnemiesInRange(const shared_ptr<Object> requester, float range)
{
	vector<weak_ptr<Object>> rets;
	if (requester == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::CollectEnemiesInRange] requester is nullptr\n");
		return;
	}

	const Protocol::CampType team = requester->GetTeamFlag();
	const GameMath::Vector3 requesterPos = requester->GetPosVector();
	const float rangeSquare = range * range;
	// 선형탐색의 범위를 자신의 라인 안으로만 한정한다.
	//const shared_ptr<Minion>& asMinion = requester->IsMinion() ? static_pointer_cast<Minion>(requester) : nullptr;
	auto asMinion = dynamic_pointer_cast<Minion>(requester);
	const uint8 myLaneId = asMinion ? asMinion->_laneId : 0;
	if (asMinion == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::CollectEnemiesInRange] asMinion is nullptr\n");
		return;
	}
	// 이 반복문에 들어가지도 못하고 함수 다운
	for (auto& [id, obj] : _objects)
	{
		if (obj == nullptr)
		{
			cout << "obj is nullptr" << endl;
			continue;
		}

		if (!obj || obj == requester /*|| obj->IsDead()*/)
			continue;

		if (obj->GetTeamFlag() == team)
			continue;

		// (선택) 미니언이면 같은 laneId 대상만
		if (asMinion && obj->IsMinion())
		{
			// 타겟이 플레이어/미니언/포탑일 수 있으니, laneId를 어떻게 꺼낼지 정책 필요
			// 가장 단순: 타겟 위치로 grid에서 laneId 조회
			GameMath::Vector3 nowPos = obj->GetPosVector();
			uint8 targetLaneId = _navigationSystem.lock()->GetLaneId(_navigationSystem.lock()->GetGridCells(), nowPos);
			if (targetLaneId != myLaneId)
				continue;
		}

		const GameMath::Vector3 p = obj->GetPosVector();
		float dx = p._x - requesterPos._x;
		float dz = p._z - requesterPos._z;
		if (dx * dx + dz * dz <= rangeSquare)
			rets.push_back(obj);
	}
	asMinion->SetMinionTarget(rets);
}

void Room::HandleMinionMove(shared_ptr<Minion> minion, GameMath::Vector3 dest, float speed, float deltaTime, uint8 laneId, int32 wpIndex)
{
	// _pathPending은 이 함수가 종료될 때 반드시 해제되어야 함 (성공/실패 무관)
	struct PendingGuard {
		shared_ptr<Minion>& m;
		~PendingGuard() {
			if (m) m->ClearPathPending();
		}
	} guard{ minion };

	if (minion == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] minion is nullptr\n");
		return;
	}
	// DEBUG
	GameMath::Vector3 debugPos = minion->GetPosVector();
	//GConsoleLogger->WriteStdOut(Color::WHITE, L"[Room::HandleMinionMove] startworld = %.3f, %.3f\n", debugPos._x, debugPos._z);

	shared_ptr<Navigation::NavigationSystem> navSystem = _navigationSystem.lock();
	shared_ptr<Navigation::WalkableGrid> gridPtr = _roomWalkableGrid.lock();
	if (navSystem == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] navSystem is nullptr\n");
		return;
	}
	if (gridPtr == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] gridPtr is nullptr\n");
		return;
	}

	Navigation::WalkableGrid& grid = *gridPtr;

	uint8 minionLaneId = minion->GetLaneId();
	shared_ptr<Navigation::LaneRoute> route = minion->GetLaneRoute().lock();
	if (route == nullptr || route->waypoints.empty())
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] lane route invalid\n");
		return;
	}

	if (wpIndex < 0 || wpIndex >= static_cast<int32>(route->waypoints.size()))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] minion waypoint is invalid\n");
		return;
	}

	// start cell
	const GameMath::Vector3& startPos = minion->GetPosVector();
	GameMath::Vector3 tStartPos = startPos;
	const uint8 startLane = navSystem->GetLaneId(grid, tStartPos);

	// [CHANGED] 기존: startLane==0이면 즉시 return
	// [CHANGED] 변경: isOffLane 플래그로 처리, 가장 가까운 waypoint로 우회 경로 탐색
	bool isOffLane = (startLane == 0);
	GameMath::Vector3 resolvedTarget;

	if (isOffLane)
	{
		//GConsoleLogger->WriteStdOut(Color::YELLOW,	L"[Room::HandleMinionMove] startLane=0 (off-lane). objId=%d -> finding nearest WP\n", minion->GetObjectId());

		// [CHANGED] 가장 가까운 waypoint 탐색 (Euclidean distance)
		float minDist = FLT_MAX;
		int32 nearestIdx = 0;
		for (int32 i = 0; i < static_cast<int32>(route->waypoints.size()); ++i)
		{
			const GameMath::Vector3& wp = route->waypoints[i];
			float dx = wp._x - startPos._x;
			float dz = wp._z - startPos._z;
			float dist = dx * dx + dz * dz;
			if (dist < minDist)
			{
				minDist = dist;
				nearestIdx = i;
			}
		}
		resolvedTarget = route->waypoints[nearestIdx];
		//GConsoleLogger->WriteStdOut(Color::YELLOW,	L"[Room::HandleMinionMove] off-lane nearest WP[%d]=(%.2f,%.2f)\n", nearestIdx, resolvedTarget._x, resolvedTarget._z);
	}

	int32 sx = 0, sz = 0, tx = 0, tz = 0;
	if (!navSystem->WorldToGrid(grid, startPos._x, startPos._z, sx, sz))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] WorldToGrid(start) fail\n");
		return;
	}

	// [CHANGED] 기존: 항상 route->waypoints[wpIndex]를 target으로 사용 + targetLane 검증
	// [CHANGED] 변경: isOffLane이면 nearest WP를 target으로, targetLane 검증 skip
	if (!isOffLane)
	{
		const GameMath::Vector3& targetPos = route->waypoints[wpIndex];
		GameMath::Vector3 tTargetPos = targetPos;
		const uint8 targetLane = navSystem->GetLaneId(grid, tTargetPos);
		if (targetLane != 0 && targetLane != minionLaneId)
		{
			GConsoleLogger->WriteStdErr(Color::YELLOW,
				L"[Room::HandleMinionMove] targetLane invalid. objId=%d wp=%d allowLane=%d targetLane=%d target=(%.2f,%.2f)\n",
				minion->GetObjectId(), wpIndex, minionLaneId, targetLane, targetPos._x, targetPos._z);
			return;
		}
		resolvedTarget = targetPos;
	}

	if (!navSystem->WorldToGrid(grid, resolvedTarget._x, resolvedTarget._z, tx, tz))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] WorldToGrid(target) fail\n");
		return;
	}

	// --- A* PathFinding ---
	// [CHANGED] 기존: 항상 minionLaneId 필터 사용
	// [CHANGED] 변경: isOffLane이면 laneId=0 (필터 없음) 으로 중앙 구간 통과 허용
	uint8 pathLaneFilter = (isOffLane || grid.At(tx, tz).laneId == 0) ? 0 : minionLaneId;

	vector<Navigation::GridCell*> gridPath;
	//GConsoleLogger->WriteStdOut(Color::WHITE, L"[Room::HandleMinionMove] sx : %d, sz : %d, tx : %d, tz : %d, laneFilter : %d\n", sx, sz, tx, tz, pathLaneFilter);

	bool ok = navSystem->FindPath(grid, sx, sz, tx, tz, gridPath, pathLaneFilter); // [CHANGED]

	//GConsoleLogger->WriteStdOut(Color::WHITE, L"[Room::HandleMinionMove] FindPath ok=%d gridPathSize=%d\n", ok ? 1 : 0, static_cast<int32>(gridPath.size()));

	if (!ok || gridPath.empty())
	{
		GConsoleLogger->WriteStdErr(Color::YELLOW, L"[Room::HandleMinionMove] FindPath failed (lane filtered or no path)\n");
		return;
	}

	// --- GridPath -> NavPath (world space) ---
	if (!gridPath.empty())
	{
		Navigation::GridCell* first = gridPath.front();
		Navigation::GridCell* last = gridPath.back();
		float wx0 = grid.origin._x + (first->x + 0.5f) * grid.cellSize;
		float wz0 = grid.origin._z + (first->z + 0.5f) * grid.cellSize;
		float wx1 = grid.origin._x + (last->x + 0.5f) * grid.cellSize;
		float wz1 = grid.origin._z + (last->z + 0.5f) * grid.cellSize;
		//GConsoleLogger->WriteStdOut(Color::YELLOW,	L"[DIAG] gridPath[0]=(%d,%d) world=(%.2f,%.2f)  gridPath[last]=(%d,%d) world=(%.2f,%.2f)\n",			first->x, first->z, wx0, wz0,			last->x, last->z, wx1, wz1);
		//GConsoleLogger->WriteStdOut(Color::YELLOW,	L"[DIAG] startPos=(%.2f,%.2f) destPos=(%.2f,%.2f) resolvedTarget=(%.2f,%.2f)\n", startPos._x, startPos._z, dest._x, dest._z, resolvedTarget._x, resolvedTarget._z);
	}

	vector<GameMath::Vector3> navPath;
	navPath.reserve(gridPath.size());
	navPath.push_back(startPos);

	int successCount = 0;
	int failCount = 0;

	for (Navigation::GridCell* cell : gridPath)
	{
#if 1
		GameMath::Vector3 wp;
		wp._x = grid.origin._x + (cell->x + 0.5f) * grid.cellSize;
		wp._z = grid.origin._z + (cell->z + 0.5f) * grid.cellSize;
		wp._y = 0.0f;
		navPath.push_back(wp);
		++successCount;
#else
		GameMath::Vector3 wp;
		if (navSystem->GridToWorld(grid, cell->x, cell->z, wp))
		{
			navPath.push_back(wp);
			++successCount;
		}
		else
		{
			++failCount;
			const Navigation::GridCell& c = grid.At(cell->x, cell->z);
			GConsoleLogger->WriteStdErr(
				Color::RED,
				L"[Room::HandleMinionMove] GridToWorld failed for (%d,%d) walkable=%d laneId=%d\n",
				cell->x, cell->z,
				c.walkable ? 1 : 0,
				c.laneId);
		}
#endif
	}

	//GConsoleLogger->WriteStdOut(Color::GREEN,	L"[Room::HandleMinionMove] navPath built. success=%d fail=%d\n",		successCount,		failCount);

	if (navPath.empty())
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] navPath is empty AFTER conversion\n");
		return;
	}

	navPath.push_back(dest);

	if (navPath.empty())
		return;

	// Path Smoothing
	// [CHANGED] 기존: minionLaneId 필터로 스무딩
	// [CHANGED] 변경: pathLaneFilter 사용 (off-lane이면 0, 정상이면 minionLaneId)
	size_t beforeSize = navPath.size();
	navPath = SmoothPath(navPath, grid, navSystem, pathLaneFilter); // [CHANGED]
	//GConsoleLogger->WriteStdOut(Color::GREEN, L"[SmoothPath] %zu → %zu nodes\n", beforeSize, navPath.size());

	// --- 미니언에 이동 경로 전달 ---
	minion->RequestMove(navPath);

	// S_MINION_MOVE 브로드캐스트
	Protocol::S_MINION_MOVE minionMovePkt;
	minionMovePkt.set_object_id(minion->GetObjectId());
	minionMovePkt.set_speed(minion->GetMoveSpeed());

	Protocol::PosInfo* startPosInfo = minionMovePkt.mutable_start_pos();
	startPosInfo->set_x(minion->GetPosVector()._x);
	startPosInfo->set_y(minion->GetPosVector()._y);
	startPosInfo->set_z(minion->GetPosVector()._z);

	for (const auto& wp : navPath)
	{
		Protocol::PosInfo* pathPoint = minionMovePkt.add_nav_path();
		pathPoint->set_x(wp._x);
		pathPoint->set_y(wp._y);
		pathPoint->set_z(wp._z);
	}

	SendBufferRef minionMoveBuffer = ClientPacketHandler::MakeSendBuffer(minionMovePkt);
	Broadcast(minionMoveBuffer);
}


void Room::HandleMinionAttack(shared_ptr<Minion> attacker, shared_ptr<Object> target)
{
	if (attacker == nullptr || target == nullptr)
	{
		return;
	}

	if (target->IsDead())
		return;

	// 데미지 적용
	uint64 dmg = attacker->GetStatInfo().attack();
	bool died = target->ApplyDamage(dmg);

	GConsoleLogger->WriteStdOut(Color::GREEN,
		L"[Room::HandleMinionAttack] attacker=%d target=%d dmg=%llu died=%d\n",
		attacker->GetObjectId(), target->GetObjectId(), dmg, died);

	// S_SKILL 브로드캐스트 (클라이언트에 피격 알림)
	Protocol::S_SKILL skillPkt;
	skillPkt.set_skill_id(1);  // 0 = 기본공격
	skillPkt.set_attacker_id(attacker->GetObjectId());
	skillPkt.set_target_id(target->GetObjectId());
	Broadcast(ClientPacketHandler::MakeSendBuffer(skillPkt));

	// S_HP_CHANGE 브로드캐스트 추가
	Protocol::S_HP_CHANGE hpPkt;
	hpPkt.set_target_id(target->GetObjectId());
	hpPkt.set_current_hp(target->GetHp());
	hpPkt.set_max_hp(target->GetMaxHp());
	Broadcast(ClientPacketHandler::MakeSendBuffer(hpPkt));

	// 사망 처리
	if (died)
	{
		auto attackerPlayer = dynamic_pointer_cast<Player>(attacker);
		if (attackerPlayer)
			GiveGold(attackerPlayer, 50);

		HandleRemoveObject(target->GetObjectId(), attacker->GetObjectId());
	}
}

void Room::HandleChaseMove(shared_ptr<Minion> minion, GameMath::Vector3 dest, float speed, float deltaTime, uint8 laneId)
{
	if (minion == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleChaseMove] minion is nullptr\n");
		return;
	}

	shared_ptr<Navigation::NavigationSystem> navSystem = _navigationSystem.lock();
	shared_ptr<Navigation::WalkableGrid> gridPtr = _roomWalkableGrid.lock();
	if (navSystem == nullptr || gridPtr == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleChaseMove] nav is nullptr\n");
		return;
	}

	Navigation::WalkableGrid& grid = *gridPtr;

	// startCell
	const GameMath::Vector3& startPos = minion->GetPosVector();
	int32 sx = 0, sz = 0, tx = 0, tz = 0;
	if (!navSystem->WorldToGrid(grid, startPos._x, startPos._z, sx, sz))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleChaseMove] WorldToGrid(start) fail\n");
		return;
	}

	// dest cell: laneId 필터 없이 허용, 플레이어의 위치는 다른 laneId일 수 있음
	GameMath::Vector3 clampedDest = dest;
	if (!navSystem->WorldToGrid(grid, clampedDest._x, clampedDest._z, tx, tz))
	{
		// 그리드 범위로 클램프 후 재시도
		float minX = grid.origin._x + grid.cellSize;
		float maxX = grid.origin._x + (grid.width - 1) * grid.cellSize;
		float minZ = grid.origin._z + grid.cellSize;
		float maxZ = grid.origin._z + (grid.height - 1) * grid.cellSize;

		clampedDest._x = max(minX, min(dest._x, maxX));
		clampedDest._z = max(minZ, min(dest._z, maxZ));

		if (!navSystem->WorldToGrid(grid, clampedDest._x, clampedDest._z, tx, tz))
		{
			// 클램프 후에도 실패 → 타겟 포기, 레인으로 복귀
			minion->ClearChaseTarget();
			return;
		}
		// 클램프된 좌표로 경로 탐색 진행
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleChaseMove] WorldToGrid(dest) fail\n");
		//return;
	}

	// A* 알고리즘 -> laneId = 0으로 레인 필터를 해제한다 -> Chase는 Lane 경계를 넘을 수 있다.
	vector<Navigation::GridCell*> gridPath;
	//bool ok = navSystem->FindPath(grid, sx, sz, tx, tz, gridPath, laneId);
	bool ok = navSystem->FindPath(grid, sx, sz, tx, tz, gridPath, 0);
	if (ok == false || gridPath.empty())
	{
		GConsoleLogger->WriteStdErr(Color::YELLOW, L"[Room::HandleChaseMove] FindPath failed\n");
		minion->ClearChaseTarget();  // ← 추가
		return;
	}

	// GridsPath -> world navpath
	vector<GameMath::Vector3> navPath;
	navPath.reserve(gridPath.size() + 1);
	for (Navigation::GridCell* cell : gridPath)
	{
		GameMath::Vector3 wp;
		wp._x = grid.origin._x + (cell->x + 0.5f) * grid.cellSize;
		wp._z = grid.origin._z + (cell->z + 0.5f) * grid.cellSize;
		wp._y = 0.0f;
		navPath.push_back(wp);
	}
	navPath.push_back(dest);	// 마지막은 타겟의 실제 위치

	// Path Smoothing
	size_t before = navPath.size();
	navPath = SmoothPath(navPath, grid, navSystem, 0);	// 레인과 무관하게 스무딩한다
	//GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::HandleChaseMove] SmoothPath %zu -> %zu\n", before, navPath.size());

	// 미니언에 경로 전달
	minion->RequestMove(navPath);

	// S_MINION_MOVE 브로드캐스트
	Protocol::S_MINION_MOVE pkt;
	pkt.set_object_id(minion->GetObjectId());
	Protocol::PosInfo* startInfo = pkt.mutable_start_pos();
	startInfo->set_x(startPos._x);
	startInfo->set_y(startPos._y);
	startInfo->set_z(startPos._z);
	for (const auto& wp : navPath)
	{
		Protocol::PosInfo* p = pkt.add_nav_path();
		p->set_x(wp._x);
		p->set_y(wp._y);
		p->set_z(wp._z);
	}
	SendBufferRef buf = ClientPacketHandler::MakeSendBuffer(pkt);
	Broadcast(buf);
}

void Room::HandleRemoveObject(int32 targetId, int32 attackerId)
{
	auto it = _objects.find(targetId);
	if (it == _objects.end())
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleRemoveObject] id not found: %d\n", targetId);
		return;
	}

	shared_ptr<Object> obj = it->second;

	// KDA 처리
	if (attackerId != -1)
	{
		auto attackerIt = _players.find(attackerId);
		if (attackerIt != _players.end())
		{
			attackerIt->second->_kill++;
		}

		if (obj->GetObjectType() == Protocol::OBJECT_TYPE_PLAYER)
		{
			auto targetPlayerIt = _players.find(targetId);
			if (targetPlayerIt != _players.end())
				targetPlayerIt->second->_death++;
		}
	}

	Protocol::S_DIE diePkt;
	diePkt.set_target_id(targetId);
	//diePkt.set_attacker_id(attackerId);
	Broadcast(ClientPacketHandler::MakeSendBuffer(diePkt));  // ← Broadcast 누락도 수정

	// 플레이어 → 리스폰 타이머 (제거 안 함)
	if (obj->GetObjectType() == Protocol::OBJECT_TYPE_PLAYER)
	{
		obj->SetIsDead(true);
		_respawnTimers[targetId] = 5.0f; // 5초
		return;
	}

	_objects.erase(it);
}

void Room::HandleTurretAttack(int32 attckerId, int32 targetId)
{
	Protocol::S_SKILL skillPkt;
	skillPkt.set_skill_id(1);
	skillPkt.set_attacker_id(attckerId);
	skillPkt.set_target_id(targetId);

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(skillPkt);
	Broadcast(sendBuffer);

	// S_HP_CHANGE 브로드캐스트 (HP바 갱신용)
	auto it = _objects.find(targetId);
	if (it == _objects.end()) return;

	shared_ptr<Object> target = it->second;
	Protocol::S_HP_CHANGE hpPkt;
	hpPkt.set_target_id(targetId);
	hpPkt.set_current_hp(target->GetHp());
	hpPkt.set_max_hp(target->GetMaxHp());
	Broadcast(ClientPacketHandler::MakeSendBuffer(hpPkt));
}

void Room::HandleNexusDead(Protocol::CampType deadTeam)
{
	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Room::HandleNexusDead] Game over\n");

	Protocol::S_END_GAME endPkt;
	Broadcast(ClientPacketHandler::MakeSendBuffer(endPkt));
}

void Room::HandleBaronChase(shared_ptr<Baron> baron, GameMath::Vector3 dest, float speed, float deltaTime)
{
	if (baron == nullptr || baron->IsDead())
		return;

	// 단일 waypoint
	vector<GameMath::Vector3> path = { dest };
	baron->RequestMove(path);
	baron->ClearPathPending();

	// S_MINON_MOVE 브로드캐스트
	Protocol::S_MINION_MOVE pkt;
	pkt.set_object_id(baron->GetObjectId());

	GameMath::Vector3 startPos = baron->GetPosVector();
	Protocol::PosInfo* startInfo = pkt.mutable_start_pos();
	startInfo->set_x(startPos._x);
	startInfo->set_y(startPos._y);
	startInfo->set_z(startPos._z);

	Protocol::PosInfo* wp = pkt.add_nav_path();
	wp->set_x(dest._x);
	wp->set_y(dest._y);
	wp->set_z(dest._z);

	Broadcast(ClientPacketHandler::MakeSendBuffer(pkt));
}

void Room::HandleBaronAttack(shared_ptr<Baron> baron, int32 targetId)
{
	if (baron == nullptr || baron->IsDead())
		return;

	unordered_map<int32, ObjectRef>::iterator it = _objects.find(targetId);
	if (it == _objects.end() || it->second->IsDead())
		return;

	shared_ptr<Object> target = it->second;

	int32 damage = baron->GetStatInfo().attack();
	//int32 defense = target->GetStatInfo().defense()
	int32 final = max(1, damage);
	int32 newHp = std::max(0, static_cast<int32>(target->GetStatInfo().hp() - final));

	Protocol::StatInfo stat = target->GetStatInfo();
	stat.set_hp(newHp);
	target->SetHp(newHp);

	Protocol::S_SKILL skillPkt;
	skillPkt.set_attacker_id(baron->GetObjectId());
	skillPkt.set_target_id(targetId);
	skillPkt.set_skill_id(1);
	Broadcast(ClientPacketHandler::MakeSendBuffer(skillPkt));

	// 사망 처리
	if (newHp <= 0)
		HandleRemoveObject(targetId, baron->GetObjectId());
}

void Room::HandleBaronAoe(shared_ptr<Baron> baron, int32 skillType)
{
	if (baron == nullptr || baron->IsDead())
		return;

	// skillType 1 : AOE 슬램, skillType 2 : 독장판
	float aoeRange = (skillType == 1) ? 5.0f : 6.0f;
	int32 aoeDamage = (skillType == 1) ? 120 : 150;
	int32 commandId = (skillType == 1) ? 201 : 202;

	GameMath::Vector3 baronPos = baron->GetPosVector();

	// S_SKILL AOE 브로드 캐스트 이펙트용
	Protocol::S_SKILL skillPkt;
	skillPkt.set_attacker_id(baron->GetObjectId());
	skillPkt.set_skill_id(commandId);
	Broadcast(ClientPacketHandler::MakeSendBuffer(skillPkt));

	// 범위 내 플레이어 전원 데미지
	for (auto& [id, obj] : _objects)
	{
		if (obj->GetObjectType() != Protocol::ObjectType::OBJECT_TYPE_PLAYER)
		{
			continue;
		}
	
		if (obj->IsDead())
		{
			continue;
		}

		GameMath::Vector3 nowPos = obj->GetPosVector();
		float dist = GameMath::Vector3::GetDistTanceXZ(baronPos, nowPos);
		if (dist > aoeRange)
			continue;

		//int32 defense
		int32 final = max(1, aoeDamage);
		int32 newHp = max(0, static_cast<int32>(obj->GetStatInfo().hp() - final));

		Protocol::StatInfo stat = obj->GetStatInfo();
		stat.set_hp(newHp);
		obj->SetHp(newHp);

		Protocol::S_HP_CHANGE hpPkt;
		hpPkt.set_target_id(id);
		hpPkt.set_current_hp(newHp);
		Broadcast(ClientPacketHandler::MakeSendBuffer(hpPkt));

		if (newHp <= 0)
			HandleRemoveObject(id, baron->GetObjectId());
	}
}

void Room::GiveCardReward(Protocol::CampType camp, int32 cardId)
{
	for (auto& [id, player] : _players)
	{
		if (player->GetCampType() != camp)
			continue;

		player->AddCardToDeck(cardId);

		Protocol::S_HAND_SYNC syncPkt;
		for (int32 cid : player->GetHandCards())
			syncPkt.add_card_ids(cid);

		shared_ptr<Session> playerSession = player->GetSession().lock();
		if (playerSession )
		{
			SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(syncPkt);
			playerSession->Send(sendBuffer);
		}
	}
}

void Room::HandleRespawnPlayer(int32 playerId)
{
	auto it = _objects.find(playerId);
	if (it == _objects.end()) return;

	shared_ptr<Object> player = it->second;
	player->FullHeal();

	// 팀별 스폰 위치
	GameMath::Vector3 spawnPos =
		(player->GetTeamFlag() == static_cast<uint8>(Protocol::CAMP_HUMAN))
		? GameMath::Vector3{ -60.0f, 0.0f, 0.0f }
	: GameMath::Vector3{ 60.0f, 0.0f, 0.0f };
	player->SetPosVector(spawnPos);

	Protocol::S_RESPAWN pkt;
	pkt.set_player_id(playerId);
	pkt.set_x(spawnPos._x);
	pkt.set_y(0.0f);
	pkt.set_z(spawnPos._z);
	pkt.set_current_hp(player->GetHp());
	pkt.set_max_hp(player->GetMaxHp());
	Broadcast(ClientPacketHandler::MakeSendBuffer(pkt));
}

void Room::BroadcastMoving(const ObjectRef& obj)
{
	// Moving Start
	Protocol::S_MOVE movePkt;
	movePkt.set_object_id(obj->GetObjectId());
	// TODO : POS는 & 형태로 가져오는 것이 유리할 것이다
	Protocol::PosInfo* pos = movePkt.mutable_server_pos_info();
	*pos = obj->GetPosInfo();
	pos->set_state(obj->GetMoveState());
	//cout << obj->GetObjectId() << " : " <<  pos->state() << endl;
	obj->OnMoveBroadcastSent(); // 타이머/플래그 리셋

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(movePkt);
	Broadcast(sendBuffer);
}

void Room::BroadcastMovingEnd(const ObjectRef& obj)
{
	// Moving End
	Protocol::S_MOVE_END endMovePkt;
	endMovePkt.set_object_id(obj->GetObjectId());
	Protocol::PosInfo* pos = endMovePkt.mutable_server_pos_info();
	*pos = obj->GetPosInfo();
	pos->set_state(obj->GetMoveState());
	//cout << pos->state() << endl;

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(endMovePkt);
	Broadcast(sendBuffer);
}

void Room::HandleSelectCharacter(PlayerRef player, Protocol::PlayerType type)
{
	int32 playerId = player->GetObjectId();

	// 이미 다른 플레이어가 선택한 캐릭터면 거절 (브로드캐스트 안 함)
	for (auto& [selectedType, selectedPlayerId] : _pendingSelections)
	{
		if (selectedType == (int32)type && selectedPlayerId != playerId)
			return;
	}

	// 이전 선택 취소 브로드캐스트
	auto prevIt = _pendingSelections.find(playerId);
	if (prevIt != _pendingSelections.end())
	{
		Protocol::S_CHARACTER_SELECTED cancelPkt;
		cancelPkt.set_player_id(playerId);
		cancelPkt.set_player_type((Protocol::PlayerType)prevIt->second);
		cancelPkt.set_is_cancel(true);
		Broadcast(ClientPacketHandler::MakeSendBuffer(cancelPkt));
	}

	// 새 선택 등록
	_pendingSelections[playerId] = (int32)type;

	// 선택 브로드캐스트
	Protocol::S_CHARACTER_SELECTED pkt;
	pkt.set_player_id(playerId);
	pkt.set_player_type(type);
	pkt.set_is_cancel(false);
	Broadcast(ClientPacketHandler::MakeSendBuffer(pkt));
}

void Room::HandleConfirmCharacter(PlayerRef player, Protocol::PlayerType type)
{
	GConsoleLogger->WriteStdOut(Color::WHITE, L"[Room::HandleConfirmCharacter] Called\n");

	player->SetPlayerType(type);
	_pendingSelections.erase(player->GetObjectId());
	Enter(player); // S_ENTER_GAME 전송
}


void Room::InitLaneRouteBin()
{
	// navigation system
	shared_ptr<Navigation::NavigationSystem> navSystem = _navigationSystem.lock();
	shared_ptr<Navigation::WalkableGrid> gridPtr = _roomWalkableGrid.lock();

	if (navSystem == nullptr || gridPtr == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"NavigationSystem or WalkableGrid is nullptr\n");
		return;
	}
	Navigation::WalkableGrid& grid = *gridPtr;

	// laneId 목록 수집
	unordered_set<int32> laneIds;
	laneIds.reserve(_laneIdCnt);
	for (int32 z = 0; z < grid.height; ++z)
	{
		for (int32 x = 0; x < grid.width; ++x)
		{
			const Navigation::GridCell& cell = grid.At(x, z);
			if (cell.walkable == false)
				continue;

			// invalid 타일
			if (cell.laneId <= 0)
				continue;

			laneIds.insert(cell.laneId);
		}
	}

	if (laneIds.empty() == true)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::InitLaneRoute] LaneId is invalid\n");
		return;
	}

	// laneId 별 LaneRoute 생성
	for (int32 laneId : laneIds)
	{
		shared_ptr<Navigation::LaneRoute> route = make_shared<Navigation::LaneRoute>();
		route->laneId = static_cast<uint8>(laneId);

		// 간단 휴리스틱 알고리즘 구현
		for (int32 z = 0; z < grid.height; ++z)
		{
			int32 chosenX = -1;
			for (int32 x = 0; x < grid.width; ++x)
			{
				const Navigation::GridCell& cell = grid.At(x, z);
				if (cell.walkable == false)
					continue;

				if (cell.laneId != laneId)
					continue;

				chosenX = x;
				break;
			}

			if (chosenX == -1)
				continue;

			GameMath::Vector3 wp;
			// GridToWorld 성공시에만 waypoint 추가
			if (!navSystem->GridToWorld(grid, chosenX, z, wp))
				continue;

			route->waypoints.push_back(wp);
		}

		if (route->waypoints.empty())
		{
			GConsoleLogger->WriteStdErr(Color::RED, L"[Room::InitLaneRoute] lane has no waypoints. laneId=%d\n", laneId);
			continue;
		}

		// Room의 laneRoute에 등록한다.
		SetLaneRoute(laneId, route);

		GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::InitLaneRoute] laneId=%d, waypoints=%d\n",
			laneId, static_cast<int32>(route->waypoints.size()));
	}
}

void Room::InitLaneRouteJson()
{
	// 1) NavigationSystem, Grid 유효 여부 체크 (필요하면 유지)
	shared_ptr<Navigation::NavigationSystem> navSystem = _navigationSystem.lock();
	shared_ptr<Navigation::WalkableGrid> gridPtr = _roomWalkableGrid.lock();

	if (navSystem == nullptr || gridPtr == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED,
			L"[Room::InitLaneRoute] NavigationSystem or WalkableGrid is nullptr\n");
		return;
	}

	// 2) laneRoutes.json 로드
	std::unordered_map<int32, shared_ptr<Navigation::LaneRoute>> loadedRoutes;

	// 파일 경로는 네가 실제 배포 구조에 맞춰 조정
	// 예: "./Data/laneRoutes.json" 또는 "Config/laneRoutes.json"
	std::string path = "../../GW2_Client/Assets/NavMeshExport/laneRoutes.json";

	if (!LaneRouteLoader::LoadLaneRoutesFromJson(path, loadedRoutes))
	{
		GConsoleLogger->WriteStdErr(Color::RED,
			L"[Room::InitLaneRoute] Failed to load lane routes from %S\n", path.c_str());
		return;
	}


	// 3) Room 내부 테이블에 등록
	for (auto& [laneId, route] : loadedRoutes)
	{
		SetLaneRoute(laneId, route);
	}

	GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::InitLaneRoute] lane routes initialized. count=%d\n", static_cast<int32>(_laneRoute.size()));
}

void Room::GiveGold(PlayerRef player, int64 amount)
{
	player->_gold += amount;

	Protocol::S_GOLD_UPDATE pkt;
	pkt.set_gold(player->_gold);

	SessionRef session = player->GetSession().lock();
	if (session)
		session->Send(ClientPacketHandler::MakeSendBuffer(pkt));
}

void Room::HandleBuyCard(PlayerRef player, int32 cardId)
{
	CardStat stat = GLobby->GetCardStat(cardId);
	// 존재하지 않는 카드
	if (stat.id == 0)
	{
		return;
	}

	Protocol::S_BUY_RESULT result;
	result.set_card_id(cardId);

	// 골드 부족
	if (player->_gold < stat.price)
	{
		result.set_success(false);
		result.set_gold(player->_gold);
		SessionRef session = player->GetSession().lock();
		if (session)
		{
			session->Send(ClientPacketHandler::MakeSendBuffer(result));
		}
		return;
	}

	// 구매 성공
	player->_gold -= stat.price;
	player->_cardManager.AddCardToDeck(*player, cardId);

	result.set_success(true);
	SessionRef session = player->GetSession().lock();
	if (session) session->Send(ClientPacketHandler::MakeSendBuffer(result));

	GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::HandleBuyCard] playerId=%d cardId=%d gold=%lld\n",
		player->GetObjectId(), cardId, player->_gold);
}

void Room::HandleRemoveCard(PlayerRef player, int32 cardId)
{
	Protocol::S_BUY_RESULT result;
	result.set_card_id(cardId);

	if (player->_cardManager.CanRemoveCard(*player) == false)
	{
		result.set_success(false);
		result.set_gold(player->_gold);
		SessionRef session = player->GetSession().lock();
		if (session)
		{
			SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(result);
			session->Send(sendBuffer);
			return;
		}
	}

	player->_cardManager.RemoveCardFromDeck(*player, cardId);
	result.set_success(true);
	result.set_gold(player->_gold);
	SessionRef session = player->GetSession().lock();
	if (session)
	{
		SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(result);
		session->Send(sendBuffer);
	}
}

weak_ptr<Navigation::LaneRoute> Room::GetLaneRoute(int32 laneId) const
{
	auto it = _laneRoute.find(laneId);
	if (it == _laneRoute.end())
		return {};
	return it->second;
}

void Room::SetLaneRoute(int32 laneId, shared_ptr<Navigation::LaneRoute> route)
{
	_laneRoute[laneId] = move(route);
}

vector<GameMath::Vector3> Room::SmoothPath(const vector<GameMath::Vector3>& path, const Navigation::WalkableGrid& grid, shared_ptr<Navigation::NavigationSystem> navSystem, uint8 laneId)
{
	// 노드가 2개 이하면 스무딩 불필요
	if (path.size() <= 2)
		return path;

	// line of sight : 두 world 좌표 사이가 직선 통과 가능한가?
	auto lineOfSight = [&](const GameMath::Vector3& from, const GameMath::Vector3& to) -> bool
		{
			int32 x0, z0, x1, z1;
			if (!navSystem->WorldToGrid(grid, from._x, from._z, x0, z0))
				return false;
			if (!navSystem->WorldToGrid(grid, to._x, to._z, x1, z1))
				return false;

			// Bresenham 직선 래스터라이즈
			int32 dx = abs(x1 - x0);
			int32 dz = abs(z1 - z0);
			int32 sx = (x0 < x1) ? 1 : -1;
			int32 sz = (z0 < z1) ? 1 : -1;
			int32 err = dx - dz;
			int32 cx = x0, cz = z0;

			while (true)
			{
				// 그리드 범위 초과 -> 통과 불가
				if (cx < 0 || cz < 0 || cx >= grid.width || cz >= grid.height)
					return false;

				const Navigation::GridCell& cell = grid.At(cx, cz);

				// 가동 불가 지역
				if (!cell.walkable)
					return false;

				// laneId 필터
				if (laneId != 0 && cell.laneId != laneId)
					return false;

				// 목적지 도달
				if (cx == x1 && cz == z1)
					break;

				// Bresenham 진행
				int32 e2 = 2 * err;
				if (e2 > -dz)
				{
					err -= dz;
					cx += sx;
				}

				if (e2 < dx)
				{
					err += dx;
					cz += sz;
				}
			}
			return true;
		};
	// Greedy anchor 처리
	vector<GameMath::Vector3> smoothed;
	smoothed.reserve(16);
	smoothed.push_back(path[0]);

	// anchor 탐색
	size_t anchor = 0;
	while (anchor < path.size() - 1)
	{
		// anchor에서 직선으로 닿을 수 있는 가장 먼 노드를 찾는다
		size_t reach = anchor + 1;
		for (size_t i = anchor + 2; i < path.size(); ++i)
		{
			if (lineOfSight(path[anchor], path[i]))
				reach = i;
			// 레인이 꺾이는 경우를 위해 끝까지 탐색한다. -> break가 없다
		}
		smoothed.push_back(path[reach]);
		anchor = reach;
	}
	return smoothed;
}

void Room::StartGame()
{
}

void Room::SyncObjectsToPlayer(PlayerRef newPlayer)
{
	auto session = newPlayer->GetSession().lock();
	if (session == nullptr) return;

	for (auto& [id, obj] : _objects)
	{
		if (id == newPlayer->GetObjectId()) continue;

		// 1. 오브젝트 스폰 정보
		Protocol::S_ENTER_GAME syncPkt;
		Protocol::ObjectInfo* info = new Protocol::ObjectInfo();
		Protocol::PosInfo* pos = new Protocol::PosInfo();

		GameMath::Vector3 objPos = obj->GetPosVector();
		pos->set_x(objPos._x);
		pos->set_y(objPos._y);
		pos->set_z(objPos._z);
		info->set_object_type(obj->GetObjectType());
		info->set_object_id(id);
		info->set_team_flag(obj->GetTeamFlag());
		info->set_allocated_pos_info(pos);
		syncPkt.set_allocated_player(info);
		session->Send(ClientPacketHandler::MakeSendBuffer(syncPkt));

		// 2. 현재 HP 동기화
		Protocol::S_HP_CHANGE hpPkt;
		hpPkt.set_target_id(id);
		hpPkt.set_current_hp(obj->GetHp());
		hpPkt.set_max_hp(obj->GetMaxHp());
		session->Send(ClientPacketHandler::MakeSendBuffer(hpPkt));
	}
}
