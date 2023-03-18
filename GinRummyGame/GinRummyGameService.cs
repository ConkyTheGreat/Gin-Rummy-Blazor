using BlazorGinRummy.GinRummyGame.Helpers;
using BlazorGinRummy.GinRummyGame.Models;
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

        public string GameStateMessage { get; private set; } = "";

        public GinRummyGameService()
        {
            deck = CreateShuffledDeck();
            DealOutHands();
            //SortHandsDetectMelds();
            //DetermineIfKnockingEligible();

            //isPlayerOneTurn = GameLogicMethods.DetermineDealer();
            isPlayerOneTurn = true; // TODO: remove hard coding
            GameStateMessage = GameLogicMethods.CurrentPlayerString(isPlayerOneTurn) + " turn";
        }

        public void PlayerChooseDiscard(bool isCurrentlyPlayerOneTurn, int userInput)
        {
            pickedUpCard = discardPile.Last(); // TODO: remove, add choice elsewhere
            DiscardFromHand(isCurrentlyPlayerOneTurn, userInput);
            isPlayerOneTurn = !isPlayerOneTurn;
            GameStateMessage = GameLogicMethods.CurrentPlayerString(isPlayerOneTurn) + " turn"; // TODO: remove
        }

        public bool GetPlayerTurn()
        {
            return isPlayerOneTurn;
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

        public List<Card> GetDeck()
        {
            return deck;
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

        //public void DiscardFromDeck()
        //{
        //    deck.Remove(deck.Last());
        //}

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
