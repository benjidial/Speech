using System;
using System.Collections.Generic;
using System.IO;

namespace Benji.Speech
{
  class Person
  {
    class Fact
    {
      public static readonly List<Fact> facts;
      public readonly string tellFirst, tellFirstNegative, tellThirdStart, tellThirdStartNegative, tellThirdEnd, tellThirdEndNegative, askSecond, askThirdStart, askThirdEnd;

      private Fact(string tF, string tFN, string tTS, string tTSN, string tTE, string tTEN, string aS, string aTS, string aTE)
      {
        tellFirst = tF;
        tellFirstNegative = tFN;
        tellThirdStart = tTS;
        tellThirdStartNegative = tTSN;
        tellThirdEnd = tTE;
        tellThirdEndNegative = tTEN;
        askSecond = aS;
        askThirdStart = aTS;
        askThirdEnd = aTE;
      }

      static Fact( )
      {
        StreamReader r = File.OpenText("facts.cfg");
        facts = new List<Fact>();
        while (!r.EndOfStream)
          facts.Add(new Fact(r.ReadLine(), r.ReadLine(), r.ReadLine(), r.ReadLine(), r.ReadLine(), r.ReadLine(), r.ReadLine(), r.ReadLine(), r.ReadLine()));
        r.Close();
      }
    }

    private static readonly List<string> names;
    private readonly List<Fact> facts;
    private readonly string name;
    private Dictionary<Person, double> personOpinions = new Dictionary<Person, double>();
    private Dictionary<Fact, double> factOpinions = new Dictionary<Fact, double>();
    private Dictionary<Person, string> nameKnowledge = new Dictionary<Person, string>();
    private Dictionary<string, Person> nameKnowledgeReverse = new Dictionary<string, Person>();
    private Dictionary<Tuple<Person, Fact>, bool> factKnowledge = new Dictionary<Tuple<Person, Fact>, bool>();
    private Person conversation = null;
    public static Person player;
    private Queue<string> sayings = new Queue<string>();

    public Person( )
    {
      facts = new List<Fact>();
      name = names[Program.prng.Next(names.Count)];
      do
        facts.Add(Fact.facts[Program.prng.Next(Fact.facts.Count)]);
      while (Program.prng.NextDouble() > 0.3);
      foreach (Person p in Program.people)
      {
        personOpinions.Add(p, 1.0);
        p.personOpinions.Add(this, 1.0);
      }
      foreach (Fact f in Fact.facts)
        factOpinions.Add(f, facts.Contains(f) ? 2 : 1);
    }

    private Person(bool b) { }

    static Person( )
    {
      names = new List<string>(File.ReadLines("names.cfg"));
      player = new Person(false);
    }

