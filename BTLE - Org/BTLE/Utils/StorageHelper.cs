using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

// File and Stream I/O
// https://msdn.microsoft.com/en-us/library/k3352a4t(v=vs.110).aspx

// Common I/O Tasks
// https://msdn.microsoft.com/en-us/library/ms404278(v=vs.110).aspx

// Asynchronous File I/O
// https://msdn.microsoft.com/en-us/library/kztecsys(v=vs.110).aspx

namespace BTLE.Utils
    {
    public static class StorageHelper
        {
        #region File Read/Write/Exist

        /// <summary>Returns if a file is found in the specified storage strategy</summary>
        /// <param name="key">Path of the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>Boolean: true if found, false if not found</returns>
        public static async Task<bool> FileExistsAsync( string key, StorageStrategies location = StorageStrategies.Local ) => ( await GetIfFileExistsAsync( key, location ) ) != null;

        public static async Task<bool> FileExistsAsync( string key, StorageFolder folder ) => ( await GetIfFileExistsAsync( key, folder ) ) != null;

        /// <summary>Deletes a file in the specified storage strategy</summary>
        /// <param name="path">Path of the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        private static async Task<bool> DeleteFileAsync( string path, StorageStrategies location = StorageStrategies.Local )
            {
            StorageFile file = await GetIfFileExistsAsync( path, location );

            if ( file != null )
                {
                await file.DeleteAsync();
                }

            return !( await FileExistsAsync( path, location ) );
            }

        /// <summary>Reads and de-serializes a file into specified type T</summary>
        /// <typeparam name="T">Specified type into which to de-serializes file content</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>Specified type T</returns>
        public static async Task<T> ReadFileAsync<T>( string key, StorageStrategies location = StorageStrategies.Local )
            {
            // fetch file
            StorageFile file = await GetIfFileExistsAsync( key, location );
            if ( file == null )
                {
                return default( T );
                }

            // read content
            string _String = await FileIO.ReadTextAsync( file );

            // convert to obj
            var result = Deserialize<T>( _String );

            return result;
            }

        /// <summary>Serializes an object and write to file in specified storage strategy</summary>
        /// <typeparam name="T">Specified type of object to serialize</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="value">Instance of object to be serialized and written</param>
        /// <param name="location">Location storage strategy</param>
        public static async Task<bool> WriteFileAsync<T>( string key, T value, StorageStrategies location = StorageStrategies.Local )
            {
            // create file
            StorageFile file = await CreateFileAsync( key, location, CreationCollisionOption.ReplaceExisting );

            // convert to string
            string _String = Serialize( value );

            // save string to file
            await FileIO.WriteTextAsync( file, _String );

            // result
            return await FileExistsAsync( key, location );
            }

        private static async Task<StorageFile> CreateFileAsync( string key,
                                                                StorageStrategies location = StorageStrategies.Local,
                                                                CreationCollisionOption option = CreationCollisionOption.OpenIfExists )
            {
            switch ( location )
                {
                case StorageStrategies.Local:
                    return await ApplicationData.Current.LocalFolder.CreateFileAsync( key, option );

                case StorageStrategies.Roaming:
                    return await ApplicationData.Current.RoamingFolder.CreateFileAsync( key, option );

                case StorageStrategies.Temporary:
                    return await ApplicationData.Current.TemporaryFolder.CreateFileAsync( key, option );

                default:
                    throw new NotSupportedException( location.ToString() );
                }
            }

        private static async Task<StorageFile> GetIfFileExistsAsync( string key,
                                                                     StorageFolder folder,
                                                                     CreationCollisionOption option = CreationCollisionOption.FailIfExists )
            {
            StorageFile retval;
            try
                {
                retval = await folder.GetFileAsync( key );
                }
            catch ( FileNotFoundException )
                {
                Debug.WriteLine( "** Error ** - GetIfFileExistsAsync:FileNotFoundException" );
                return null;
                }
            return retval;
            }

        /// <summary>Returns a file if it is found in the specified storage strategy</summary>
        /// <param name="key">Path of the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        /// <param name="option">Specifies what to do if a file or folder with the specified name already exists in the current folder when you create a new file or folder</param>
        /// <returns>StorageFile</returns>
        private static async Task<StorageFile> GetIfFileExistsAsync( string key,
                                                                     StorageStrategies location = StorageStrategies.Local,
                                                                     CreationCollisionOption option = CreationCollisionOption.FailIfExists )
            {
            StorageFile retval;
            try
                {
                switch ( location )
                    {
                    case StorageStrategies.Local:
                        retval = await ApplicationData.Current.LocalFolder.GetFileAsync( key );
                        break;

                    case StorageStrategies.Roaming:
                        retval = await ApplicationData.Current.RoamingFolder.GetFileAsync( key );
                        break;

                    case StorageStrategies.Temporary:
                        retval = await ApplicationData.Current.TemporaryFolder.GetFileAsync( key );
                        break;

                    default:
                        throw new NotSupportedException( location.ToString() );
                    }
                }
            catch ( FileNotFoundException )
                {
                Debug.WriteLine( "** Error ** - GetIfFileExistsAsync:FileNotFoundException" );
                return null;
                }

            return retval;
            }
        #endregion

        /// <summary>Serializes the specified object as a JSON string</summary>
        /// <param name="objectToSerialize">Specified object to serialize</param>
        /// <returns>JSON string of serialized object</returns>
        private static string Serialize( object objectToSerialize )
            {
            using ( var stream = new MemoryStream() )
                {
                try
                    {
                    var serializer = new DataContractJsonSerializer( objectToSerialize.GetType() );
                    serializer.WriteObject( stream, objectToSerialize );
                    stream.Position = 0;
                    var reader = new StreamReader( stream );
                    return reader.ReadToEnd();
                    }
                catch ( Exception e )
                    {
                    Debug.WriteLine( "** Error ** - Serialize:" + e.Message );
                    return string.Empty;
                    }
                }
            }

        /// <summary>De-serializes the JSON string as a specified object</summary>
        /// <typeparam name="T">Specified type of target object</typeparam>
        /// <param name="jsonString">JSON string source</param>
        /// <returns>Object of specified type</returns>
        private static T Deserialize<T>( string jsonString )
            {
            using ( var stream = new MemoryStream( Encoding.Unicode.GetBytes( jsonString ) ) )
                {
                var serializer = new DataContractJsonSerializer( typeof( T ) );
                return (T)serializer.ReadObject( stream );
                }
            }

        public enum StorageStrategies
            {
            /// <summary>Local, isolated folder</summary>
            Local,

            /// <summary>Cloud, isolated folder. 100k cumulative limit.</summary>
            Roaming,

            /// <summary>Local, temporary folder (not for settings)</summary>
            Temporary
            }

        public static async void DeleteFileFireAndForget( string key, StorageStrategies location )
            {
            await DeleteFileAsync( key, location );
            }

        public static async void WriteFileFireAndForget<T>( string key, T value, StorageStrategies location )
            {
            await WriteFileAsync( key, value, location );
            }

        // var appData = await ReadDataFileAsync<AppData>( AppDataFileName );
        public static async Task<T> ReadDataFileAsync<T>( string fileName )
            {
            // Get a reference to the Local Folder
            bool isFileExist = await FileExistsAsync( fileName, ApplicationData.Current.LocalFolder );

            if ( !isFileExist )
                {
                return default( T );
                }

            var value = await ReadFileAsync<T>( fileName );

#if DEBUG
            if ( value == null )
                {
                Debug.WriteLine( "error ReadFileAsync" );
                }
#endif

            return value;
            }

        // bool result = await WriteDataFileAsync( AppDataFileName, appData );
        public static async Task<bool> WriteDataFileAsync<T>( string fileName, T value )
            {
            bool result = await WriteFileAsync( fileName, value );
            return result;
            }

        public static void SerializeTransactionIdDictionary( Dictionary<byte, byte> dictionary, Stream stream )
            {
            BinaryWriter writer = new BinaryWriter( stream );
            writer.Write( dictionary.Count );
            foreach ( var kvp in dictionary )
                {
                writer.Write( kvp.Key );
                writer.Write( kvp.Value );
                }
            writer.Flush();
            }

        public static Dictionary<byte, byte> DeserializeTransactionIdDictionary( Stream stream )
            {
            BinaryReader reader = new BinaryReader( stream );
            int count = reader.ReadInt32();
            var dictionary = new Dictionary<byte, byte>( count );
            for ( int n = 0 ; n < count ; n++ )
                {
                var key = reader.ReadByte();
                var value = reader.ReadByte();
                dictionary.Add( key, value );
                }
            return dictionary;
            }
        }
    }
