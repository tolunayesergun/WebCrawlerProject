﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebCrawlerProject.Models;

namespace WebCrawlerProject.Helpers
{
    public static class HtmlParser
    {
        public static List<WordModel> GetWordsAsList(HtmlDocument document, string url)
        {
            var title = GetTitleText(document);
            var meta = GetMetaText(document);
            var wordList = new List<WordModel>();
            //GetWords metodundan string olarak gelen veriyi string list olarak döner
            var result = GetWords(document);
            if (result.Split(" ").Any(x => !string.IsNullOrEmpty(x)))
            {
                var words = result.Split(" ").Where(x => !string.IsNullOrEmpty(x)).ToList();
                foreach (var item in words)
                {
                    var numberTest = int.TryParse(item, out int testResult);
                    if (!numberTest)
                    {
                        if (wordList.Any(x => x.Word.ToUpper().Replace("İ", "I") == item.ToUpper().Replace("İ", "I")))
                        {
                            var prev = wordList.FirstOrDefault(x => x.Word.ToUpper().Replace("İ", "I") == item.ToUpper().Replace("İ", "I"));
                            prev.Frequency += 1;
                            prev.Score += prev.Word.Length > 3 ? 3 : 1; //uzunluğu 3 karakterden fazla olan kelimeler daha fazla puan alsın
                        }
                        else //kelime ilk defa geliyorsa ekle
                        {
                            var isInUrl = url.ToUpper().Contains(item.ToUpper());
                            var isInTitle = title.ToUpper().Contains(item.ToUpper());
                            var isInMeta = meta.ToUpper().Contains(item.ToUpper());

                            var score = item.Length > 3 ? 2 : 1;//uzunluğu 3 karakterden fazla olan kelimeler daha fazla puan alsın
                            if (score == 2)
                            {
                                score += isInUrl ? 6 : 0; //url de geçiyorsa 6 puan daha ekle
                                score += isInTitle ? 5 : 0; //site başlığında geçiyorsa 5 puan daha ekle
                                score += isInMeta ? 4 : 0; //site seo taglerinde geçiyorsa 5 puan daha ekle
                            }

                            wordList.Add(new WordModel
                            {
                                Word = item,
                                Frequency = 1,
                                Score = score
                            });
                        }
                    }
                }
            }

            if (wordList != null)
            {
                wordList = wordList.OrderByDescending(x => x.Score).ToList();
            }

            return wordList;
        }

        public static string GetWords(HtmlDocument document)
        {
            var sb = new StringBuilder();

            //Header Textler (Page Title - Meta SEO tagler)
            sb.Append(GetTitleText(document));
            sb.Append(GetMetaText(document));

            sb.Append(" ");

            //img taglerden alt bilgisi al
            sb.Append(GetImgAltText(document));

            sb.Append(" ");

            //sayfa içindeki metin içerikleri al
            var chunks = new List<string>();
            foreach (var item in document.DocumentNode.DescendantsAndSelf())
            {
                if (item.NodeType == HtmlNodeType.Text)
                {
                    var parentNode = item.ParentNode.Name.ToUpper();
                    if (parentNode != "STYLE" && parentNode != "SCRIPT" && item.InnerText.Trim() != "")
                    {
                        chunks.Add(ClearSpecialChars(item.InnerText.Trim()));
                    }
                }
            }
            if (chunks != null && chunks.Count() > 0)
            {
                sb.Append(String.Join(" ", chunks));
            }

            //yukarıdaki tüm bilgileri birleştir, temizle ve string olarak geri dön
            return ClearSpecialChars(sb.ToString());
        }

        //Header bilgileri almak için (title ve meta seo tagleri)
        public static string GetTitleText(HtmlDocument document)
        {
            var result = "";
            if (document.DocumentNode.SelectSingleNode("//title") != null)
            {
                result = document.DocumentNode.SelectSingleNode("//title").InnerText;
            }

            return result;
        }

        public static string GetMetaText(HtmlDocument document)
        {
            var result = "";

            //meta dataları da al
            var list = document.DocumentNode.SelectNodes("//meta");
            if (list != null)
            {
                foreach (var node in list)
                {
                    string name = node.GetAttributeValue("name", "");
                    if (name.ToLower() == "description" || name.ToLower() == "keywords" || name.ToLower() == "author")
                    {
                        result += node.GetAttributeValue("content", "");
                    }
                }
            }

            return result;
        }

        //resimlerin alt taglerinde yazan içerikleri alıyoruz
        public static string GetImgAltText(HtmlDocument document)
        {
            string result = "";
            var altTexts = document.DocumentNode.Descendants("img")
                                .Select(e => e.GetAttributeValue("alt", null))
                                .Where(s => !String.IsNullOrEmpty(s));

            if (altTexts != null && altTexts.Count() > 0)
            {
                result = String.Join(" ", altTexts.ToList());
            }

            return result;
        }

