using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Studio.Foundation.Json;
using Studio.Foundation.Json.Core.Conversion;

namespace InfoPlus.ApplicationToolkit.Entities
{
    public class InfoPlusEntity
    {
        static readonly string FIELD_PREFIX = "field";
        static readonly string GROUP_PREFIX = "group";
        public static readonly string FIELD_SUFFIX_NAME = "_Name";
        public static readonly string FIELD_SUFFIX_ATTR = "_Attr";


        /// <summary>
        /// Instance Id for the current Instance
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Business Id passed from the Application while Intance Starting.
        /// </summary>
        public string BusinessId { get; set; }

        /// <summary>
        /// Access Uri, if any.
        /// </summary>
        public string RenderUri { get; set; }

        /// <summary>
        /// Owner of the Entity, most probably, a Form.
        /// </summary>
        public string InstanceOwner { get; set; }

        private int _entityIndex = -1;
        public int EntityIndex
        {
            get { return _entityIndex; }
            set { _entityIndex = value; }
        }

        // public InfoPlusUser workflowOperator { get; set; }
        // public DateTime ? operateTime { get; set; }

        public static T Convert<T>(InfoPlusEvent e)
        {
            if (null == e) return default(T);

            // 1. FormData
            T o = default(T);
            InfoPlusEntity.Convert<T>(e.FormData, e.Fields, ref o);

            // 2.InfoPlusEntity Related
            InfoPlusEntity en = o as InfoPlusEntity;
            if (null != en && null != e.Step)
            {
                // RenderUri
                try { en.RenderUri = e.Step.RenderUri; }
                catch (Exception) { }

                // Instance
                if(null != e.Step.Instance)
                {
                    en.InstanceId = e.Step.Instance.InstanceId;
                    en.BusinessId = e.Step.Instance.BusinessId;
                    if(null != e.Step.Instance.Creator)
                        en.InstanceOwner = e.Step.Instance.Creator.Account;
                }

                // en.workflowOperator = e.User;
                // en.operateTime = UnixTime.ToDateTime(e.When);
            }

            return o;
        }
        

        // Convert data recursively.
        public static T Convert<T>(IDictionary<string, object> data, IList<FormField> fields, ref T o)
        {
            if (null == data) return default(T);
            Type type = typeof(T);
            object x = o;
            o = (T)InfoPlusEntity.Convert(data, fields, ref x, 0, type, 0);
            return o;
        }

        static object Convert(IDictionary<string, object> data, IList<FormField> fields, ref object o, int depth, Type type, int index)
        {
            depth++;
            // not created before? create it.
            if (null == o)
            {
                o = Activator.CreateInstance(type);
                var propIndex = type.GetProperty("EntityIndex");
                if (null != propIndex && propIndex.PropertyType == typeof(int))
                {
                    propIndex.SetValue(o, index, null);
                }
                /*
                var fieldIndex = type.GetField("EntityIndex");
                if (null != fieldIndex && fieldIndex.FieldType == typeof (int))
                {
                    fieldIndex.SetValue(o, index);
                }
                */
            }

            if (null == o) throw new Exception("Activator.CreateInstance<T> failed. where T is " + type.FullName);

