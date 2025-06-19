using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

class Program
{
    private static readonly HttpClient Client = new HttpClient();
    private const string Url = "https://static.henderson.ru/files/feeds/imshop_v2.xml"; 
    private const string OutputFile = "imshop_v2.xml";

    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Загружаем XML файл...");
            await DownloadXmlAsync(Url, OutputFile);

            Console.WriteLine("Обрабатываем данные...");
            var groupIds = ExtractGroupIdsWithConditions(OutputFile);

            if (groupIds.Any())
            {
                Console.WriteLine("Найденные уникальные group_id:");
                foreach (var id in groupIds)
                {
                    Console.WriteLine(id);
                }

                Console.WriteLine($"\nОбщее количество: {groupIds.Count}");
            }
            else
            {
                Console.WriteLine("Совпадений не найдено");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    private static async Task DownloadXmlAsync(string url, string filename)
    {
        var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream);
    }

    private static HashSet<string> ExtractGroupIdsWithConditions(string filePath)
    {
        var groupIds = new HashSet<string>();
        var doc = XDocument.Load(filePath);

        foreach (var offer in doc.Descendants("offer"))
        {
            // Получаем group_id из атрибутов
            var groupIdAttribute = offer.Attribute("group_id");
            if (groupIdAttribute == null) continue;

            var groupId = groupIdAttribute.Value.Trim();
            if (string.IsNullOrEmpty(groupId)) continue;

            // Проверяем наличие oldprice
            var oldPrice = offer.Element("oldprice");
            if (oldPrice == null || string.IsNullOrWhiteSpace(oldPrice.Value)) continue;

            // Проверяем наличие badge с нужным текстом
            var badge = offer.Element("badge");
            if (badge != null && badge.Value.Trim() == "До -20%")
            {
                groupIds.Add(groupId);
            }
        }

        return groupIds;
    }
}