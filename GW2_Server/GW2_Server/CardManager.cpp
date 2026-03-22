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


}

void CardManager::HasCard(Player& player, int32 cardId)
{
}

int32 CardManager::PickRandomFromDeck(Player& player)
{
	return int32();
}
