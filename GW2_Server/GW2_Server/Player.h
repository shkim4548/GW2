#pragma once
#include "Object.h"
#include "CardManager.h"

class Player : public Object
{
public:
	Player();
	virtual ~Player();

	void InitPlayer(shared_ptr<Room> room);

	void SetPlayerId(int32 id) { _objectId = id; }
	void SetSession(GameSessionRef session) { _session = session; }

	// Player 타입 지정
	void SetPlayerType(Protocol::PlayerType type) { _playerType = type; }
	Protocol::PlayerType GetPlayerType() const { return _playerType; }

	int64 GetPlayerId() { return _objectId; }
	weak_ptr<GameSession> GetSession() { return _session.load(); }

	// Contents
	void StartMove(const GameMath::Vector3& startPos, const GameMath::Vector3& targetPos, const vector<Navigation::GridCell*>& path, int32 clientTime);
	virtual void UpdateController(float deltaTime) override;

protected:
	virtual void OnDead() override;

private:
	// Server System
	string name;
	Protocol::PlayerType type = Protocol::PLAYER_TYPE_NONE;
	atomic<weak_ptr<GameSession>> _session;
	int32 roomId;

	// Contents
	Protocol::PosInfo pos;
	GameMath::Vector3 posVector;
	vector<GameMath::Vector3> path;
	int32 currentPathIndex;
	bool _isMoving = false;

	Protocol::PlayerType _playerType = Protocol::PLAYER_TYPE_POLICE;


public:
	vector<int32> _deck;
	vector<int32> _hand;
	CardManager _cardManager;	// 순수 로직담당이므로, 포인터로 선언하지 않음 직업 특성이 필요해진다면 포인터로 변경해야함
	int64         _gold = 0;       // 현재 골드
	float         _goldTimer = 0.0f; // 자동 수입 타이머

	int32 _kill = 0;
	int32 _death = 0;
	int32 _assist = 0;

	// 버프 상태
	float _attackMult = 1.0f;
	float _attackBuffTimer = 0.0f;
	float _defenseReduct = 0.0f;  // 데미지 감소율 (0~1)
	float _defenseBuffTimer = 0.0f;
	float _speedMult = 1.0f;
	float _speedBuffTimer = 0.0f;
	float _attackSpeedMult = 1.0f;
	float _attackSpeedBuffTimer = 0.0f;

	float _attackInterval = 1.0f;   // JSON에서 로드
	float _attackCooldown = 0.0f;   // 현재 남은 쿨다운
};

