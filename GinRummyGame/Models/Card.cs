using BlazorGinRummy.GinRummyGame.Enums;
using System.Text;

namespace BlazorGinRummy.GinRummyGame.Models
{
    public class Card
    {
        public bool IsInMeld { get; set; } = false;
        public bool IsMeld3or4ofKind { get; set; } = false;
        public int MeldGroupIdentifier { get; set; } = -1;
        public Rank Rank { get; set; }
        public Suit Suit { get; set; }
        public override string ToString()
        {
            var sb = new StringBuilder();

            switch (Rank)
            {
                case Rank.Ace:
                    sb.Append('A');
                    break;

                case Rank.Deuce:
                    sb.Append('2');
                    break;

                case Rank.Three:
                    sb.Append('3');
                    break;

                case Rank.Four:
                    sb.Append('4');
                    break;

                case Rank.Five:
                    sb.Append('5');
                    break;

                case Rank.Six:
                    sb.Append('6');
                    break;

                case Rank.Seven:
                    sb.Append('7');
                    break;

                case Rank.Eight:
                    sb.Append('8');
                    break;

                case Rank.Nine:
                    sb.Append('9');
                    break;

                case Rank.Ten:
                    sb.Append('T');
                    break;

                case Rank.Jack:
                    sb.Append('J');
                    break;

                case Rank.Queen:
                    sb.Append('Q');
                    break;

                case Rank.King:
                    sb.Append('K');
                    break;
            }

            switch (Suit)
            {
                case Suit.Spades:
                    sb.Append('♠');
                    break;

                case Suit.Clubs:
                    sb.Append('♣');
                    break;

                case Suit.Hearts:
                    sb.Append('♥');
                    break;

                case Suit.Diamonds:
                    sb.Append('♦');
                    break;
            }

            return sb.ToString();
        }

        public string FullNameString()
        {
            int rank = (int)this.Rank;
            int suit = (int)this.Suit;

            string[,] cardString = { 
                { "", "", "", "", "" }, 
                { "", "ace_of_spades", "ace_of_clubs", "ace_of_hearts", "ace_of_diamonds" },
                { "", "2_of_spades", "2_of_clubs", "2_of_hearts", "2_of_diamonds" },
                { "", "3_of_spades", "3_of_clubs", "3_of_hearts", "3_of_diamonds" },
                { "", "4_of_spades", "4_of_clubs", "4_of_hearts", "4_of_diamonds" },
                { "", "5_of_spades", "5_of_clubs", "5_of_hearts", "5_of_diamonds" },
                { "", "6_of_spades", "6_of_clubs", "6_of_hearts", "6_of_diamonds" },
                { "", "7_of_spades", "7_of_clubs", "7_of_hearts", "7_of_diamonds" },
                { "", "8_of_spades", "8_of_clubs", "8_of_hearts", "8_of_diamonds" },
                { "", "9_of_spades", "9_of_clubs", "9_of_hearts", "9_of_diamonds" },
                { "", "10_of_spades", "10_of_clubs", "10_of_hearts", "10_of_diamonds" },
                { "", "jack_of_spades", "jack_of_clubs", "jack_of_hearts", "jack_of_diamonds" },
                { "", "queen_of_spades", "queen_of_clubs", "queen_of_hearts", "queen_of_diamonds" },
                { "", "king_of_spades", "king_of_clubs", "king_of_hearts", "king_of_diamonds" },
            };

            return cardString[rank, suit];
        }

    }
}
