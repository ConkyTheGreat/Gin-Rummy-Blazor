using BlazorGinRummy.GinRummyGame.Helpers;
using BlazorGinRummy.GinRummyGame.Models;
using static BlazorGinRummy.GinRummyGame.Helpers.GameLogicMethods;
using System.Text;

namespace BlazorGinRummy.GinRummyGame
{
    public class GinRummyGameService
    {
        public List<Card> deck { get; private set; } = new List<Card>(); // TODO: remove public accessor?
        private const int HAND_SIZE = 10;
        public List<Card> discardPile { get; private set; } = new List<Card>();
        public List<Card> handPlayerOne { get; private set; } = new List<Card>(); // Human player
        public List<Card> handPlayerTwo { get; private set; } = new List<Card>(); // Simple computer player

        public bool canPlayerOneKnock { get; private set; } = false;
        private bool canPlayerTwoKnock = false;
        private bool isGameOver = false;

        public bool isPlayerOneTurn { get; private set; }

        private Card? pickedUpCard;
        public Card? playerOnePickedUpCard { get; private set; }

        private int handPlayerOneValue;
        private int handPlayerTwoValue;
        private int playerOneRoundScore;
        private int playerTwoRoundScore;
        private int winnerNumber = 0;

        public bool isFirstTurnChanceToPickupFromDiscardPile { get; private set; } = true;
        public bool isWaitingForPlayerOneInput { get; private set; } = false;
        public bool isPickedUpCardSet { get; private set; } = false;
        private bool didNonDealerPickupAtFirstChance = false;
        public bool isPlayerOneMakingFirstCardChoice { get; private set; } = false;
        private bool didPlayerOneStartAsDealer;

        // TODO: set up event handler for handling new messages to print to UI console?
        public List<string> GameStateMessage { get; private set; } = new();

        public GinRummyGameService()
        {
            deck = CreateShuffledDeck();
            DealOutHands();
            SortHandsDetectMelds();
            DetermineIfKnockingEligible();

            //isPlayerOneTurn = GameLogicMethods.DetermineDealer();
            isPlayerOneTurn = false; // TODO: remove hard coding
            didPlayerOneStartAsDealer = isPlayerOneTurn;
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " turn"); // TODO: remove

            FirstTurnChanceToPickupFromDiscardPile_Initialize();
        }

