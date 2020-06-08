﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :4.0.30319.42000
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TemplateBuilder.Properties {
    using System;
    
    
    /// <summary>
    ///   Une classe de ressource fortement typée destinée, entre autres, à la consultation des chaînes localisées.
    /// </summary>
    // Cette classe a été générée automatiquement par la classe StronglyTypedResourceBuilder
    // à l'aide d'un outil, tel que ResGen ou Visual Studio.
    // Pour ajouter ou supprimer un membre, modifiez votre fichier .ResX, puis réexécutez ResGen
    // avec l'option /str ou régénérez votre projet VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class TemplateValidatorResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal TemplateValidatorResources() {
        }
        
        /// <summary>
        ///   Retourne l'instance ResourceManager mise en cache utilisée par cette classe.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("TemplateBuilder.Properties.TemplateValidatorResources", typeof(TemplateValidatorResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Remplace la propriété CurrentUICulture du thread actuel pour toutes
        ///   les recherches de ressources à l'aide de cette classe de ressource fortement typée.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Aucune Variation trouvée.
        /// </summary>
        internal static string ContentValidation_AucuneVariation {
            get {
                return ResourceManager.GetString("ContentValidation_AucuneVariation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Le dossier {0} n&apos;existe pas, le content root est-il le bon ?.
        /// </summary>
        internal static string ContentValidation_DossierNonExistant {
            get {
                return ResourceManager.GetString("ContentValidation_DossierNonExistant", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Le fichier html du ContentKind {0} est introuvable..
        /// </summary>
        internal static string ContentValidation_FichierHtmlNonExistant {
            get {
                return ResourceManager.GetString("ContentValidation_FichierHtmlNonExistant", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Le fichier suivant n&apos;est pas utilisé : {0}.
        /// </summary>
        internal static string ContentValidation_FichierInutile {
            get {
                return ResourceManager.GetString("ContentValidation_FichierInutile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Anomalies secondaires : .
        /// </summary>
        internal static string ContentValidation_FichierInutiles {
            get {
                return ResourceManager.GetString("ContentValidation_FichierInutiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Le fichier {0} du ContentKind {1} n&apos;a pas pu être récupéré..
        /// </summary>
        internal static string ContentValidation_FichierNonExistant {
            get {
                return ResourceManager.GetString("ContentValidation_FichierNonExistant", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à La ressource partagée de type {0} est introuvable..
        /// </summary>
        internal static string ContentValidation_SharedResourcesIntrouvable {
            get {
                return ResourceManager.GetString("ContentValidation_SharedResourcesIntrouvable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Analyse de la structure du dossier.
        /// </summary>
        internal static string ContentValidation_Title {
            get {
                return ResourceManager.GetString("ContentValidation_Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à La variation {0} présente les anomalies suivantes :.
        /// </summary>
        internal static string ContentValidation_VariationAnomalie {
            get {
                return ResourceManager.GetString("ContentValidation_VariationAnomalie", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Analyse du template {0}.
        /// </summary>
        internal static string Process_Title {
            get {
                return ResourceManager.GetString("Process_Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Récupération du template.
        /// </summary>
        internal static string XmlRecuperation_Title {
            get {
                return ResourceManager.GetString("XmlRecuperation_Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Aucun .xml n&apos;est présent à la racine de votre dossier.
        /// </summary>
        internal static string XmlValidation_NoXml {
            get {
                return ResourceManager.GetString("XmlValidation_NoXml", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Problème avec la récupération de votre contenu.
        /// </summary>
        internal static string XmlValidation_StructureDossierAnomalie {
            get {
                return ResourceManager.GetString("XmlValidation_StructureDossierAnomalie", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Validation du fichier .xml.
        /// </summary>
        internal static string XmlValidation_Title {
            get {
                return ResourceManager.GetString("XmlValidation_Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Plusieurs .xml semblent être à la racine de votre dossier.
        /// </summary>
        internal static string XmlValidation_TooManyXml {
            get {
                return ResourceManager.GetString("XmlValidation_TooManyXml", resourceCulture);
            }
        }
    }
}
