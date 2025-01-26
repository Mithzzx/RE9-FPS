using System;
using System.Text.RegularExpressions;

namespace AssetInventory
{
    public sealed class SemVer : IComparable
    {
        public readonly int ComponentCount = 1;
        public readonly int Major;
        public readonly int Minor;
        public readonly string MinorQualifier;
        public readonly bool SmallerMinorQualifier;
        public readonly int Micro;
        public readonly string MicroQualifier;
        public readonly bool SmallerMicroQualifier;
        public readonly int Patch;
        public readonly string PatchQualifier;
        public readonly bool SmallerPatchQualifier;

        public bool IsValid;

        public string CleanedVersion => _originalVersion;

        private readonly string _originalVersion;
        private static readonly Regex NumbersOnly = new Regex("[0-9]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex NoLeadingChars = new Regex("[^0-9]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public SemVer(string version)
        {
            IsValid = true;

            _originalVersion = version;
            if (!string.IsNullOrEmpty(version))
            {
                string[] components = version.Split('.');
                ComponentCount = components.Length;

                // remove characters in first segment, like "v", "final"...
                components[0] = NoLeadingChars.Replace(components[0], "");

                // override stored version with cleaned up version
                if (!string.IsNullOrWhiteSpace(components[0])) _originalVersion = string.Join(".", components);

                if (int.TryParse(components[0], out Major))
                {
                    if (components.Length >= 2)
                    {
                        Match match = NumbersOnly.Match(components[1]);
                        if (match.Success)
                        {
                            if (int.TryParse(match.Value, out Minor))
                            {
                                if (match.Length < components[1].Length)
                                {
                                    MinorQualifier = components[1].Substring(match.Length);
                                    if (MinorQualifier.StartsWith("-"))
                                    {
                                        MinorQualifier = MinorQualifier.Substring(1);
                                        SmallerMinorQualifier = true;
                                    }
                                }
                            }
                            else
                            {
                                MinorQualifier = components[1];
                            }
                        }

                        if (components.Length >= 3)
                        {
                            match = NumbersOnly.Match(components[2]);
                            if (match.Success)
                            {
                                if (int.TryParse(match.Value, out Micro))
                                {
                                    if (match.Length < components[2].Length)
                                    {
                                        MicroQualifier = components[2].Substring(match.Length);
                                        if (MicroQualifier.StartsWith("-"))
                                        {
                                            MicroQualifier = MicroQualifier.Substring(1);
                                            SmallerMicroQualifier = true;
                                        }
                                    }
                                }
                                else
                                {
                                    MicroQualifier = components[2];
                                }
                            }
                            if (components.Length >= 4)
                            {
                                match = NumbersOnly.Match(components[3]);
                                if (match.Success)
                                {
                                    if (int.TryParse(match.Value, out Patch))
                                    {
                                        if (match.Length < components[3].Length)
                                        {
                                            PatchQualifier = components[3].Substring(match.Length);
                                            if (PatchQualifier.StartsWith("-"))
                                            {
                                                PatchQualifier = PatchQualifier.Substring(1);
                                                SmallerPatchQualifier = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        PatchQualifier = components[3];
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    IsValid = false;
                }
            }
        }

        public static bool operator ==(SemVer version1, SemVer version2)
        {
            return version1?._originalVersion == version2?._originalVersion;
        }

        public static bool operator !=(SemVer version1, SemVer version2)
        {
            return version1?._originalVersion != version2?._originalVersion;
        }

        public static bool operator >=(SemVer version1, SemVer version2)
        {
            return version1 == version2 || version1 > version2;
        }

        public static bool operator <=(SemVer version1, SemVer version2)
        {
            return version1 == version2 || version1 < version2;
        }

        public static bool operator >(SemVer version1, SemVer version2)
        {
            if (version1 == null) return false;
            if (version2 == null) return true;

            if (version1.Major > version2.Major) return true;
            if (version1.Major < version2.Major) return false;

            if (version1.Minor > version2.Minor) return true;
            if (version1.Minor < version2.Minor) return false;

            if (version1.MinorQualifier != null || version2.MinorQualifier != null)
            {
                if (version1.MinorQualifier == null) return version2.SmallerMinorQualifier;
                if (version2.MinorQualifier == null) return !version1.SmallerMinorQualifier;

                if (string.CompareOrdinal(version1.MinorQualifier, version2.MinorQualifier) > 0) return true;
                if (string.CompareOrdinal(version1.MinorQualifier, version2.MinorQualifier) < 0) return false;
            }

            if (version1.Micro > version2.Micro) return true;
            if (version1.Micro < version2.Micro) return false;

            if (version1.MicroQualifier != null || version2.MicroQualifier != null)
            {
                if (version1.MicroQualifier == null) return version2.SmallerMicroQualifier;
                if (version2.MicroQualifier == null) return !version1.SmallerMicroQualifier;

                if (string.CompareOrdinal(version1.MicroQualifier, version2.MicroQualifier) > 0) return true;
                if (string.CompareOrdinal(version1.MicroQualifier, version2.MicroQualifier) < 0) return false;
            }

            if (version1.Patch > version2.Patch) return true;
            if (version1.Patch < version2.Patch) return false;

            if (version1.PatchQualifier != null || version2.PatchQualifier != null)
            {
                if (version1.PatchQualifier == null) return version1.ComponentCount > version2.ComponentCount;
                if (version2.PatchQualifier == null) return version1.ComponentCount > version2.ComponentCount;

                if (string.CompareOrdinal(version1.PatchQualifier, version2.PatchQualifier) > 0) return true;
                if (string.CompareOrdinal(version1.PatchQualifier, version2.PatchQualifier) < 0) return false;
            }

            return false;
        }

        public static bool operator <(SemVer version1, SemVer version2)
        {
            if (version2 == null) return false;
            if (version1 == null) return true;

            if (version1.Major < version2.Major) return true;
            if (version1.Major > version2.Major) return false;

            if (version1.Minor < version2.Minor) return true;
            if (version1.Minor > version2.Minor) return false;

            if (version1.MinorQualifier != null || version2.MinorQualifier != null)
            {
                if (version1.MinorQualifier == null) return !version2.SmallerMinorQualifier;
                if (version2.MinorQualifier == null) return version1.SmallerMinorQualifier;

                if (string.CompareOrdinal(version1.MinorQualifier, version2.MinorQualifier) < 0) return true;
                if (string.CompareOrdinal(version1.MinorQualifier, version2.MinorQualifier) > 0) return false;
            }

            if (version1.Micro < version2.Micro) return true;
            if (version1.Micro > version2.Micro) return false;

            if (version1.MicroQualifier != null || version2.MicroQualifier != null)
            {
                if (version1.MicroQualifier == null) return !version2.SmallerMicroQualifier;
                if (version2.MicroQualifier == null) return version1.SmallerMicroQualifier;

                if (string.CompareOrdinal(version1.MicroQualifier, version2.MicroQualifier) < 0) return true;
                if (string.CompareOrdinal(version1.MicroQualifier, version2.MicroQualifier) > 0) return false;
            }

            if (version1.Patch < version2.Patch) return true;
            if (version1.Patch > version2.Patch) return false;

            if (version1.PatchQualifier != null || version2.PatchQualifier != null)
            {
                if (version1.PatchQualifier == null) return version1.ComponentCount < version2.ComponentCount;
                if (version2.PatchQualifier == null) return version1.ComponentCount < version2.ComponentCount;

                if (string.CompareOrdinal(version1.PatchQualifier, version2.PatchQualifier) < 0) return true;
                if (string.CompareOrdinal(version1.PatchQualifier, version2.PatchQualifier) > 0) return false;
            }

            return false;
        }

        private bool Equals(SemVer other)
        {
            return _originalVersion == other._originalVersion;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SemVer)obj);
        }

        public override int GetHashCode()
        {
            return _originalVersion != null ? _originalVersion.GetHashCode() : 0;
        }

        public int CompareTo(object obj)
        {
            if (!(obj is SemVer version)) return 1;

            if (version == this) return 0;

            return version > this ? -1 : 1;
        }

        public override string ToString()
        {
            return $"Semantic Version '{_originalVersion}' (Valid: {IsValid})";
        }

        public bool OnlyDiffersInPatch(SemVer other)
        {
            return Major == other.Major && Minor == other.Minor;
        }
    }
}
