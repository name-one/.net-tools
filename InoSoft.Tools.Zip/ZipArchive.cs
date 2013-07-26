using System;
using System.Collections;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;

namespace InoSoft.Tools.Zip
{
    public class ZipArchive : IDisposable
    {
        private static readonly Type _type =
            Assembly.GetAssembly(typeof(ZipPackage)).GetType("MS.Internal.IO.Zip.ZipArchive");

        private static readonly Type CompressionMethodEnumType =
            Assembly.GetAssembly(typeof(ZipPackage)).GetType("MS.Internal.IO.Zip.CompressionMethodEnum");

        private static readonly Type DeflateOptionEnumType =
            Assembly.GetAssembly(typeof(ZipPackage)).GetType("MS.Internal.IO.Zip.DeflateOptionEnum");

        private readonly object _instance;

        internal ZipArchive(object zipArchive)
        {
            _instance = zipArchive;
        }

        public FileAccess OpenAccess
        {
            get
            {
                return (FileAccess)_type
                    .GetProperty("OpenAccess", ReflectionHelper.InstanceBinding)
                    .GetValue(_instance, null);
            }
        }

        public static ZipArchive OpenOnFile(string path, FileMode mode, FileAccess access, FileShare share, bool streaming)
        {
            return new ZipArchive(_type
                .GetMethod("OpenOnFile", ReflectionHelper.StaticBinding)
                .Invoke(null, new object[] { path, mode, access, share, streaming }));
        }

        public static ZipArchive OpenOnStream(Stream stream, FileMode mode, FileAccess access, bool streaming)
        {
            return new ZipArchive(_type
                .GetMethod("OpenOnStream", ReflectionHelper.StaticBinding)
                .Invoke(null, new object[] { stream, mode, access, streaming }));
        }

        public static void VerifyVersionNeededToExtract(ushort version)
        {
            _type.GetMethod("VerifyVersionNeededToExtract", ReflectionHelper.StaticBinding)
                .Invoke(null, new object[] { version });
        }

        public ZipFileInfo AddFile(string zipFileName, CompressionMethod compressionMethod, DeflateOption deflateOption)
        {
            return new ZipFileInfo(_type
                .GetMethod("AddFile", ReflectionHelper.InstanceBinding)
                .Invoke(_instance, new[]
                {
                    zipFileName,
                    compressionMethod.ConvertEnum(CompressionMethodEnumType),
                    deflateOption.ConvertEnum(DeflateOptionEnumType)
                }));
        }

        public void Close()
        {
            Dispose();
        }

        public void DeleteFile(string zipFileName)
        {
            _type.GetMethod("DeleteFile", ReflectionHelper.InstanceBinding)
                .Invoke(_instance, new object[] { zipFileName });
        }

        public void Dispose()
        {
            ((IDisposable)_instance).Dispose();
        }

        public bool FileExists(string zipFileName)
        {
            return (bool)_type
                .GetMethod("FileExists", ReflectionHelper.InstanceBinding)
                .Invoke(_instance, new object[] { zipFileName });
        }

        public void Flush()
        {
            _type.GetMethod("Flush", ReflectionHelper.InstanceBinding)
                .Invoke(_instance, new object[0]);
        }

        public ZipFileInfo GetFile(string zipFileName)
        {
            return new ZipFileInfo(_type
                .GetMethod("GetFile", ReflectionHelper.InstanceBinding)
                .Invoke(_instance, new object[] { zipFileName }));
        }

        public ZipFileInfo[] GetFiles()
        {
            return ((IEnumerable)_type
                .GetMethod("GetFiles", ReflectionHelper.InstanceBinding)
                .Invoke(_instance, new object[0]))
                .Cast<object>()
                .Select(x => new ZipFileInfo(x))
                .ToArray();
        }

        #region Equality

        public static bool operator !=(ZipArchive left, ZipArchive right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(ZipArchive left, ZipArchive right)
        {
            return Equals(left, right);
        }

        public bool Equals(ZipArchive other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other._instance, _instance);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(ZipArchive)) return false;
            return Equals((ZipArchive)obj);
        }

        public override int GetHashCode()
        {
            return _instance.GetHashCode();
        }

        #endregion Equality
    }
}