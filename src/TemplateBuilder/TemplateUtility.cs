using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using TemplateBuilder.Properties;

namespace TemplateBuilder
{

    public class TemplateUtility
    {



        #region Events
        public event EventHandler<ProcessStartArgs> ProcessStart;
        protected virtual void OnProcessStart(ProcessStartArgs e) => ProcessStart?.Invoke(this, e);

        public event EventHandler<ProcessCompletionArgs> ProcessCompletion;
        protected virtual void OnProcessCompletion(ProcessCompletionArgs e) => ProcessCompletion?.Invoke(this, e);


        public event EventHandler<ProcessStepStartArgs> ProcessStepStart;
        protected virtual void OnProcessStepStart(ProcessStepStartArgs e) => ProcessStepStart?.Invoke(this, e);

        public event EventHandler<ProcessStepCompletionArgs> ProcessStepCompletion;
        protected virtual void OnProcessStepCompletion(ProcessStepCompletionArgs e) => ProcessStepCompletion?.Invoke(this, e);

        public event EventHandler<ProcessAnomaly> ProcessStepAnomaly;
        protected virtual void OnProcessStepAnomaly(ProcessAnomaly e) => ProcessStepAnomaly?.Invoke(this, e);


        private ProcessStartArgs _processStartArgs;
        private ProcessCompletionArgs _processCompletionArgs;
        private string temp_dir = @"alt_temp";
        #endregion

        private string[] _unusedFiles { get; set; }

        public TemplateUtility(string schemaPath, string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("L'url du repository du template est vide.");
            if (string.IsNullOrEmpty(schemaPath))
                throw new ArgumentException("Impossible de récupérer le schéma.");

            _processStartArgs = new ProcessStartArgs()
            {
                SchemaUri = schemaPath,
                TemplateDirectoryPath = path,
                Libelle = string.Format(TemplateValidatorResources.Process_Title, path)
            };

            _unusedFiles = new string[] { };
        }


