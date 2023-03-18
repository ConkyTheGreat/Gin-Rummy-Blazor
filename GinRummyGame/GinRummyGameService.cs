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

        public bool isFirstTurnChanceToPickupFromDiscardPile { get; private set; } = true;
        public bool isWaitingForPlayerOneInput { get; private set; } = false;
        public bool isPickedUpCardSet { get; private set; } = false;
        private bool didNonDealerPickupAtFirstChance = false;

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

            FirstTurnChanceToPickupFromDiscardPile_Initialize();
        }

        public void PlayerPickedUpCardFromDeck()
        {

        }

        public void PlayerPickedUpCardFromDiscardPile()
        {
            pickedUpCard = discardPile.Last();
            GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " picked up " + pickedUpCard.ToString();
            GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " - Enter number 0-9 to select card from hand to discard.";
            isWaitingForPlayerOneInput = true;
            isPickedUpCardSet = true; 
        }

        public void PlayerChoseDiscard(int userInput)
        {
            DiscardFromHand(true, userInput);
            isPlayerOneTurn = !isPlayerOneTurn;
            GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " turn"; // TODO: remove
            isWaitingForPlayerOneInput = false;
            isPickedUpCardSet = false;

            if (isFirstTurnChanceToPickupFromDiscardPile) FirstTurnChanceToPickupFromDiscardPile_Finalize(false);
        }

        public void PlayerChoseToPass()
        {
            isPlayerOneTurn = !isPlayerOneTurn;
            isWaitingForPlayerOneInput = false;

            if (isFirstTurnChanceToPickupFromDiscardPile) FirstTurnChanceToPickupFromDiscardPile_Finalize(true);
        }

        private void FirstTurnChanceToPickupFromDiscardPile_Initialize()
        {
            if (canPlayerOneKnock || canPlayerTwoKnock)
            {
                GameStateMessage = "MISDEAL - atleast one player can knock before any cards have been exchanged.";
                isGameOver = true;
                isFirstTurnChanceToPickupFromDiscardPile = false;
                winnerNumber = 0;
                return;
            }

            isPlayerOneTurn = !isPlayerOneTurn;
            GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " (NON-DEALER) - Press 'd' if you wish to pick up from the discard pile, or 'n' if you wish to pass without discarding.";

            OfferChanceToPickUpFirstCardFromDiscardPile();

            if (isWaitingForPlayerOneInput) return;

            FirstTurnChanceToPickupFromDiscardPile_DealerTurn();
        }

        private void FirstTurnChanceToPickupFromDiscardPile_Finalize(bool didPlayerPass)
        {
            if (didPlayerPass)
            {
                didNonDealerPickupAtFirstChance = false;
            }
            else
            {
                didNonDealerPickupAtFirstChance = true;
            }

            FirstTurnChanceToPickupFromDiscardPile_DealerTurn();
        }

        private void FirstTurnChanceToPickupFromDiscardPile_DealerTurn()
        {
            isPlayerOneTurn = !isPlayerOneTurn;

            // If non-dealer passed up first chance at discard pile, dealer is given chance to pickup the card
            if (!didNonDealerPickupAtFirstChance)
            {
                GameStateMessage = "Non-dealer chose to pass - dealer now has chance to pick up card from discard pile.";
                GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " - Press 'd' if you wish to pick up from the discard pile, or 'n' if you wish to pass.";

                OfferChanceToPickUpFirstCardFromDiscardPile();

                isPlayerOneTurn = !isPlayerOneTurn;
            }

            isFirstTurnChanceToPickupFromDiscardPile = false;
            SortHandsDetectMelds();
            DetermineIfKnockingEligible();
        }

        private void OfferChanceToPickUpFirstCardFromDiscardPile()
        {
            if (isPlayerOneTurn)
            {
                isWaitingForPlayerOneInput = true;
                return;

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

                    GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " has picked up " + discardPileCard.ToString();
                }
            }
        }

        private void DiscardFromHand(bool isPlayerOneTurn, int userInput)
        {
            if (isPlayerOneTurn)
            {
                discardPile.Add(handPlayerOne[userInput]);              
                handPlayerOne[userInput] = pickedUpCard;
            }
            else
            {
                discardPile.Add(handPlayerTwo[userInput]);
                handPlayerTwo[userInput] = pickedUpCard;
            }

            GameStateMessage = CurrentPlayerString(isPlayerOneTurn) + " discarded " + discardPile.Last().ToString();
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
