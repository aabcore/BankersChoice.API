using System;

namespace BankersChoice.API.Randomization
{
    public static class AabRoutingNumbers
    {
        public const string NumberOne = "963876207";
        public const string NumberTwo = "640479489";
        public const string NumberThree = "310264758";
        public const string NumberFour = "613844969";
        public const string NumberFive = "265902275";

        public static string GetRandomRoutingNumber()
        {
            var rand = new Random();
            var num = rand.Next(1, 6);
            return num switch
            {
                1 => NumberOne,
                2 => NumberTwo,
                3 => NumberThree,
                4 => NumberFour,
                5 => NumberFive,
                _ => throw new Exception("Couldn't make routing number")
            };
        }
    }
}