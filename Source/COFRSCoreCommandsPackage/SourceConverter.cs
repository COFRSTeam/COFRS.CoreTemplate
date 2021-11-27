using COFRSCoreCommon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommandsPackage
{
    /// <summary>
    /// Converts a source column value to the destination value
    /// </summary>
    public static class SourceConverter
    {
        #region GUID
        /// <summary>
        /// Converts a source <see cref="DBColumn"/> value to an <see cref="Guid"/>
        /// </summary>
        /// <param name="sourceMember">The <see cref="DBColumn>"/> source member to convert.</param>
        /// <param name="isUndefined"><see langword="true"/> if the transformation is undefined; <see langword="false"/> otherwise.</param>
        public static string ToGuid(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? Guid.Empty : Guid.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Guid", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Guid?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName} ? Guid.Empty";
            }
            else
            {
                isUndefined = true;
                return $"(Guid) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        /// <summary>
        /// Converts a source <see cref="DBColumn"/> value to an <see cref="Guid"/>
        /// </summary>
        /// <param name="sourceMember">The <see cref="DBColumn>"/> source member to convert.</param>
        /// <param name="isUndefined"><see langword="true"/> if the transformation is undefined; <see langword="false"/> otherwise.</param>
        public static string ToNullableGuid(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : Guid.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Guid", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Guid?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else
            {
                isUndefined = true;
                return $"(Guid) AFunc(source.{sourceMember.ColumnName})";
            }
        }
        #endregion

        #region URI
        /// <summary>
        /// Converts a source <see cref="DBColumn"/> value to an <see cref="Uri"/>
        /// </summary>
        /// <param name="sourceMember">The <see cref="DBColumn>"/> source member to convert.</param>
        /// <param name="isUndefined"><see langword="true"/> if the transformation is undefined; <see langword="false"/> otherwise.</param>
        public static string ToUri(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : new Uri(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Uri", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else
            {
                isUndefined = true;
                return $"(Uri) AFunc(source.{sourceMember.ColumnName})";
            }
        }
        #endregion

        #region Image
        /// <summary>
        /// Converts a source <see cref="DBColumn"/> value to an <see cref="Image"/>
        /// </summary>
        /// <param name="sourceMember">The <see cref="DBColumn>"/> source member to convert.</param>
        /// <param name="isUndefined"><see langword="true"/> if the transformation is undefined; <see langword="false"/> otherwise.</param>
        public static string ToImage(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ?  default(Image) : ImageEx.Parse(Convert.FromBase64String(source.{sourceMember.ColumnName}))";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length == 0 ) ? default(Image) : ImageEx.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length == 0 ) ? default(Image) : ImageEx.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length == 0 ) ? default(Image) : ImageEx.Parse(source.{sourceMember.ColumnName}.ToArray())";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Image", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName}";
            }
            else
            {
                isUndefined = true;
                return $"(Image) AFunc(source.{sourceMember.ColumnName})";
            }
        }
        #endregion

        #region bytes
        /// <summary>
        /// Converts a source <see cref="DBColumn"/> value to an byte list
        /// </summary>
        /// <param name="sourceMember">The <see cref="DBColumn>"/> source member to convert.</param>
        /// <param name="isUndefined"><see langword="true"/> if the transformation is undefined; <see langword="false"/> otherwise.</param>
        public static string ToByteList(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : Convert.FromBase64String(source.{sourceMember.ColumnName}.ToList())";
            }
            else if (string.Equals(sourceMember.ModelDataType, "char[]", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length() == 0 ) ? null : Convert.FromBase64CharArray(source.{sourceMember.ColumnName},0,source.{sourceMember.ColumnName}.Length).ToList()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Image", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName} == null ? null : source.{sourceMember.ColumnName}.GetBytes().ToList()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length() == 0 ) ? null : source.{sourceMember.ColumnName}.ToList()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length() == 0 ) ? null : source.{sourceMember.ColumnName}.ToList()";
            }
            else
            {
                isUndefined = true;
                return $"(List<byte>) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToEnumerableBytes(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : Convert.FromBase64String(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "char[]", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length == 0 ) ? null : Convert.FromBase64CharArray(source.{sourceMember.ColumnName},0,source.{sourceMember.ColumnName}.Length)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Image", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length == 0 ) ? null : source.{sourceMember.ColumnName} == null ? null : ImageEx.Parse(source.{sourceMember.ColumnName}.GetBytes())";
            }
            else if (string.Equals(sourceMember.ModelDataType, "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length == 0 ) ? null : source.{sourceMember.ColumnName}.ToArray()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length == 0 ) ? null : source.{sourceMember.ColumnName}";
            }
            else
            {
                isUndefined = true;
                return $"(IEnumerable<byte>) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToByteArray(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : Convert.FromBase64String(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "char[]", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length == 0 ) ? null : Convert.FromBase64CharArray(source.{sourceMember.ColumnName},0,source.{sourceMember.ColumnName}.Length)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Image", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length == 0 ) ? null : source.{sourceMember.ColumnName} == null ? null : source.{sourceMember.ColumnName}.GetBytes()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length == 0 ) ? null : source.{sourceMember.ColumnName}.ToArray()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                return $"(source.{sourceMember.ColumnName} == null || source.{sourceMember.ColumnName}.Length == 0 ) ? null : source.{sourceMember.ColumnName}.ToArray()";
            }
            else
            {
                isUndefined = true;
                return $"(byte[]) AFunc(source.{sourceMember.ColumnName})";
            }
        }
        #endregion

        #region string
        public static string ToString(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Guid", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToString()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Guid?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue() ? source.{sourceMember.ColumnName}.ToString() : string.Empty";
            }
            else if (string.Equals(sourceMember.ModelDataType, "char[]", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName} == null ? string.Empty : new string(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName} == null ? string.Empty : Convert.ToBase64String(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "Image", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName} == null ? string.Empty : Convert.ToBase64String(source.{sourceMember.ColumnName}.GetBytes())";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToString()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value.ToString() : string.Empty";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToString(\"o\", CultureInfo.CurrentCulture)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToString(\"o\", CultureInfo.CurrentCulture)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToString(@\"dd\\.hh\\:mm\\:ss\\.fffffff\")";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToString(\"o\", CultureInfo.CurrentCulture) : string.Empty";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToString(\"o\", CultureInfo.CurrentCulture) : string.Empty";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToString(@\"dd\\.hh\\:mm\\:ss\\.fffffff\") : string.Empty";
            }
            else if (string.Equals(sourceMember.ModelDataType, "BitArray", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName} == null ? string.Empty : source.{sourceMember.ColumnName}.ToString()";
            }
            else
            {
                return $"source.{sourceMember.ColumnName}.ToString()";
            }
        }
        #endregion

        #region TimeSpan
        public static string ToTimeSpan(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? TimeSpan.Zero : TimeSpan.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"TimeSpan.FromMilliseconds(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? TimeSpan.FromMilliseconds(source.{sourceMember.ColumnName}) : TimeSpan.Zero";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase))
            {
                return $"new TimeSpan(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? new TimeSpan(source.{sourceMember.ColumnName}) : TimeSpan.Zero";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase))
            {
                return $"TimeSpan.FromSeconds((double) source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? TimeSpan.FromSeconds((double) source.{sourceMember.ColumnName}) : TimeSpan.Zero";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : TimeSpan.Zero";
            }
            else
            {
                isUndefined = true;
                return $"(TimeSpan) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableTimeSpan(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : (TimeSpan?) TimeSpan.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"(TimeSpan?) TimeSpan.FromMilliseconds(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (TimeSpan?) TimeSpan.FromMilliseconds(source.{sourceMember.ColumnName}) : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase))
            {
                return $"(TimeSpan?) new TimeSpan(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (TimeSpan?) new TimeSpan(source.{sourceMember.ColumnName}) : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase))
            {
                return $"(TimeSpan?) TimeSpan.FromSeconds((double)source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (TimeSpan?) TimeSpan.FromSeconds((double)source.{sourceMember.ColumnName}) : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"(TimeSpan?) source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else
            {
                isUndefined = true;
                return $"(TimeSpan) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        #endregion

        #region Dates
        public static string ToDateTimeOffset(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(DateTimeOffset) : DateTimeOffset.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase))
            {
                return $"DateTimeOffset.FromUnixTimeMilliseconds(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(source.{sourceMember.ColumnName}) : default(DateTimeOffset)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"new DateTimeOffset(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? new DateTimeOffset(source.{sourceMember.ColumnName}) : default(DateTimeOffset)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : default(DateTimeOffset)";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableDateTimeOffset(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : (DateTimeOffset?) DateTimeOffset.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase))
            {
                return $"(DateTimeOffset?) DateTimeOffset.FromUnixTimeMilliseconds(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (DateTimeOffset?) DateTimeOffset.FromUnixTimeMilliseconds(source.{sourceMember.ColumnName}.Value) : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"(DateTimeOffset?) new DateTimeOffset(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (DateTimeOffset?) new DateTimeOffset(source.{sourceMember.ColumnName}.Value) : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                return $"(DateTimeOffset?) source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToDateTime(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? DateTime.Parse(source.{sourceMember.ColumnName}) : default(DateTime)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase))
            {
                return $"DateTime.FromBinary(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? DateTime.FromBinary(source.{sourceMember.ColumnName}) : default(DateTime)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.DateTime";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value.DateTime : default(DateTime)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : default(DateTime)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase))
            {
                return $"DateTime.FromOADate((double) source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? DateTime.FromOADate((double) source.{sourceMember.ColumnName}.Value) : default(DateTime)";
            }
            else
            {
                isUndefined = true;
                return $"(DateTime) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableDateTime(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? DateTime.Parse(source.{sourceMember.ColumnName}) : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase))
            {
                return $"DateTime.FromBinary(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? DateTime.FromBinary(source.{sourceMember.ColumnName}.Value) : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                return $"(DateTime?) source.{sourceMember.ColumnName}.DateTime";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (DateTime?) source.{sourceMember.ColumnName}.Value.DateTime : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"(DateTime?) source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase))
            {
                return $"DateTime.FromOADate((double) source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? DateTime.FromOADate((double) source.{sourceMember.ColumnName}.Value) : null";
            }
            else
            {
                isUndefined = true;
                return $"(DateTime) AFunc(source.{sourceMember.ColumnName})";
            }
        }
        #endregion

        #region char
        public static string ToChar(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? char.MinValue : char.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "object", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToChar(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "object?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToChar(source.{sourceMember.ColumnName}) : char.MinValue";
            }
            else
            {
                isUndefined = true;
                return $"(char) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableChar(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : char.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "object", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToChar(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "object?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToChar(source.{sourceMember.ColumnName}) : null";
            }
            else
            {
                isUndefined = true;
                return $"(char?) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        #endregion

        #region Boolean
        public static string ToBoolean(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? false : bool.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "object", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToBoolean(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "object", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToBoolean(source.{sourceMember.ColumnName}) : false";
            }
            else
            {
                isUndefined = true;
                return $"(bool) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableBoolean(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : bool.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "object", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToBoolean(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "object", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToBoolean(source.{sourceMember.ColumnName}) : null";
            }
            else
            {
                isUndefined = true;
                return $"(bool?) AFunc(source.{sourceMember.ColumnName})";
            }
        }
        #endregion

        #region Numeric
        public static string ToDouble(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(double) : double.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.TotalMilliseconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.TotalMilliseconds : default(double)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToOADate()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToOADate() : default(double)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName} : default(double)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToDouble(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToDouble(source.{sourceMember.ColumnName}) : default(double)";
            }
            else
            {
                isUndefined = true;
                return $"(double) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableDouble(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : double.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.TotalMilliseconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.TotalMilliseconds : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToOADate()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToOADate() : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName} : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToDouble(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToDouble(source.{sourceMember.ColumnName}) : null";
            }
            else
            {
                isUndefined = true;
                return $"(double) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToFloat(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(float) : double.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.TotalMilliseconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.TotalMilliseconds : default(float)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToOADate()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToOADate() : default(float)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName} : default(float)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToDouble(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToDouble(source.{sourceMember.ColumnName}) : default(float)";
            }
            else
            {
                isUndefined = true;
                return $"(double) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableFloat(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : double.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.TotalMilliseconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.TotalMilliseconds : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToOADate()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToOADate() : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName} : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToDouble(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToDouble(source.{sourceMember.ColumnName}) : null";
            }
            else
            {
                isUndefined = true;
                return $"(double) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToDecimal(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(decimal) : double.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.TotalMilliseconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.TotalMilliseconds : default(decimal)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToOADate()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToOADate() : default(decimal)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName} : default(decimal)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToDouble(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToDouble(source.{sourceMember.ColumnName}) : default(decimal)";
            }
            else
            {
                isUndefined = true;
                return $"(double) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableDecimal(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : double.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.TotalMilliseconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.TotalMilliseconds : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToOADate()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToOADate() : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName} : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToDouble(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToDouble(source.{sourceMember.ColumnName}) : null";
            }
            else
            {
                isUndefined = true;
                return $"(double) AFunc(source.{sourceMember.ColumnName})";
            }
        }
        #endregion

        #region integers
        public static string ToULong(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(ulong) : ulong.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : default(ulong)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToUInt64(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToUInt64(source.{sourceMember.ColumnName}.Value) : default(ulong)";
            }
            else
            {
                isUndefined = true;
                return $"(ulong) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableULong(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : ulong.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToUInt64(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToUInt64(source.{sourceMember.ColumnName}.Value) : null";
            }
            else
            {
                isUndefined = true;
                return $"(ulong) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToLong(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(long) : long.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : default(long)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToBinary()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value.ToBinary() : default(long)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.ToUnixTimeMilliseconds()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value.ToUnixTimeMilliseconds() : default(long)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.Ticks";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value.Ticks : default(long)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToInt64(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToInt64(source.{sourceMember.ColumnName}.Value) : default(long)";
            }
            else
            {
                isUndefined = true;
                return $"(ulong) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableLong(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : long.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return $"(long?) source.{sourceMember.ColumnName}.ToBinary()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (long?) source.{sourceMember.ColumnName}.Value.ToBinary() : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                return $"(long?) source.{sourceMember.ColumnName}.ToUnixTimeMilliseconds()";
            }
            else if (string.Equals(sourceMember.ModelDataType, "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (long?) source.{sourceMember.ColumnName}.Value.ToUnixTimeMilliseconds() : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"(long?) source.{sourceMember.ColumnName}.Ticks";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (long?) source.{sourceMember.ColumnName}.Value.Ticks : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToInt64(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToInt64(source.{sourceMember.ColumnName}.Value) : null";
            }
            else
            {
                isUndefined = true;
                return $"(ulong) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToUInt(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(uint) : uint.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : default(uint)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToUInt32(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToUInt32(source.{sourceMember.ColumnName}.Value) : default(uint)";
            }
            else
            {
                isUndefined = true;
                return $"(uint) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableUInt(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : uint.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToUInt32(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToUInt32(source.{sourceMember.ColumnName}.Value) : null";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToInt(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(int) : int.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : default(int)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"(int) source.{sourceMember.ColumnName}.TotalSeconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (int) source.{sourceMember.ColumnName}.Value.TotalSeconds : default(int)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToInt32(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToInt32(source.{sourceMember.ColumnName}.Value) : default(int)";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableInt(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : int.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"(int?) source.{sourceMember.ColumnName}.TotalSeconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (int?) source.{sourceMember.ColumnName}.Value.TotalSeconds : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToInt32(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToInt32(source.{sourceMember.ColumnName}.Value) : null";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToUShort(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(ushort) : ushort.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : default(ushort)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToUInt16(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToUInt16(source.{sourceMember.ColumnName}.Value) : default(ushort)";
            }
            else
            {
                isUndefined = true;
                return $"(uint) AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableUShort(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : uint.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToUInt16(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToUInt16(source.{sourceMember.ColumnName}.Value) : null";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToShort(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(short) : short.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : default(short)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"(short) source.{sourceMember.ColumnName}.TotalSeconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (short) source.{sourceMember.ColumnName}.Value.TotalSeconds : default(short)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToInt16(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToInt16(source.{sourceMember.ColumnName}.Value) : default(short)";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableShort(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : short.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"(short?) source.{sourceMember.ColumnName}.TotalSeconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (short?) source.{sourceMember.ColumnName}.Value.TotalSeconds : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToInt16(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToInt16(source.{sourceMember.ColumnName}.Value) : null";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToSByte(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(sbyte) : sbyte.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : default(sbyte)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"(sbyte) source.{sourceMember.ColumnName}.TotalSeconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (sbyte) source.{sourceMember.ColumnName}.Value.TotalSeconds : default(sbyte)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToInt8(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToInt8(source.{sourceMember.ColumnName}.Value) : default(sbyte)";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableSByte(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : sbyte.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"(sbyte?) source.{sourceMember.ColumnName}.TotalSeconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (sbyte?) source.{sourceMember.ColumnName}.Value.TotalSeconds : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToInt8(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToInt8(source.{sourceMember.ColumnName}.Value) : null";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToByte(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? default(byte) : byte.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value : default(byte)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"(byte) source.{sourceMember.ColumnName}.TotalSeconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (byte) source.{sourceMember.ColumnName}.Value.TotalSeconds : default(byte)";
            }
            else if (string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToUInt8(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToUInt8(source.{sourceMember.ColumnName}.Value) : default(byte)";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        public static string ToNullableByte(DBColumn sourceMember, out bool isUndefined)
        {
            isUndefined = false;

            if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
            {
                return $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? null : byte.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                return $"(byte) source.{sourceMember.ColumnName}.TotalSeconds";
            }
            else if (string.Equals(sourceMember.ModelDataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? (byte) source.{sourceMember.ColumnName}.Value.TotalSeconds : null";
            }
            else if (string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                return $"Convert.ToUInt8(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.ModelDataType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                return $"source.{sourceMember.ColumnName}.HasValue ? Convert.ToUInt8(source.{sourceMember.ColumnName}.Value) : null";
            }
            else
            {
                isUndefined = true;
                return $"AFunc(source.{sourceMember.ColumnName})";
            }
        }

        #endregion
    }
}
