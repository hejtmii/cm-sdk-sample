using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Import
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new Client();
            var folderPath = ConfigurationManager.AppSettings["DatabaseFolderPath"];
            ImportActors(folderPath, client);
            ImportMovies(folderPath, client);
        }

        private static void ImportActors(string folderPath, Client client)
        {
            foreach (var actor in DatabaseEntry.CreateFromFolder(Path.Combine(folderPath, "Actors")))
            {
                Console.WriteLine($"Importing {actor.GetText("Name")}");

                var imageExternalId = ImportActorPhoto(actor, folderPath, client);

                var externalId = $"Actor - {actor.ExternalId}";

                var item = new
                {
                    name = actor.GetText("Name"),
                    type = new
                    {
                        codename = "actor"
                    },
                    sitemap_locations = new object[0]
                };

                client.Put($"items/external-id/{Uri.EscapeDataString(externalId)}", item);

                var variant = new
                {
                    item = new
                    {
                        external_id = actor.ExternalId
                    },
                    elements = new
                    {
                        name = actor.GetText("Name"),
                        born = actor.GetDateTime("Born"),
                        bio = actor.GetText("Bio"),
                        photo = new object[] { new { external_id = imageExternalId } }
                    },
                    language = new
                    {
                        id = Guid.Empty
                    }
                };

                client.Put($"items/external-id/{Uri.EscapeDataString(externalId)}/variants/{Guid.Empty}", variant);
            }
        }

        private static string ImportActorPhoto(DatabaseEntry actor, string folderPath, Client client)
        {
            var externalId = $"Actor image - {actor.ExternalId}";
            var filePath = Path.Combine(folderPath, "Actors", "images", $"{actor.ExternalId}.jpg");

            var file = client.PostFile($"files/{Uri.EscapeDataString(actor.ExternalId)}.jpg", filePath);

            var asset = new
            {
                file_reference = new
                {
                    id = file["id"].Value<string>(),
                    type = file["type"].Value<string>()
                },
                descriptions = new object[]
                {
                    new
                    {
                        language = new
                        {
                            id = Guid.Empty
                        },
                        description = actor.GetText("Name")
                    }
                }
            };

            client.Put($"assets/external-id/{Uri.EscapeDataString(externalId)}", asset);

            return externalId;
        }

        private static void ImportMovies(string folderPath, Client client)
        {
            foreach (var movie in DatabaseEntry.CreateFromFolder(Path.Combine(folderPath, "Movies")))
            {
                Console.WriteLine($"Importing {movie.GetText("Name")}");

                var imageExternalIds = ImportMoviePhotos(movie, folderPath, client);

                var externalId = $"Movie - {movie.ExternalId}";

                var item = new
                {
                    name = movie.GetText("Name"),
                    type = new
                    {
                        codename = "movie"
                    },
                    sitemap_locations = movie.GetListItems("Sitemap location").Select(x => new { codename = GetCodename(x) })
                };

                client.Put($"items/external-id/{Uri.EscapeDataString(externalId)}", item);

                var variant = new
                {
                    item = new
                    {
                        external_id = movie.ExternalId
                    },
                    elements = new
                    {
                        name = movie.GetText("Name"),
                        description = movie.GetText("Description"),
                        synopsis = movie.GetText("Synopsis"),
                        release_date = movie.GetDateTime("Release date"),
                        genre = movie.GetListItems("Genres").Select(x => new { codename = GetCodename(x) }),
                        cast = movie.GetListItems("Cast").Select(x => new { external_id = $"Actor - {x}" }),
                        imdb_rating = movie.GetNumber("IMDB rating"),
                        rating = new object[] { new { codename = GetCodename(movie.GetText("Rating")) } },
                        slug = movie.GetText("Slug"),
                        photos = imageExternalIds.Select(x => new { external_id = x })
                    },
                    language = new
                    {
                        id = Guid.Empty
                    }
                };

                client.Put($"items/external-id/{Uri.EscapeDataString(externalId)}/variants/{Guid.Empty}", variant);
            }
        }

        private static string[] ImportMoviePhotos(DatabaseEntry movie, string folderPath, Client client)
        {
            folderPath = Path.Combine(folderPath, "Movies", "images", movie.ExternalId);
            var externalIds = new List<string>();

            foreach (var filePath in Directory.EnumerateFiles(folderPath, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                var externalId = $"Movie image - {movie.ExternalId} ({Path.GetFileNameWithoutExtension(filePath)})";
                var file = client.PostFile($"files/{Uri.EscapeDataString(Path.GetFileName(filePath))}", filePath);

                var asset = new
                {
                    file_reference = new
                    {
                        id = file["id"].Value<string>(),
                        type = file["type"].Value<string>()
                    },
                    descriptions = new object[]
                    {
                    new
                    {
                        language = new
                        {
                            id = Guid.Empty
                        },
                        description = movie.GetText("Name")
                    }
                    }
                };

                client.Put($"assets/external-id/{Uri.EscapeDataString(externalId)}", asset);

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
