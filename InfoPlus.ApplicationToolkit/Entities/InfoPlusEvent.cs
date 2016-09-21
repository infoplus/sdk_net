using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Studio.Foundation.Json;
using Studio.Foundation.Json.Core.Conversion;

namespace InfoPlus.ApplicationToolkit.Entities
{
    public class InfoPlusEvent
    {
        public const string KEY_URL = "_VAR_URL";

        // who, sometimes can also be found in Step.AssignUser
        public InfoPlusUser User { get; set; }
        // when
        public long When { get; set; }

        // what data it is,
        // especially, ON_FIELD_CHANGING will assign value here to indicate: 
        //      which field changed, changed to what.
        public IDictionary<string, object> FormData { get; set; }

        // where the data located.
        // public string Path { get; set; }
        // Field Events
        public string Group { get; set; }
        public string Field { get; set; }
        public string FieldPath { get; set; }

        // suggestion data
        public CodeSuggestion Suggestion { get; set; }

        // how could the data be?
        public IList<FormField> Fields { get; set; }

        // which step/instance/definition/form/application
        public FormStep Step { get; set; }

        // which action & users are chosen
        public string ActionCode { get; set; }
        public string ActionName { get; set; }
        public IList<FormStep> NextSteps { get; set; }
        public FormStep EndStep { get; set; }

        // result
        public ResponseEntity<object> Result { get; set; }

        /// <summary>
        /// Whether release/beta instance 
        /// </summary>
        public bool Release { get; set; }

        /// <summary>
        /// AccessTokens for resources that: defined in workflow resources, granted by user, fetched by engine GUI
        /// only available in Events that after user interact
        /// </summary>
        public IDictionary<string, string> Tokens { get; set; }

        // where, can be found in this.Step.RenderUri.

        public TChangedEntity LocateChangedObject<TChangedEntity>(InfoPlusEntity entity) where TChangedEntity : class
        {
            FormField eventField = this.EventField;
            string[] objNames;

            if (eventField == null)
            {
                if (this.Group == null)
                {
                    return default(TChangedEntity);
                }
                FormField fakeField = new FormField {GroupName = this.Group};
                objNames = fakeField.GroupObjectNames;
            } else {
                objNames = eventField.GroupObjectNames;
            }
            if (entity is TChangedEntity)
            {
                return entity as TChangedEntity;
            }

            
            string[] paths = this.FieldPath.Split('_');
            int i = 1;
            Object result = entity;
            foreach (var objName in objNames)
            {
                Type type = result.GetType();
                PropertyInfo property = type.GetProperty(objName);
                result = ((IList)property.GetValue(result, null))[int.Parse(paths[i++])];
                if (result is TChangedEntity)
                {
                    return (TChangedEntity)result;
                }
            }

            return default(TChangedEntity);
        }

        public TSplitEntity LocateSplitObject<TSplitEntity>(InfoPlusEntity entity) where TSplitEntity : class
        {
            string splitIdentifier = this.Step.SplitIdentifier;
            string[] paths = splitIdentifier.Split(new char[] { ',' }, StringSplitOptions.None);
            if (paths.Length == 1 || string.IsNullOrEmpty(paths[2]))
            {
                return entity as TSplitEntity;
            }
            return InfoPlusEntity.Locate<TSplitEntity>(entity, paths[2]);
        }

        public FormField EventField
        {
            get
            {
                return Fields.FirstOrDefault(field => field.Name == this.Field);
            }
        }

        public IList<FormField> ChangeableFields
        {
            get
            {
                FormField eventField = this.EventField;
                return this.Fields.Where(field => eventField.GroupName == field.GroupName).ToList();
            }
        }


        public static bool ArrayEqual(object[] a1, object[] a2)
        {
            if (a1.Length != a2.Length) return false;
            for (var i = 0; i < a1.Length; i++)
            {
                if (a1[i].GetType().IsArray && a2[i].GetType().IsArray)
                {
                    if (ArrayEqual((object[])a1[i], (object[])a2[i]) == false)
                    {
                        return false;
                    }
                }
                else
                {
                    if (a1[i] != a2[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public IDictionary<string, object> ConvertChanged<T>(T changedEntity) where T : InfoPlusEntity
        {
            IDictionary<string, object> result = new Dictionary<string, object>();
            result.Add("form", JsonConvert.ExportToString(changedEntity));
            if (changedEntity.EntityPath != null)
            {
                result.Add("path", changedEntity.EntityPath);
            }
            return result;
        }

        /// <summary>
        /// query params from from start/render uri
        /// </summary>
        public IDictionary<string, string> CustomizedQueryParams
        {
            get
            {
                try
                {
                    string urlAttrsJson = (string)this.FormData[KEY_URL + InfoPlusEntity.FIELD_SUFFIX_ATTR];
                    IDictionary<string, string> urlAttrs = JsonConvert.Import<IDictionary<string, string>>(urlAttrsJson);
                    return (null != urlAttrs) ? urlAttrs : new Dictionary<string, string>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex);
                    return new Dictionary<string, string>();
                }
            }
        }

        /*
        public IDictionary<string, object> ConvertChanged<T>(T changedEntity)
        {
            IList<FormField> changeableFields = this.ChangeableFields;
            T originalEntity = InfoPlusEntity.Convert<T>(this);
            var oldDic = InfoPlusEntity.Convert(originalEntity, changeableFields);
            var newDic = InfoPlusEntity.Convert(changedEntity, changeableFields);
            IDictionary<string, object> result = new Dictionary<string, object>();

            string[] paths = this.FieldPath.Split('_');

            foreach (var pair in newDic)
            {
                if (!oldDic.ContainsKey(pair.Key))
                {
                    continue;
                }
                var oldValue = oldDic[pair.Key];
                var newValue = pair.Value;
                if (oldValue.GetType().IsArray && newValue.GetType().IsArray)
                {

                    if (paths.Length > 1)
                    {
                        bool foundErr = false;
                        for (int i = 1; i < paths.Length; i++)
                        {
                            if (!oldValue.GetType().IsArray || !newValue.GetType().IsArray)
                            {
                                foundErr = true;
                                break;
                            }
                            object[] oldValueArray = (object[])oldValue;
                            object[] newValueArray = (object[])newValue;
                            var index = int.Parse(paths[i]);
                            if (index >= oldValueArray.Length || index >= newValueArray.Length)
                            {
                                foundErr = true;
                                break;
                            }
                            oldValue = oldValueArray[index];
                            newValue = newValueArray[index];
                        }
                        if (!foundErr)
                        {
                            if (oldValue != null)
                            {
                                if (!oldValue.Equals(newValue))
                                {
                                    result[pair.Key] = newValue;
                                }
                            }
                            else
                            {
                                if (newValue != null)
                                {
                                    result[pair.Key] = newValue;
                                }
                            }    
                        }
                    }
                }
                else
                {
                    if (oldValue != null)
                    {
                        if (!oldValue.Equals(newValue))
                        {
                            result.Add(pair);
                        }
                    }
                    else
                    {
                        if (newValue != null)
                        {
                            result[pair.Key] = newValue;
                        }
                    }
                }
            }
            return result;
        }
        */
    }
}
