using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using TemplateBuilder.Properties;

namespace TemplateBuilder
{

    public class TemplateUtility
    {

        public event EventHandler<ProcessStepCompletionArgs> ProcessStepCompletion;
        protected virtual void OnProcessStepCompletion(ProcessStepCompletionArgs e) => ProcessStepCompletion?.Invoke(this, e);


        public event EventHandler<ProcessStepStartArgs> ProcessStepStart;
        protected virtual void OnProcessStepStart(ProcessStepStartArgs e) => ProcessStepStart?.Invoke(this, e);


        public event EventHandler<ProcessAnomaly> ProcessStepAnomaly;
        protected virtual void OnProcessStepAnomaly(ProcessAnomaly e) => ProcessStepAnomaly?.Invoke(this, e);



        public event EventHandler<ProcessStartArgs> ProcessStart;
        protected virtual void OnProcessStart(ProcessStartArgs e) => ProcessStart?.Invoke(this, e);


        public event EventHandler<ProcessCompletionArgs> ProcessCompletion;
        protected virtual void OnProcessCompletion(ProcessCompletionArgs e) => ProcessCompletion?.Invoke(this, e);

        private string _repositoryRegex = @"([\S]+)\/([\S]+)";
        private string _tempGithubFolder;

        private ProcessStartArgs _processStartArgs;
        private ProcessCompletionArgs _processCompletionArgs;
        private string[] _unusedFiles { get; set; }

        public TemplateUtility(string schemaUri, string path)
        {
            if (string.IsNullOrEmpty(schemaUri))
                throw new ArgumentException("Le chemin du schéma est vide.");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("L'url du repository du template est vide.");

            if (!IsSchemaAvailable(schemaUri))
                throw new ArgumentException("Impossible de récupérer le schéma.");

            _processStartArgs = new ProcessStartArgs()
            {
                SchemaUri = schemaUri,
                TemplatePath = path,
                Libelle = string.Format(TemplateValidatorResources.Process_Title, path)
            };
        }




        public TemplateValidationResult StartTemplateValidationProcess()
        {
            _unusedFiles = new string[] { };
            _processStartArgs.StartTime = DateTime.Now;
            OnProcessStart(_processStartArgs);
            _processCompletionArgs = new ProcessCompletionArgs(_processStartArgs);

            _processCompletionArgs.XmlDocumentInformation = GetContent();
            if (_processCompletionArgs.XmlDocumentInformation.IsLoaded)
                if (ValidateXml())
                    ValidateContent();
            else
                {
                    throw new ArgumentException("Impossible de récupérer le xml.");
                }


            _processCompletionArgs.CompletionTime = DateTime.Now;
            _processCompletionArgs.IsSuccess = _processCompletionArgs.StepProcess.Where(x => !x.IsSuccess).Count() == 0;
            OnProcessCompletion(_processCompletionArgs);

            var result = new TemplateValidationResult()
            {
                DurationInSeconds = (_processCompletionArgs.CompletionTime - _processCompletionArgs.StartTime).TotalSeconds,
                Libelle = _processCompletionArgs.Libelle,
                IsSuccess = _processCompletionArgs.StepProcess.Where(x => !x.IsSuccess).Count() == 0,
                UnusedFiles = _unusedFiles,
            };
            foreach (ProcessStepCompletionArgs scargs in _processCompletionArgs.StepProcess)
            {
                var scr = new StepValidationResult(scargs);
                result.Steps.Add(scr);
            }
            return result;
        }
        public bool StartZipProcess(string zipFileName, bool ignoreUnusedFiles = true)
        {
            return StartZipProcess(_processStartArgs.TemplatePath, zipFileName, ignoreUnusedFiles);
        }
        public bool StartZipProcess(string from, string zipFileName,  bool ignoreUnusedFiles = true)
        {
            var fullTargetPath = zipFileName;
            if (Path.GetExtension(fullTargetPath) == string.Empty)
                throw new Exception("Fichier non précisé dans le chemin d'archivage");


            if (File.Exists(fullTargetPath))
                File.Delete(fullTargetPath);

            if (ignoreUnusedFiles && _unusedFiles != null && _unusedFiles.Length > 0)
            {
                var tempDir = Path.Combine(from, @"alt_temp");
                //var dirs = Directory.GetDirectories(from, "*", SearchOption.AllDirectories);
                try
                {
                    var so = new DirectoryInfo(from);
                    var te = new DirectoryInfo(tempDir);
                    CopyAll(so, te);
                }
                catch (Exception e)
                {
                    Directory.Delete(tempDir, true);
                    throw e;
                }
                ZipFile.CreateFromDirectory(tempDir, fullTargetPath);
                Directory.Delete(tempDir, true);
                return true;
            }

            ZipFile.CreateFromDirectory(from, fullTargetPath);
            return true;
        }
        private void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {

            if (!Directory.Exists(target.FullName))
                Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                if (_unusedFiles.Contains(fi.FullName.ToLower())) continue;
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                if (diSourceSubDir.FullName.Equals(target.FullName))
                    continue;
                bool hasUsedFiles = false;
                foreach (FileInfo fi in diSourceSubDir.GetFiles())
                {
                    if (_unusedFiles.Contains(fi.FullName.ToLower())) continue;
                    hasUsedFiles = true;
                }
                if (hasUsedFiles)
                {
                    DirectoryInfo nextTargetSubDir =
                        target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyAll(diSourceSubDir, nextTargetSubDir);
                }
            }
        }