        public void PlayerOneChoseKnock()
        {
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " has chosen to knock and end the game.");
            isGameOver = true;
            NonKnockerCombinesUnmatchedCardsWithKnockersMelds();
            UpdatePlayerScoresAfterKnocking();
        }

        public void PlayerOneChoseKeepPlaying()
        {
            ChangePlayerTurn();
            SimpleAgentPlaysHand();
            // TODO: reset bool flags
        }

        private void NonKnockerCombinesUnmatchedCardsWithKnockersMelds()
        {
            if (isPlayerOneTurn) handPlayerTwo = HandMethods.NonKnockerCombinesUnmatchedCardsWithKnockersMelds(handPlayerOne, handPlayerTwo);
            else handPlayerOne = HandMethods.NonKnockerCombinesUnmatchedCardsWithKnockersMelds(handPlayerTwo, handPlayerOne);
        }

        private void UpdatePlayerScoresAfterKnocking()
        {
            handPlayerOneValue = HandMethods.CalculateHandValue(handPlayerOne);
            handPlayerTwoValue = HandMethods.CalculateHandValue(handPlayerTwo);

            int points = handPlayerOneValue - handPlayerTwoValue;

            if (isPlayerOneTurn)
            {
                if (points == 0)
                {
                    playerTwoRoundScore += 10;
                    winnerNumber = 2;
                    return;
                }

                if (points < 0)
                {
                    playerOneRoundScore += Math.Abs(points);
                    winnerNumber = 1;
                }
                else
                {
                    playerTwoRoundScore += Math.Abs(points);
                    playerTwoRoundScore += 10;
                    winnerNumber = 2;
                }
            }
            else
            {
                if (points == 0)
                {
                    playerOneRoundScore += 10;
                    winnerNumber = 1;
                    return;
                }

                if (points > 0)
                {
                    playerTwoRoundScore += Math.Abs(points);
                    winnerNumber = 2;
                }
                else
                {
                    playerOneRoundScore += Math.Abs(points);
                    playerOneRoundScore += 10;
                    winnerNumber = 1;
                }
            }
        }

        public void PlayerOnePickedUpCardFromDeck()
        {
            pickedUpCard = deck.Last();
            playerOnePickedUpCard = pickedUpCard;
            deck.Remove(deck.Last());
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " picked up " + pickedUpCard.ToString());
            isWaitingForPlayerOneInput = true;
            isPickedUpCardSet = true;

            // TODO: disable discard button in UI
        }

        public void PlayerOnePickedUpCardFromDiscardPile()
        {
            pickedUpCard = discardPile.Last();
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " picked up " + pickedUpCard.ToString());
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " - Enter number 0-9 to select card from hand to discard.");
            isWaitingForPlayerOneInput = true;
            isPickedUpCardSet = true;

            // TODO: disable deck button in UI
        }

        private void ChangePlayerTurn()
        {
            isPlayerOneTurn = !isPlayerOneTurn;
        }

        private void PlayerOneDiscardedTasks()
        {
            pickedUpCard = null;
            playerOnePickedUpCard = null;
            isPickedUpCardSet = false;

            SortHandsDetectMelds();
            DetectIfGinHasOccurred();
            DetermineIfKnockingEligible();
            PromptPlayerToKnock();
        }

        public void PlayerOneChoseDiscard_CardFromDeck()
        {
            discardPile.Add(pickedUpCard);
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " discarded " + discardPile.Last().ToString());

            PlayerOneDiscardedTasks();

            if (canPlayerOneKnock) return;
            else ChangePlayerTurn();
        }

        public void PlayerOneChoseDiscard(int userInput)
        {
            discardPile.Add(handPlayerOne[userInput]);
            handPlayerOne[userInput] = pickedUpCard;

            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " discarded " + discardPile.Last().ToString());

            isWaitingForPlayerOneInput = false;
            isPickedUpCardSet = false;

            PlayerOneDiscardedTasks();

            if (canPlayerOneKnock) return;
            else ChangePlayerTurn();

            if (isPlayerOneMakingFirstCardChoice)
            {
                //FirstTurnChanceToPickupFromDiscardPile_Finalize(false);
                didNonDealerPickupAtFirstChance = true;
                isPlayerOneMakingFirstCardChoice = false;
                isFirstTurnChanceToPickupFromDiscardPile = false;

                if (!didPlayerOneStartAsDealer)
                {
                    ChangePlayerTurn();
                    FirstTurnChanceToPickupFromDiscardPile_DealerTurn();
                }
            }
        }

        public void PlayerOneChosePass()
        {
            // isPlayerOneTurn = !isPlayerOneTurn;
            isWaitingForPlayerOneInput = false;
            isPlayerOneMakingFirstCardChoice = false;

            didNonDealerPickupAtFirstChance = false;
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " has chosen to pass.");

            if (didPlayerOneStartAsDealer)
            {
                isPlayerOneTurn = !isPlayerOneTurn;
            }
            else
            {
                FirstTurnChanceToPickupFromDiscardPile_DealerTurn();
            }
        }

        private void FirstTurnChanceToPickupFromDiscardPile_Initialize()
        {
            if (canPlayerOneKnock || canPlayerTwoKnock)
            {
                GameStateMessage.Add("MISDEAL - atleast one player can knock before any cards have been exchanged.");
                isGameOver = true;
                isFirstTurnChanceToPickupFromDiscardPile = false;
                winnerNumber = 0;
                return;
            }

            isPlayerOneTurn = !isPlayerOneTurn;
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " (NON-DEALER) - Press 'd' if you wish to pick up from the discard pile, or 'n' if you wish to pass without discarding.");

            OfferChanceToPickUpFirstCardFromDiscardPile();

            if (isWaitingForPlayerOneInput) return;
            FirstTurnChanceToPickupFromDiscardPile_DealerTurn();
            //OfferChanceToPickUpFirstCardFromDiscardPile();
        }


        private void FirstTurnChanceToPickupFromDiscardPile_DealerTurn()
        {
            isPlayerOneTurn = !isPlayerOneTurn;

            // If non-dealer passed up first chance at discard pile, dealer is given chance to pickup the card
            if (!didNonDealerPickupAtFirstChance)
            {
                GameStateMessage.Add("Non-dealer chose to pass - dealer now has chance to pick up card from discard pile.");
                GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " - Press 'd' if you wish to pick up from the discard pile, or 'n' if you wish to pass.");

                OfferChanceToPickUpFirstCardFromDiscardPile();

                if (isWaitingForPlayerOneInput) return;

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
                isPlayerOneMakingFirstCardChoice = true;
                isWaitingForPlayerOneInput = true;
                return;
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

                        GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " has chosen to pass.");
                    }
                    else
                    {
                        handPlayerTwo.Remove(highestDeadwoodCard);
                        discardPile.Remove(discardPileCard);
                        discardPile.Add(highestDeadwoodCard);

                        didNonDealerPickupAtFirstChance = true;

                        GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " picked up " + discardPileCard.ToString());
                        GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " discarded " + highestDeadwoodCard.ToString());
                    }
                }
                else
                {
                    handPlayerTwo.Remove(highestDeadwoodCard);
                    discardPile.Remove(discardPileCard);
                    discardPile.Add(highestDeadwoodCard);

                    didNonDealerPickupAtFirstChance = true;

                    GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " has picked up " + discardPileCard.ToString());
                    GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " discarded " + highestDeadwoodCard.ToString());
                }
            }
        }

        public void SimpleAgentPlaysHand()
        {
            var discardPileCard = discardPile.Last();

            handPlayerTwo.Add(discardPileCard);
            handPlayerTwo = HandMethods.DetermineMeldsInHand(handPlayerTwo);

            var nonMeldedCards = handPlayerTwo.Where(c => !c.IsInMeld).ToList();

            // If hand is in gin, remove a card from the players hand
            if (nonMeldedCards.Count == 0)
            {
                var groupedMelds = handPlayerTwo.GroupBy(c => c.MeldGroupIdentifier).ToList();

                var largestMeldGroup = groupedMelds.Where(m => (m.Count() > 3)).First(); // Find a meld with more than 3 cards in it

                handPlayerTwo.Remove(largestMeldGroup.Last());

                return;
            }

            // If card from discard pile doesn't form a meld, pick up a card from the deck
            if (nonMeldedCards.Contains(discardPileCard))
            {
                handPlayerTwo.Remove(discardPileCard);

                pickedUpCard = deck.Last();
                GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " has chosen to pick up a card from the deck.");
                GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " picked up " + pickedUpCard.ToString());

                deck.Remove(deck.Last());

                handPlayerTwo.Add(pickedUpCard);
                handPlayerTwo = HandMethods.DetermineMeldsInHand(handPlayerTwo);

                nonMeldedCards = handPlayerTwo.Where(c => !c.IsInMeld).ToList();

                // If hand is in gin, remove a card from the players hand
                if (nonMeldedCards.Count == 0)
                {
                    var groupedMelds = handPlayerTwo.GroupBy(c => c.MeldGroupIdentifier).ToList();

                    var largestMeldGroup = groupedMelds.Where(m => (m.Count() > 3)).First(); // Find a meld with more than 3 cards in it

                    handPlayerTwo.Remove(largestMeldGroup.Last());

                    return;
                }

                var highestDeadwoodCard = nonMeldedCards.OrderByDescending(c => c.Rank).First();

                handPlayerTwo.Remove(highestDeadwoodCard);
                discardPile.Add(highestDeadwoodCard);

                GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " discarded " + highestDeadwoodCard.ToString());
            }

            // If card did complete a meld, discard the highest deadwood value non-melded card remaining in hand
            else
            {
                var highestDeadwoodCard = nonMeldedCards.OrderByDescending(c => c.Rank).First();

                handPlayerTwo.Remove(highestDeadwoodCard);
                discardPile.Remove(discardPileCard);
                discardPile.Add(highestDeadwoodCard);

                GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " picked up " + discardPileCard.ToString());
                GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " discarded " + highestDeadwoodCard.ToString());
            }

            DetectIfGinHasOccurred();
            DetermineIfKnockingEligible();
            PromptPlayerToKnock(); // TODO: implement
            isPlayerOneTurn = !isPlayerOneTurn;
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

        private void DetectIfGinHasOccurred()
        {
            if (isPlayerOneTurn)
            {
                if (HandMethods.DetectGin(handPlayerOne))
                {
                    playerOneRoundScore += 20;
                    playerOneRoundScore += HandMethods.CalculateHandValue(handPlayerTwo);
                    winnerNumber = 1;
                    isGameOver = true;
                }
            }
            else
            {
                if (HandMethods.DetectGin(handPlayerTwo))
                {
                    playerTwoRoundScore += 20;
                    playerTwoRoundScore += HandMethods.CalculateHandValue(handPlayerOne);
                    winnerNumber = 2;
                    isGameOver = true;
                }
            }
        }

        private void PromptPlayerToKnock()
        {
            if (isGameOver) return;
            if ((isPlayerOneTurn && !canPlayerOneKnock) || (!isPlayerOneTurn && !canPlayerTwoKnock)) return;

            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " can knock (hand value less than 10 points) " +
                "- press 'k' if you wish to knock, or 'd' if you wish to continue playing.");

            if (isPlayerOneTurn)
            {
                // TODO: Make knock/not knock buttons only available options.
                isWaitingForPlayerOneInput = true;
                return;
            }
            else
            {
                // TODO: uncomment
                //GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " has chosen to knock and end the game.");
                //isGameOver = true;
                //NonKnockerCombinesUnmatchedCardsWithKnockersMelds();
                //UpdatePlayerScoresAfterKnocking();
            }
        }
    }
}