        public TemplateValidationResult StartTemplateValidationProcess()
        {
            _processStartArgs.StartTime = DateTime.Now;
            OnProcessStart(_processStartArgs);



            _processCompletionArgs = new ProcessCompletionArgs(_processStartArgs);
            _processCompletionArgs.XmlDocumentInformation = GetXMLContent();


            if (_processCompletionArgs.XmlDocumentInformation.IsLoaded)
                if (ValidateXMLFormat())
                {
                    _processCompletionArgs.XmlDocumentInformation.Variables = GetVariables(_processCompletionArgs.XmlDocumentInformation.Document);
                    ValidateContent();
                }
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

        #region ZIP Process
        public bool StartZipProcess(string zipFileName)
        {
            return StartZipProcess(_processStartArgs.TemplateDirectoryPath, zipFileName);
        }
        public bool StartZipProcess(string from, string zipFileName)
        {
            var fullTargetPath = zipFileName;
            if (Path.GetExtension(fullTargetPath) == string.Empty)
                throw new Exception("Fichier non précisé dans le chemin d'archivage");


            if (File.Exists(fullTargetPath))
                File.Delete(fullTargetPath);


            var tempDir = Path.Combine(from, temp_dir);
            try
            {
                var so = new DirectoryInfo(from);
                var te = new DirectoryInfo(tempDir);
                CopyUsedFilesForZip(so, te);
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
        private void CopyUsedFilesForZip(DirectoryInfo source, DirectoryInfo target)
        {
            if (!Directory.Exists(target.FullName))
                Directory.CreateDirectory(target.FullName);

            foreach (FileInfo fi in source.GetFiles())
            {
                if (_unusedFiles != null && _unusedFiles.Contains(fi.FullName.ToLower())) continue;
                if (fi.FullName.Contains(".git")) continue;
                Console.WriteLine(@"Copie de {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                if (diSourceSubDir.FullName.Equals(target.FullName))
                    continue;
                bool hasUsedFiles = false;
                foreach (FileInfo fi in diSourceSubDir.GetFiles())
                {
                    if (_unusedFiles != null && _unusedFiles.Contains(fi.FullName.ToLower())) continue;
                    if (fi.FullName.Contains(".git")) continue;
                    hasUsedFiles = true;
                }
                if (hasUsedFiles)
                {
                    DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyUsedFilesForZip(diSourceSubDir, nextTargetSubDir);
                }
            }
        }
        #endregion

        private XmlDocumentInformation GetXMLContent()
        {
            var startArgs = new ProcessStepStartArgs()
            {
                StartTime = DateTime.Now,
                Libelle = TemplateValidatorResources.XmlRecuperation_Title,
            };
            var completeArgs = new ProcessStepCompletionArgs(startArgs)
            {
                IsSuccess = true
            };
            OnProcessStepStart(startArgs);
            _processCompletionArgs.StepProcess.Add(completeArgs);


            XmlDocumentInformation xdi = new XmlDocumentInformation();
            string[] rootFiles = Directory.GetFiles(_processStartArgs.TemplateDirectoryPath, "*.xml", SearchOption.TopDirectoryOnly);

            if (rootFiles.Length != 1)
            {
                if (rootFiles.Length > 1)
                    AddAnomaly(completeArgs, TemplateValidatorResources.XmlValidation_TooManyXml, true);
                else if (rootFiles.Length == 0)
                    AddAnomaly(completeArgs, TemplateValidatorResources.XmlValidation_NoXml, true);

                completeArgs.CompletionTime = DateTime.Now;
                OnProcessStepCompletion(completeArgs);
                return xdi;
            }

            foreach(var r in rootFiles)
            {
                ReplaceEnvVar(r);
            }

            XmlDocument doc = new XmlDocument();
            foreach (string fileName in rootFiles)
            {
                var completePath = Path.Combine(_processStartArgs.TemplateDirectoryPath, Path.GetFileName(fileName));
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
        private List<Variable> GetVariables(XmlDocument doc)
        {
            var startArgs = new ProcessStepStartArgs()
            {
                StartTime = DateTime.Now,
                Libelle = TemplateValidatorResources.XmlRecuperation_Variables,
            };
            var completeArgs = new ProcessStepCompletionArgs(startArgs)
            {
                IsSuccess = true
            };
            OnProcessStepStart(startArgs);
            _processCompletionArgs.StepProcess.Add(completeArgs);


            // Récupération des variables
            List<Variable> variables = new List<Variable>();
            var vars = doc.DocumentElement.SelectNodes("//*[local-name()='Variable']");

            if (vars.Count > 0)
            {
                foreach (XmlNode variableNode in vars)
                {
                    var variableCode = variableNode.Attributes.GetNamedItem("code").Value;
                    var variableLabel = variableNode.Attributes.GetNamedItem("label").Value;
                    var variableKind = variableNode.Attributes.GetNamedItem("kind").Value;
                    var variableDefaultValue = variableNode.Attributes.GetNamedItem("defaultValue");


                    if (!Enum.TryParse(variableKind, out VariableKind vk))
                    {
                        AddAnomaly(completeArgs, string.Format(TemplateValidatorResources.XmlRecuperation_TypeVariableInvalide, variableLabel), true);
                    }
                    Variable variable = new Variable(variableCode, variableLabel, vk, variableDefaultValue == null ? "" : variableDefaultValue.Value);


                    XmlNode currentNode = variableNode;
                    while (currentNode.ParentNode != null && !currentNode.Name.Equals("Settings"))
                    {
                        var parent = currentNode.ParentNode;
                        switch (parent.Name)
                        {
                            case "SettingsPage":
                                variable.SettingsPage = parent.Attributes.GetNamedItem("kind").Value;
                                break;
                            case "Group":
                                variable.Group = parent.Attributes.GetNamedItem("code").Value;
                                break;
                            case "Variation":
                                variable.Variation = parent.Attributes.GetNamedItem("code").Value;
                                break;
                        }
                        currentNode = currentNode.ParentNode;
                    }
                    if (!variables.Contains(variable))
                        variables.Add(variable);
                }
            }

            completeArgs.CompletionTime = DateTime.Now;
            OnProcessStepCompletion(completeArgs);
            return variables;
        }

        private bool ValidateXMLFormat()
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


            settings.ValidationEventHandler += (s, e) => AddAnomaly(completeArgs, e.Message, true);

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

            var rootPath = _processStartArgs.TemplateDirectoryPath;
            string[] fileEntries;
            //List<string> unusedFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories).Where(x => x.Contains(temp_dir)).Select(x => x.ToLowerInvariant()).ToList();
            List<string> unusedFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories).Select(x => x.ToLowerInvariant()).ToList();
            unusedFiles.Remove(_processCompletionArgs.XmlDocumentInformation.FileCompletePath.ToLowerInvariant()); // On remove le .xml racine.
            XmlDocument doc = _processCompletionArgs.XmlDocumentInformation.Document;

            var contentNode = doc.DocumentElement.GetElementsByTagName("Content");
            var rootAttribute = contentNode[0].Attributes.GetNamedItem("root");

            if (!rootAttribute.Value.Equals("."))
                rootPath = Path.Combine(_processStartArgs.TemplateDirectoryPath, rootAttribute.Value);


            var variationsElements = contentNode[0].ChildNodes;
            List<string> variationsCodes = new List<string>();
            // Les variations et parts
            if (variationsElements.Count == 0)
                AddAnomaly(completeArgs, TemplateValidatorResources.ContentValidation_AucuneVariation, true);
            else
            {
                foreach (XmlNode n in variationsElements)
                {
                    if (!n.Name.Equals("Variation")) continue;
                    var variationcode = n.Attributes.GetNamedItem("code").Value;
                    variationsCodes.Add(variationcode);

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
                                AddAnomaly(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_DossierNonExistant, currDir), true);
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
                                AddAnomaly(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierHtmlNonExistant, variationContentKind), true);
                            }
                        }
                        else if (t.ChildNodes.Cast<XmlNode>().Where(p => p.Name.Equals("File")).Count() >= 1)
                        {
                            var fileNodes = t.ChildNodes.Cast<XmlNode>().Where(p => p.Name.Equals("File")).ToList();
                            var dir = Path.Combine(rootPath, variationcode);
                            var filesDirFounded = false;

                            for (int i = 0; i < fileNodes.Count(); i++)
                            {
                                if (!filesDirFounded && File.Exists(Path.Combine(rootPath, fileNodes[i].Attributes.GetNamedItem("path").Value)))
                                    fileEntries = Directory.GetFiles(rootPath, fileNodes[i].Attributes.GetNamedItem("path").Value);
                                else
                                {
                                    AddAnomaly(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierNonExistant, fileNodes[i].Attributes.GetNamedItem("path").Value, variationContentKind), true);
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
                                    AddAnomaly(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierNonExistant, fileNodes[i].Attributes.GetNamedItem("path").Value, variationContentKind), true);
                            }
                        }
                    }
                }
            }




