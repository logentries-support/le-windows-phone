using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace LogEntries
{
    [DataContractAttribute]
    internal class Message
    {
        [DataMemberAttribute]
        public int Id { get; set; }
        [DataMemberAttribute]
        public DateTime TimeStamp { get; set; }
        [DataMemberAttribute]
        public Severity Severity { get; set; }
        [DataMemberAttribute]
        public string Text { get; set; }

        public Message()
        { }

        public Message(string message, Severity severity)
        {
            Inilialize(message, severity);
        }

        public Message(Dictionary<object, object> messages, Severity severity)
        {
            Inilialize(String.Join(" ", messages.Select(o => o.Key.ToString() + "=" + o.Value.ToString()).ToList()), severity);
        }

        private void Inilialize(string message, Severity severity)
        {
            Random random = new Random();

            Id = random.Next();
            TimeStamp = DateTime.Now;
            Severity = severity;
            Text = message;
        }

        public override string ToString()
        {
            string date = TimeStamp.ToString("ddd dd MMM HH:mm:ss ", CultureInfo.InvariantCulture) + TimeStamp.ToString("zzz yyyy", CultureInfo.InvariantCulture).Replace(":", "");

            if (Severity == Severity.Log)
            {
                return String.Format("{0}, {1}", date, Text);
            }
            else
            {
                return String.Format("{0}, severity={1}, {2}", date, Severity.ToString(), Text);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj as Message == null) return false;

            return (obj as Message).Id == Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }

    internal enum Severity
    {
        Log,
        Debug,
        Info,
        Notice,
        Warning,
        Error,
        Critical,
        Alert,
        Emergency,
        Crash
    }
}
