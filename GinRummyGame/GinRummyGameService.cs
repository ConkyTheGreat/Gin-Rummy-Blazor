using BlazorGinRummy.GinRummyGame.Helpers;
using BlazorGinRummy.GinRummyGame.Models;
using System.Text;
using static BlazorGinRummy.GinRummyGame.Helpers.GameLogicMethods;

namespace BlazorGinRummy.GinRummyGame
{
    public class GinRummyGameService
    {
        private List<Card> Deck;
        private const int HAND_SIZE = 10;
        public List<Card> DiscardPile { get; private set; }
        public List<Card> HandPlayerOne { get; private set; } // Human player
        public List<Card> HandPlayerTwo { get; private set; }  // Simple computer player

        public bool CanPlayerOneKnock { get; private set; } // false
        public bool IsPlayerOneStillPlaying { get; private set; } // true
        private bool CanPlayerTwoKnock; // false
        public bool IsGameOver { get; private set; } // false

        public bool IsPlayerOneTurn { get; private set; }

        private Card? PickedUpCard;
        public Card? PlayerOnePickedUpCard { get; private set; }

        private int handPlayerOneValue;
        private int handPlayerTwoValue;
        public int PlayerOneRoundScore { get; private set; } // 0
        public int PlayerTwoRoundScore { get; private set; } // 0

        public int PlayerOneGameScore { get; private set; } = 0;
        public int PlayerTwoGameScore { get; private set; } = 0;

        public bool IsWaitingForPlayerOneInput { get; private set; } // false
        private bool didNonDealerPickupAtFirstChance; // false
        public bool IsPlayerOneMakingFirstCardChoice { get; private set; } // false
        private bool didPlayerOneStartAsDealer;
        public bool DidPlayerOnePickupCard { get; private set; }

        // TODO: rewrite index into a component, create another seperate component for score listing
        // TODO: have toggle button to see opponents hand or hide it
        public List<string> GameStateMessage { get; private set; } // initialize new

        public GinRummyGameService()
        {
            // TODO: See how to incorporate game history into browser storage
            StartNewGame();
        }

        public void StartNewGame()
        {
            Deck = DeckMethods.ShuffleDeck(DeckMethods.CreateDeck());

            DiscardPile = new();
            HandPlayerOne = new();
            HandPlayerTwo = new();
            GameStateMessage = new();

            PickedUpCard = null;
            PlayerOnePickedUpCard = null;

            IsPlayerOneStillPlaying = true;
            CanPlayerOneKnock = false;
            CanPlayerTwoKnock = false;
            IsGameOver = false;
            IsWaitingForPlayerOneInput = false;
            didNonDealerPickupAtFirstChance = false;
            IsPlayerOneMakingFirstCardChoice = false;
            DidPlayerOnePickupCard = false;

            IsPlayerOneTurn = DetermineDealer();
            didPlayerOneStartAsDealer = IsPlayerOneTurn;

            handPlayerOneValue = 0;
            handPlayerTwoValue = 0;
            PlayerOneRoundScore = 0;
            PlayerTwoRoundScore = 0;

            DealOutHands();
            SortHandsDetectMelds();
            DetermineIfKnockingEligible();

            FirstTurnChanceToPickupFromDiscardPile_Initialize();
        }

        public void PlayerOneChoseKnock()
        {
            AddGameStateMessage_PlayerKnocked();
            NonKnockerCombinesUnmatchedCardsWithKnockersMelds();
            UpdatePlayerScoresAfterKnocking();
            ExecuteGameOverTasks();
        }

        public void PlayerOneChoseKeepPlaying()
        {
            GameStateMessage.Add(CurrentPlayerString(IsPlayerOneTurn) + " has chosen to continue playing.");
            IsPlayerOneStillPlaying = true;
            PrepareNextTurn();
            SimpleAgentPlaysHand();
        }

