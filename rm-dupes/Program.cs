using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace rm_dupes
{
    class Program
    {
        static int Main(string[] args)
        {
            ///Argument handling
            bool Verbose = false;
            bool Remove = false;
            string Source = "";
            string Dest = "";
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-v":
                        Verbose = true;
                        break;
                    case "-r":
                        Remove = true;
                        break;
                    case "-h":
                        //TODO Help message
                        Console.WriteLine("correct usage: rm-dupes [-v] [-r] KEEP DELETE");
                        return 0;

                    default:
                        if (Source != "")
                        {
                            if (Dest != "")
                            {
                                Console.WriteLine("Wrong syntax, correct usage: rm-dupes [-v] [-r] KEEP DELETE");
                                return 1;
                            }
                            else
                                Dest = arg;
                        }
                        else
                            Source = arg;

                        break;
                }
            }



            if(Source == "" || Dest == "")
            {
                Console.WriteLine("Wrong syntax, correct usage: rm-dupes KEEP DELETE");
                return 1;
            }
            if(!(Directory.Exists(Source)))
            {
                Console.WriteLine("Directory {0} doesn't exist", Source);
                return 2;
            }
            if (!(Directory.Exists(Dest)))
            {
                Console.WriteLine("Directory {0} doesn't exist", Dest);
                return 2;
            }
            if (Verbose) Console.WriteLine("Generating Hashes for the first folder...");
            Hash[] KeepHashes = GenerateHashes(args[0]);

            if (Verbose) Console.WriteLine("Sorting Hashes...");
            Array.Sort(KeepHashes);

            if (Verbose) Console.WriteLine("Comparing Hashes...");
            string[] DupesToDelete = CheckHashesForDupes(args[1], KeepHashes);

            foreach (string v in DupesToDelete)
            {
                Console.WriteLine(v);
            }

            if (Remove)
            {
                if (Verbose) Console.WriteLine("Removing duplicates from {0} ...", Dest);
                foreach (string file in DupesToDelete)
                {
                    File.Delete(file);
                }
            }

            if (Verbose) Console.WriteLine("Finished");

            return 0;
        }

        private class Hash : IComparable
        {
            const int HASHLENGTH = 20;
            private byte[] _bytes;
            public byte[] bytes
            {
                get
                {
                    return _bytes;
                }
                set
                {
                    if (value.Length == HASHLENGTH)
                        _bytes = value;
                    else
                        throw new InvalidDataException("byte[] length MUST be HASHLENGTH");
                }
            }
            public int CompareTo(object obj)
            {
                if (obj == null)
                    return 1;
                Hash hash = obj as Hash;
                if (hash != null)
                {
                    if (_bytes == hash.bytes)
                        return 0;
                    for (int i = 0; i < HASHLENGTH; i++)
                    {
                        if (_bytes[i] > hash.bytes[i])
                            return 1;
                        if (_bytes[i] < hash.bytes[i])
                            return -1;
                    }
                    return 0;
                }
                else
                    throw new ArgumentException("The argument supplied is not an hash");
            }
            public Hash(byte[] inbytes)
            {
                _bytes = inbytes;
            }
            public Hash(){}
        }

        private static string[] CheckHashesForDupes(string path, Hash[] hashes)
        {
            List<string> LocalDupes = new List<string>();
            foreach(string file in Directory.GetFiles(path))
            {
                using (SHA1 sha1 = SHA1.Create())
                {
                    using (FileStream fs = File.OpenRead(file))
                    {
                        if (Array.BinarySearch(hashes, new Hash(sha1.ComputeHash(fs))) > 0)
                            LocalDupes.Add(file);
                    }
                }
            }

            string[] JoinedDupes = LocalDupes.ToArray();
            foreach (string directory in Directory.GetDirectories(path))
            {
                JoinedDupes = JoinArrays(JoinedDupes, CheckHashesForDupes(directory, hashes));
            }
            return JoinedDupes;
        }

        private static Hash[] GenerateHashes(string path)
        {
            string[] files = Directory.GetFiles(path);
            Hash[] WorkingDirHashes = new Hash[files.Length];
            using (SHA1 sha1 = SHA1.Create())
            {
                for (int i = 0; i < files.Length; i++)
                {
                    using (FileStream fs = File.OpenRead(files[i]))
                    {
                        WorkingDirHashes[i] = new Hash(sha1.ComputeHash(fs));
                    }
                }
            }
            Hash[] JoinedHashes = WorkingDirHashes;
            foreach (string directory in Directory.GetDirectories(path))
            {
                JoinedHashes = JoinArrays(JoinedHashes, GenerateHashes(directory));
            }
            return JoinedHashes;
        }

        private static T[] JoinArrays<T>(T[] array1, T[] array2)
        {
            T[] NewArray = new T[array1.Length + array2.Length];
            Array.Copy(array1, 0, NewArray, 0, array1.Length);
            Array.Copy(array2, 0, NewArray, array1.Length, array2.Length);
            return NewArray;
        }
    }
}
