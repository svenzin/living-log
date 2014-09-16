using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_gps_cli
{
    public class Property
    {
        public string Section;
        public string Name;
        public string DefaultValue;
    }

    public class Configuration
    {
        #region Property management

        private ConfigFile m_configFile;
        
        private string GetProperty(string section, string name, string defaultValue)
        {
            if (m_configFile != null && m_configFile.Exists(section, name))
            {
                return m_configFile.Get(section, name);
            }
            else
            {
                return defaultValue;
            }
        }
        
        private void SetProperty(string section, string name, string value)
        {
            if (m_configFile == null)
            {
                m_configFile = new living_gps_cli.ConfigFile();
            }
            m_configFile.Set(section, name, value);
        }
        
        private void ResetProperty(string section, string name)
        {
            if (m_configFile != null)
            {
                m_configFile.Reset(section, name);
            }
        }
        
        #endregion

        public string Get(Property p)
        {
            return GetProperty(p.Section, p.Name, p.DefaultValue);
        }

        public void Set(Property p, string value)
        {
            SetProperty(p.Section, p.Name, value);
        }

        public void Reset(Property p)
        {
            ResetProperty(p.Section, p.Name);
        }

        public void Load(string filename)
        {
            m_configFile = new ConfigFile(filename);
        }

        public void Save(string filename)
        {
            m_configFile.Write(filename);
        }
    }
}
