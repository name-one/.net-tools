using System;
using System.IO;
using System.IO.Packaging;
using System.Reflection;

namespace InoSoft.Tools.Zip
{
    public class ZipFileInfo
    {
        private static readonly Type _type =
            Assembly.GetAssembly(typeof(ZipPackage)).GetType("MS.Internal.IO.Zip.ZipFileInfo");

        private readonly object _instance;

        internal ZipFileInfo(object zipFileInfo)
        {
            _instance = zipFileInfo;
        }

        public CompressionMethod CompressionMethod
        {
            get
            {
                return _type
                    .GetProperty("CompressionMethod", ReflectionHelper.InstanceBinding)
                    .GetValue(_instance, null)
                    .ConvertEnum<CompressionMethod>();
            }
        }

        public DeflateOption DeflateOption
        {
            get
            {
                return _type
                    .GetProperty("DeflateOption", ReflectionHelper.InstanceBinding)
                    .GetValue(_instance, null)
                    .ConvertEnum<DeflateOption>();
            }
        }

        public bool FolderFlag
        {
            get
            {
                return (bool)_type
                    .GetProperty("FolderFlag", ReflectionHelper.InstanceBinding)
                    .GetValue(_instance, null);
            }
        }

        public DateTime LastModFileDateTime
        {
            get
            {
                return (DateTime)_type
                    .GetProperty("LastModFileDateTime", ReflectionHelper.InstanceBinding)
                    .GetValue(_instance, null);
            }
        }

        public string Name
        {
            get
            {
                return (string)_type
                    .GetProperty("Name", ReflectionHelper.InstanceBinding)
                    .GetValue(_instance, null);
            }
        }

        public bool VolumeLabelFlag
        {
            get
            {
                return (bool)_type
                    .GetProperty("VolumeLabelFlag", ReflectionHelper.InstanceBinding)
                    .GetValue(_instance, null);
            }
        }

        public ZipArchive ZipArchive
        {
            get
            {
                return new ZipArchive(_type
                    .GetProperty("ZipArchive", ReflectionHelper.InstanceBinding)
                    .GetValue(_instance, null));
            }
        }

        public Stream GetStream(FileMode mode, FileAccess access)
        {
            return (Stream)_type
                .GetMethod("GetStream", ReflectionHelper.InstanceBinding)
                .Invoke(_instance, new object[] { mode, access });
        }

        #region Equality

        public static bool operator !=(ZipFileInfo left, ZipFileInfo right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(ZipFileInfo left, ZipFileInfo right)
        {
            return Equals(left, right);
        }

        public bool Equals(ZipFileInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other._instance, _instance);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(ZipFileInfo)) return false;
            return Equals((ZipFileInfo)obj);
        }

        public override int GetHashCode()
        {
            return _instance.GetHashCode();
        }

        #endregion Equality
    }
}