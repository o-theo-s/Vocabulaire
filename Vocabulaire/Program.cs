using System;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Text;
using System.Collections.Generic;

namespace Vocabulaire
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.UTF8;

            var defColor = Console.ForegroundColor;
            string filePath = null, wordsToTestReply;
            int testNum, corrects;
            Dictionary<string, string> mistakes = new Dictionary<string, string>();
            ConsoleKey repeat;

            if (args.Length > 0)
                filePath = Path.GetFullPath(args[0]);
            while (!File.Exists(filePath) || !filePath.EndsWith(".voc"))
            {
                Console.WriteLine("Entrer le fichier de vocabulaire :");
                string input = Console.ReadLine();
                try
                {
                    filePath = Path.GetFullPath(input);
                }
                catch (Exception)
                {
                    filePath = string.Empty;
                }
            }

            string[] lines = File.ReadAllLines(filePath);

            Console.WriteLine($"Test orthographique de : {Path.GetFileName(filePath)}");
            do
            {
                Console.WriteLine($"Combien de mots voulez-vous tester ({lines.Length} au total) ? ");
                wordsToTestReply = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(wordsToTestReply))
                {
                    testNum = lines.Length;
                    break;
                }
            } while (!int.TryParse(wordsToTestReply, out testNum));

            var w = lines.
                OrderBy(_ => Guid.NewGuid()).
                Where((l, i) => i < testNum).
                ToDictionary(l => l.Split(',').First().Trim(), l => l.Split(',').Last().Trim());

            do
            {
                Console.Clear();

                if (mistakes.Any())
                    w = mistakes.OrderBy(_ => Guid.NewGuid()).ToDictionary(m => m.Key, m => m.Value);

                mistakes = new Dictionary<string, string>();
                corrects = 0;

                foreach (var pair in w)
                {
                    string[] synonyms = pair.Value.Split('/').Select(s => s.Trim()).ToArray();

                    Console.WriteLine($"Que signifie le mot \" {pair.Key} \" {(synonyms.Length > 1 ? "(" + synonyms.Length + " synonymes au total) " : "")}?");

                    bool allSynonymsCorrect = true;
                    for (int i = 0; i < synonyms.Length; i++)
                    {
                        if (synonyms.Length > 1)
                            Console.WriteLine("Synonyme numero " + (i + 1) + " :");

                        string reply = Console.ReadLine().Trim();

                        if (synonyms.Contains(reply, StringComparer.InvariantCultureIgnoreCase))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Correct !\n");

                            Console.ForegroundColor = defColor;
                        }
                        else if (synonyms.Select(s => ToUnaccented(s)).Contains(reply, StringComparer.InvariantCultureIgnoreCase))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("Presque ! ");

                            Console.ForegroundColor = defColor;
                            Console.WriteLine($"Faites attention aux accents : \" {synonyms.First(s => ToUnaccented(s) == reply)} \" \n");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("Faux ! ");

                            Console.ForegroundColor = defColor;
                            Console.WriteLine($"La réponse correcte est \" {pair.Value} \".\n");

                            mistakes.Add(pair.Key, pair.Value);
                            allSynonymsCorrect = false;

                            break;
                        }
                    }

                    if (allSynonymsCorrect)
                        corrects++;
                }

                Console.ForegroundColor = corrects < w.Count / 2 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"\n***  Vous avez écrit {corrects}/{w.Count} mots correctement !  ***\n");
                Console.ForegroundColor = defColor;

                if (corrects == w.Count)
                    break;

                do
                {
                    Console.WriteLine("Voulez-vous répéter le test pour les mots incorrects ? (o/n)");
                    repeat = Console.ReadKey().Key;
                    Console.WriteLine("\n");
                } while (repeat != ConsoleKey.O && repeat != ConsoleKey.N);

            } while (repeat == ConsoleKey.O);

            Console.WriteLine("Appuyez sur une touche pour quitter...");
            Console.ReadKey();
        }

        public static string ToUnaccented(string str)
        {
            string fullCanonicalDecompositionNormalized = str.Normalize(NormalizationForm.FormD);
            var unaccented = fullCanonicalDecompositionNormalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);

            return new string(unaccented.ToArray());
        }
    }
}
