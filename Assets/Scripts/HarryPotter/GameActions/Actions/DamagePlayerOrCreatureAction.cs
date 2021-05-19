using System.Collections.Generic;
using HarryPotter.Data.Cards;
using HarryPotter.Data.Cards.CardAttributes.Abilities;
using HarryPotter.GameActions.ActionParameters;
using HarryPotter.Systems.Core;

namespace HarryPotter.GameActions.Actions
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // NOTE: Class is only instantiated through ability loader
    public class DamagePlayerOrCreatureAction : GameAction, IAbilityLoader
    {
        public List<Card> Targets { get; private set; }
        public int Amount { get; private set; }
        
        public void Load(IContainer game, Ability ability)
        {
            var parameter = DamagePlayerOrCreatureParameter.FromString(ability.GetParams(nameof(DamagePlayerOrCreatureAction)));

            Amount = parameter.Amount;
            SourceCard = ability.Owner;
            Player = SourceCard.Owner;

            Targets = ability.TargetSelector.SelectTargets(game, ability.Owner);
        }

        public override string ToString() => string.Empty;
    }
}