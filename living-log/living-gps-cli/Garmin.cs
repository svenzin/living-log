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
            public readonly DriveInfo Drive;
            public readonly string Name;
            public readonly string Id;
            public readonly DirectoryInfo ActivityDir;
            public readonly string ActivityFilter;
            public readonly bool IsValid;

            public Device(DriveInfo drive)
            {
                Drive = drive;

                try
                {
                    XDocument doc = XDocument.Load(drive.RootDirectory.FullName + "/Garmin/GarminDevice.xml");
                    var ns = doc.Root.Name.Namespace;
                    Name = doc.Root.Element(ns + "Model").Element(ns + "Description").Value;
                    Id = doc.Root.Element(ns + "Id").Value;
                    var activity = doc.Root
                        .Descendants(ns + "DataType")
                        .Where(e => e.Element(ns + "Name").Value == "FIT_TYPE_4")
                        .Descendants(ns + "File")
                        .Where(e => e.Element(ns + "TransferDirection").Value == "OutputFromUnit")
                        .First().Element(ns + "Location");
                    ActivityDir = new DirectoryInfo(Drive.RootDirectory + activity.Element(ns + "Path").Value);
                    ActivityFilter = "*." + activity.Element(ns + "FileExtension").Value;

                    IsValid = true;
                }
                catch (Exception e)
                {
                    IsValid = false;
                }
            }
        }

        public static List<Device> DetectDevices()
        {
            return DriveInfo.GetDrives()
                .Where((drive) => File.Exists(drive.RootDirectory.FullName + "/Garmin/GarminDevice.xml"))
                .Select(d => new Device(d))
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