            foreach (string key in data.Keys)
            {
                string property;
                string fieldName;
                bool isName = InfoPlusEntity.ParsePropertyName(key, out property, out fieldName);
                
                object val = data[key];
                Array arr = InfoPlusEntity.ObjectToArray(val);
                // Primitive.
                if (null == arr)
                {
                    InfoPlusEntity.SetValue(o, property, val);
                }
                else
                {
                    // If key is "_Name", find the origal fieldName and resize the Array
                    Array a0 = null;
                    if (isName) 
                    {
                        if (false == data.ContainsKey(fieldName))
                            arr = new object[0];
                        else
                        {
                            object v0 = data[fieldName];
                            a0 = InfoPlusEntity.ObjectToArray(v0);
                            if (null == a0 || 0 == a0.Length)
                                arr = new object[0];
                            else if (a0.Length < arr.Length)
                                arr = arr.Cast<object>().Take(a0.Length).ToArray();
                        }
                    }

                    string groupName = null;
                    if (null != fields)
                    {
                        var field = (from f in fields where f.Name == fieldName select f).FirstOrDefault();
                        if (null != field)
                        {
                            string[] groupNames = field.GroupName.Split(new string[] { "//" },
                                StringSplitOptions.RemoveEmptyEntries);
                            // valid?
                            if (groupNames.Length <= depth) continue;

                            groupName = groupNames[depth];
                            groupName = ParseGroupName(groupName);
                        }
                    }
                    // groupName found, try to find/create property as an array.
                    if (null != groupName)
                    {
                        PropertyInfo group = type.GetProperty(groupName);
                        if (null == group) continue;
                        Type groupType = group.PropertyType;
                        if (InfoPlusEntity.IsArray(groupType))
                        {
                            // get element type
                            Type elementType = groupType.IsGenericType ?
                                groupType.GetGenericArguments()[0] : groupType.GetElementType();

                            for (int i = 0; i < arr.Length; i++)
                            {
                                object groupObject = group.GetValue(o, null);
                                // 1. Create
                                if (null == groupObject)
                                {
                                    // Create one.
                                    if (groupType.IsArray)
                                    {
                                        groupType = elementType.MakeArrayType();
                                        groupObject = Activator.CreateInstance(groupType, arr.Length);
                                    }
                                    else
                                    {
                                        // Generic Interface
                                        if (true == groupType.IsInterface)
                                            groupType = typeof(List<>).MakeGenericType(elementType);
                                        groupObject = Activator.CreateInstance(groupType);
                                    }
                                    group.SetValue(o, groupObject, null);
                                }
                                // 2.Resize
                                if (groupType.IsArray)
                                {
                                    var groupArray = ((Array)groupObject);
                                    int oldLength = groupArray.Length;
                                    // Resize & Clone
                                    if (oldLength < arr.Length)
                                    {
                                        MethodInfo method = typeof(Array).GetMethod("Resize");
                                        MethodInfo generic = method.MakeGenericMethod(elementType);
                                        object[] args = new object[] { groupObject, arr.Length };
                                        generic.Invoke(null, args);
                                        for (int n = 0; n < oldLength; n++)
                                            ((Array)args[0]).SetValue(groupArray.GetValue(n), n);
                                        groupObject = args[0];
                                        group.SetValue(o, groupObject, null);
                                    }
                                }

                                groupObject = group.GetValue(o, null);
                                groupType = groupObject.GetType();


                                Array ga = InfoPlusEntity.ObjectToArray(groupObject);
                                object elementObject = null;
                                // Load
                                if (i < ga.Length)
                                    elementObject = ga.GetValue(i);

                                IDictionary<string, object> d = new Dictionary<string, object>();
                                d.Add(key, arr.GetValue(i));
                                if (isName && null != a0)
                                    d.Add(fieldName, a0.GetValue(i));
                                InfoPlusEntity.Convert(d, fields, ref elementObject, depth, elementType, i);

                                // Save
                                if (groupType.IsArray)
                                {
                                    ((Array)groupObject).SetValue(elementObject, i);
                                }
                                else if (i >= ga.Length)
                                {
                                    groupType.InvokeMember("Add",
                                        BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public,
                                        null, groupObject, new object[] { elementObject });
                                }

                            }
                        }
                    }
                }
            }
            return o;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns>remove prefix "group"</returns>
        static string ParseGroupName(string groupName)
        {
            if (null != groupName && groupName.StartsWith(InfoPlusEntity.GROUP_PREFIX))
                return groupName.Substring(InfoPlusEntity.GROUP_PREFIX.Length);
            return groupName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="?"></param>
        /// <returns>true if end with "_Name"</returns>
        static bool ParsePropertyName(string key, out string property, out string field)
        {
            field = property = key;
            if (null == key) return false;
            bool isName = key.EndsWith(InfoPlusEntity.FIELD_SUFFIX_NAME);
            if (isName)
                field = key.Substring(0, key.Length - InfoPlusEntity.FIELD_SUFFIX_NAME.Length);

            property = field;
            if (field.StartsWith(InfoPlusEntity.FIELD_PREFIX) && key.Length > InfoPlusEntity.FIELD_PREFIX.Length)
                property = key.Substring(InfoPlusEntity.FIELD_PREFIX.Length);

            return isName;
        }

        static void SetValue(object o, string key, object val)
        {
            if(null == o || string.IsNullOrEmpty(key))
                return;
            string fieldName, propertyName;
            bool isName = InfoPlusEntity.ParsePropertyName(key, out propertyName, out fieldName);

            // get right property
            PropertyInfo property = o.GetType().GetProperty(propertyName);
            if (null == property || false == property.CanWrite) return;

            var v = val;
            Type expect = property.PropertyType;
            string s = null == v ? string.Empty : v.ToString();
            if (expect == typeof(CodeItem) || expect == typeof(InfoPlusUser))
            {
                if (null == val || val.GetType() == typeof(string))
                {
                    object code = property.GetValue(o, null);
                    if (null == code)
                    {
                        code = Activator.CreateInstance(expect);
                        property.SetValue(o, code, null);
                    }
                    string propertyNameOfProperty = isName ?
                        ((expect == typeof(CodeItem)) ? "CodeName" : "TrueName") :
                        ((expect == typeof(CodeItem)) ? "CodeId" : "Account");

                    PropertyInfo propertyOfProperty = expect.GetProperty(propertyNameOfProperty);
                    propertyOfProperty.SetValue(code, val, null);
                }
            }
            else
            {
                if (null != val)
                {
                    Type actual = val.GetType();
                    if (false == expect.Equals(actual))
                    {
                        object parsed;
                        bool result = InfoPlusEntity.TryParse(s, out parsed, expect);
                        if (true == result)
                            v = parsed;
                        else
                            return;
                    }
                }
                // null val for value type? just do nothing.
                else if (true == expect.IsValueType)
                    return;
                property.SetValue(o, v, null);
            }
        }

        static bool TryParse<T>(string s, out T o)
        {
            object x;
            var result = InfoPlusEntity.TryParse(s, out x, typeof(T));
            o = (T)x;
            return result;
        }

        static bool TryParse(string s, out object o, Type type)
        {
            o = InfoPlusEntity.GetDefaultValue(type);
            try
            {
                if (type == typeof(string))
                {
                    o = s;
                    return true;
                }

                if (type.IsPrimitive || type == typeof(decimal) || type == typeof(decimal))
                {
                    object[] args = new object[] { s, o };
                    bool result = (bool)type.InvokeMember("TryParse",
                            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static,
                            null, null, args);
                    o = args[1];
                    // try parse int/long as double
                    if (false == result && (type == typeof(int) || type == typeof(long)))
                    {
                        double d;
                        result = double.TryParse(s, out d);
                        if (true == result)
                        {
                            // NOTE: don't use type == int ? (int)d : (long)d; its wrong. by marstone, 2014/03/04
                            if (type == typeof(int))
                                o = (int)d;
                            else
                                o = (long)d;
                        }
                    }
                    return result;
                }
                else if (type == typeof(DateTime))
                {
                    long timestamp;
                    if (false == long.TryParse(s, out timestamp))
                        timestamp = (long)double.Parse(s);
                    o = UnixTime.ToDateTime(timestamp);
                    return true;
                }
                else if (type.IsGenericType && false == type.IsGenericTypeDefinition)
                {
                    var generic = type.GetGenericTypeDefinition();
                    if (generic == typeof(Nullable<>))
                    {
                        if (s == null)
                        {
                            o = null;
                            return true;
                        }
                        return InfoPlusEntity.TryParse(s, out o, type.GetGenericArguments()[0]);
                    }
                }
                return false;
            }
            catch (Exception) { return false; }
        }

        public static IDictionary<string, object> Convert<T>(T o, IList<FormField> fields)
        {
            IDictionary<string, object> data = null;
            if (null == fields) return data;
            int min = fields.Select(f => InfoPlusEntity.CalculateGroupDepth(f.GroupName)).Min();
            var g0 = fields.Where(f => InfoPlusEntity.CalculateGroupDepth(f.GroupName) == min)
                .Select(f => f.GroupName).First();

            InfoPlusEntity.Convert(o, fields, g0, null, 0, ref data);
            return data;
        }

        static void Convert(object o, IList<FormField> fields, string groupPath, string dataPath, int depth, ref IDictionary<string, object> data)
        {
            if (null == o) return;
            if (null == dataPath) dataPath = string.Empty;
            Type type = o.GetType();
            if (null == data) data = new Dictionary<string, object>();

            // current level, read property directly. 
            // should always be equal, Only, except the initial call.
            string[] groups = groupPath.Split(new string[] { "//" }, StringSplitOptions.RemoveEmptyEntries);
            if (groups.Length > depth + 1)
            {
                string gn = InfoPlusEntity.ParseGroupName(groups[depth + 1]);
                PropertyInfo property = type.GetProperty(gn);
                if (null == property)
                    return;
                object deeps = property.GetValue(o, null);
                Array arr = InfoPlusEntity.ObjectToArray(deeps);
                if (null == arr)
                    return;
                for (int n = 0; n < arr.Length; n++)
                {
                    object deep = arr.GetValue(n);
                    InfoPlusEntity.Convert(deep, fields, groupPath, dataPath + "/" + n, depth + 1, ref data);
                }
            }
            else
            {
                var currentFields = fields.Where(f => f.GroupName == groupPath);
                foreach (FormField field in currentFields)
                {
                    string propertyName, fieldName;
                    InfoPlusEntity.ParsePropertyName(field.Name, out propertyName, out fieldName);
                    PropertyInfo property = type.GetProperty(propertyName);
                    if (null == property)
                        continue;

                    object val = property.GetValue(o, null);
                    InfoPlusEntity.FormDataAssign(fieldName, val, data, dataPath);
                }

                var deepers = from f in fields
                              where InfoPlusEntity.CalculateGroupDepth(f.GroupName) > depth && f.GroupName.Contains(groupPath)
                              select f;

                if (deepers.Count() > 0)
                {
                    int min = deepers.Select(f => InfoPlusEntity.CalculateGroupDepth(f.GroupName)).Min();
                    var groupNames = deepers.Where(f => InfoPlusEntity.CalculateGroupDepth(f.GroupName) == min)
                        .Select(f => f.GroupName).Distinct().ToList();

                    foreach (string groupName in groupNames)
                    {
                        string[] gns = groupName.Split(new string[] { "//" }, StringSplitOptions.RemoveEmptyEntries);

                        if (depth < gns.Length - 1)
                        {
                            string gn = InfoPlusEntity.ParseGroupName(gns[depth + 1]);
                            PropertyInfo property = type.GetProperty(gn);
                            if (null == property)
                                continue;
                            object deeps = property.GetValue(o, null);
                            Array arr = InfoPlusEntity.ObjectToArray(deeps);
                            if (null == arr)
                                continue;
                            // assign inner field to array in case of empty.
                            if (0 == arr.Length)
                            {
                                var groupFields = deepers.Where(f => f.GroupName == groupName);
                                foreach (var field in groupFields)
                                {
                                    InfoPlusEntity.FormDataAssign(field.Name, new Array[0], data, dataPath);
                                }
                            }
                            
                            for (int n = 0; n < arr.Length; n++)
                            {
                                object deep = arr.GetValue(n);
                                InfoPlusEntity.Convert(deep, fields, groupName, dataPath + "/" + n, depth + 1, ref data);
                            }
                        }
                    }
                }
            }
        }

        static object DecodeValue(object val, out string name, out string attr)
        {
            name = null;
            attr = null;
            if (null != val)
            {
                Type type = val.GetType();
                if (type.IsGenericType && false == type.IsGenericTypeDefinition)
                {
                    var generic = type.GetGenericTypeDefinition();
                    if (generic == typeof (Nullable<>))
                    {
                        var property = type.GetProperty("Value");
                        var realVal = property.GetValue(val, null);
                        return DecodeValue(realVal, out name, out attr);
                    }
                }
                if (val is CodeItem)
                {
                    var entity = val as CodeItem;
                    name = entity.CodeName;
                    if (null != entity.Attributes && entity.Attributes.Count > 0)
                        attr = JsonConvert.ExportToString(entity.Attributes);
                    val = entity.CodeId;
                }
                if (val is InfoPlusUser)
                {
                    name = (val as InfoPlusUser).TrueName;
                    val = (val as InfoPlusUser).Account;
                }
                if (val is DateTime) val = UnixTime.ToInt64((DateTime)val);
            }
            return val;
        }

        
        /// <summary>
        /// Assign key, key_Name & key_Attr
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="data"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        static bool FormDataAssign(string key, object val, IDictionary<string, object> data, string path)
        {
            if (string.IsNullOrEmpty(key) || null == data)
                return false;

            string[] splits = null;

            if (false == string.IsNullOrEmpty(path))
                splits = path.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

            string name;
            string attr;
            val = InfoPlusEntity.DecodeValue(val, out name, out attr);
            // Save name too.
            if (null != name)
                FormDataAssign(key + FIELD_SUFFIX_NAME, name, data, path);
            if (null != attr) 
                FormDataAssign(key + FIELD_SUFFIX_ATTR, attr, data, path);
                

            if(null == splits || 0 == splits.Length)
            {
                data[key] = val;
                return true;
            }

            try
            {
                int index = int.Parse(splits[0]);
                object[] o = null;
                if (false == data.ContainsKey(key))
                {
                    o = new object[index + 1];
                    data.Add(key, o);
                }
                else
                { 
                    o = data[key] as object[];
                    if (null == o)
                    {
                        data[key] = new object[index + 1];
                        o = data[key] as object[];
                    }
                    else if(o.Length <= index)
                    {
                        var x = new object[index + 1];
                        for (int i = 0; i < o.Length; i++)
                            x[i] = o[i];
                        data[key] = x;
                        o = x;
                    }
                }

                object[] a = o;
                int prev = index;
                int next = index;
                for (int i = 0; i < splits.Length - 1; i++)
                {
                    prev = int.Parse(splits[i]);
                    next = int.Parse(splits[i + 1]);

                    a = o[prev] as object[];
                    if (null == a)
                    {
                        o[prev] = new object[next + 1];
                        a = o[prev] as object[];
                    }
                    else if (a.Length <= next)
                    {
                        var x = new object[next + 1];
                        for (int j = 0; j < a.Length; j++)
                            x[j] = a[j];
                        o[prev] = x;
                        a = x;
                    }
                    // move pointer to next.
                    o = a;
                }

                o[next] = val;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static int CalculateGroupDepth(string groupName)
        {
            return groupName.Where(c => c == '/').Count() / 2;
        }

        static Array ObjectToArray(object o)
        {
            if (null == o) return null;
            Type type = o.GetType();
            // string is an exception
            if (type == typeof(string) || type == typeof(decimal) || type.IsPrimitive) return null;
            if (type.IsArray) return (Array)o;
            if (typeof(IEnumerable<object>).IsAssignableFrom(type))
                return (o as IEnumerable<object>).ToArray();
            else if (typeof(IEnumerable).IsAssignableFrom(type))
                return (o as IEnumerable).Cast<object>().ToArray();
            return null;
        }

        static IDictionary<Type, object> DefaultValueTypes = new Dictionary<Type, object>(); 

        public static object GetDefaultValue(Type type)
        {
            if (!type.IsValueType) return null;
            object defaultValue;
            lock (DefaultValueTypes)
            {
                if (!DefaultValueTypes.TryGetValue(type, out defaultValue))
                {
                    defaultValue = Activator.CreateInstance(type);
                    DefaultValueTypes[type] = defaultValue;
                }
            }
            return defaultValue;
        }

        static bool IsArray(Type type)
        {
            if(null == type)return false;
            if(type.IsArray && type != typeof(string)) return true;

            if (false == type.IsGenericType) return false;
            if (type.IsGenericTypeDefinition) return false;

            Type[] args = type.GetGenericArguments();
            // Array must has ONLY one generice argument?
            // Marstone said no. because of : IDictionary<T, T>
            bool isIList = typeof(IList).IsAssignableFrom(type);
            bool isIListT = false;
            bool isICollectionT = false;
            if (1 == args.Length)
            {
                isIListT = typeof(IList<>).MakeGenericType(args).IsAssignableFrom(type);
                isICollectionT = typeof(ICollection<>).MakeGenericType(args).IsAssignableFrom(type);
            }
            return isICollectionT || isIList || isIListT;
        
        }

        static bool IsDerived(Type ancestor, Type offspring)
        {
            if (ancestor.IsGenericTypeDefinition)
                if (offspring.IsGenericType && false == offspring.IsGenericTypeDefinition)
                    return ancestor.MakeGenericType(offspring.GetGenericArguments()).IsAssignableFrom(offspring);
            // 
            return ancestor.IsAssignableFrom(offspring);
        }

        /// <summary>
        /// search the node in the path of entity from top to leaf, return if:
        /// 1.node is T
        /// 2.reach the leaf
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static T Locate<T>(object entity, string path) where T : class
        {
	        var paths = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
	        // reach the end, end of recursive
	        if (!paths.Any())
	            return entity as T;
	
	        foreach (var prop in entity.GetType().GetProperties())
	        {
	            var type = prop.PropertyType;
                if (type == typeof(string) || type == typeof(decimal) || type.IsPrimitive) continue;
	            if (type.IsArray || typeof(IEnumerable<object>).IsAssignableFrom(type))
	            {
	                var index = int.Parse(paths[0]);
	                var propVal = prop.GetValue(entity, null);
                    if (propVal == null) continue;
                    var propArr = (propVal as Array ?? (propVal as IEnumerable<object>).ToArray());
	                if(propArr.Length > index)
	                {
                        var next = propArr.GetValue(index);
                        if (next is T)
                            return next as T;
                        var result = Locate<T>(next, string.Join("/", paths, 1, paths.Length - 1));
	                    if (null != result)
	                        return result;
	                }
	            }
	        }
	        return null;
	    }

    }
}
