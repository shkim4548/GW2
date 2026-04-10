#pragma once
#include "GameLogic.h"

namespace GameMath{ struct Vector3; struct movement; }
namespace Navigation {  struct GridCell;  class NavigationSystem; }
using NavPath = std::vector<GameMath::Vector3>;
constexpr float MOVE_BROADCAST_INTERVAL = 0.1f; // 100ms (10Hz), 가장 일반적인 온라인 게임 브로드캐스트 주기

class Object : public enable_shared_from_this<Object>
{
public:
	Object();
	virtual ~Object();

	int32 GetObjectId() { return _objectInfo.object_id(); }
	Protocol::PosInfo GetPosInfo() { return _objectInfo.pos_info(); }
	GameMath::Vector3 GetPosVector() const { return _posVector; }
	Protocol::StatInfo GetStatInfo() const { return _objectInfo.stat_info(); }
	Protocol::MoveState GetMoveState() const { return _objectInfo.pos_info().state(); }
	Protocol::ObjectType GetObjectType() const { return _objectInfo.object_type(); }
	Protocol::CampType GetTeamFlag() const { return static_cast<Protocol::CampType>(_objectInfo.team_flag());	}
	

	float GetHp() { return _objectInfo.stat_info().hp(); }
	float GetYaw() { return _objectInfo.pos_info().yaw(); }
	bool GetIsDead() { return _isDead; }



	void FullHeal();
	uint64 Heal(uint64 amount);
	// TODO : 사망 여부 체크 로직 필요
	bool IsDead();

	void SetObjectId(int64 id) { _objectInfo.set_object_id(id); }
	void SetPosInfo(Protocol::PosInfo posInfo);
	void SetPosVector(GameMath::Vector3& posVector);
	void SetMoveState(Protocol::MoveState moveState) { _objectInfo.mutable_pos_info()->set_state(moveState); }
	void SetIsMoving(bool isMoving) { _isMoving = isMoving; }
	void SetRoomId(int32 roomId) { _objectInfo.set_room_id(roomId); }
	void SetIsDead(bool isDead) { _isDead = isDead; }
	void SetCampType(Protocol::CampType camp) { _objectInfo.set_team_flag(camp); }
	void SetObjectType(Protocol::ObjectType type) { _objectInfo.set_object_type(type); }

	// Navigation
	void SetPath(const NavPath& path);
	bool GetIsMoving() const { return _isMoving; }
	virtual void UpdateMovement(float deltaTime);
	void RequestMove(const vector<GameMath::Vector3>& path);
	void RequestMoveFrom(const vector<GameMath::Vector3>& path, int32 startIndex);
	void PostUpdate();
	
	// MOVEMENT SYSTEM
	virtual void UpdateController(float deltaTime);
	
	// Network Helper
	void MarkForceBroadcastMove();
	void OnMoveBroadcastSent();

	// Game Room Logic
	void AccumulateMoveTime(float deltaTime);
	bool ShouldBroadcastMove() const;
	void ResetBroadcastTimer();
	bool ValidateMovement(float deltaTime);
	
	// TYPE HELPER
	bool IsPlayer() const { return _objectInfo.object_type() == Protocol::ObjectType::OBJECT_TYPE_PLAYER; }
	bool IsMinion() const {	return _objectInfo.object_type() == Protocol::ObjectType::OBJECT_TYPE_MINION; }
	bool IsTurret() const {	return _objectInfo.object_type() == Protocol::ObjectType::OBJECT_TYPE_TURRET; }
	bool IsNexus()  const {	return _objectInfo.object_type() == Protocol::ObjectType::OBJECT_TYPE_NEXUS;	}
	
	// STATE HELPER
	bool GetIsMoving() { return _isMoving; }

	// STAT HELPER
	bool ApplyDamage(uint64 dmg);
	uint64_t GetHp()    const { return _objectInfo.stat_info().hp(); }
	uint64_t GetMaxHp() const { return _objectInfo.stat_info().max_hp(); }
	void SetHp(uint64_t hp) { _objectInfo.mutable_stat_info()->set_hp(hp); }
	void SetMaxHp(uint64_t hp) { _objectInfo.mutable_stat_info()->set_max_hp(hp); }

	// Debug
	int32 GetRoomId() { return _objectInfo.room_id(); }
	virtual void OnDead() = 0;

public:
	NavPath  _path;
	size_t   _pathIndex = 0;
	bool    _isStunned = false;
	float   _stunTimer = 0.0f;
	Protocol::ObjectInfo _objectInfo;

protected:

	GameMath::Vector3  _posVector;      
	GameMath::Movement _movement;
	weak_ptr<Room>     _room;
	weak_ptr<Navigation::NavigationSystem> _navigationSystem;
	float   _moveSpeed = 100.0f;
	float   _moveBroadcastElapsed = 0.0f;
	bool    _forceBroadcastMove = false;
	bool    _isMoving = false;
	bool    _isDead = false;

};

