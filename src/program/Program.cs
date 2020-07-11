using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml;
using TemplateBuilder;

namespace program
{
    class Program
    {
        public const string TemplateXsd = "https://schemas.altazion.com/sys/template.xsd";
        public const string SlideXsd = "https://schemas.altazion.com/store/slide.xsd";

        public enum BuildKind
        {
            Template,
            Slide
        }

        public class BuildFolder
        {
            public string Folder { get; set; }
            public BuildKind Kind { get; set; }

        }
        static void Main(string[] args)
        {
            var folders = GetFolders();

            if (folders.Count == 0)
                throw new Exception("Il n'y a aucun élément à compiler");

            var t = (from z in folders where z.Kind == BuildKind.Template select z).FirstOrDefault();
            // pour rester identique : si il y a un template
            // on verifie qu'il n'y a rien d'autre
            if(t!=null && folders.Count==1)
                BuildTemplate(t.Folder);
            else
            {
                foreach(var slide in folders)
                {
                    if (slide.Kind == BuildKind.Slide)
                        BuildSlide(slide.Folder);
                }
            }
        }

        private static void BuildSlide(string folder)
        {
            
        }

        private static List<BuildFolder> GetFolders()
        {
            List<BuildFolder> ret = new List<BuildFolder>();

            ParseFolder(Directory.GetCurrentDirectory(), Directory.GetCurrentDirectory(), ret);


            return ret;
        }

        private static void ParseFolder(string rootPath, string path, List<BuildFolder> retArray)
        {
            BuildFolder fld = null;
            foreach (var file in Directory.GetFiles(path,"*.xml"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                if(doc.DocumentElement.NamespaceURI.Equals(SlideXsd, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (fld != null)
                        throw new ApplicationException("Il y a plusieurs fichiers .xml de compilation dans le dossier : " + path.Substring(rootPath.Length));
                    else
                    {
                        fld = new BuildFolder()
                        {
                            Folder = path,
                            Kind = BuildKind.Slide
                        };
                    }
                }
                else if (doc.DocumentElement.NamespaceURI.Equals(TemplateXsd, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (fld != null)
                        throw new ApplicationException("Il y a plusieurs fichiers .xml de compilation dans le dossier : " + path.Substring(rootPath.Length));
                    else
                    {
                        fld = new BuildFolder()
                        {
                            Folder = path,
                            Kind = BuildKind.Template
                        };
                    }
                }
            }
            if(fld!=null)
                retArray.Add(fld);

            foreach(var subfol in Directory.GetDirectories(path))
            {
                ParseFolder(rootPath, subfol, retArray);
            }

        }

        private static void BuildTemplate(string buildPath)
        {
            var url = TemplateXsd ;
            string savePath = DownloadSchema(url);

            TemplateUtility util = new TemplateUtility(savePath, buildPath);

            util.ProcessStepStart += Validation_stepStarted;
            util.ProcessStepCompletion += Validation_stepCompleted;
            util.ProcessStart += Validation_ProcessStarted;
            util.ProcessCompletion += Validation_ProcessCompleted;
            var result = util.StartTemplateValidationProcess();

            if (result.IsSuccess)
                util.StartZipProcess("final.zip");
            else
                throw new Exception("Echec du traitement de votre template");
        }

        private static string DownloadSchema(string url)
        {
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(url));
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

            return savePath;
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
