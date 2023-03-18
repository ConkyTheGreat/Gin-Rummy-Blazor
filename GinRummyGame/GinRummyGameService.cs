using BlazorGinRummy.GinRummyGame.Helpers;
using BlazorGinRummy.GinRummyGame.Models;
using static BlazorGinRummy.GinRummyGame.Helpers.GameLogicMethods;
using System.Text;

namespace BlazorGinRummy.GinRummyGame
{
    public class GinRummyGameService
    {
        public List<Card> deck { get; private set; } = new List<Card>();
        private const int HAND_SIZE = 10;
        public List<Card> discardPile { get; private set; } = new List<Card>();
        public List<Card> handPlayerOne { get; private set; } = new List<Card>(); // Human player
        public List<Card> handPlayerTwo { get; private set; } = new List<Card>(); // Simple computer player

        private bool canPlayerOneKnock = false;
        private bool canPlayerTwoKnock = false;
        private bool isGameOver = false;

        public bool isPlayerOneTurn { get; private set; }

        private Card? pickedUpCard;

        private int handPlayerOneValue;
        private int handPlayerTwoValue;
        private int playerOneRoundScore;
        private int playerTwoRoundScore;
        private int winnerNumber = 0;

        // TODO: set up event handler for handling new messages to print to UI console?
        public string GameStateMessage { get; private set; } = "";

        public GinRummyGameService()
        {
            deck = CreateShuffledDeck();
            DealOutHands();
            SortHandsDetectMelds();
            DetermineIfKnockingEligible();

            //isPlayerOneTurn = GameLogicMethods.DetermineDealer();
            isPlayerOneTurn = true; // TODO: remove hard coding
            GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " turn";

            FirstTurnChanceToPickupFromDiscardPile();
        }

        public void PlayerChooseDiscard(bool isCurrentlyPlayerOneTurn, int userInput)
        {
            pickedUpCard = discardPile.Last(); // TODO: remove, add choice elsewhere
            DiscardFromHand(isCurrentlyPlayerOneTurn, userInput);
            isPlayerOneTurn = !isPlayerOneTurn;
            GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " turn"; // TODO: remove
        }

        private void FirstTurnChanceToPickupFromDiscardPile()
        {
            if (canPlayerOneKnock || canPlayerTwoKnock)
            {
                GameStateMessage = "MISDEAL - atleast one player can knock before any cards have been exchanged.";
                isGameOver = true;
                winnerNumber = 0;
                return;
            }

            isPlayerOneTurn = !isPlayerOneTurn;
            GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " (NON-DEALER) - Press 'd' if you wish to pick up from the discard pile, or 'n' if you wish to pass without discarding.";

            bool didNonDealerPickupAtFirstChance = false;

            OfferChanceToPickUpFirstCardFromDiscardPile();

            isPlayerOneTurn = !isPlayerOneTurn;

            // If non-dealer passed up first chance at discard pile, dealer is given chance to pickup the card
            if (!didNonDealerPickupAtFirstChance)
            {
                GameStateMessage = "Non-dealer chose to pass - dealer now has chance to pick up card from discard pile.";
                GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " - Press 'd' if you wish to pick up from the discard pile, or 'n' if you wish to pass.";

                OfferChanceToPickUpFirstCardFromDiscardPile();

                isPlayerOneTurn = !isPlayerOneTurn;
            }

            SortHandsDetectMelds();
            DetermineIfKnockingEligible();

            void OfferChanceToPickUpFirstCardFromDiscardPile()
            {
                if (isPlayerOneTurn)
                {
                    // if picking up from discard pile
                    pickedUpCard = discardPile.Last();
                    discardPile.Remove(discardPile.Last());
                    GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " picked up " + pickedUpCard.ToString();
                    GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " - Enter number 0-9 to select card from hand to discard.";

                    // get card from player hand they chose to discard
                    // DiscardFromHand(isPlayerOneTurn, userInput);
                    // didNonDealerPickupAtFirstChance = true;



                    // if passing (need to get user input somehow)
                    GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " has chosen to pass.";
                }
                else
                {
                    var discardPileCard = discardPile.Last();

                    handPlayerTwo.Add(discardPileCard);
                    handPlayerTwo = HandMethods.DetermineMeldsInHand(handPlayerTwo);

                    var nonMeldedCards = handPlayerTwo.Where(c => !c.IsInMeld).ToList();
                    var highestDeadwoodCard = nonMeldedCards.OrderByDescending(c => c.Rank).First();

                    if (nonMeldedCards.Contains(discardPileCard))
                    {
                        if (highestDeadwoodCard == discardPileCard)
                        {
                            handPlayerTwo.Remove(discardPileCard);
                            didNonDealerPickupAtFirstChance = false;

                            GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " has chosen to pass.";
                        }
                        else
                        {
                            handPlayerTwo.Remove(highestDeadwoodCard);
                            discardPile.Remove(discardPileCard);
                            discardPile.Add(highestDeadwoodCard);

                            didNonDealerPickupAtFirstChance = true;

                            GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " picked up " + discardPileCard.ToString();
                        }
                    }
                    else
                    {
                        handPlayerTwo.Remove(highestDeadwoodCard);
                        discardPile.Remove(discardPileCard);
                        discardPile.Add(highestDeadwoodCard);

                        didNonDealerPickupAtFirstChance = true;

                        GameStateMessage = "PLAYER TWO has picked up " + discardPileCard.ToString();
                    }
                }
            }
        }

        private void DiscardFromHand(bool isPlayerOneTurn, int userInput)
        {
            if (isPlayerOneTurn)
            {
                discardPile.Add(handPlayerOne[userInput]);
                //WriteLine("\n" + CurrentPlayerString(isPlayerOneTurn) + " discarded " + discardPile.Last().ToString() + "\n");
                handPlayerOne[userInput] = pickedUpCard;
            }
            else
            {
                discardPile.Add(handPlayerTwo[userInput]);
                //WriteLine("\n" + CurrentPlayerString(isPlayerOneTurn) + " discarded " + discardPile.Last().ToString() + "\n");
                handPlayerTwo[userInput] = pickedUpCard;
            }
        }

        private List<Card> CreateShuffledDeck()
        {
            return DeckMethods.ShuffleDeck(DeckMethods.CreateDeck());
        }

        private void DealOutHands()
        {
            for (int i = 0; i < HAND_SIZE; i++)
            {
                handPlayerOne.Add(deck.Last());
                deck.Remove(deck.Last());

                handPlayerTwo.Add(deck.Last());
                deck.Remove(deck.Last());
            }

            discardPile.Add(deck.Last());
            deck.Remove(deck.Last());
        }

        public string HandToString(List<Card> hand)
        {
            var sb = new StringBuilder();

            foreach (var card in hand)
            {
                sb.Append(card.ToString());
                sb.Append(' ');
            }

            return sb.ToString();
        }

        private void SortHandsDetectMelds()
        {
            handPlayerOne = HandMethods.DetermineMeldsInHand(handPlayerOne);
            handPlayerTwo = HandMethods.DetermineMeldsInHand(handPlayerTwo);
        }

        private void DetermineIfKnockingEligible()
        {
            if (isGameOver) return;

            canPlayerOneKnock = HandMethods.CanPlayerKnock(handPlayerOne);
            canPlayerTwoKnock = HandMethods.CanPlayerKnock(handPlayerTwo);
        }
    }
}
