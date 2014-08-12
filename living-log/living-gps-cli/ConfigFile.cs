using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_gps_cli
{
    public class ConfigFile
    {
        private Dictionary<string, Dictionary<string, string>> m_dictionary;

        public ConfigFile()
        {
            m_dictionary = new Dictionary<string, Dictionary<string, string>>();
        }

        public ConfigFile(string path)
            : this()
        {
            if (File.Exists(path))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    string section = string.Empty;
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine().Trim();

                        int comment = line.IndexOf('#');
                        if (comment >= 0)
                        {
                            line = line.Substring(0, comment - 1).Trim();
                        }

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            section = line.Trim(new char[] { '[', ']' });
                        }
                        else
                        {
                            int split = line.IndexOf('=');
                            if (split >= 0)
                            {
                                string key = line.Substring(0, split - 1).Trim();
                                string value = line.Substring(split + 1).Trim();

                                Set(section, key, value);
                            }
                        }
                    }
                }
            }
        }

        public void Write(string path)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                foreach (var section in m_dictionary)
                {
                    writer.WriteLine("[" + section.Key + "]");
                    foreach (var value in section.Value)
                    {
                        writer.WriteLine(value.Key + " = " + value.Value);
                    }
                    writer.WriteLine();
                }
            }
        }

        public string Get(string name) { return Get(string.Empty, name); }
        public string Get(string section, string name) { return m_dictionary[section.ToLower()][name.ToLower()]; }

        public void Set(string name, string value) { Set(string.Empty, name, value); }
        public void Set(string section, string name, string value)
        {
            string s = section.ToLower();
            if (!m_dictionary.ContainsKey(s) || m_dictionary[s] == null)
            {
                m_dictionary[s] = new Dictionary<string, string>();
            }

            m_dictionary[s][name.ToLower()] = value;
        }
    }
}