    public void Step( )
    {
      if (conversation != null)
      {
        string s;
        try { s = conversation == player ? Program.sayings.Dequeue() : conversation.Listen(); }
        catch (InvalidOperationException) { s = null; }
        if (s != null)
        {
          foreach (Fact fact in Fact.facts)
            if (s == fact.tellFirst)
            {
              factKnowledge.Add(new Tuple<Person, Fact>(conversation, fact), true);
              if (factOpinions[fact] < 1)
                personOpinions[conversation] /= 2;
              else if (factOpinions[fact] > 1)
                personOpinions[conversation] *= 2;
              if (personOpinions[conversation] < 1)
                factOpinions[fact] /= 2;
              else if (personOpinions[conversation] > 1)
                factOpinions[fact] *= 2;
              sayings.Enqueue(facts[Program.prng.Next(facts.Count)].tellFirst);
              goto newConversation;
            }
            else if (s == fact.tellFirstNegative)
            {
              factKnowledge.Add(new Tuple<Person, Fact>(conversation, fact), false);
              if (factOpinions[fact] > 1)
                personOpinions[conversation] /= 2;
              if (personOpinions[conversation] > 1)
                factOpinions[fact] /= 2;
              sayings.Enqueue(facts[Program.prng.Next(facts.Count)].askSecond);
              goto newConversation;
            }
            else if (s.StartsWith(fact.tellThirdStart) && s.StartsWith(fact.tellThirdEnd))
            {
              string n = s.Substring(fact.tellThirdStart.Length, s.Length - fact.tellThirdStart.Length - fact.tellThirdEnd.Length);
              if (nameKnowledge.ContainsValue(n))
              {
                Person p = nameKnowledgeReverse[n];
                factKnowledge.Add(new Tuple<Person, Fact>(p, fact), true);
                if (factOpinions[fact] < 1)
                  personOpinions[p] /= 2;
                else if (factOpinions[fact] > 1)
                  personOpinions[p] *= 2;
                if (personOpinions[p] < 1)
                  factOpinions[fact] /= 2;
                else if (personOpinions[p] > 1)
                  factOpinions[fact] *= 2;
                Tuple<Person, Fact> otherFact = new List<Tuple<Person, Fact>>(factKnowledge.Keys)[Program.prng.Next(factKnowledge.Count)];
                sayings.Enqueue(factKnowledge[otherFact] ? otherFact.Item2.tellThirdStart + nameKnowledge[otherFact.Item1] + otherFact.Item2.tellThirdEnd : otherFact.Item2.tellThirdStartNegative + nameKnowledge[otherFact.Item1] + otherFact.Item2.tellThirdEndNegative);
              }
              else
                sayings.Enqueue("I don't know " + n + ".");
              goto newConversation;
            }
            else if (s.StartsWith(fact.tellThirdStartNegative) && s.StartsWith(fact.tellThirdEndNegative))
            {
              string n = s.Substring(fact.tellThirdStartNegative.Length, s.Length - fact.tellThirdStartNegative.Length - fact.tellThirdEndNegative.Length);
              if (nameKnowledge.ContainsValue(n))
              {
                Person p = nameKnowledgeReverse[n];
                factKnowledge.Add(new Tuple<Person, Fact>(p, fact), false);
                if (factOpinions[fact] > 1)
                  personOpinions[p] /= 2;
                if (personOpinions[p] > 1)
                  factOpinions[fact] /= 2;
                Tuple<Person, Fact> otherFact = new List<Tuple<Person, Fact>>(factKnowledge.Keys)[Program.prng.Next(factKnowledge.Count)];
                sayings.Enqueue(factKnowledge[otherFact] ? otherFact.Item2.tellThirdStart + nameKnowledge[otherFact.Item1] + otherFact.Item2.tellThirdEnd : otherFact.Item2.tellThirdStartNegative + nameKnowledge[otherFact.Item1] + otherFact.Item2.tellThirdEndNegative);
              }
              else
                sayings.Enqueue("I don't know " + n + ".");
              goto newConversation;
            }
            else if (s == fact.askSecond)
            {
              sayings.Enqueue(facts.Contains(fact) ? fact.tellFirst : fact.tellFirstNegative);
              goto newConversation;
            }
            else if (s.StartsWith(fact.askThirdStart) && s.StartsWith(fact.askThirdEnd))
            {
              string n = s.Substring(fact.askThirdStart.Length, s.Length - fact.askThirdStart.Length - fact.askThirdEnd.Length);
              if (nameKnowledge.ContainsValue(n))
              {
                bool does;
                sayings.Enqueue(factKnowledge.TryGetValue(new Tuple<Person, Fact>(nameKnowledgeReverse[n], fact), out does) ? does ? fact.tellThirdStart + n + fact.tellThirdEnd : fact.tellThirdStartNegative + n + fact.tellThirdEndNegative : "I don't know.");
              }
              else
                sayings.Enqueue("I don't know " + n + ".");
              goto newConversation;
            }
          if (s.StartsWith("Are you ") && s.EndsWith("?"))
            sayings.Enqueue("I am " + name + ".");
          else if (s.StartsWith("I am ") && s.EndsWith("."))
          {
            string n = s.Substring(5, s.Length - 6);
            nameKnowledge.Add(conversation, n);
            nameKnowledgeReverse.Add(n, conversation);
            sayings.Enqueue(Program.prng.Next(2) == 0 ? "I am " + name + "." : facts[Program.prng.Next(facts.Count)].askSecond);
          }
          else if (s == "Who are you?")
            sayings.Enqueue("I am " + name + ".");
          else if (s.StartsWith("Do you like ") && s.EndsWith("?"))
          {
            string n = s.Substring(12, s.Length - 13);
            if (nameKnowledge.ContainsValue(n))
            {
              Person p = nameKnowledgeReverse[n];
              sayings.Enqueue((personOpinions[p] < 1 ? "I do not like " : personOpinions[p] > 1 ? "I like " : "I am neutral about ") + n + ".");
            }
            else
              sayings.Enqueue("I don't know " + n + ".");
          }
          else if (s.StartsWith("I like ") && s.EndsWith("."))
          {
            string n = s.Substring(7, s.Length - 8);
            if (nameKnowledge.ContainsValue(n))
            {
              Person p = nameKnowledgeReverse[n];
              if (personOpinions[p] > 1)
                personOpinions[conversation] *= 2;
              else if (personOpinions[p] < 1)
                personOpinions[conversation] /= 2;
              else if (personOpinions[conversation] > 1)
                personOpinions[p] *= 2;
              sayings.Enqueue((personOpinions[p] < 1 ? "I do not like " : personOpinions[p] > 1 ? "I like " : "I am neutral about ") + n + ".");
            }
            else
              sayings.Enqueue("I don't know " + n + ".");
          }
          else if (s.StartsWith("I don't like") && s.EndsWith("."))
          {
            string n = s.Substring(13, s.Length - 14);
            if (nameKnowledge.ContainsValue(n))
            {
              Person p = nameKnowledgeReverse[n];
              if (personOpinions[p] > 1)
                personOpinions[conversation] /= 2;
              else if (personOpinions[p] < 1)
                personOpinions[conversation] *= 2;
              else if (personOpinions[conversation] > 1)
                personOpinions[p] /= 2;
              sayings.Enqueue((personOpinions[p] < 1 ? "I do not like " : personOpinions[p] > 1 ? "I like " : "I am neutral about ") + n + ".");
            }
            else
              sayings.Enqueue("I don't know " + n + ".");
          }
          else if (s.StartsWith("I am neutral about ") && s.EndsWith("."))
          {
            string n = s.Substring(13, s.Length - 14);
            if (nameKnowledge.ContainsValue(n))
            {
              Person p = nameKnowledgeReverse[n];
              sayings.Enqueue((personOpinions[p] < 1 ? "I do not like " : personOpinions[p] > 1 ? "I like " : "I am neutral about ") + n + ".");
            }
            else
              sayings.Enqueue("I don't know " + n + ".");
          }
          else if (s.StartsWith("I don't know ") && s.EndsWith("."))
            sayings.Enqueue(facts[Program.prng.Next(facts.Count)].askSecond);
        }
      newConversation:
        if (Program.prng.NextDouble() < 0.075)
          conversation = null;
      }
      if (conversation == null && Program.prng.NextDouble() > 0.75)
        conversation = Program.people[Program.prng.Next(Program.people.Count)];
    }