        private void NonKnockerCombinesUnmatchedCardsWithKnockersMelds()
        {
            if (IsPlayerOneTurn) HandPlayerTwo = HandMethods.NonKnockerCombinesUnmatchedCardsWithKnockersMelds(HandPlayerOne, HandPlayerTwo);
            else HandPlayerOne = HandMethods.NonKnockerCombinesUnmatchedCardsWithKnockersMelds(HandPlayerTwo, HandPlayerOne);
        }

        private void UpdatePlayerScoresAfterKnocking()
        {
            handPlayerOneValue = HandMethods.CalculateHandValue(HandPlayerOne);
            handPlayerTwoValue = HandMethods.CalculateHandValue(HandPlayerTwo);

            int points = handPlayerOneValue - handPlayerTwoValue;

            if (IsPlayerOneTurn)
            {
                if (points == 0)
                {
                    PlayerTwoRoundScore += 10;
                    return;
                }

                if (points < 0)
                {
                    PlayerOneRoundScore += Math.Abs(points);
                }
                else
                {
                    PlayerTwoRoundScore += Math.Abs(points);
                    PlayerTwoRoundScore += 10;
                }
            }
            else
            {
                if (points == 0)
                {
                    PlayerOneRoundScore += 10;
                    return;
                }

                if (points > 0)
                {
                    PlayerTwoRoundScore += Math.Abs(points);
                }
                else
                {
                    PlayerOneRoundScore += Math.Abs(points);
                    PlayerOneRoundScore += 10;
                }
            }
        }

        public void PlayerOnePickedUpCardFromDeck()
        {
            AddGameStateMessage_PlayerChoseToPickupCardFromDeck();

            PickedUpCard = Deck.Last();
            Deck.Remove(Deck.Last());

            PlayerOnePickedUpCardTasks();
        }

        public void PlayerOnePickedUpCardFromDiscardPile()
        {
            PickedUpCard = DiscardPile.Last();
            DiscardPile.Remove(DiscardPile.Last());

            PlayerOnePickedUpCardTasks();
        }

        private void PlayerOnePickedUpCardTasks()
        {
            PlayerOnePickedUpCard = PickedUpCard;

            AddGameStateMessage_PlayerPickedUpCard(PickedUpCard.ToString());
            GameStateMessage.Add(CurrentPlayerString(IsPlayerOneTurn) + " - Select a card from your hand to discard.");
            DidPlayerOnePickupCard = true;
        }

        private void PrepareNextTurn()
        {
            if (Deck.Count == 0)
            {
                EndOfDeckReached();
                return;
            }

            IsPlayerOneTurn = !IsPlayerOneTurn;
        }

        public void PlayerOneChoseDiscard_DeckCard()
        {
            DiscardPile.Add(PickedUpCard);

            ExecutePlayerOneAfterDiscardTasks();
        }

        public void PlayerOneChoseDiscard(int userInput)
        {
            DiscardPile.Add(HandPlayerOne[userInput]);
            HandPlayerOne[userInput] = PickedUpCard;

            ExecutePlayerOneAfterDiscardTasks();
        }

        private void ExecutePlayerOneAfterDiscardTasks()
        {
            AddGameStateMessage_PlayerDiscarded(DiscardPile.Last().ToString());

            SortHandsDetectMelds();
            DetectIfGinHasOccurred();
            DetermineIfKnockingEligible();
            PromptPlayerToKnock();

            PickedUpCard = null;
            PlayerOnePickedUpCard = null;
            DidPlayerOnePickupCard = false;

            if (!IsGameOver && IsPlayerOneStillPlaying) PrepareNextTurn();

            if (IsPlayerOneMakingFirstCardChoice)
            {
                IsPlayerOneMakingFirstCardChoice = false;

                if (!didPlayerOneStartAsDealer)
                {
                    didNonDealerPickupAtFirstChance = true;
                    IsWaitingForPlayerOneInput = false;
                    PrepareNextTurn();
                    FirstTurnChanceToPickupFromDiscardPile_DealerTurn();
                }
            }
        }

