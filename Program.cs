using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

using KenticoCloud.ContentManagement;
using KenticoCloud.ContentManagement.Models.Items;
using KenticoCloud.ContentManagement.Models.Assets;

namespace Import
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new ContentManagementOptions
            {
                ProjectId = ConfigurationManager.AppSettings["ProjectId"],
                ApiKey = ConfigurationManager.AppSettings["ContentManagementApiKey"]
            };
            var client = new ContentManagementClient(options);

            var folderPath = ConfigurationManager.AppSettings["DatabaseFolderPath"];

            ImportActors(folderPath, client);
            ImportMovies(folderPath, client);
        }

        private static void ImportActors(string folderPath, ContentManagementClient client)
        {
            foreach (var actor in DatabaseEntry.CreateFromFolder(Path.Combine(folderPath, "Actors")))
            {
                Console.WriteLine($"Importing {actor.GetText("Name")}");

                var imageExternalId = ImportActorPhoto(actor, folderPath, client);

                var externalId = $"Actor - {actor.ExternalId}";

                var item = new ContentItemUpsertModel
                {
                    Name = actor.GetText("Name"),
                    Type = ContentTypeIdentifier.ByCodename("actor"),
                };

                client.UpsertContentItemByExternalIdAsync(externalId, item);

                var itemIdentifier = ContentItemIdentifier.ByExternalId(actor.ExternalId);
                var languageIdentifier = LanguageIdentifier.DEFAULT_LANGUAGE;

                var variant = new ContentItemVariantUpsertModel
                {
                    Elements = new
                    {
                        name = actor.GetText("Name"),
                        born = actor.GetDateTime("Born"),
                        bio = actor.GetText("Bio"),
                        photo = new [] { AssetIdentifier.ByExternalId(imageExternalId) }
                    },
                };

                client.UpsertContentItemVariantAsync(
                    new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier),
                    variant
                );
            }
        }

        private static string ImportActorPhoto(DatabaseEntry actor, string folderPath, ContentManagementClient client)
        {
            var externalId = $"Actor image - {actor.ExternalId}";
            var filePath = Path.Combine(folderPath, "Actors", "images", $"{actor.ExternalId}.jpg");
            var contentType = "image/jpeg";

            var descriptions = new List<AssetDescription> {
                new AssetDescription
                {
                    Language = LanguageIdentifier.DEFAULT_LANGUAGE,
                    Description = actor.GetText("Name")
                }
            };

            client.UpsertAssetByExternalIdAsync(
                actor.ExternalId,
                new FileContentSource(filePath, contentType),
                new List<AssetDescription>()
            );

            return externalId;
        }

        private static void ImportMovies(string folderPath, ContentManagementClient client)
        {
            foreach (var movie in DatabaseEntry.CreateFromFolder(Path.Combine(folderPath, "Movies")))
            {
                Console.WriteLine($"Importing {movie.GetText("Name")}");

                var imageExternalIds = ImportMoviePhotos(movie, folderPath, client);

                var externalId = $"Movie - {movie.ExternalId}";

                var item = new ContentItemUpsertModel
                {
                    Name = movie.GetText("Name"),
                    Type = ContentTypeIdentifier.ByCodename("movie"),
                    SitemapLocations = movie.GetListItems("Sitemap location").Select(x => SitemapNodeIdentifier.ByCodename(GetCodename(x)))
                };

                client.UpsertContentItemByExternalIdAsync(externalId, item);

                var itemIdentifier = ContentItemIdentifier.ByExternalId(externalId);
                var languageIdentifier = LanguageIdentifier.DEFAULT_LANGUAGE;

                var variant = new ContentItemVariantUpsertModel
                {
                    Elements = new
                    {
                        name = movie.GetText("Name"),
                        description = movie.GetText("Description"),
                        synopsis = movie.GetText("Synopsis"),
                        release_date = movie.GetDateTime("Release date"),
                        genre = movie.GetListItems("Genres").Select(x => TaxonomyTermIdentifier.ByCodename(GetCodename(x))),
                        cast = movie.GetListItems("Cast").Select(x => ContentItemIdentifier.ByExternalId($"Actor - {x}")),
                        imdb_rating = movie.GetNumber("IMDB rating"),
                        rating = new [] { MultipleChoiceOptionIdentifier.ByCodename(GetCodename(movie.GetText("Rating"))) },
                        slug = movie.GetText("Slug"),
                        photos = imageExternalIds.Select(imageExternalId => AssetIdentifier.ByExternalId(imageExternalId))
                    },
                };

                client.UpsertContentItemVariantAsync(new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier), variant);
            }
        }

        private static string[] ImportMoviePhotos(DatabaseEntry movie, string folderPath, ContentManagementClient client)
        {
            folderPath = Path.Combine(folderPath, "Movies", "images", movie.ExternalId);
            var externalIds = new List<string>();
            var contentType = "image/jpeg";

            foreach (var filePath in Directory.EnumerateFiles(folderPath, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                var externalId = $"Movie image - {movie.ExternalId} ({Path.GetFileNameWithoutExtension(filePath)})";

                var descriptions = new List<AssetDescription> {
                    new AssetDescription
                    {
                        Language = LanguageIdentifier.DEFAULT_LANGUAGE,
                        Description = movie.GetText("Name")
                    }
                };

                var file = client.UpsertAssetByExternalIdAsync(movie.ExternalId, new FileContentSource(filePath, contentType), new List<AssetDescription>());
                
                externalIds.Add(externalId);
            }

            return externalIds.ToArray();
        }

        private static string GetCodename(string text)
        {
            return text.Replace(' ', '_').ToLowerInvariant();
        }
    }
}