        //gereksiz karakterleri temizleyip sadece içeriği bırakıyoruz. (regex ile de yapılabilir)
        public static string ClearSpecialChars(this string str)
        {
            str = str.Replace("&copy", "");
            str = str.Replace("&COPY", "");
            str = str.Replace("&Copy", "");
            string pattern = @"[^a-zA-Z0-9 ıİüÜşŞçÇğĞöÖ]";
            str = Regex.Replace(str, pattern, "");
            str = str.Replace("  ", " ");

            #region CommentBlock
            //str = str.Replace(".", "");
            //str = str.Replace(",", "");
            //str = str.Replace(";", "");
            //str = str.Replace(":", "");
            //str = str.Replace("-", "");
            //str = str.Replace("_", "");
            //str = str.Replace("?", "");
            //str = str.Replace("*", "");
            //str = str.Replace("\\", "");
            //str = str.Replace("=", "");
            //str = str.Replace("}", "");
            //str = str.Replace(")", "");
            //str = str.Replace("]", "");
            //str = str.Replace("(", "");
            //str = str.Replace("[", "");
            //str = str.Replace("/", "");
            //str = str.Replace("{", "");
            //str = str.Replace("&", "");
            //str = str.Replace("%", "");
            //str = str.Replace("½", "");
            //str = str.Replace("+", "");
            //str = str.Replace("$", "");
            //str = str.Replace("^", "");
            //str = str.Replace("#", "");
            //str = str.Replace("'", "");
            //str = str.Replace("£", "");
            //str = str.Replace("!", "");
            //str = str.Replace("~", "");
            //str = str.Replace("`", "");
            //str = str.Replace("´", "");
            //str = str.Replace("¨", "");
            //str = str.Replace("\"", "");
            #endregion CommentBlock

            return str;
        }

        public static UrlModel GetPageInfoByUrl(string baseUrl)
        {
            var result = new UrlModel { Level = 1, Url = baseUrl };
            var processed = new List<String> { baseUrl };
            var document = Crawler.GetHtmlContent(baseUrl);

            result.WordList = GetWordsAsList(document, baseUrl);

            foreach (HtmlNode link in document.DocumentNode.SelectNodes("//a[@href]"))
            {
                var href = link.Attributes.FirstOrDefault(x => x.Name.ToUpper() == "HREF");
                if (href != null && !href.Value.ToUpper().StartsWith("TEL") && !href.Value.ToUpper().StartsWith("MAILTO") && GetBaseUrl(ConvertUrl(href.Value, baseUrl)) == GetBaseUrl(ConvertUrl(baseUrl, baseUrl)))
                {
                    var tempPointer = new UrlModel { Level = 2, Url = ConvertUrl(href.Value, baseUrl) };
                    var convertedHref = ConvertUrl(href.Value, baseUrl);
                    if (!processed.Any(x => x.ToUpper() == convertedHref.ToUpper()))
                    {
                        processed.Add(convertedHref);
                        result.ChildUrlList.Add(new UrlModel { Level = 2, Url = convertedHref });
                    }
                }
            }

            GetSecondLevelUrls(result, processed);

            return result;
        }

        public static UrlModel GetSecondLevelUrls(UrlModel model, List<String> processed)
        {
            foreach (var item in model.ChildUrlList)
            {
                var document = Crawler.GetHtmlContent(item.Url);

                item.WordList = GetWordsAsList(document, item.Url);

                foreach (HtmlNode link in document.DocumentNode.SelectNodes("//a[@href]"))
                {
                    var href = link.Attributes.FirstOrDefault(x => x.Name.ToUpper() == "HREF");
                    if (href != null && !href.Value.ToUpper().StartsWith("TEL") && !href.Value.ToUpper().StartsWith("MAILTO") && GetBaseUrl(ConvertUrl(href.Value, model.Url)) == GetBaseUrl(ConvertUrl(model.Url, model.Url)))
                    {
                        var tempPointer = new UrlModel { Level = 2, Url = ConvertUrl(href.Value, model.Url) };
                        var convertedHref = ConvertUrl(href.Value, model.Url);
                        if (!processed.Any(x => x.ToUpper() == convertedHref.ToUpper()))
                        {
                            processed.Add(convertedHref);
                            var thirdLevelDocument = Crawler.GetHtmlContent(convertedHref);
                            item.ChildUrlList.Add(new UrlModel { Level = 3, Url = convertedHref, WordList = GetWordsAsList(thirdLevelDocument, convertedHref) });
                        }
                    }
                }
            }

            return model;
        }

        public static string GetBaseUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "";
            }
            else
            {
                url = url.ToUpper();
                url = url.Replace("HTTP://", "");
                url = url.Replace("HTTPS://", "");
                var splitter = url.Split("/");
                if (splitter != null && splitter.Length > 0)
                {
                    url = splitter[0];
                }
                else
                {
                    splitter = url.Split("?");
                    if (splitter != null && splitter.Length > 0)
                    {
                        url = splitter[0];
                    }
                }
                return url;
            }
        }

        public static string ConvertUrl(string url, string baseUrl)
        {
            var upperUrl = url.ToUpper();
            if (!upperUrl.StartsWith("HTTP"))
            {
                if (upperUrl.StartsWith("/"))
                {
                    if (upperUrl == "/" || upperUrl == "/#")
                    {
                        url = baseUrl;
                    }
                    else
                    {
                        url = baseUrl + url;
                    }
                }
                else
                {
                    if (upperUrl == "#")
                    {
                        url = baseUrl;
                    }
                    else
                    {
                        url = baseUrl + "/" + url;
                    }
                }
            }
            return url;
        }
    }
}
