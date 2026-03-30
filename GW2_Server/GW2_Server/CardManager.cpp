#include "pch.h"
#include "Player.h"
#include "CardManager.h"

CardManager::CardManager()
{
}

CardManager::~CardManager()
{
}

void CardManager::InitDeck(Player& player)
{
	// 30개 풀에서 12개 랜덤 선택
	static const vector<int32> ALL_CARD_IDS = {
		101,102,103,104,105,106,107,108,109,110,
		111,112,113,114,115,116,117,118,119,120,
		121,122,123,124,125,126,127,128,129,130
	};

	static mt19937 rng(random_device{}());

	vector<int32> pool = ALL_CARD_IDS;
	shuffle(pool.begin(), pool.end(), rng);

	player._deck.clear();
	player._hand.clear();

	for (int32 i = 0; i < 12; ++i)
		player._deck.push_back(pool[i]);

	for (int32 i = 0; i < MAX_HAND_SIZE; ++i)
		DrawCard(player);
}

void CardManager::ResetHand(Player& player)
{
	player._hand.clear();
	for (int32 i = 0; i < MAX_HAND_SIZE; ++i)
		DrawCard(player);
}

void CardManager::DrawCard(Player& player)
{
	if (player._deck.empty())
	{
		return;
	}

	if (static_cast<int32>(player._hand.size()) >= MAX_HAND_SIZE)
	{
		return;
	}

	int32 cardId = PickRandomFromDeck(player);
	player._hand.push_back(cardId);

	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[CardManager::DrawCard] playerId=%d drew cardId=%d handSize=%d\n",
		player.GetObjectId(), cardId, (int32)player._hand.size());
}

bool CardManager::UseCard(Player& player, int32 cardId)
{
	auto it = find(player._hand.begin(), player._hand.end(), cardId);
	if (it == player._hand.end())
	{
		return false;
	}

	player._hand.erase(it);
	GConsoleLogger->WriteStdOut(Color::GREEN, L"[CardManager::UseCard] playerId=%d used cardId=%d\n",
		player.GetObjectId(), cardId);

	DrawCard(player);
	return true;
}

bool CardManager::HasCard(Player& player, int32 cardId)
{
	return find(player._hand.begin(), player._hand.end(), cardId) != player._hand.end();
}

bool CardManager::CanAddCard(Player& player)
{
	return true; // 덱 상한선 없음
}

bool CardManager::CanRemoveCard(Player& player)
{
	return static_cast<int32>(player._deck.size()) > MIN_DECK_SIZE;
}

void CardManager::AddCardToDeck(Player& player, int32 cardId)
{
	player._deck.push_back(cardId);
}

void CardManager::RemoveCardFromDeck(Player& player, int32 cardId)
{
	if (!CanRemoveCard(player)) 
		return;
	auto it = find(player._deck.begin(), player._deck.end(), cardId);
	if (it != player._deck.end())
		player._deck.erase(it);
}

int32 CardManager::PickRandomFromDeck(Player& player)
{
	static mt19937 rng(random_device{}());
	uniform_int_distribution<int32> dist(0, static_cast<int32>(player._deck.size()) - 1);
	return player._deck[dist(rng)];
}