        private bool ValidateXml()
        {
            var startArgs = new ProcessStepStartArgs()
            {
                StartTime = DateTime.Now,
                Libelle = TemplateValidatorResources.XmlValidation_Title,
            };
            var completeArgs = new ProcessStepCompletionArgs(startArgs)
            {
                IsSuccess = true
            };


            _processCompletionArgs.StepProcess.Add(completeArgs);
            OnProcessStepStart(startArgs);


            XmlSchemaSet sc = new XmlSchemaSet();
            sc.Add(null, _processStartArgs.SchemaUri);
            XmlReaderSettings settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = sc
            };


            settings.ValidationEventHandler += (s, e) => AddError(completeArgs, e.Message, true);

            using (XmlReader reader = XmlReader.Create(_processCompletionArgs.XmlDocumentInformation.FileCompletePath, settings))
            {
                while (reader.Read()) ;
            }
            completeArgs.CompletionTime = DateTime.Now;
            OnProcessStepCompletion(completeArgs);
            return completeArgs.IsSuccess;
        }
        private bool ValidateContent()
        {
            var startArgs = new ProcessStepStartArgs()
            {
                StartTime = DateTime.Now,
                Libelle = TemplateValidatorResources.ContentValidation_Title,
            };
            var completeArgs = new ProcessStepCompletionArgs(startArgs)
            {
                IsSuccess = true
            };
            _processCompletionArgs.StepProcess.Add(completeArgs);

            OnProcessStepStart(startArgs);

            var rootPath = _processStartArgs.TemplatePath;
            string[] fileEntries;
            List<string> unusedFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories).Select(x => x.ToLowerInvariant()).ToList();
            unusedFiles.Remove(_processCompletionArgs.XmlDocumentInformation.FileCompletePath.ToLowerInvariant()); // On remove le .xml racine.
            XmlDocument doc = _processCompletionArgs.XmlDocumentInformation.Document;

            var contentNode = doc.DocumentElement.GetElementsByTagName("Content");
            var rootAttribute = contentNode[0].Attributes.GetNamedItem("root");

            if (!rootAttribute.Value.Equals("."))
                rootPath = Path.Combine(_processStartArgs.TemplatePath, rootAttribute.Value);


            var variationsElements = doc.DocumentElement.GetElementsByTagName("Variation");

            if (variationsElements.Count == 0)
                AddError(completeArgs, TemplateValidatorResources.ContentValidation_AucuneVariation, true);
            else
            {
                foreach (XmlNode n in variationsElements)
                {
                    var variationLabel = n.Attributes.GetNamedItem("label").Value;
                    foreach (XmlNode t in n.ChildNodes)
                    {
                        var variationContentKind = t.Attributes.GetNamedItem("kind").Value;

                        if (!t.Name.Equals("VariationContent")) continue;

                        if (t.ChildNodes.Cast<XmlNode>().Where(p => p.Name.Equals("Folder")).Count() == 1)
                        {
                            bool htmlFound = false;
                            var folderNode = t.ChildNodes.Cast<XmlNode>().Where(p => p.Name.Equals("Folder")).FirstOrDefault();
                            var partPath = folderNode.Attributes.GetNamedItem("path").Value;
                            var currDir = Path.Combine(rootPath, partPath);
                            if (Directory.Exists(currDir))
                                fileEntries = Directory.GetFiles(currDir, "*", SearchOption.AllDirectories);
                            else
                            {
                                AddError(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_DossierNonExistant, currDir), true);
                                continue;
                            }

                            for (int i = 0; i < fileEntries.Length; i++)
                            {
                                var fileName = GetFormatedName(fileEntries[i].ToLowerInvariant());
                                if (!fileName.Contains(t.Attributes.GetNamedItem("kind").Value.ToLowerInvariant())) continue;
                                unusedFiles.Remove(fileName);
                                switch (Path.GetExtension(fileName).ToLower())
                                {
                                    case ".html":
                                        htmlFound = true;
                                        break;
                                    case ".css":
                                        break;
                                    case ".js":
                                        break;
                                }

                            }
                            if (!htmlFound)
                            {
                                AddError(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierHtmlNonExistant, variationContentKind), true);
                            }
                        }
                        else if (t.ChildNodes.Cast<XmlNode>().Where(p => p.Name.Equals("File")).Count() >= 1)
                        {
                            var fileNodes = t.ChildNodes.Cast<XmlNode>().Where(p => p.Name.Equals("File")).ToList();
                            var dir = Path.Combine(rootPath, variationLabel);
                            var filesDirFounded = false;

                            for (int i = 0; i < fileNodes.Count(); i++)
                            {
                                if (!filesDirFounded && File.Exists(Path.Combine(rootPath, fileNodes[i].Attributes.GetNamedItem("path").Value)))
                                    fileEntries = Directory.GetFiles(rootPath, fileNodes[i].Attributes.GetNamedItem("path").Value);
                                else
                                {
                                    AddError(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierNonExistant, fileNodes[i].Attributes.GetNamedItem("path").Value, variationContentKind), true);
                                    continue;
                                }

                                var fileAttributeValue = fileNodes[i].Attributes.GetNamedItem("path").Value;
                                bool fileFound = false;
                                for (int z = 0; z < fileEntries.Length; z++)
                                {
                                    var fileName = GetFormatedName(fileEntries[z].ToLowerInvariant());
                                    unusedFiles.Remove(fileName);
                                    switch (Path.GetExtension(fileName).ToLower())
                                    {
                                        case ".html":
                                            fileFound = true;
                                            break;
                                        case ".css":
                                            fileFound = true;
                                            break;
                                        case ".js":
                                            fileFound = true;
                                            break;
                                    }

                                }
                                if (!fileFound)
                                    AddError(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierNonExistant, fileNodes[i].Attributes.GetNamedItem("path").Value, variationContentKind), true);
                            }
                        }
                    }
                }
            }

            foreach (XmlNode t in doc.DocumentElement.GetElementsByTagName("Shared"))
            {
                var sharedKind = t.Attributes.GetNamedItem("kind").Value.ToLower();


                if (t.ChildNodes.Cast<XmlNode>().Where(p => p.Name.Equals("Folder")).Count() == 1)
                {
                    var folderNode = t.ChildNodes.Cast<XmlNode>().Where(p => p.Name.Equals("Folder")).FirstOrDefault();
                    var path = folderNode.Attributes.GetNamedItem("path").Value;
                    var currDir = Path.Combine(rootPath, path);
                    if (Directory.Exists(currDir))
                        fileEntries = Directory.GetFiles(currDir, "*", SearchOption.AllDirectories);
                    else
                    {
                        AddError(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_DossierNonExistant, currDir), true);
                        continue;
                    }


                    for (int i = 0; i < fileEntries.Length; i++)
                    {
                        bool kindFound = false;
                        var fileName = GetFormatedName(fileEntries[i].ToLowerInvariant());
                        if (!fileName.Contains(t.Attributes.GetNamedItem("kind").Value.ToLowerInvariant())) continue;
                        switch (Path.GetExtension(fileName))
                        {
                            case ".html":
                                if (sharedKind.Equals("html"))
                                    kindFound = true;
                                break;
                            case ".css":
                                if (sharedKind.Equals("css"))
                                    kindFound = true;
                                break;
                            case ".js":
                                if (sharedKind.Equals("js"))
                                    kindFound = true;
                                break;
                        }
                        if (kindFound)
                            unusedFiles.Remove(fileName);
                    }
                }
                else if (t.ChildNodes.Cast<XmlNode>().Where(p => p.Name.Equals("File")).Count() >= 1)
                {
                    var fileNodes = t.ChildNodes.Cast<XmlNode>().Where(p => p.Name.Equals("File")).ToList();
                    var dir = Path.Combine(rootPath);
                    var filesDirFounded = false;

                    for (int i = 0; i < fileNodes.Count(); i++)
                    {
                        if (!filesDirFounded && File.Exists(Path.Combine(rootPath, fileNodes[i].Attributes.GetNamedItem("path").Value)))
                            fileEntries = Directory.GetFiles(rootPath, fileNodes[i].Attributes.GetNamedItem("path").Value);
                        else
                        {
                            AddError(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierNonExistant, fileNodes[i].Attributes.GetNamedItem("path").Value, sharedKind), true);
                            continue;
                        }

                        var fileAttributeValue = fileNodes[i].Attributes.GetNamedItem("path").Value;
                        bool fileFound = false;
                        for (int z = 0; z < fileEntries.Length; z++)
                        {
                            var fileName = GetFormatedName(fileEntries[z].ToLowerInvariant());
                            switch (Path.GetExtension(fileName).ToLower())
                            {
                                case ".html":
                                    if (sharedKind.Equals("html"))
                                    {
                                        fileFound = true;
                                        unusedFiles.Remove(fileName);
                                    }
                                    break;
                                case ".css":
                                    if (sharedKind.Equals("css"))
                                    {
                                        fileFound = true;
                                        unusedFiles.Remove(fileName);
                                    }
                                    break;
                                case ".js":
                                    if (sharedKind.Equals("js"))
                                    {
                                        fileFound = true;
                                        unusedFiles.Remove(fileName);
                                    }
                                    break;
                            }
                            if (fileFound)
                                break;
                        }
                        if (!fileFound)
                            AddError(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierNonExistant, fileNodes[i].Attributes.GetNamedItem("path").Value, sharedKind), true);
                    }
                }
            }

            for (int i = 0; i < unusedFiles.Count; i++)
                AddError(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierInutile, unusedFiles[i]), false);


            _unusedFiles = unusedFiles.ToArray();
            completeArgs.CompletionTime = DateTime.Now;
            OnProcessStepCompletion(completeArgs);
            return completeArgs.IsSuccess;

        }

        private XmlDocumentInformation GetContent()
        {
            var startArgs = new ProcessStepStartArgs()
            {
                StartTime = DateTime.Now,
                Libelle = TemplateValidatorResources.XmlRecuperation_Title,
            };

            OnProcessStepStart(startArgs);

            var completeArgs = new ProcessStepCompletionArgs(startArgs)
            {
                IsSuccess = true
            };

            _processCompletionArgs.StepProcess.Add(completeArgs);



            XmlDocumentInformation xdi = new XmlDocumentInformation();
            string[] rootFiles = Directory.GetFiles(_processStartArgs.TemplatePath, "*.xml", SearchOption.TopDirectoryOnly);

            // Non pas zéro, pas deux, pas dix, pas cent, pas mille, pas mille deux cent dix mais un .xml !
            if (rootFiles.Length != 1)
            {
                if (rootFiles.Length > 1)
                    AddError(completeArgs, TemplateValidatorResources.XmlValidation_TooManyXml, true);
                else if (rootFiles.Length == 0)
                    AddError(completeArgs, TemplateValidatorResources.XmlValidation_NoXml, true);

                completeArgs.CompletionTime = DateTime.Now;
                OnProcessStepCompletion(completeArgs);
                return xdi;
            }

            XmlDocument doc = new XmlDocument();

            foreach (string fileName in rootFiles)
            {
                var completePath = Path.Combine(_processStartArgs.TemplatePath, Path.GetFileName(fileName));
                switch (Path.GetExtension(fileName).ToLowerInvariant())
                {
                    case ".xml":
                        using (var entryStream = File.OpenRead(completePath))
                        {
                            try
                            {

                                doc.Load(entryStream);
                                xdi.FileCompletePath = completePath;
                                xdi.IsLoaded = true;
                            }
                            catch (Exception e) 
                            { 
                            }
                        }
                        break;
                }
            }

            xdi.Document = doc;

            completeArgs.CompletionTime = DateTime.Now;
            OnProcessStepCompletion(completeArgs);
            return xdi;
        }
        private void AddError(ProcessStepCompletionArgs datas, string message, bool isError)
        {
            datas.Anomalies.Add(new ProcessAnomaly()
            {
                IsError = isError,
                Message = message
            });
            if (isError)
                datas.IsSuccess = false;

            OnProcessStepAnomaly(datas.Anomalies.Last());
        }

        private string GetFormatedName(string name)
        {
            return name.Replace(@"/", @"\");
        }

        private bool IsSchemaAvailable(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var restponse = client.GetAsync(url).Result;

                return restponse.StatusCode == System.Net.HttpStatusCode.OK;
            }
        }


    }

    public class ProcessAnomaly
    {
        public bool IsError { get; set; }
        public string Message { get; set; }
    }
    public class XmlDocumentInformation
    {
        public bool IsLoaded { get; set; }
        public string FileCompletePath { get; set; }
        public XmlDocument Document { get; set; }
    }


    public class ProcessStepStartArgs : EventArgs
    {
        public string Libelle { get; set; }
        public DateTime StartTime { get; set; }

    }
    public class ProcessStepCompletionArgs : EventArgs
    {
        public string Libelle { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime CompletionTime { get; set; }
        public List<ProcessAnomaly> Anomalies { get; set; }
        public ProcessStepCompletionArgs() : base()
        {
            Anomalies = new List<ProcessAnomaly>();
        }
        public ProcessStepCompletionArgs(ProcessStepStartArgs startArgs) : base()
        {
            Libelle = startArgs.Libelle;
            StartTime = startArgs.StartTime;
            Anomalies = new List<ProcessAnomaly>();
        }
    }


    public class ProcessStartArgs : ProcessStepStartArgs
    {
        public string SchemaUri { get; set; }
        public string TemplatePath { get; set; }
    }
    public class ProcessCompletionArgs
    {
        public string Libelle { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime CompletionTime { get; set; }
        public string SchemaUri { get; set; }
        public string TemplatePath { get; set; }
        public XmlDocumentInformation XmlDocumentInformation { get; set; }
        public List<ProcessStepCompletionArgs> StepProcess { get; set; }

        public ProcessCompletionArgs() : base()
        {
            StepProcess = new List<ProcessStepCompletionArgs>();
        }
        public ProcessCompletionArgs(ProcessStartArgs startargs) : base()
        {
            StepProcess = new List<ProcessStepCompletionArgs>();
            Libelle = startargs.Libelle;
            SchemaUri = startargs.SchemaUri;
            TemplatePath = startargs.TemplatePath;
            StartTime = startargs.StartTime;
        }
    }
    public class TemplateValidationResult
    {
        public List<StepValidationResult> Steps { get; set; }
        public bool IsSuccess { get; set; }
        public string Libelle { get; set; }
        public double DurationInSeconds { get; set; }
        public string[] UnusedFiles { get; set; }
        public TemplateValidationResult()
        {
            Steps = new List<StepValidationResult>();
        }
    }
    public class StepValidationResult
    {
        public string Libelle { get; set; }
        public bool IsSuccess { get; set; }
        public List<ProcessAnomaly> Anomalies { get; set; }

        public StepValidationResult(ProcessStepCompletionArgs psca)
        {
            Libelle = psca.Libelle;
            IsSuccess = psca.IsSuccess;
            Anomalies = psca.Anomalies;
        }
    }


}
