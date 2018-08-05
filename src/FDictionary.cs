using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;
/*

Copyright (C) 2016-2018 by Vladimir Novick http://www.linkedin.com/in/vladimirnovick ,

    vlad.novick@gmail.com , http://www.sgcombo.com , https://github.com/Vladimir-Novick

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/
namespace com.sgcombo.dataindex
{
    /// <summary>
    ///   Make dictionary with file data storage
    ///   Using: Create memory index from long data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FDictionary<T> : IDisposable
    {

        public Dictionary<String, Int64> dictionary = new Dictionary<String, Int64>();

        private FileStream fs = null;

        private string tempFileName = null;

        public List<Int64> Indexes { get
            {
                return dictionary.Values.ToList();
            }
                
         }

        /// <summary>
        ///     Initializes a new instance of the FDictionary class
        ///     that is empty, has the default initial capacity, and uses the default equality
        ///     comparer for the key type.
        ///     Create temporary file to store values
        /// </summary>
        public FDictionary()
        {
            tempFileName = Path.GetTempFileName();
            fs = new FileStream(tempFileName, FileMode.Create,FileAccess.ReadWrite );
        }

        /// <summary>
        ///     Initializes a new instance of the FDictionary class
        ///     that is empty, has the default initial capacity, and uses the default equality
        ///     comparer for the key type.
        ///     Create/Open file to store/read values
        /// </summary>
        /// <param name="fileName">
        ///     FileName to values store
        /// </param>
        /// <param name="fileMode">
        ///    A constant that determines how to open or create the file.
        ///    Type: System.IO.FileMode
        ///    FileMode : FileMode.Create, FileMode.Open
        /// </param>
        /// <param name="share">
        ///    Contains constants for controlling the kind of access other FileStream objects can have to the same file.
        ///    Type: System.IO.FileShare
        ///    default: FileShare.ReadWrite
        /// </param>
        public FDictionary(String fileName, FileMode fileMode, FileShare share = FileShare.ReadWrite)
        {
            tempFileName = fileName;
            fs = new FileStream(tempFileName, fileMode, FileAccess.ReadWrite, share);
        }


        /// <summary>
        ///   Gets a collection containing the keys in the FDictionary.
        /// </summary>
        /// <returns>
        ///     A FDictionary containing the keys in
        ///     the FDictionary.
        /// </returns>
        public List<String> Keys()
        {
            return dictionary.Keys.ToList();
        }

        public int Count()
        {
          return  dictionary.Count();
        }

        /// <summary>
        ///     Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">
        ///    The key of the value to get.
        /// </param>
        /// <param name="value">
        ///     When this method returns, contains the value associated with the specified key,
        ///     if the key is found; otherwise, the default value for the type of the value parameter.
        ///     This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        ///    true if the FDictionary contains an element with
        ///    the specified key; otherwise, false.
        /// </returns>
        /// <Exceptions>
        ///   T:System.ArgumentNullException:
        ///     key is null.
        ///</Exceptions>
        public bool TryGetValue(String key, out T value)
        {
            Int64 index;
            if (dictionary.TryGetValue(key, out index))
            {
                value = GetValue(index);
                return true;
            }
            value = default(T);
                return false;
        }
        /// <summary>
        ///  Get Object Value by Virtual index 
        /// </summary>
        /// <param name="index"></param>
        ///   Virtual Index
        /// <returns></returns>
        public T GetValue(long index)
        {
            T value;
            String line = GetString(index);
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            value = JsonConvert.DeserializeObject<T>(line, settings);
            return value;
        }



        /// <summary>
        ///    Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">
        ///    The key of the element to add.
        /// </param>
        /// <param name="value">
        ///  The value of the element to add. The value can be null for reference types.
        /// </param>
        /// <returns>
        ///    The value of the element to add. The value can be null for reference types.
        /// </returns>
        /// <Exceptions>
        ///   T:System.ArgumentNullException:
        ///     key is null.
        ///</Exceptions>
        public bool TryAdd(String key, T value)
        {
            Int64 oldIndex;
            if (!dictionary.TryGetValue(key, out oldIndex)){
               
                long index = fs.Length;
                using (var writer = new BinaryWriter(fs, Encoding.UTF8,true))
                {
                    fs.Seek(index, SeekOrigin.Begin);
                    var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                    var text = JsonConvert.SerializeObject(value, settings);
                    writer.Write(text);
                    dictionary.TryAdd(key, index);
                }
                return true;

            }

            return false;
        }

        private string GetString(Int64 index)
        {
            using (var br = new BinaryReader(fs, Encoding.UTF8, true))

            {
                fs.Seek(index, SeekOrigin.Begin);
                return br.ReadString();
            }
        }


        /// <summary>
        ///   Releases all resources used by the FDictionary and delete temp file.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent Dispose() from being called Twice
        }

        /// <summary>
        ///   
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (fs != null) {
                        fs.Close();
                        fs.Dispose();
                        File.Delete(tempFileName);
                    }
                }
                catch
                {

                }
            }
        }
    }
}
