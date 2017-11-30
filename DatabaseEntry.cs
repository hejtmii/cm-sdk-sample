using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Import
{
    public sealed class DatabaseEntry
    {
        private static readonly CultureInfo _cultureInfo = CultureInfo.CreateSpecificCulture("en-US");
        private readonly Dictionary<string, string> _properties;
        private readonly string _externalId;

        public string ExternalId
        {
            get
            {
                return _externalId;
            }
        }

        private DatabaseEntry(string externalId, Dictionary<string, string> properties)
        {
            _externalId = externalId;
            _properties = properties;
        }

        public decimal GetNumber(string name)
        {
            return decimal.Parse(_properties[name], _cultureInfo);
        }

        public DateTime GetDateTime(string name)
        {
            return DateTime.Parse(_properties[name], _cultureInfo);
        }

        public string GetText(string name)
        {
            return _properties[name];
        }

        public string[] GetListItems(string name)
        {
            return _properties[name].Split(new string[] { ", " }, StringSplitOptions.None);
        }

        public static DatabaseEntry CreateFromFile(string filePath)
        {
            var properties = new Dictionary<string, string>();
            var lines = File.ReadAllLines(filePath);
            var position = 0;

            while (position < lines.Length)
            {
                var name = lines[position];
                var builder = new StringBuilder();
                position++;

                while (position < lines.Length && !string.IsNullOrWhiteSpace(lines[position]))
                {
                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                    }
                    builder.Append(lines[position]);
                    position++;
                }

                properties[name] = builder.ToString();
                position++;
            }

            return new DatabaseEntry(Path.GetFileNameWithoutExtension(filePath), properties);
        }

        public static IEnumerable<DatabaseEntry> CreateFromFolder(string folderPath)
        {
            return Directory.EnumerateFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly).Select(CreateFromFile);
        }
    }
}