        public void PlayerOneChosePass()
        {
            IsPlayerOneMakingFirstCardChoice = false;

            AddGameStateMessage_PlayerPassed();

            if (didPlayerOneStartAsDealer)
            {
                PrepareNextTurn();
                SimpleAgentPlaysHand();
            }
            else
            {
                IsWaitingForPlayerOneInput = false;
                didNonDealerPickupAtFirstChance = false;
                FirstTurnChanceToPickupFromDiscardPile_DealerTurn();
            }
        }

        private void FirstTurnChanceToPickupFromDiscardPile_Initialize()
        {
            if (CanPlayerOneKnock || CanPlayerTwoKnock)
            {
                GameStateMessage.Add("MISDEAL - atleast one player can knock before any cards have been exchanged.");
                ExecuteGameOverTasks();
                return;
            }

            GameStateMessage.Add($"First turn phase - the non-dealer ({CurrentPlayerString(!didPlayerOneStartAsDealer)}) may choose " +
                $"to pick up the first card from the discard pile, or pass. If the non-dealer passes, " +
                $"the dealer ({CurrentPlayerString(didPlayerOneStartAsDealer)}) is given the " +
                "opportunity to pick up the first card from the discard pile, or pass.");

            PrepareNextTurn();
            AddGameStateMessage_PromptPlayerPickOrPass();

            OfferChanceToPickUpFirstCardFromDiscardPile();

            if (IsWaitingForPlayerOneInput) return;
            FirstTurnChanceToPickupFromDiscardPile_DealerTurn();
        }

        private void FirstTurnChanceToPickupFromDiscardPile_DealerTurn()
        {
            PrepareNextTurn();

            // If non-dealer passed up first chance at discard pile, dealer is given chance to pickup the card
            if (!didNonDealerPickupAtFirstChance)
            {
                GameStateMessage.Add("Non-dealer chose to pass - dealer now has chance to pick up card from discard pile.");
                AddGameStateMessage_PromptPlayerPickOrPass();

                OfferChanceToPickUpFirstCardFromDiscardPile();

                if (IsWaitingForPlayerOneInput) return;

                PrepareNextTurn();
            }

            SortHandsDetectMelds();
            DetermineIfKnockingEligible();
        }

        private void OfferChanceToPickUpFirstCardFromDiscardPile()
        {
            if (IsPlayerOneTurn)
            {
                IsPlayerOneMakingFirstCardChoice = true;
                IsWaitingForPlayerOneInput = true;
                return;
            }
            else
            {
                var discardPileCard = DiscardPile.Last();

                HandPlayerTwo.Add(discardPileCard);
                HandPlayerTwo = HandMethods.DetermineMeldsInHand(HandPlayerTwo);

                var nonMeldedCards = HandPlayerTwo.Where(c => !c.IsInMeld).ToList();
                var highestDeadwoodCard = nonMeldedCards.OrderByDescending(c => c.Rank).First();

                if (nonMeldedCards.Contains(discardPileCard))
                {
                    if (highestDeadwoodCard == discardPileCard)
                    {
                        HandPlayerTwo.Remove(discardPileCard);
                        didNonDealerPickupAtFirstChance = false;

                        AddGameStateMessage_PlayerPassed();
                    }
                    else
                    {
                        PlayerTwoPickUpAndDiscard(discardPileCard, highestDeadwoodCard);
                    }
                }
                else
                {
                    PlayerTwoPickUpAndDiscard(discardPileCard, highestDeadwoodCard);
                }
            }

            void PlayerTwoPickUpAndDiscard(Card discardPileCard, Card highestDeadwoodCard)
            {
                HandPlayerTwo.Remove(highestDeadwoodCard);
                DiscardPile.Remove(discardPileCard);
                DiscardPile.Add(highestDeadwoodCard);

                didNonDealerPickupAtFirstChance = true;

                AddGameStateMessage_PlayerPickedUpCard(discardPileCard.ToString());
                AddGameStateMessage_PlayerDiscarded(highestDeadwoodCard.ToString());
            }
        }

