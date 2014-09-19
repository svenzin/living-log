using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace living_gps_cli
{
    public static class Garmin
    {
        public class Device
        {
            public DriveInfo Drive { get; private set; }
            public string Name { get; private set; }
            public string Id { get; private set; }
            public DirectoryInfo ActivityDir { get; private set; }
            public string ActivityFilter { get; private set; }
            public bool IsValid { get; private set; }

            public static Device FromXml(DriveInfo drive)
            {
                var result = new Device();

                result.Drive = drive;
                try
                {
                    XDocument doc = XDocument.Load(drive.RootDirectory.FullName + "/Garmin/GarminDevice.xml");
                    var ns = doc.Root.Name.Namespace;
                    result.Name = doc.Root.Element(ns + "Model").Element(ns + "Description").Value;
                    result.Id = doc.Root.Element(ns + "Id").Value;
                    var activity = doc.Root
                        .Descendants(ns + "DataType")
                        .Where(e => e.Element(ns + "Name").Value == "FIT_TYPE_4")
                        .Descendants(ns + "File")
                        .Where(e => e.Element(ns + "TransferDirection").Value == "OutputFromUnit")
                        .First().Element(ns + "Location");
                    result.ActivityDir = new DirectoryInfo(result.Drive.RootDirectory + activity.Element(ns + "Path").Value);
                    result.ActivityFilter = "*." + activity.Element(ns + "FileExtension").Value;

                    result.IsValid = true;
                }
                catch (Exception e)
                {
                    result.IsValid = false;
                }
                
                return result;
            }

            public static Device FromFit(DriveInfo drive)
            {
                var result = new Device();
                result.Drive = drive;
                result.IsValid = false;

                var file = new FileInfo(drive.RootDirectory.FullName + "/Garmin/Device.fit");
                if (file != null && file.Exists)
                {
                    var fitFile = file.OpenRead();
                    var reader = new Dynastream.Fit.Decode();
                    if (reader.IsFIT(fitFile) && reader.CheckIntegrity(fitFile))
                    {
                        var trigger = new Dynastream.Fit.MesgBroadcaster();
                        reader.MesgEvent += trigger.OnMesg;
                        reader.MesgDefinitionEvent += trigger.OnMesgDefinition;

                        trigger.FileIdMesgEvent += (s, e) =>
                        {
                            var m = e.mesg as Dynastream.Fit.FileIdMesg;
                            result.Name = m.GetProduct().ToString();
                            result.Id = m.GetSerialNumber().ToString();
                        };
                        trigger.FileCapabilitiesMesgEvent += (s, e) =>
                        {
                            var m = e.mesg as Dynastream.Fit.FileCapabilitiesMesg;
                            if (m.GetType() == Dynastream.Fit.File.Activity && (m.GetFlags() & 0x02) != 0)
                            {
                                result.ActivityDir = new DirectoryInfo(
                                    drive.RootDirectory.FullName + "/Garmin/" +
                                    Encoding.UTF8.GetString(m.GetDirectory())
                                    );
                                result.ActivityFilter = "*.fit";
                            }
                        };

                        reader.Read(fitFile);
                        fitFile.Close();

                        result.IsValid = true;
                    }
                }

                return result;
            }
        }

        public static List<Device> DetectDevices()
        {
            return DriveInfo.GetDrives()
                .Where((drive) => File.Exists(drive.RootDirectory.FullName + "/Garmin/Device.fit"))
                .Select(d => Device.FromFit(d))
                .ToList();
        }

        public static Device Open(int deviceIndex)
        {
            var devices = DetectDevices();
            if (0 <= deviceIndex && deviceIndex < devices.Count)
            {
                return devices[deviceIndex];
            }
            return null;
        }

        public static IEnumerable<FileInfo> List(Device d)
        {
            return d.ActivityDir.EnumerateFiles(d.ActivityFilter);
        }
    }
}
