using System;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class FunnyTramMenuLabels
    {
        private static readonly string[] Labels =
        [
            "I like trains",
            "Choo choo choose a save",
            "All aboard the pain train",
            "Thomas has seen things",
            "Trammy McTramface",
            "Press F to pay tramfare",
            "Hop on the hype tram",
            "This tram runs on screams",
        ];

        internal static string PickRandom()
        {
            return Labels[UnityEngine.Random.Range(0, Labels.Length)];
        }
    }
}