        private void EndOfDeckReached()
        {
            GameStateMessage.Add("End of deck reached - tallying points remaining in player hands.");

            handPlayerOneValue = HandMethods.CalculateHandValue(HandPlayerOne);
            handPlayerTwoValue = HandMethods.CalculateHandValue(HandPlayerTwo);

            PlayerOneRoundScore += handPlayerTwoValue;
            PlayerTwoRoundScore += handPlayerOneValue;

            ExecuteGameOverTasks();
        }

        public void SimpleAgentPlaysHand()
        {
            if (IsGameOver) return;

            var discardPileCard = DiscardPile.Last();

            HandPlayerTwo.Add(discardPileCard);
            HandPlayerTwo = HandMethods.DetermineMeldsInHand(HandPlayerTwo);

            var nonMeldedCards = HandPlayerTwo.Where(c => !c.IsInMeld).ToList();

            // If hand is in gin, remove a card from the players hand
            if (nonMeldedCards.Count == 0)
            {
                var groupedMelds = HandPlayerTwo.GroupBy(c => c.MeldGroupIdentifier).ToList();

                var largestMeldGroup = groupedMelds.Where(m => (m.Count() > 3)).First(); // Find a meld with more than 3 cards in it

                HandPlayerTwo.Remove(largestMeldGroup.Last());

                return;
            }

            // If card from discard pile doesn't form a meld, pick up a card from the Deck
            if (nonMeldedCards.Contains(discardPileCard))
            {
                HandPlayerTwo.Remove(discardPileCard);

                PickedUpCard = Deck.Last();
                AddGameStateMessage_PlayerChoseToPickupCardFromDeck();
                AddGameStateMessage_PlayerPickedUpCard(PickedUpCard.ToString());

                Deck.Remove(Deck.Last());

                HandPlayerTwo.Add(PickedUpCard);
                HandPlayerTwo = HandMethods.DetermineMeldsInHand(HandPlayerTwo);

                nonMeldedCards = HandPlayerTwo.Where(c => !c.IsInMeld).ToList();

                // If hand is in gin, remove a card from the players hand
                if (nonMeldedCards.Count == 0)
                {
                    var groupedMelds = HandPlayerTwo.GroupBy(c => c.MeldGroupIdentifier).ToList();

                    var largestMeldGroup = groupedMelds.Where(m => (m.Count() > 3)).First(); // Find a meld with more than 3 cards in it

                    HandPlayerTwo.Remove(largestMeldGroup.Last());

                    return;
                }

                var highestDeadwoodCard = nonMeldedCards.OrderByDescending(c => c.Rank).First();

                HandPlayerTwo.Remove(highestDeadwoodCard);
                DiscardPile.Add(highestDeadwoodCard);

                AddGameStateMessage_PlayerDiscarded(highestDeadwoodCard.ToString());
            }

            // If card did complete a meld, discard the highest deadwood value non-melded card remaining in hand
            else
            {
                var highestDeadwoodCard = nonMeldedCards.OrderByDescending(c => c.Rank).First();

                HandPlayerTwo.Remove(highestDeadwoodCard);
                DiscardPile.Remove(discardPileCard);
                DiscardPile.Add(highestDeadwoodCard);

                AddGameStateMessage_PlayerPickedUpCard(discardPileCard.ToString());
                AddGameStateMessage_PlayerDiscarded(highestDeadwoodCard.ToString());
            }

            DetectIfGinHasOccurred();
            DetermineIfKnockingEligible();
            PromptPlayerToKnock();
            PrepareNextTurn();
        }

