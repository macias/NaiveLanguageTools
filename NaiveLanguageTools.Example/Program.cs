using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            bool reporting = false;

            //NaiveLanguageTools.ParsingTools.SymbolSets.BuilderSets.internalTest();

            while (true)
            {
                Console.WriteLine("Enter option:");
                Console.WriteLine("[1] introduction with calculator");
                Console.WriteLine("[2] patterns and forking");
                Console.WriteLine("[3] chemical formula");
                Console.WriteLine("[r] turn " + (reporting ? "off" : "on") + " detailed reports");
                Console.WriteLine();
                Console.Write("Enter empty line to quit: ");
                string option = Console.ReadLine();
                if (option == "1")
                    Calculator.Calculator.Run(reporting);
                else if (option == "2")
                    PatternsAndForking.PatternsAndForking.Run(reporting);
                else if (option == "3")
                    ChemicalFormula.ChemicalFormula.Run(reporting);
                else if (option == "r")
                    reporting = !reporting;
                else if (option == "")
                    break;

                else
                    Console.WriteLine("Unrecognized command: " + option);

                Console.WriteLine();
            }
        }
    }
}
