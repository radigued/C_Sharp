
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LISA.Dblib;
using System.Data.Entity.Infrastructure;

namespace LISA.CatalogImport
{
    class Program
    {
        public const string ARGS_HELP = "/?";
        public const string ARGS_DIRECTORY = "/d";

        static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == ARGS_DIRECTORY)
            {
                //boucle qui affiche les arguments du tableau args
                foreach (string argument in args)
                {
                    Console.Write(argument + " ");
                }
                //Autre syntaxe :
                //args.ToList().ForEach(argument => Console.Write(argument + " "));

                //Vérifier que le dossier existe. 
                if (Directory.Exists(args[1]))
                {
                    //Lister les sous-dossier. Un sous-dossier pour un lot d'import.
                    foreach (string subDirectory in Directory.EnumerateDirectories(args[1], "*", SearchOption.TopDirectoryOnly))
                    {
                        //Rechercher dans chaque sous-dossier qu'il existe un unique fichier "*.xml". 
                        List<string> files = Directory.EnumerateFiles(subDirectory, "*.xml", SearchOption.TopDirectoryOnly).ToList();
                        if (files.Count == 1)
                        {
                            string filePath = files.First();

                            ImportXMLFile(filePath);
                        }
                        else
                        {
                            //Afficher une erreur si ce n'est pas le cas.
                            Console.WriteLine("Impossible de trouver un fichier xml dans le dossier " + subDirectory);
                        }
                        //Directory.Delete(subDirectory, true); //true pour forcer la suppression récursive.
                    }
                }
                else
                {
                    //Afficher un message d'erreur si ce n'est pas le cas
                    Console.WriteLine("Le dossier n'existe pas");
                }
            }
            else
            {
                //On affiche la documentation
                Console.WriteLine("Processus d'import d'un dossier dans la base de données LISA.");
                Console.WriteLine(ARGS_DIRECTORY + " <Dossier à vérifier> : Précise le dossier à vérifier pour l'import");
                Console.WriteLine(ARGS_HELP + " : Affiche la documentation");
                Console.WriteLine("LISA.CatalogImport.exe " + ARGS_HELP);
                Console.WriteLine("\t\tAffiche l'aide");
                Console.WriteLine("LISA.CatalogImport.exe " + ARGS_DIRECTORY + " <Chemin du dossier>");
                Console.WriteLine("\t\tImporte les catalogues dans le dossier spécifié");
                Console.WriteLine("Exemple : ");
                Console.WriteLine("\t\tLISA.CatalogImport.exe " + ARGS_DIRECTORY + " \"C:\\Mon dossier\\\"");
            }
            //Compilation conditionnelle. Si le symbole de compilation DEBUG est défini, le programme ne fermera pas directement.
            //DEBUG n'est pas défini si la configuration de génération est en mode RELEASE.
#if DEBUG
            Console.WriteLine("Appuyez sur une touche pour quitter...");
            Console.ReadKey();
#endif
        }

        #region Fonction d'import XML
        private static void ImportXMLFile(string filePath)
        {
            //Console.WriteLine("Import du fichier : " + filePath);
            //Console.WriteLine(string.Format("Import du fichier : {0} {1:00}", filePath, 5));
            Console.WriteLine($"Import du fichier : {filePath}"); //Nouvelle syntaxe pour string.format depuis C# 6

            try
            {
                using (LISAEntities entities = new LISAEntities())
                {
                    entities.Database.Connection.Open();

                    XDocument document = XDocument.Load(filePath);

                    foreach (XElement operationElement in document.Descendants(XName.Get("operation")))
                    {
                        Operation operation = ParseOperationElement(operationElement, entities);

                        foreach (XElement catalogElement in operationElement.Elements(XName.Get("catalog")))
                        {
                            Catalogue catalogue = ParseCatalogElement(catalogElement, entities, operation);

                            XElement pagesElement = catalogElement.Element(XName.Get("pages"));
                            //XElement shopsElement = catalogElement.Element(XName.Get("shops"));

                            foreach (XElement pageElement in pagesElement.Elements())
                            {
                                Page page = ParsePageElement(pageElement, entities, catalogue);

                                XElement productsElement = pageElement.Element(XName.Get("products"));

                                foreach (XElement productElement in productsElement.Elements())
                                {
                                    Article article = ParseArticleElement(productElement, entities);
                                }
                            }

                            foreach (XElement shopElement in catalogElement.Descendants(XName.Get("shop")))
                            {
                                ParseShopElement(shopElement, entities, catalogue);
                            }
                        }
                    }

                    entities.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                //TODO : Voir comment gérer les erreurs d'import.
                Console.WriteLine($"Impossible d'importer le fichier {filePath + Environment.NewLine + ex.ToString()}");
            }
        }
        #endregion

        #region Fonction DateTime UNIX
        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp).ToLocalTime();
        }
        #endregion

        #region Parse Operation
        private static Operation ParseOperationElement(XElement operationElement, LISAEntities entities)
        {
            Operation result = null;

            //Récupérer les données de l'opération dans le fichier XML
            long operationId = long.Parse(operationElement.Attribute(XName.Get("id")).Value);
            string operationCode = operationElement.Element(XName.Get("code")).Value;
            string operationTitle = operationElement.Element(XName.Get("title")).Value;
            DateTime operationStartDateTime = UnixTimeStampToDateTime(double.Parse(operationElement.Element(XName.Get("startDate")).Value));
            DateTime operationEndDateTime = UnixTimeStampToDateTime(double.Parse(operationElement.Element(XName.Get("endDate")).Value));

            //Vérifier si l'opération n'existe pas déjà en base
            result = entities.Operations.FirstOrDefault(o => o.ImportId == operationId);

            //Créer l'opération si elle n'existe pas
            if (result == null)
            {
                result = new Operation()
                {
                    ImportId = operationId,
                    Code = operationCode,
                    Titre = operationTitle,
                    DateDebut = operationStartDateTime,
                    DateFin = operationEndDateTime
                };

                entities.Operations.Add(result);
            }
            else
            {
                //A vous de voir ce que l'on fait si elle existe déjà.
            }

            return result;
        }
        #endregion

        #region Parse Catalogue
        private static Catalogue ParseCatalogElement(XElement catalogElement, LISAEntities entities, Operation operation)
        {
            Catalogue result = new Catalogue();

            long catalogueId = long.Parse(catalogElement.Attribute(XName.Get("id")).Value);
            string catalogueType = catalogElement.Element(XName.Get("type")).Value;
            string catalogueLabel = catalogElement.Element(XName.Get("label")).Value;
            string catalogueSpeed = catalogElement.Element(XName.Get("speed")).Value;
            int catalogueWidth = int.Parse(catalogElement.Element(XName.Get("catalogWidth")).Value);
            int catalogueHeight = int.Parse(catalogElement.Element(XName.Get("catalogHeight")).Value);

            result = entities.Catalogues.FirstOrDefault(c => c.ImportId == catalogueId);

            if (result == null)
            {
                result = new Catalogue()
                {
                    ImportId = catalogueId,
                    Type = catalogueType,
                    Libelle = catalogueLabel,
                    Vitesse = catalogueSpeed,
                    Largeur = catalogueWidth,
                    Hauteur = catalogueHeight,
                    Operation = operation
                };

                entities.Catalogues.Add(result);
            }
            else
            {
                entities.Catalogues.Remove(result);
            }

            return result;
        }
        #endregion

        #region Parse MagasinCatalogue
        private static void ParseShopElement(XElement shopElement, LISAEntities entities, Catalogue catalogue)
        {
            MagasinCatalogue result = null;
            Magasin magasin = null;

            DateTime shopStartDateTime = UnixTimeStampToDateTime(double.Parse(shopElement.Element(XName.Get("displayStartDate")).Value));
            DateTime shopEndDateTime = UnixTimeStampToDateTime(double.Parse(shopElement.Element(XName.Get("displayEndDate")).Value));

            magasin = ParseShopElement_Magasin(shopElement, entities);

            result = entities.MagasinCatalogues.FirstOrDefault(mc => mc.IdMagasin == magasin.Id && mc.IdCatalogue == catalogue.Id);

            if (result == null)
            {
                result = new MagasinCatalogue()
                {
                    Magasin = magasin,
                    Catalogue = catalogue,
                    DateDebut = shopStartDateTime,
                    DateFin = shopEndDateTime
                };

                entities.MagasinCatalogues.Add(result);
            }
        }
        #endregion

        #region Parse Magasin
        private static Magasin ParseShopElement_Magasin(XElement shopElement, LISAEntities entities)
        {
            Magasin result = null;

            long shopId = long.Parse(shopElement.Attribute(XName.Get("id")).Value);

            result = entities.Magasins.FirstOrDefault(m => m.ImportId == shopId);

            if (result == null)
            {
                result = new Magasin()
                {
                    ImportId = shopId,
                    Libelle = shopId.ToString()
                };

                entities.Magasins.Add(result);
            }

            return result;
        }
        #endregion

        #region Parse Pages
        private static Page ParsePageElement(XElement pageElement, LISAEntities entities, Catalogue catalogue)
        {
            Page result = new Page();

            long pageId = long.Parse(pageElement.Attribute(XName.Get("id")).Value);
            int number = int.Parse(pageElement.Element(XName.Get("number")).Value);

            result = entities.Pages.FirstOrDefault(p => p.ImportId == pageId);

            if (result == null)
            {
                result = new Page()
                {
                    ImportId = pageId,
                    Numero = number,
                    Catalogue = catalogue
                };
                entities.Pages.Add(result);
            }
            return result;
        }
        #endregion

        #region Parse Categories
        private static Categorie ParseCategoryElement(XElement categoryElement, LISAEntities entities)
        {
            Categorie result = new Categorie();

            long categoryId = long.Parse(categoryElement.Attribute(XName.Get("id")).Value);
            string categoryLabel = categoryElement.Element(XName.Get("label")).Value;

            result = new Categorie()
            {
                Libelle = categoryLabel
            };
            entities.Categories.Add(result);

            return result;
        }
        #endregion

        #region Parse Articles

        private static Article ParseArticleElement(XElement productElement, LISAEntities entities)
        {
            Article result = new Article();

            long articleId = long.Parse(productElement.Attribute(XName.Get("id")).Value);
            string articleCode = productElement.Element(XName.Get("code")).Value;
            string articleLabel = productElement.Element(XName.Get("label")).Value;
            string articleDescription = productElement.Element(XName.Get("description")).Value;
            string articleUnite = productElement.Element(XName.Get("packaging")).Value;

            result = entities.Articles.FirstOrDefault(c => c.ImportId == articleId);
        
            if (result == null)
            {
                result = new Article()
                {
                    ImportId = articleId,
                    Code = articleCode,
                    Libelle = articleLabel,
                    Description = articleDescription,
                    Unite = articleUnite
                };
        
                entities.Articles.Add(result);
            }
            else
            {
                entities.Articles.Remove(result);
            }
        
            return result;
        }

        #endregion

        #region Parse PrixCatalogueArticle

        private static PrixCatalogueArticle ParsePriceCategoryProduct(XElement productElement, LISAEntities entities, Catalogue catalogue, Article article)
        {
            PrixCatalogueArticle result = null;

            long articleCategorieId = long.Parse(productElement.Element(XName.Get("id")).Value);
            decimal articleCategoriePrix = decimal.Parse(productElement.Element(XName.Get("price")).Value);
            decimal articleCategoriePrixAvantCoupon = decimal.Parse(productElement.Element(XName.Get("price_before_coupon")).Value);
            decimal articleCategoriePrixAvantCroise = decimal.Parse(productElement.Element(XName.Get("price_crossed")).Value);
            decimal articleCategorieReductionEuro = decimal.Parse(productElement.Element(XName.Get("Reduction_euro")).Value);
            int articleCategorieReductionPourcent = int.Parse(productElement.Element(XName.Get("Reduction_percent")).Value);
            decimal articleCategorieAvantageEuro = decimal.Parse(productElement.Element(XName.Get("Avantage_euro")).Value);
            int articleCategorieAvantagePourcent = int.Parse(productElement.Element(XName.Get("Avantage_percent")).Value);
            decimal articleCategorieEcotaxe = decimal.Parse(productElement.Element(XName.Get("ecotaxe")).Value);


            result = entities.PrixCatalogueArticles.FirstOrDefault(a => a.IdCatalogue == catalogue.Id && a.IdArticle == article.Id);

            if (result == null)
            {
                result = new PrixCatalogueArticle()
                {
                    Article = article,
                    Catalogue = catalogue,
                    Prix = articleCategoriePrix,
                    PrixAvantCoupon = articleCategoriePrixAvantCoupon,
                    PrixAvantCroise = articleCategoriePrixAvantCroise,
                    ReductionEuro = articleCategorieReductionEuro,
                    ReductionPourcent = articleCategorieReductionPourcent,
                    AvantageEuro = articleCategorieAvantageEuro,
                    AvantagePourcent = articleCategorieAvantagePourcent,
                    Ecotaxe = articleCategorieEcotaxe
                };

                entities.PrixCatalogueArticles.Add(result);
            }

            return result;
        }

        #endregion


    }
}
