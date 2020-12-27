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
            var saveW = new Dictionary<string, string>(w);

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

                Console.Write("\n***  Vous avez écrit ");
                Console.ForegroundColor = corrects < w.Count / 2 ? ConsoleColor.Red : ConsoleColor.Green;
                Console.Write($"{corrects}/{w.Count}");
                Console.ForegroundColor = defColor;
                Console.WriteLine(" mots correctement !  ***\n");

                if (corrects == w.Count)
                    break;

                do
                {
                    Console.WriteLine("Voulez-vous répéter le test pour les mots incorrects ? (o/n)");
                    repeat = Console.ReadKey().Key;
                    Console.WriteLine("\n");
                } while (repeat != ConsoleKey.O && repeat != ConsoleKey.N);

            } while (repeat == ConsoleKey.O);

            ConsoleKey save = default;
            while (testNum < lines.Length && save != ConsoleKey.O && save != ConsoleKey.N)
            {
                Console.WriteLine("Voulez-vous enregistrer les mots de ce test pour révision ?");
                save = Console.ReadKey().Key;
                Console.WriteLine();
            }

            if (save == ConsoleKey.O)
            {
                string fileName = null;
                while (string.IsNullOrWhiteSpace(fileName))
                {
                    Console.WriteLine("Entrez le nom du fichier :");
                    fileName = Console.ReadLine().Trim();
                }
                
                if (fileName.Contains('.'))
                {
                    string[] tokens = fileName.Split('.');
                    fileName = string.Join("", fileName.Take(tokens.Length - 1));
                }

                try
                {
                    File.WriteAllLines(fileName + ".voc", saveW.Select(kv => kv.Key + ", " + kv.Value));
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Un problème est survenu lors de l'enregistrement du fichier");
                    Console.ForegroundColor = defColor;
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Le fichier a été enregistré avec succès");
                Console.ForegroundColor = defColor;

                Console.WriteLine();
            }

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
