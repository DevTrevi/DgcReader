using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic
{
    /// <summary>
    /// Service for retrieving affected fields data from a json payload
    /// </summary>
    public interface IAffectedFieldsDataRetriever
    {
        /// <summary>
        /// Returns data affected by the rule validation
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="dataJsonNode"></param>
        /// <param name="certificateType"></param>
        /// <returns></returns>
        string GetAffectedFieldsData(RuleEntry rule, JObject dataJsonNode, CertificateType certificateType);
    }

    /// <inheritdoc/>
    public class DefaultAffectedFieldsDataRetriever : IAffectedFieldsDataRetriever
    {
        private readonly JObject SchemaJson;
        private readonly ILogger? logger;

        /// <summary>
        /// Cosntructor
        /// </summary>
        /// <param name="logger"></param>
        public DefaultAffectedFieldsDataRetriever(ILogger? logger)
        {
            SchemaJson = JObject.Parse(Encoding.UTF8.GetString(Resources.CovPassSdk.json_schema_v1));
            this.logger = logger;
        }

        /// <inheritdoc/>
        public string GetAffectedFieldsData(RuleEntry rule, JObject dataJsonNode, CertificateType certificateType)
        {
            var affectedFields = new StringBuilder();

            foreach(var affectedField in rule.AffectedString)
            {
                string? description = null;
                try
                {
                    var token = SchemaJson.SelectToken(GetSchemaPath(certificateType, affectedField.Split('.').Last()));
                    description = token?.ToString();
                }
                catch (Exception e)
                {
                    logger?.LogWarning($"Error while reading affected field description from schema: {e}");
                }

                string? value = null;
                try
                {
                    var token = dataJsonNode.SelectToken(ToJsonNetPath($"payload.{affectedField}"));
                    value = token?.ToString();
                }
                catch (Exception e)
                {
                    logger?.LogWarning($"Error while reading payload affected field from dataJsonNode: {e}");
                }


                if (!string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(value))
                    affectedFields.AppendLine($"{description}: {value}");
            }

            return affectedFields.ToString().Trim();
        }


        private string GetSchemaPath(CertificateType certificateType, string key)
        {
            var subPath = "";
            switch (certificateType)
            {
                case CertificateType.TEST:
                    subPath = "test_entry";
                    break;
                case CertificateType.RECOVERY:
                    subPath = "recovery_entry";
                    break;
                case CertificateType.VACCINATION:
                    subPath = "vaccination_entry";
                    break;
            }

            return ToJsonNetPath($"$defs.{subPath}.properties.{key}.description");
        }

        private string ToJsonNetPath(string path)
        {
            var splitted = path.Split('.');

            var s = "";
            foreach (var item in splitted)
            {
                if(int.TryParse(item, out var value))
                {
                    s += $"[{item}]";
                }
                else
                {
                    s += $".{item}";
                }
            }
            return s.Trim('.');
        }

    }
}