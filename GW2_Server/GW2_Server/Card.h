#pragma once



class Card
{
public:
	Card();
	virtual ~Card();

	// Getter

private:
	void UpdateDeck();

private:
	struct CardSpec
	{
		int32 skillId;
		int32 damage;
	};
};

