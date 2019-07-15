// Sitecore.Data.StandardValuesProvider
using Sitecore;
using Sitecore.Caching;
using Sitecore.Collections;
using Sitecore.Common;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Engines;
using Sitecore.Data.Engines.DataCommands;
using Sitecore.Data.Events;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.LanguageFallback;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;

namespace Sitecore.Support.Data
{



    /// <summary>
    /// StandardValuesProvider
    /// </summary>
    public class StandardValuesProvider : Sitecore.Data.StandardValuesProvider
    {
        /// <summary>
        /// Gets the standard value of a field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        public override string GetStandardValue(Field field)
        {
            Assert.ArgumentNotNull(field, "field");
            if (field.ID == FieldIDs.SourceItem || field.ID == FieldIDs.Source)
            {
                return string.Empty;
            }

            SafeDictionary<ID, string> standardValues = GetStandardValues(field.Item);
            if (standardValues == null)
            {
                return null;
            }

            return standardValues[field.ID];
        }


        /// <summary>
        /// Adds the standard values of a template to a dictionary.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="database">The database.</param>
        /// <param name="language">The language.</param>
        /// <param name="result">The result.</param>
        private void AddStandardValues(Template template, Database database, Language language,
            SafeDictionary<ID, string> result)
        {
            ID standardValueHolderId = template.StandardValueHolderId;
            if (!ID.IsNullOrEmpty(standardValueHolderId))
            {
                bool? currentValue = Switcher<bool?, LanguageFallbackItemSwitcher>.CurrentValue;
                Item item = default(Item);
                if (currentValue == false)
                {
                    try
                    {
                        Switcher<bool?, LanguageFallbackItemSwitcher>.Exit();
                        item = ItemManager.GetItem(standardValueHolderId, language, Version.Latest, database,
                            SecurityCheck.Disable);
                    }
                    finally
                    {
                        Switcher<bool?, LanguageFallbackItemSwitcher>.Enter(currentValue);
                    }
                }
                else
                {
                    item = ItemManager.GetItem(standardValueHolderId, language, Version.Latest, database,
                        SecurityCheck.Disable);
                }

                if (item != null)
                {
                    foreach (Field field in item.Fields)
                    {
                        if (!result.ContainsKey(field.ID))
                        {
                            string value = field.GetValue(false, true);
                            if (value != null)
                            {
                                result[field.ID] = value;
                            }
                        }
                    }

                    if (!result.ContainsKey(FieldIDs.StandardValueHolderId))
                    {
                        result[FieldIDs.StandardValueHolderId] = standardValueHolderId.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Adds the standard values to cache.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="values">The values.</param>
        private void AddStandardValuesToCache(Item item, SafeDictionary<ID, string> values)
        {
            StandardValuesCache standardValuesCache = item.Database.Caches.StandardValuesCache;
            standardValuesCache.AddStandardValues(item, values);
        }

        /// <summary>
        /// Gets the standard values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private SafeDictionary<ID, string> GetStandardValues(Item item)
        {
            if (ID.IsNullOrEmpty(item.TemplateID))
            {
                return null;
            }

            SafeDictionary<ID, string> standardValuesFromCache = GetStandardValuesFromCache(item);
            if (standardValuesFromCache != null)
            {
                return standardValuesFromCache;
            }

            standardValuesFromCache = ReadStandardValues(item.TemplateID, item.Database, item.Language);


            if (LanguageFallbackItemSwitcher.CurrentValue == null)
            {
                if (Sitecore.Context.Site == null || Sitecore.Context.Site.Name == "publisher" ||
                    Sitecore.Context.Site.Name == "scheduler")
                {
                    return standardValuesFromCache;
                }
            }

            AddStandardValuesToCache(item, standardValuesFromCache);
            return standardValuesFromCache;
        }

        /// <summary>
        /// Gets the standard values from cache.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private SafeDictionary<ID, string> GetStandardValuesFromCache(Item item)
        {
            StandardValuesCache standardValuesCache = item.Database.Caches.StandardValuesCache;
            return standardValuesCache.GetStandardValues(item);
        }
        
        /// <summary>
        /// Gets the standard values.
        /// </summary>
        /// <param name="templateId">The template id.</param>
        /// <param name="database">The database.</param>
        /// <param name="language">The language.</param>
        /// <returns></returns>
        private SafeDictionary<ID, string> ReadStandardValues(ID templateId, Database database, Language language)
        {
            SafeDictionary<ID, string> result = new SafeDictionary<ID, string>();
            Template template = TemplateManager.GetTemplate(templateId, database);
            if (template == null)
            {
                return result;
            }

            AddStandardValues(template, database, language, result);
            TemplateList baseTemplates = template.GetBaseTemplates();
            foreach (Template item in baseTemplates)
            {
                AddStandardValues(item, database, language, result);
            }

            return result;
        }
    }
}