        private void DealOutHands()
        {
            for (int i = 0; i < HAND_SIZE; i++)
            {
                HandPlayerOne.Add(Deck.Last());
                Deck.Remove(Deck.Last());

                HandPlayerTwo.Add(Deck.Last());
                Deck.Remove(Deck.Last());
            }

            DiscardPile.Add(Deck.Last());
            Deck.Remove(Deck.Last());
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
            HandPlayerOne = HandMethods.DetermineMeldsInHand(HandPlayerOne);
            HandPlayerTwo = HandMethods.DetermineMeldsInHand(HandPlayerTwo);
        }

        private void DetermineIfKnockingEligible()
        {
            if (IsGameOver) return;

            CanPlayerOneKnock = HandMethods.CanPlayerKnock(HandPlayerOne);
            CanPlayerTwoKnock = HandMethods.CanPlayerKnock(HandPlayerTwo);
        }

        private void DetectIfGinHasOccurred()
        {
            if (IsPlayerOneTurn)
            {
                if (HandMethods.DetectGin(HandPlayerOne))
                {
                    PlayerOneRoundScore += 20;
                    PlayerOneRoundScore += HandMethods.CalculateHandValue(HandPlayerTwo);
                    ExecuteGameOverTasks();
                }
            }
            else
            {
                if (HandMethods.DetectGin(HandPlayerTwo))
                {
                    PlayerTwoRoundScore += 20;
                    PlayerTwoRoundScore += HandMethods.CalculateHandValue(HandPlayerOne);
                    ExecuteGameOverTasks();
                }
            }
        }

        private void ExecuteGameOverTasks()
        {
            PlayerOneGameScore += PlayerOneRoundScore;
            PlayerTwoGameScore += PlayerTwoRoundScore;
            IsGameOver = true;
            GameStateMessage.Add("GAME OVER");
        }

        private void PromptPlayerToKnock()
        {
            if (IsGameOver) return;
            if ((IsPlayerOneTurn && !CanPlayerOneKnock) || (!IsPlayerOneTurn && !CanPlayerTwoKnock)) return;

            AddGameStateMessage_PlayerCanKnock();

            if (IsPlayerOneTurn)
            {
                IsPlayerOneStillPlaying = false;
                return;
            }
            else
            {
                // TODO: uncomment
                AddGameStateMessage_PlayerKnocked();
                NonKnockerCombinesUnmatchedCardsWithKnockersMelds();
                UpdatePlayerScoresAfterKnocking();
                ExecuteGameOverTasks();
            }
        }

        private void AddGameStateMessage_PlayerCanKnock()
        {
            GameStateMessage.Add(CurrentPlayerString(IsPlayerOneTurn) + " can knock (hand value less than 10 points).");
        }

        private void AddGameStateMessage_PlayerKnocked()
        {
            GameStateMessage.Add(CurrentPlayerString(IsPlayerOneTurn) + " has chosen to knock and end the game.");
        }

        private void AddGameStateMessage_PlayerPickedUpCard(string card)
        {
            GameStateMessage.Add(CurrentPlayerString(IsPlayerOneTurn) + " picked up " + card);
        }

        private void AddGameStateMessage_PlayerChoseToPickupCardFromDeck()
        {
            GameStateMessage.Add(CurrentPlayerString(IsPlayerOneTurn) + " has chosen to pick up a card from the deck.");
        }

        private void AddGameStateMessage_PlayerDiscarded(string card)
        {
            GameStateMessage.Add(CurrentPlayerString(IsPlayerOneTurn) + " discarded " + card);
        }

        private void AddGameStateMessage_PlayerPassed()
        {
            GameStateMessage.Add(CurrentPlayerString(IsPlayerOneTurn) + " has chosen to pass.");
        }

        private void AddGameStateMessage_PromptPlayerPickOrPass()
        {
            GameStateMessage.Add(CurrentPlayerString(IsPlayerOneTurn) + " - Click the card in the discard pile if you wish to pick it up, or press the pass button.");
        }
    }
}