    public string Listen( )
    {
      return sayings.Dequeue();
    }
  }

  class Program
  {
    public static Random prng = new Random();

    public static List<Person> people = new List<Person>();
    private static List<string> names = new List<string>();

    public static Queue<string> sayings = new Queue<string>();

    static int Main( )
    {
      people.Add(Person.player);
      names.Add(null);
      Console.WriteLine("Talk: T\nListen: L\nQuit: Q\nWait: Any");
      while (true)
      {
        while (prng.NextDouble() < 0.1)
        {
          people.Add(new Person());
          names.Add(null);
          Console.WriteLine("Someone walked in.");
        }
        while (prng.NextDouble() < 0.1 && people.Count != 1)
        {
          int i = prng.Next(people.Count - 1) + 1;
          Console.WriteLine("{0} walked out.", names[i] ?? "Someone");
          people.RemoveAt(i);
          names.RemoveAt(i);
        }
        foreach (Person person in people)
          person.Step();

        switch (Console.ReadKey(true).Key)
        {
        case ConsoleKey.T:
          sayings.Enqueue(Console.ReadLine());
          break;
        case ConsoleKey.L:
          string n = Console.ReadLine();
          try { Console.WriteLine(people[n == "" ? prng.Next(people.Count - 1) + 1 : names.IndexOf(n)].Listen()); }
          catch (InvalidOperationException) { }
          break;
        case ConsoleKey.Q:
          return 0;
        }
      }
    }
  }
}
