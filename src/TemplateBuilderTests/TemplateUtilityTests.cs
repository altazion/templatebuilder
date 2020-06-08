using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TemplateBuilder;

namespace TemplateBuilderTests
{
    [TestClass]
    public class TemplateUtilityTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var schemaPath = @"..\..\..\resources\shared\template_schema.xsd";
            var ds01 = @"..\..\..\resources\dataset01\";
            var ds02 = @"..\..\..\resources\dataset02\";
            var ds03 = @"..\..\..\resources\dataset03\";
            var ds04 = @"..\..\..\resources\dataset04\";
            var ds05 = @"..\..\..\resources\dataset05\";
            var ds06 = @"..\..\..\resources\dataset06\template\";
            var ds06ArchiveWrong = @"..\..\..\resources\dataset06\";
            var ds06ArchiveGood = @"..\..\..\resources\dataset06\testarchive.zip";

            try
            {
                var tvr00 = new TemplateUtility(@"", ds01);
                Assert.Fail("Le chemin du schema ne peut pas être vide");
            }
            catch (ArgumentException) { }

            try
            {
                var tvr00 = new TemplateUtility(schemaPath, @"");
                Assert.Fail("Le chemin du dossier ne peut pas être vide");
            } 
            catch (ArgumentException){}

            // La structure est  valide, le xml aussi
            var tvr01 = new TemplateUtility(schemaPath, ds01).StartTemplateValidationProcess();
            Assert.IsTrue(tvr01.IsSuccess);

            /* La structure est toujours valide, mais le xml ne contient pas la variation "RedTheme", 
            on doit donc avoir 6 anomalies secondaires concernant les fichiers inutilisés du dossier RedTheme */
            var tvr02 = new TemplateUtility(schemaPath, ds02).StartTemplateValidationProcess();
            Assert.IsTrue(tvr02.IsSuccess);
            Assert.AreEqual(6, tvr02.Steps.Sum(x => x.Anomalies.Count));

            // Pas de dossier RedTheme mais la référence est présente dans le .xml 
            var tvr03 = new TemplateUtility(schemaPath, ds03).StartTemplateValidationProcess();
            Assert.IsFalse(tvr03.IsSuccess);
            Assert.AreEqual(6, tvr03.Steps.Sum(x => x.Anomalies.Count));

            // Cette fois, pas de .xml
            var tvr04 = new TemplateUtility(schemaPath, ds04).StartTemplateValidationProcess();
            Assert.IsFalse(tvr04.IsSuccess);
            Assert.AreEqual(1, tvr04.Steps.Sum(x => x.Anomalies.Count));
            Assert.AreEqual(1, tvr04.Steps.Count);

            // Le .xml ne respecte pas le schéma
            var tvr05 = new TemplateUtility(schemaPath, ds05).StartTemplateValidationProcess();
            Assert.IsFalse(tvr05.IsSuccess);
            Assert.AreEqual(1, tvr05.Steps.Sum(x => x.Anomalies.Count));
            Assert.AreEqual(2, tvr05.Steps.Count);


            // tout est ok, on sauvegarde le zip.
            var tu = new TemplateUtility(schemaPath, ds06);
            var tvr06 = tu.StartTemplateValidationProcess();
            Assert.IsTrue(tvr06.IsSuccess);
            try
            {
                tu.StartZipProcess(ds06ArchiveWrong);
                Assert.Fail("Le process est supposé throw une exception car le nom du fichier n'est pas précisé.");
            }
            catch (Exception) { }

            tu.StartZipProcess(ds06ArchiveGood);
            Assert.IsTrue(File.Exists(ds06ArchiveGood));


            

        }
    }
}