            CheckPublishSettings(doc, rootPath, variationsCodes, unusedFiles, completeArgs);
            CheckSharedFiles(doc, rootPath, unusedFiles, completeArgs);

            for (int i = 0; i < unusedFiles.Count; i++)
            {
                AddAnomaly(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierInutile, unusedFiles[i]), false);
            }


            _unusedFiles = unusedFiles.ToArray();
            completeArgs.CompletionTime = DateTime.Now;
            OnProcessStepCompletion(completeArgs);
            return completeArgs.IsSuccess;

        }



        private string GetFormatedName(string name)
        {
            return name; // github
            return name.Replace(@"/", @"\"); // local
        }
        private void CheckPublishSettings(XmlDocument doc, string rootPath, List<string> variationsCodes, List<string> unusedFiles, ProcessStepCompletionArgs completeArgs)
        {
            var publishSettings = doc.DocumentElement.GetElementsByTagName("PublishSettings");

            if (publishSettings.Count == 1)
            {
                // Les settings 
                var settings = doc.DocumentElement.SelectNodes("//*[local-name()='Setting']");
                if (settings.Count > 0)
                {
                    foreach (XmlNode t in settings)
                    {
                        bool matchingVariableFound = false;
                        var code = t.Attributes.GetNamedItem("code").Value;

                        for (int i = 0; i < _processCompletionArgs.XmlDocumentInformation.Variables.Count; i++)
                        {
                            var variable = _processCompletionArgs.XmlDocumentInformation.Variables[i];

                            if (code.Equals(variable.GetFormatedCode()))
                            {
                                var value = t.Attributes.GetNamedItem("value").Value;
                                bool isValueValid = true;
                                if (variable.Kind == VariableKind.Number)
                                {
                                    if (!int.TryParse(value, out int intR) && !float.TryParse(value, out float floatR))
                                    {
                                        isValueValid = false;
                                    }

                                }
                                else if (variable.Kind == VariableKind.InteractiveCatalogGuid)
                                {
                                    if (!Guid.TryParse(value, out Guid guidrR))
                                    {
                                        isValueValid = false;
                                    }
                                }
                                else if (variable.Kind == VariableKind.Date)
                                {
                                    if (!DateTime.TryParse(value, out DateTime datetimeR))
                                    {
                                        isValueValid = false;
                                    }
                                }

                                if (!isValueValid)
                                {
                                    AddAnomaly(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_SettingTypeInvalide, value, variable.Kind), true);
                                }

                                matchingVariableFound = true;


                            }
                        }
                        if (!matchingVariableFound)
                        {
                            AddAnomaly(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_SettingNoMatchWithVariable, code), true);
                        }
                    }
                }
                // variations
                var publishvariations = doc.DocumentElement.GetElementsByTagName("Variations");
                if (publishvariations.Count == 1)
                {
                    bool found = false;
                    var publishvariation = publishvariations[0];
                    foreach (XmlNode child in publishvariation.ChildNodes)
                    {
                        if (!child.Name.Equals("Variation")) continue;
                        var code = child.Attributes.GetNamedItem("code").Value;

                        for (int i = 0; i < variationsCodes.Count; i++)
                        {
                            var variation = variationsCodes[i];

                            if (code.Equals(variation, StringComparison.InvariantCultureIgnoreCase))
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                        AddAnomaly(completeArgs, TemplateValidatorResources.ContentValidation_DefaultVariationInvalide, true);
                }
                else
                {
                    AddAnomaly(completeArgs, TemplateValidatorResources.ContentValidation_DefaultVariationInvalide, true);

                }
            }
        }

        private void CheckSharedFiles(XmlDocument doc, string rootPath, List<string> unusedFiles, ProcessStepCompletionArgs completeArgs)
        {
            string[] fileEntries;
            var shareds = doc.DocumentElement.GetElementsByTagName("Shared");
            if (shareds.Count > 0)
            {
                // Les éléments partagés
                foreach (XmlNode t in shareds)
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
                            AddAnomaly(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_DossierNonExistant, currDir), true);
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
                                    ReplaceRelativePath(fileEntries[i].ToLowerInvariant());
                                    if (sharedKind.Equals("html"))
                                        kindFound = true;
                                    break;
                                case ".css":
                                    ReplaceRelativePath(fileEntries[i].ToLowerInvariant());
                                    if (sharedKind.Equals("css"))
                                        kindFound = true;
                                    break;
                                case ".js":
                                    ReplaceRelativePath(fileEntries[i].ToLowerInvariant());
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
                                AddAnomaly(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierNonExistant, fileNodes[i].Attributes.GetNamedItem("path").Value, sharedKind), true);
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
                                        ReplaceRelativePath(fileEntries[i].ToLowerInvariant());
                                        if (sharedKind.Equals("html"))
                                        {
                                            fileFound = true;
                                            unusedFiles.Remove(fileName);
                                        }
                                        break;
                                    case ".css":
                                        ReplaceRelativePath(fileEntries[i].ToLowerInvariant());
                                        if (sharedKind.Equals("css"))
                                        {
                                            fileFound = true;
                                            unusedFiles.Remove(fileName);
                                        }
                                        break;
                                    case ".js":
                                        ReplaceRelativePath(fileEntries[i].ToLowerInvariant());
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
                                AddAnomaly(completeArgs, string.Format(TemplateValidatorResources.ContentValidation_FichierNonExistant, fileNodes[i].Attributes.GetNamedItem("path").Value, sharedKind), true);
                        }
                    }
                }
            }
        }

        private void AddAnomaly(ProcessStepCompletionArgs datas, string message, bool isError)
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

        private static Regex _relativePath1 = new Regex(@"(\/app\/vending\/)?themes\/[0-9|a-f|A-F|-]*\/(?<capture>.*)", RegexOptions.IgnoreCase);

        private static void ReplaceRelativePath(string fileToChange)
        {
            var str = File.ReadAllText(fileToChange);
            var str2 = _relativePath1.Replace(str, @"~/${capture}");
            if (!str.Equals(str2))
                File.WriteAllText(fileToChange, str2);
        }

        private static Regex _envVarInXml = new Regex(@"\{\{(?<envName>.*)\}\}", RegexOptions.IgnoreCase);

        private static void ReplaceEnvVar(string fileToChange)
        {
            var str = File.ReadAllText(fileToChange);

            bool hasChanged = false;
            var matches = _envVarInXml.Matches(str);
            if (matches != null)
            {
                for(int i=matches.Count -1; i>=0; i--)
                {
                    string toReplace = matches[i].Value;
                    string env = toReplace.Substring(2, toReplace.Length - 4);
                    env = GetEnvValue(env);

                    str = str.Replace(toReplace, env);
                    hasChanged = true;
                }
            }

            if(hasChanged)
                File.WriteAllText(fileToChange, str);
        }

        private static string GetEnvValue(string env)
        {
            if (env == null)
                env = "";
            string evt;
            switch(env.ToLowerInvariant())
            {
                case "git_repo_name":
                    evt = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
                    if(!string.IsNullOrEmpty(evt) && evt.IndexOf("/")>=0)
                        return evt.Substring(evt.IndexOf("/") + 1);
                    return "";
                case "git_repo_owner":
                    evt = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
                    if (!string.IsNullOrEmpty(evt) && evt.IndexOf("/") >= 0)
                        return evt.Substring(0, evt.IndexOf("/"));
                    return "";
                default:
                    evt = Environment.GetEnvironmentVariable(env);
                    if (evt == null)
                    {
                        evt = Environment.GetEnvironmentVariable(env.ToUpperInvariant());
                        if (evt == null)
                            evt = Environment.GetEnvironmentVariable(env.ToLowerInvariant());
                    }

                    return evt==null?"":evt;
            }

            return "";
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
        public List<Variable> Variables { get; set; }
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
        public string TemplateDirectoryPath { get; set; }
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
            TemplatePath = startargs.TemplateDirectoryPath;
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




    public class Condition
    {
        public string Target { get; set; }
        public ConditionOperator Operator { get; set; }
        public string Value { get; set; }
    }
    public class Variable
    {
        public string Code { get; set; }
        public string Label { get; set; }
        public string DefaultValue { get; set; }
        public string Variation { get; set; }
        public string Group { get; set; }
        public string SettingsPage { get; set; }
        public VariableKind Kind { get; set; }
        public Condition Condition { get; set; }
        public Variable(string c, string l, VariableKind vk)
        {
            Code = c;
            Label = l;
            Kind = vk;
        }
        public Variable(string c, string l, VariableKind vk, string dv) : this(c, l, vk)
        {
            DefaultValue = dv;
        }
        public string GetFormatedCode()
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Variation))
                sb.Append(Variation + ".");
            if (!string.IsNullOrEmpty(Group))
                sb.Append(Group + ".");
            if (!string.IsNullOrEmpty(Code))
                sb.Append(Code);

            return sb.ToString();
        }
    }
    public enum ConditionOperator
    {
        Equal, NotEqual
    }
    public enum VariableKind
    {
        ImageUrl,
        HtmlLine,
        HtmlBlock,
        SearchUrl,
        Number,
        Color,
        Font,
        Date,
        Spring,
        OperationGuid,
        OperationKind,
        InteractiveCatalogGuid
    }


}
