using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;

namespace LogEntries
{
    internal class Storage
    {
        /// <summary>
        /// LogEntries folder
        /// </summary>
        private const string folder = "logentries";

        private static IsolatedStorageFile Store
        {
            get
            {
                return IsolatedStorageFile.GetUserStoreForApplication();
            }
        }

        public static void SaveToFile(string filename, object objForSave)
        {
            lock (Store)
            {
                if (!Store.DirectoryExists(folder))
                {
                    Store.CreateDirectory(folder);
                }

                try
                {
                    using (IsolatedStorageFileStream file = new IsolatedStorageFileStream(folder + "/" + filename, FileMode.Create, FileAccess.Write, Store))
                    {
                        Serialize(file, objForSave);

                        file.Close();
                    };
                }
                catch
                {
                    Debug.WriteLine("save logentries data failed");
                }
            }
        }

        public static T LoadFromFile<T>(string filename)
        {
            T obj = default(T);

            if (!Store.DirectoryExists(folder))
            {
                Store.CreateDirectory(folder);
            }

            if (!Store.FileExists(folder + "/" + filename)) return obj;

            lock (Store)
            {
                try
                {
                    using (IsolatedStorageFileStream file = new IsolatedStorageFileStream(folder + "/" + filename, FileMode.Open, FileAccess.Read, Store))
                    {
                        obj = (T)Deserialize(file, typeof(T));

                        file.Close();
                    };
                }
                catch
                {
                    Debug.WriteLine("logentries queue lost");

                    DeleteFile(folder + "/" + filename);
                }
            }

            return obj;
        }

        public static void DeleteFile(string path)
        {
            if (Store.FileExists(path))
            {
                Store.DeleteFile(path);
            }
        }

        private static void Serialize(Stream streamObject, object objForSerialization)
        {
            if (objForSerialization == null || streamObject == null)
                return;

            DataContractSerializer ser = new DataContractSerializer(objForSerialization.GetType());
            ser.WriteObject(streamObject, objForSerialization);
        }

        private static object Deserialize(Stream streamObject, Type serializedObjectType)
        {
            if (serializedObjectType == null || streamObject == null)
                return null;
            
            DataContractSerializer ser = new DataContractSerializer(serializedObjectType);
            return ser.ReadObject(streamObject);            
        }
    }
}
