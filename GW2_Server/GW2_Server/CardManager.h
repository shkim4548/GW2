#pragma once

class Player;

class CardManager
{
public:
	CardManager();
	virtual ~CardManager();

	// 초기화
	void InitDeck(Player& player);
	void ResetHand(Player& player);

	// 손패 관리
	void DrawCard(Player& player);
	bool UseCard(Player& player, int32 cardId);
	void HasCard(Player& player, int32 cardId);

	// 상점용

	// Getter

private:
	int32 PickRandomFromDeck(Player& player);

public:
	static constexpr int32 MAX_HAND_SIZE = 4;
	static constexpr int32 MIN_DECK_SIZE = 4;

private:
	struct CardSpec
	{
		int32 skillId;
		int32 damage;
	};
};

