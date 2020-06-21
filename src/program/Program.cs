using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TemplateBuilder;

namespace program
{
    class Program
    {
        static void Main(string[] args)
        {
            var xsdPath = "http://schemas.simplement-e.com/sys/template.xsd";
            TemplateUtility util = new TemplateUtility(xsdPath, Directory.GetCurrentDirectory());

            util.ProcessStepStart += Validation_stepStarted;
            util.ProcessStepCompletion += Validation_stepCompleted;
            //util.ProcessStepAnomaly += Validation_stepAnomaly; 
            util.ProcessStart += Validation_ProcessStarted;
            util.ProcessCompletion += Validation_ProcessCompleted;
            var result = util.StartTemplateValidationProcess();

            if (result.IsSuccess)
                util.StartZipProcess("final.zip");
            else
                throw new Exception();


        }

        public static void Validation_ProcessStarted(object sender, ProcessStartArgs e)
        {
            Console.WriteLine("Début du process de validation du template {0} le {1} à {2}", e.Libelle, e.StartTime.ToString("MM/dd/yyyy"), e.StartTime.ToString("HH:mm"));
        }
        public static void Validation_ProcessCompleted(object sender, ProcessCompletionArgs e)
        {
            Console.WriteLine("{0} de l'{1} le {2} à {3} | Durée totale : {4} secondes", e.IsSuccess ? "Succès" : "Echec", e.Libelle, e.CompletionTime.ToString("MM/dd/yyyy"), e.CompletionTime.ToString("HH:mm"), (e.CompletionTime - e.StartTime).TotalSeconds);
            if (!e.IsSuccess)
            {
                Console.WriteLine("{0} anomalies dont {1} critiques", e.StepProcess.Sum(sp => sp.Anomalies.Count), e.StepProcess.Sum(sp => sp.Anomalies.Where(anom => anom.IsError).Count()));
            }
            Console.WriteLine("- - - - - -   - - - - - - - - ");
            Console.WriteLine("");
        }




        public static void Validation_stepStarted(object sender, ProcessStepStartArgs e)
        {
            Console.WriteLine(" |Début de : {0} le {1} à {2}", e.Libelle, e.StartTime.ToString("MM/dd/yyyy"), e.StartTime.ToString("HH:mm"));
        }
        public static void Validation_stepAnomaly(object sender, ProcessAnomaly e)
        {
            Console.ForegroundColor = e.IsError ? ConsoleColor.Red : ConsoleColor.Yellow;
            Console.WriteLine("[!] Anomalie {0} détectée !", e.IsError ? "Critique" : "Secondaire");
            Console.WriteLine("  - " + e.Message);
            Console.ResetColor();
        }
        public static void Validation_stepCompleted(object sender, ProcessStepCompletionArgs e)
        {
            Console.WriteLine(" |{0} de : {1} le {2} à {3} | Durée totale : {4} secondes", e.IsSuccess ? "Succès" : "Echec", e.Libelle, e.CompletionTime.ToString("MM/dd/yyyy"), e.CompletionTime.ToString("HH:mm"), (e.CompletionTime - e.StartTime).TotalSeconds);
            if (!e.IsSuccess)
            {
                Console.WriteLine(" |{0} anomalies dont {1} critiques", e.Anomalies.Count, e.Anomalies.Where(x => x.IsError).Count());
                for (int i = 0; i < e.Anomalies.Count; i++)
                {
                    Console.ForegroundColor = e.Anomalies[i].IsError ? ConsoleColor.Red : ConsoleColor.Yellow;
                    Console.WriteLine(" |[" + (i + 1) + "] " + e.Anomalies[i].Message);
                    Console.ResetColor();
                }
            }
            Console.WriteLine("");

        }
    }
}
