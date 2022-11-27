﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Xsl;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using StackExchange.DataExplorer.Models;

namespace StackExchange.DataExplorer.Helpers
{
    public enum ResultColumnType
    {
        Default,
        Post,
        User,
        Number,
        Date,
        Text,
        Url, 
        SuggestedEdit,
        Site,
        Comment
    }

    public class ResultColumnInfo
    {
        public static readonly Dictionary<Type, ResultColumnType> ColumnTypeMap = new Dictionary<Type, ResultColumnType>
        {
            {typeof(int), ResultColumnType.Number},
            {typeof(long), ResultColumnType.Number},
            {typeof(float), ResultColumnType.Number},
            {typeof(double), ResultColumnType.Number},
            {typeof(decimal), ResultColumnType.Number},
            {typeof(string), ResultColumnType.Text},
            {typeof(DateTime), ResultColumnType.Date}
        };

        public ResultColumnInfo()
        {
            Type = ResultColumnType.Default;
        }

        public string Name { get; set; }

        [JsonConverter(typeof (StringEnumConverter))]
        public ResultColumnType Type { get; set; }
    }

    public class MagicResult
    {
        public int Id { get; set; }
        public string Title { get; set; }

        public override string ToString() => Id.ToString();
    }

    public class MagicResultConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(object) == objectType;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                return serializer.Deserialize(reader, typeof(MagicResult));
            }

            return serializer.Deserialize(reader, reader.ValueType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    public class ResultSet
    {
        public ResultSet()
        {
            Columns = new List<ResultColumnInfo>();
            Rows = new List<List<object>>();
        }

        public List<ResultColumnInfo> Columns { get; set; }
        public List<List<object>> Rows { get; set; }
        // the position of the message when we started rendering this result set
        //  required so we can render in text
        public int MessagePosition { get; set; }
        public bool Truncated { get; set; }
    }

    public class QueryResults
    {
        private const int MAX_TEXT_COLUMN_WIDTH = 512;
        // based on this date format yyyy-MM-dd HH:mm:ss 
        // you'll need 19 characters to align textresult columns
        private const int DATE_COLUMN_WIDTH = 19; 

        private static readonly List<ResultColumnType> _nativeTypes =
            new List<ResultColumnType>
            {
                ResultColumnType.Date,
                ResultColumnType.Default,
                ResultColumnType.Number,
                ResultColumnType.Text
            };

        public QueryResults()
        {
            ResultSets = new List<ResultSet>();
            FirstRun = DateTime.UtcNow.ToString("MMM %d yyyy");
            Messages = "";
        }

        public List<ResultSet> ResultSets { get; set; }

        /// <summary>
        /// Gets and sets the xml query execution plan associated with the query results.
        /// </summary>
        public string ExecutionPlan { get; set; }

        public TargetSites TargetSites { get; set; }
        public string Messages { get; set; }
        public string Url { get; set; }
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public int QueryId { get; set; }
        public int TotalResults { get; set; }
        public int MaxResults { get; set; }
        public string FirstRun { get; set; }
        public bool Truncated { get; set; }
        public string Slug { get; set; }
        public bool TextOnly { get; set; }
        public int? ParentId { get; set; }
        public int RevisionId { get; set; }
        public bool FromCache { get; set; }
        public int QuerySetId { get; set; }

        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime? Created { get; set; }

        /// <summary>
        /// Execution time in Millisecs
        /// </summary>
        public long ExecutionTime { get; set; }

        public static JsonSerializerSettings GetSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new MagicResultConverter() }
            };
        }

        public static QueryResults FromJson(string json) => 
            JsonConvert.DeserializeObject<QueryResults>(json, GetSettings());

        public QueryResults WithCache(CachedResult cache)
        {
            if (cache?.Results == null)
            {
                return this;
            }

            Messages = cache.Messages;
            ResultSets = JsonConvert.DeserializeObject<List<ResultSet>>(cache.Results);
            ExecutionPlan = cache.ExecutionPlan;

            return this;
        }

        public QueryResults ToTextResults() => new QueryResults
        {
            ExecutionTime = ExecutionTime,
            FirstRun = FirstRun,
            MaxResults = MaxResults,
            QueryId = QueryId,
            SiteId = SiteId,
            SiteName = SiteName,
            TextOnly = true,
            Truncated = Truncated,
            Url = Url,
            Slug = Slug,
            TargetSites = TargetSites,
            ExecutionPlan = ExecutionPlan,
            ParentId = ParentId,
            RevisionId = RevisionId,
            Created = Created,
            QuerySetId = QuerySetId,
            Messages = FormatTextResults(Messages, ResultSets)
        };

        public QueryResults TransformQueryPlan() => new QueryResults
        {
            ExecutionTime = ExecutionTime,
            FirstRun = FirstRun,
            MaxResults = MaxResults,
            QueryId = QueryId,
            SiteId = SiteId,
            SiteName = SiteName,
            TextOnly = TextOnly,
            Truncated = Truncated,
            Url = Url,
            Slug = Slug,
            TargetSites = TargetSites,
            ResultSets = ResultSets,
            Messages = Messages,
            ParentId = ParentId,
            RevisionId = RevisionId,
            Created = Created,
            QuerySetId = QuerySetId,
            ExecutionPlan = TransformPlan(ExecutionPlan)
        };

        public static string FormatTextResults(string messages, List<ResultSet> resultSets)
        {
            var buffer = new StringBuilder();
            int messagePosition = 0;
            int length;

            foreach (var resultSet in resultSets)
            {
                length = resultSet.MessagePosition - messagePosition;
                if (length > 0)
                {
                    buffer.Append(messages.Substring(messagePosition, length));
                }

                messagePosition = resultSet.MessagePosition;

                buffer.AppendLine(FormatResultSet(resultSet));
            }

            length = messages.Length - messagePosition;
            if (length > 0)
            {
                buffer.Append(messages.Substring(messagePosition, length));
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Transforms an xml execution plan into html.
        /// </summary>
        /// <param name="plan">Xml query plan as a string.</param>
        /// <returns>Html query plan as a string.</returns>
        private static string TransformPlan(string plan)
        {
            if (string.IsNullOrEmpty(plan))
            {
                return null;
            }

            using (var reader = XmlReader.Create(new StringReader(plan)))
            {
                XslCompiledTransform t = new XslCompiledTransform(true);
                t.Load(HttpContext.Current.Server.MapPath(@"~/Content/qp/qp.xslt"));

                StringBuilder returnValue = new StringBuilder();

                using (var writer = XmlWriter.Create(returnValue, t.OutputSettings))
                {
                    t.Transform(reader, writer);
                }

                return returnValue.ToString();
            }
        }

        private static string FormatResultSet(ResultSet resultSet)
        {
            var buffer = new StringBuilder();
            List<int> maxLengths = GetMaxLengths(resultSet);

            for (int j = 0; j < maxLengths.Count; j++)
            {
                maxLengths[j] = Math.Min(maxLengths[j], MAX_TEXT_COLUMN_WIDTH);
                buffer.Append(resultSet.Columns[j].Name.PadRight(maxLengths[j] + 1, ' '));
            }
            buffer.AppendLine();

            for (int k = 0; k < maxLengths.Count; k++)
            {
                buffer.Append("".PadRight(maxLengths[k], '-'));
                buffer.Append(" ");
            }

            buffer.AppendLine();

            foreach (var row in resultSet.Rows)
            {
                for (int i = 0; i < resultSet.Columns.Count; i++)
                {
                    object col = row[i];

                    string currentVal;
                    if (_nativeTypes.Contains(resultSet.Columns[i].Type))
                    {
                        if (col != null && resultSet.Columns[i].Type == ResultColumnType.Date)
                        {
                            // if you change the format, also adapt DATE_COLUMN_WIDTH 
                            currentVal = Util.FromJavaScriptTime((long)col).ToString("yyyy-MM-dd HH:mm:ss"); 
                        }
                        else
                        {
                            currentVal = (col ?? "null").ToString();
                        }
                    }
                    else
                    {
                        var data = col as JContainer;
                        if (data?["title"] != null)
                        {
                            currentVal = (data.Value<string>("title") ?? "null");
                        }
                        else
                        {
                            var sInfo = col as SiteInfo;
                            currentVal = sInfo != null ? sInfo.Name : "null";
                        }
                    }
                    buffer.Append(currentVal.PadRight(maxLengths[i] + 1, ' '));
                }
                buffer.AppendLine();
            }

            return buffer.ToString();
        }

        private static List<int> GetMaxLengths(ResultSet resultSet)
        {
            var maxLengths = new List<int>();

            foreach (var colInfo in resultSet.Columns)
            {
                maxLengths.Add(colInfo.Name.Length);
            }

            foreach (var row in resultSet.Rows)
            {
                for (int i = 0; i < resultSet.Columns.Count; i++)
                {
                    var col = row[i];

                    int curLength;
                    if (_nativeTypes.Contains(resultSet.Columns[i].Type))
                    {
                        // Date is formatted later for textresults!
                        if (col != null && resultSet.Columns[i].Type == ResultColumnType.Date)
                        {
                            // col contains a long 
                            // for textresults it is formatted later
                            // instead of taking its length
                            // use the length that will come out after
                            // the String.Format is applied
                            curLength = DATE_COLUMN_WIDTH; 
                        }
                        else
                        {
                            curLength = col?.ToString().Length ?? 4;
                        }
                    }
                    else
                    {
                        var data = col as JContainer;
                        if (data == null)
                        {
                            curLength = 4;
                        }
                        else
                        {
                            curLength = (data.Value<string>("title") ?? "").Length;
                        }
                    }
                    maxLengths[i] = Math.Max(curLength, maxLengths[i]);
                }
            }
            return maxLengths;
        }

        public string ToJson() =>
            JsonConvert.SerializeObject(
                this,
                Newtonsoft.Json.Formatting.Indented,
                GetSettings()
            );

        public string GetJsonResults() =>
            JsonConvert.SerializeObject(
                ResultSets,
                Newtonsoft.Json.Formatting.None,
                GetSettings()
            );
    }
}