using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace SimpleFishingMod
{
    public static class DifficultyTemplateRegistry
    {
        private static readonly List<Func<BobberBar, diffidcultTemplate>> factories = new();

        public static void Register(Func<BobberBar, diffidcultTemplate> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            factories.Add(factory);
        }

        public static diffidcultTemplate Create(BobberBar bar)
        {
            diffidcultTemplate fallback = new Easy();

            foreach (Func<BobberBar, diffidcultTemplate> factory in factories)
            {
                diffidcultTemplate template = factory(bar);

                if (template.CanBeChonse(bar))
                    return template;
            }

            return fallback;
        }
    }
}