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
	player._deck = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
	player._hand.clear();

	for (int32 i = 0; i < MAX_HAND_SIZE; ++i)
	{
		DrawCard(player);
	}
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
