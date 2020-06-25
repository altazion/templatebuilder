﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using TemplateBuilder;

namespace program
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = "http://schemas.altazion.com/sys/template.xsd";
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), @"schema.xsd");

            if (File.Exists(savePath))
                File.Delete(savePath);

            using (HttpClient client = new HttpClient())
            {
                var response = client.GetAsync(url).Result;

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var content = response.Content.ReadAsStringAsync().Result;

                    System.IO.File.WriteAllText(savePath, content);
                }
                else
                    throw new Exception("Url du schema invalide");

            }

            TemplateUtility util = new TemplateUtility(savePath, Directory.GetCurrentDirectory());
            //TemplateUtility util = new TemplateUtility(savePath, @"d:/ttt/interactive-template-dummy");

            util.ProcessStepStart += Validation_stepStarted;
            util.ProcessStepCompletion += Validation_stepCompleted;
            //util.ProcessStepAnomaly += Validation_stepAnomaly; 
            util.ProcessStart += Validation_ProcessStarted;
            util.ProcessCompletion += Validation_ProcessCompleted;
            var result = util.StartTemplateValidationProcess();

            if (result.IsSuccess)
                util.StartZipProcess("final.zip");
            else
                throw new Exception("Echec du traitement de votre template");


        }

        public static void Validation_ProcessStarted(object sender, ProcessStartArgs e)
        {
            Console.WriteLine("Début du process de validation du template {0} le {1} à {2}", e.Libelle, e.StartTime.ToString("MM/dd/yyyy"), e.StartTime.ToString("HH:mm"));
        }
        public static void Validation_ProcessCompleted(object sender, ProcessCompletionArgs e)
        {
            var def = Console.ForegroundColor;
            if (e.IsSuccess)
                Console.ForegroundColor = ConsoleColor.Green;
            else
                Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine("{0} de l'{1} le {2} à {3} | Durée totale : {4} secondes", e.IsSuccess ? "Succès" : "Echec", e.Libelle, e.CompletionTime.ToString("MM/dd/yyyy"), e.CompletionTime.ToString("HH:mm"), (e.CompletionTime - e.StartTime).TotalSeconds);
            if (!e.IsSuccess)
            {
                Console.WriteLine("{0} anomalies dont {1} critiques", e.StepProcess.Sum(sp => sp.Anomalies.Count), e.StepProcess.Sum(sp => sp.Anomalies.Where(anom => anom.IsError).Count()));
            }
            Console.WriteLine("- - - - - -   - - - - - - - - ");
            Console.WriteLine("");

            Console.ForegroundColor = def;
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
