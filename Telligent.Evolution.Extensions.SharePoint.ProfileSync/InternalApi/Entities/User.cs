using System.Collections.Generic;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities
{
    public class User
    {
        private readonly string idFieldName = "Id";
        private readonly string emailFieldName = "Email";

        private User()
        {
            Fields = new Dictionary<string, object>();
            ExtendedAttributes = new Dictionary<string, object>();
        }

        protected User(string idFieldName, string emailFieldName)
            : this()
        {
            this.idFieldName = idFieldName;
            this.emailFieldName = emailFieldName;
        }

        public object Id
        {
            get { return this[idFieldName]; }
        }

        public string Email
        {
            get { return (string)this[emailFieldName]; }
            set { this[emailFieldName] = value; }
        }

        public Dictionary<string, object> Fields { get; set; }
        public Dictionary<string, object> ExtendedAttributes { get; set; }

        public virtual object this[string fieldName]
        {
            get
            {
                return GetField(Fields, fieldName);
            }
            set
            {
                SetField(Fields, fieldName, value);
            }
        }

        protected object GetField(Dictionary<string, object> fields, string fieldName)
        {
            object fieldVal;
            if (fields.TryGetValue(fieldName, out fieldVal))
            {
                return fieldVal;
            }
            return null;
        }

        protected void SetField(Dictionary<string, object> fields, string fieldName, object value)
        {
            if (fields.ContainsKey(fieldName))
            {
                fields[fieldName] = value;
            }
            else
            {
                fields.Add(fieldName, value);
            }
        }
    }
}
