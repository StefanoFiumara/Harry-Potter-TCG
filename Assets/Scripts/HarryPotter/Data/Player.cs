using System;
using System.Collections.Generic;
using System.Linq;
using HarryPotter.Data.Cards;
using HarryPotter.Data.Cards.CardAttributes;
using HarryPotter.Enums;
using HarryPotter.Utils;
using UnityEngine;

namespace HarryPotter.Data
{
    [CreateAssetMenu(menuName = "HarryPotter/Player")]
    public class Player : ScriptableObject
    {
        public ControlMode ControlMode { get; set; }
        
        public int Index { get; set; }
        
        public int ActionsAvailable { get; set; } 

        public Deck SelectedDeck { get; set; }
        
        public string PlayerName { get; set; }
        
        public Player EnemyPlayer { get; set; }

        public List<Card> AllCards  { get; }
               
        public List<Card> Deck       { get; }
        public List<Card> Discard    { get; }
        public List<Card> Hand       { get; }
        public List<Card> Characters { get; }
        public List<Card> Lessons    { get; }
        public List<Card> Creatures  { get; }
        public List<Card> Location   { get; }
        public List<Card> Match      { get; }
        public List<Card> Items      { get; }
        public List<Card> Adventure  { get; }

        public List<Card> this[Zones z] {
            get {
                switch (z) {
                    case Zones.Deck:       return Deck;
                    case Zones.Discard:    return Discard;
                    case Zones.Hand:       return Hand;
                    case Zones.Characters: return Characters;
                    case Zones.Lessons:    return Lessons;
                    case Zones.Creatures:  return Creatures;
                    case Zones.Location:   return Location;
                    case Zones.Match:      return Match;
                    case Zones.Items:      return Items;
                    case Zones.Adventure:  return Adventure;
                    default:
                        return null;
                }
            }
        }

        public Player()
        {
            AllCards   = new List<Card>();
            Deck       = new List<Card>();
            Discard    = new List<Card>();
            Hand       = new List<Card>();
            Characters = new List<Card>();
            Lessons    = new List<Card>();
            Creatures  = new List<Card>();
            Location   = new List<Card>();
            Match      = new List<Card>();
            Items      = new List<Card>();
            Adventure  = new List<Card>();
        }
        
        public void  Initialize()
        {
            ResetState();
            
            foreach (var cardData in SelectedDeck.Cards.Where(c => c != null))
            {
                var card = new Card(cardData, this);

                AllCards.Add(card);
                Deck.Add(card);
            }

            if (SelectedDeck.StartingCharacter != null)
            {
                var startingCharacter = new Card(SelectedDeck.StartingCharacter, this, Zones.Characters);
                
                Characters.Add(startingCharacter);
                AllCards.Add(startingCharacter);
            }
        }
        
        public HashSet<LessonType> LessonTypes => AllCards.Where(c => c.Zone.IsInPlay()).GetLessonTypes();

        public int LessonCount 
            => AllCards.Where(c => c.Zone.IsInPlay())
                .SelectMany(c => c.Attributes)
                .OfType<LessonProvider>()
                .Sum(p => p.Amount);

        public List<Card> CardsInPlay => AllCards.Where(c => c.Zone.IsInPlay()).ToList();

        public void ResetState()
        {
            ActionsAvailable = 0;
            
            AllCards.Clear();
            Deck.Clear();
            Discard.Clear();   
            Hand.Clear();      
            Characters.Clear();
            Lessons.Clear();   
            Creatures.Clear(); 
            Location.Clear();  
            Match.Clear();     
            Items.Clear();     
            Adventure.Clear();
        }
    }
}